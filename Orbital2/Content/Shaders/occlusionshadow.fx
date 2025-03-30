#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

#define PI 3.14159265359
#define TWO_PI 6.28318530718
#define Epsilon 1e-6 // Small value to prevent division by zero or floating point issues

extern float4x4 WorldViewProjection;
extern float2 lightPosition;
extern float lightRadius;
extern float2 occluderPosition;
extern float occluderRadius;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 WorldPosition : TEXCOORD0;
	float4 Color : COLOR0;
};

// There'll be an issue if a pixel is occluded twice in the same direction
// In that case, the same occlusion arc is covered but will have double the shadow effect
// Furthermore, mixing of shadows won't work correctly with simple alpha blend anyway
// Could theoretically send all occluders to the shader and make sure the shadow is only computed once per pixel
// But that has issues, and performance issues too
// How could I even ensure that a pixel is only occluded once? The penumbra triangles would still overlap
// Maybe another blending mode could solve the blending issue with this method
// Lots to think about

float NormalizeAngle(float angle)
{
    // fmod approach: more concise but potentially less precise for large angles
    // angle = fmod(angle + PI, TWO_PI);
    // if (angle < 0) angle += TWO_PI;
    // return angle - PI;

    // Floor approach: generally more robust
	return angle - TWO_PI * floor((angle + PI) / TWO_PI);
}

// Calculates the start and end angles (in radians, range [-PI, PI))
// subtended by a circular object as seen from a given position.
float2 GetArc(float2 objectPosition, float objectRadius, float2 viewPosition)
{
    float2 delta = objectPosition - viewPosition;
    float distSq = dot(delta, delta);
    float radiusSq = objectRadius * objectRadius;

    // If viewpoint is inside or exactly on the circle, it covers the full 360 degrees.
    if (distSq <= radiusSq)
    {
        return float2(-PI, PI); // Represents a full circle
    }

    float dist = sqrt(distSq);
    
    // Calculate the angle of the vector from viewPosition to objectPosition
    float centerAngle = atan2(delta.y, delta.x);

    // Calculate the half-angle subtended by the object
    // Use saturate to prevent asin domain errors due to floating point inaccuracies
    float halfAngle = asin(saturate(objectRadius / dist));

    float startAngle = centerAngle - halfAngle;
    float endAngle = centerAngle + halfAngle;

    // Normalize angles to [-PI, PI) range
    return float2(NormalizeAngle(startAngle), NormalizeAngle(endAngle));
}

float GetRelativeAnglePositive(float angle, float reference) {
    float relAngle = angle - reference;
    // Normalize to [0, TWO_PI)
    relAngle = fmod(relAngle, TWO_PI);
    if (relAngle < 0) {
        relAngle += TWO_PI;
    }
    return relAngle;
}

// Calculates the fraction of the light arc that is overlapped by the occluder arc.
// Handles angle wrap-around at -PI/+PI.
float GetPercentOccluded(float lightStartAngle, float lightEndAngle, float occluderStartAngle, float occluderEndAngle)
{
    // --- 1. Calculate the angular length of the light source ---
    // Handle the full circle case from GetArc explicitly
    bool lightIsFullCircle = (lightStartAngle == -PI && lightEndAngle == PI);
    float lightLength;
    if (lightIsFullCircle) {
        lightLength = TWO_PI;
    } else {
        // If end < start, the arc crosses the -PI/PI boundary
        lightLength = (lightEndAngle >= lightStartAngle) 
                    ? (lightEndAngle - lightStartAngle) 
                    : (TWO_PI - lightStartAngle + lightEndAngle);
    }

    // If the light source has effectively zero angular size, it cannot be occluded.
    if (lightLength < Epsilon) {
        return 0.0;
    }

    // --- 2. Handle the occluder potentially being a full circle ---
    bool occluderIsFullCircle = (occluderStartAngle == -PI && occluderEndAngle == PI);
    if (occluderIsFullCircle) {
        return 1.0; // A full circle occluder blocks everything
    }

    // Light arc in relative space: always starts at 0, ends at lightLength
    // (lightLength is already calculated correctly considering wrap-around)
    float relLightStart = 0.0;
    float relLightEnd = lightLength; // We compare against this length

    // Occluder arc in relative space
    float relOccluderStart = GetRelativeAnglePositive(occluderStartAngle, lightStartAngle);
    float relOccluderEnd = GetRelativeAnglePositive(occluderEndAngle, lightStartAngle);

    float overlap = 0.0;

    // Check if the relative occluder arc wraps around the TWO_PI boundary
    if (relOccluderStart <= relOccluderEnd) {
        // No wrap: Simple overlap check between [relLightStart, relLightEnd) and [relOccluderStart, relOccluderEnd)
        // which simplifies to [0, lightLength) and [relOccluderStart, relOccluderEnd)
        float overlapStart = max(relLightStart, relOccluderStart);
        float overlapEnd = min(relLightEnd, relOccluderEnd);
        overlap = max(0.0, overlapEnd - overlapStart);
    } else {
        // Wrap: Occluder covers [relOccluderStart, TWO_PI) U [0, relOccluderEnd)
        // Check overlap with first part: [relOccluderStart, TWO_PI) intersected with [0, lightLength)
        float overlap1Start = max(relLightStart, relOccluderStart); // = max(0, relOccluderStart) = relOccluderStart
        float overlap1End = min(relLightEnd, TWO_PI);            // = min(lightLength, TWO_PI) = lightLength
        overlap += max(0.0, overlap1End - overlap1Start);

        // Check overlap with second part: [0, relOccluderEnd) intersected with [0, lightLength)
        float overlap2Start = max(relLightStart, 0.0);            // = max(0, 0) = 0
        float overlap2End = min(relLightEnd, relOccluderEnd);      // = min(lightLength, relOccluderEnd)
        overlap += max(0.0, overlap2End - overlap2Start);
    }

    // --- 4. Calculate final percentage ---
    // Ensure overlap doesn't exceed lightLength due to potential float inaccuracies
    overlap = min(overlap, lightLength); 
    
    // Avoid division by zero (already checked lightLength > Epsilon earlier)
    return saturate(overlap / lightLength);
}

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.WorldPosition = input.Position;
	output.Color = input.Color;

	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float2 lightArc = GetArc(lightPosition, lightRadius, input.WorldPosition.xy);
	float2 occluderArc = GetArc(occluderPosition, occluderRadius, input.WorldPosition.xy);
	
	float percentOccluded = GetPercentOccluded(lightArc.x, lightArc.y, occluderArc.x, occluderArc.y);
	
	float alpha = saturate(percentOccluded);
	
	return float4(0, 0, 0, alpha);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
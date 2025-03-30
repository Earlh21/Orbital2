#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

#define VORONOI_METRIC_EUCLIDEAN         0
#define VORONOI_METRIC_EUCLIDEAN_SQ      1 // Faster, good for F1 comparisons
#define VORONOI_METRIC_MANHATTAN         2
#define VORONOI_METRIC_CHEBYSHEV         3

// Output types that naturally produce a single float value
#define VORONOI_OUTPUT_F1                0 // Distance to nearest point
#define VORONOI_OUTPUT_F2                1 // Distance to second nearest point
#define VORONOI_OUTPUT_F2_MINUS_F1       2 // Difference (ridges)

extern float4x4 WorldViewProjection;
extern float2 position;
extern float radius;
extern float time;
extern float4 color0;
extern float4 color1;
extern float warpStrength = 0.7;
extern float timeScale = 0.05;
extern float voronoiScale = 3;
extern float jitter = 1;

struct VertexShaderInput
{
	float4 Position : SV_POSITION;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 WorldPosition : TEXCOORD0;
};

// --- Hash Functions (Choose one or provide your own) ---

// Simple pseudo-random hash
float2 hash2(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
}

float3 hash3(float3 p)
{
    p = float3(dot(p, float3(127.1, 311.7, 74.7)),
               dot(p, float3(269.5, 183.3, 246.1)),
               dot(p, float3(113.5, 271.9, 124.6)));
    return frac(sin(p) * 43758.5453);
}

// --- Distance Functions ---

// Note: Using squared Euclidean distance (EUCLIDEAN_SQ) is often faster
// when you only need to *compare* distances (like finding the minimum).
// Take the sqrt() only at the end if the actual Euclidean distance is needed.

float distFunc(float2 v, int metric)
{
    v = abs(v); // Used by Manhattan and Chebyshev

    if (metric == VORONOI_METRIC_MANHATTAN)
    {
        return v.x + v.y;
    }
    else if (metric == VORONOI_METRIC_CHEBYSHEV)
    {
        return max(v.x, v.y);
    }
    else if (metric == VORONOI_METRIC_EUCLIDEAN_SQ) // Squared Euclidean
    {
        return dot(v, v);
    }
    else // Default: VORONOI_METRIC_EUCLIDEAN
    {
        return sqrt(dot(v, v)); // length(v)
    }
}

float distFunc(float3 v, int metric)
{
    v = abs(v);

    if (metric == VORONOI_METRIC_MANHATTAN)
    {
        return v.x + v.y + v.z;
    }
    else if (metric == VORONOI_METRIC_CHEBYSHEV)
    {
        return max(max(v.x, v.y), v.z);
    }
    else if (metric == VORONOI_METRIC_EUCLIDEAN_SQ) // Squared Euclidean
    {
        return dot(v, v);
    }
    else // Default: VORONOI_METRIC_EUCLIDEAN
    {
        return sqrt(dot(v, v)); // length(v)
    }
}

// --- Core Voronoi Function (2D - Single Float Return) ---

// Returns: float containing requested output (F1, F2, or F2-F1).
float VoronoiNoise2(
    float2 uv,          // Input coordinate (e.g., texture coord, world pos)
    float scale,        // Controls cell density (higher value = smaller cells)
    float jitter,       // Randomness of feature points (0=grid centers, 1=fully random)
    int distanceMetric, // VORONOI_METRIC_*
    int outputType      // VORONOI_OUTPUT_F1, VORONOI_OUTPUT_F2, or VORONOI_OUTPUT_F2_MINUS_F1
)
{
    float2 p = uv * scale;
    float2 ip = floor(p); // Integer part (cell ID)
    float2 fp = frac(p); // Fractional part (position within cell)

    float F1 = 1e10; // Distance to nearest feature point
    float F2 = 1e10; // Distance to second nearest feature point
    // No need to store cell ID or point offset if only returning F1/F2/F2-F1

    // Search 3x3 neighborhood
    for (int j = -1; j <= 1; ++j)
    {
        for (int i = -1; i <= 1; ++i)
        {
            float2 neighborCell = ip + float2(i, j);

            // Calculate feature point position within the neighbor cell
            float2 pointOffset = (hash2(neighborCell) - 0.5) * jitter + 0.5;
            float2 pointPos = neighborCell + pointOffset;

            // Vector from input point 'p' to the feature point
            float2 vecToPoint = pointPos - p;

            // Calculate distance
            float dist = distFunc(vecToPoint, distanceMetric);

            // Update F1 and F2
            if (dist < F1)
            {
                F2 = F1;
                F1 = dist;
            }
            else if (dist < F2)
            {
                F2 = dist;
            }
        }
    }

    // --- Prepare Output ---

    // Handle Squared Euclidean distance if the metric requires it
    bool isSqDist = (distanceMetric == VORONOI_METRIC_EUCLIDEAN_SQ);
    if (isSqDist) // Only need sqrt if using Euclidean SQ
    {
        F1 = sqrt(F1);
        F2 = sqrt(F2); // Need F2 sqrt'd for F2-F1 as well
    }

    // Select output type
    if (outputType == VORONOI_OUTPUT_F2)
    {
        return F2;
    }
    else if (outputType == VORONOI_OUTPUT_F2_MINUS_F1)
    {
        return F2 - F1;
    }
    else // Default: VORONOI_OUTPUT_F1
    {
        return F1;
    }
}

float VoronoiNoise3(
    float3 pos,         // Input position
    float scale,        // Controls cell density
    float jitter,       // Randomness of feature points (0=grid centers, 1=fully random)
    int distanceMetric, // VORONOI_METRIC_*
    int outputType      // VORONOI_OUTPUT_F1, VORONOI_OUTPUT_F2, or VORONOI_OUTPUT_F2_MINUS_F1
)
{
    float3 p = pos * scale;
    float3 ip = floor(p); // Integer part (cell ID)
    float3 fp = frac(p); // Fractional part (position within cell)

    float F1 = 1e10; // Distance to nearest feature point
    float F2 = 1e10; // Distance to second nearest feature point
    // No need to store cell ID or point offset if only returning F1/F2/F2-F1

    // Search 3x3x3 neighborhood
    for (int k = -1; k <= 1; ++k)
    {
        for (int j = -1; j <= 1; ++j)
        {
            for (int i = -1; i <= 1; ++i)
            {
                float3 neighborCell = ip + float3(i, j, k);

                // Calculate feature point position within the neighbor cell
                float3 pointOffset = (hash3(neighborCell) - 0.5) * jitter + 0.5;
                float3 pointPos = neighborCell + pointOffset;

                // Vector from input point 'p' to the feature point
                float3 vecToPoint = pointPos - p;

                // Calculate distance
                float dist = distFunc(vecToPoint, distanceMetric);

                // Update F1 and F2
                if (dist < F1)
                {
                    F2 = F1;
                    F1 = dist;
                }
                else if (dist < F2)
                {
                    F2 = dist;
                }
            }
        }
    }

    // --- Prepare Output ---

    // Handle Squared Euclidean distance if the metric requires it
    bool isSqDist = (distanceMetric == VORONOI_METRIC_EUCLIDEAN_SQ);
     if (isSqDist) // Only need sqrt if using Euclidean SQ
    {
        F1 = sqrt(F1);
        F2 = sqrt(F2);
    }

    // Select output type
    if (outputType == VORONOI_OUTPUT_F2)
    {
        return F2;
    }
    else if (outputType == VORONOI_OUTPUT_F2_MINUS_F1)
    {
        return F2 - F1;
    }
    else
    {
        return F1;
    }
}

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.WorldPosition = input.Position;

	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 relativePos = input.WorldPosition.xy - position;
    float distSq = dot(relativePos, relativePos);
    float radiusSq = radius * radius;

    if (distSq > radiusSq || radius <= 0.0)
    {
        discard;
    }

    float normDistSq = distSq / radiusSq;

    float zCoord = sqrt(max(0.0, 1.0 - normDistSq));

    float2 normXY = relativePos / radius;

    float warpedZ = zCoord * warpStrength;

    float3 voronoiInputPos = float3(normXY.x, normXY.y, warpedZ);

    voronoiInputPos.z += time * timeScale;
    
    float value = VoronoiNoise3(
        voronoiInputPos, 
        voronoiScale,
        jitter,
        VORONOI_METRIC_EUCLIDEAN,
        VORONOI_OUTPUT_F1
    );

    return lerp(color0, color1, value);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
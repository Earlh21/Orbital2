#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

extern float4x4 WorldViewProjection;
extern float2 line0start;
extern float2 line0end;
extern float2 line1start;
extern float2 line1end;

struct VertexShaderInput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 WorldPosition : TEXCOORD0;
	float4 Color : COLOR0;
};

float DistanceToLineSegment(float2 p, float2 lineStart, float2 lineEnd)
{
    float2 lineDir = lineEnd - lineStart;
    float2 toPoint = p - lineStart;
    float lineLengthSq = dot(lineDir, lineDir);

    float t = dot(toPoint, lineDir) / max(lineLengthSq, 1e-8);
    t = saturate(t);

    float2 projection = lineStart + t * lineDir;
    return length(p - projection);
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
    float maxDistance = 300;

	float distance0 = DistanceToLineSegment(input.WorldPosition.xy, line0start, line0end);
	float distance1 = DistanceToLineSegment(input.WorldPosition.xy, line1start, line1end);
	
	float distance = min(distance0, distance1);
	
	float alpha = saturate(1 - distance / maxDistance);
	
	return float4(input.Color.rgb, alpha);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
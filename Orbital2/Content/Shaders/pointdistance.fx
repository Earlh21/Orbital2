#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

extern float4x4 WorldViewProjection;
extern float2 source;
extern float maxDistance;
extern float4 sourceColor;

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

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.WorldPosition = input.Position;
	output.Color = input.Color;

	return output;
}

float2 hash2(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float distance = length(input.WorldPosition.xy - source);
    float alpha = saturate(1 - distance / maxDistance);
    
    float2 uv = mul(input.WorldPosition.xy, WorldViewProjection);
    float noise = hash2(uv).x * 0.02;
    
    return float4(sourceColor.rgb + noise, alpha);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

extern float4x4 WorldViewProjection;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float2 CirclePosition : TEXCOORD0;
	float CircleRadius : TEXCOORD1;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 CirclePosition : TEXCOORD0;
	float CircleRadius : TEXCOORD1;
	float2 WorldPosition : TEXCOORD2;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.WorldPosition = input.Position.xy;
	output.Color = input.Color;
	output.CirclePosition = input.CirclePosition;
	output.CircleRadius = input.CircleRadius;

	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	if (length(input.WorldPosition - input.CirclePosition) > input.CircleRadius)
    {
        discard;
    }
    
    return input.Color;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
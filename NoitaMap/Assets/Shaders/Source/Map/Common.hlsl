struct VS_INPUT
{
    float3 position : POSITION;
    float2 uv : TEXCOORD0;
    
    // --- Instance data --- ///
    
    float4x4 worldMatrix : INSTANCE0;
    
    // Texture information for calculating texture atlas uv for instanced data
    float2 texPos : INSTANCE1;
    float2 texSize : INSTANCE2;
};

struct PS_INPUT
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
};

struct PS_OUTPUT
{
    float4 color : SV_TARGET;
};
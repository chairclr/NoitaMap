struct VS_INPUT
{
    float3 position : POSITION;
    float2 uv : TEXCOORD0;
    
    // Instance data
    
    // matrix4x4
    float4 worldMatrix_0 : INSTMAT0;
    float4 worldMatrix_1 : INSTMAT1;
    float4 worldMatrix_2 : INSTMAT2;
    float4 worldMatrix_3 : INSTMAT3;
    
    float2 texPos : INSTYEAH0;
    float2 texWidth : INSTYEAH1;
};

struct PS_INPUT
{
    float4 position : SV_Position;
    float2 uv : TEXCOORD0;
};

struct PS_OUTPUT
{
    float4 color : SV_Target;
};
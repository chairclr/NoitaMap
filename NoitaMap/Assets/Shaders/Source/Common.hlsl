struct VS_INPUT
{
    float3 position : POSITION;
    float2 uv : TEXCOORD0;
    
    // --- Instance data --- ///
    
    // See VertexShader.hlsl for more information on why we do this
    float4 worldMatrix_0 : INSTWMAT0;
    float4 worldMatrix_1 : INSTWMAT1;
    float4 worldMatrix_2 : INSTWMAT2;
    float4 worldMatrix_3 : INSTWMAT3;
    
    // Texture information for calculating texture atlas uv for instanced data
    float2 texPos : INSTTEXPOS0;
    float2 texSize : INSTTEXSIZE1;
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
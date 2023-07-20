#include "Common.hlsl"

cbuffer VertexShaderBuffer : register(b0)
{
    row_major float4x4 ViewProjection; // 64 bytes
    row_major float4x4 World; // 64 bytes
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;
    
    output.uv = input.uv;
    
    output.position = mul(mul(float4(input.position, 1.0), World), ViewProjection);
    
    return output;
}
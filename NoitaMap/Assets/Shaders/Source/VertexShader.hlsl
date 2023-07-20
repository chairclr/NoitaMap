#include "Common.hlsl"

cbuffer VertexShaderBuffer : register(b0)
{
    float4x4 ViewProjection; // 64 bytes
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;
    
    output.uv = input.uv;
    
    output.position = mul(float4(input.position, 1.0), ViewProjection);
    
    return output;
}
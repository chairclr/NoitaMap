#include "Common.hlsl"

[[vk::binding(0, 0)]]
cbuffer VertexShaderBuffer : register(b0)
{
    row_major float4x4 ViewProjection; // 64 bytes
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;
    
    // We need to calculate where on the atlas this instance lies
    output.uv = input.texPos + (input.texSize * input.uv);
    
    float4 worldPosition = mul(float4(input.position, 1.0), input.worldMatrix);
    
    output.position = mul(worldPosition, ViewProjection);
    
    return output;
}
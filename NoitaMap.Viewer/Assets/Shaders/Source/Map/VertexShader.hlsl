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

    // Clamp to a small amount inside the part of the atlas where the instance lies
    float2 iHateIEEE754 = 1e-6;
    output.uv = clamp(output.uv, input.texPos + iHateIEEE754, input.texPos + input.texSize - iHateIEEE754);
    
    float4 worldPosition = mul(float4(input.position, 1.0), input.worldMatrix);
    
    output.position = mul(worldPosition, ViewProjection);
    
    return output;
}

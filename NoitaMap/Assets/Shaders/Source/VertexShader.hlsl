#include "Common.hlsl"

cbuffer VertexShaderBuffer : register(b0)
{
    row_major float4x4 ViewProjection; // 64 bytes
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;
    
    output.uv = input.texPos + (input.texWidth * input.uv);
    
    row_major float4x4 worldMatrix = float4x4(input.worldMatrix_0, input.worldMatrix_1, input.worldMatrix_2, input.worldMatrix_3);
    
    float4 worldPosition = mul(float4(input.position, 1.0), worldMatrix);
    
    output.position = mul(worldPosition, ViewProjection);
    
    return output;
}
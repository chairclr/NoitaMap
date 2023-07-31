#include "Common.hlsl"

cbuffer VertexShaderBuffer : register(b0)
{
    row_major float4x4 ViewProjection; // 64 bytes
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;
    
    // We need to calculate where on the atlas this instance lies
    output.uv = input.texPos + (input.texSize * input.uv);
    
    // Veldrid can't cope with us passing a matrix type as vertex/instance data
    // So we must split the matrix information up in to 4 different float4's and then reconstruct the matrix ourselves
    column_major float4x4 worldMatrix = float4x4(input.worldMatrix_0, input.worldMatrix_1, input.worldMatrix_2, input.worldMatrix_3);
    
    float4 worldPosition = mul(float4(input.position, 1.0), worldMatrix);
    
    output.position = mul(worldPosition, ViewProjection);
    
    return output;
}
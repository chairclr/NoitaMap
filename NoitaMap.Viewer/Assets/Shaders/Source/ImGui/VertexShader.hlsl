#include "Common.hlsl"

[[vk::binding(0, 0)]]
cbuffer ProjectionMatrixBuffer : register(b0)
{
    float4x4 ProjectionMatrix;
};

float3 SrgbToLinear(float3 srgb)
{
    return srgb * (srgb * (srgb * 0.305306011 + 0.682171111) + 0.012522878);
}

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;

    output.pos = mul(ProjectionMatrix, float4(input.pos.xy, 0.0, 1.0));
    output.col = float4(input.col.rgb, input.col.a);
    output.uv = input.uv;

    return output;
}
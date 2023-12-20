#include "Common.hlsl"

[[vk::binding(0, 1)]]
Texture2D FontTexture : register(t0);
[[vk::binding(1, 0)]]
sampler FontSampler : register(s0);

float4 PSMain(PS_INPUT input) : SV_Target
{
    float4 out_col = input.col * FontTexture.Sample(FontSampler, input.uv);

    return out_col;
}
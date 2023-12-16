#include "Common.hlsl"

[[vk::binding(0, 1)]]
SamplerState PointSamplerView : SAMPLER : register(s0);
[[vk::binding(0, 2)]]
Texture2D MainTextureView : TEXTURE : register(t0);

PS_OUTPUT PSMain(PS_INPUT input) : SV_TARGET
{
    PS_OUTPUT output;
    
    float4 textureColor = MainTextureView.Sample(PointSamplerView, input.uv);
    
    float4 finalColor = textureColor;
    
    output.color = finalColor;
    
    return output;
}
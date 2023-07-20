#include "Common.hlsl"

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;
    
    output.uv = input.uv;
    
    output.position = float4(input.position, 1.0);
    
    return output;
}
layout(location = 0) in lowp vec4 v_Colour;
layout(location = 1) in highp vec2 v_TexCoord;
layout(location = 2) in highp vec4 v_TexRect;

layout(set = 0, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 0, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

void main() {
    o_Colour = texture(sampler2D(m_Texture, m_Sampler), v_TexCoord, -0.9);
}
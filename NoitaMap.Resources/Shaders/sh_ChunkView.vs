
layout(std140, set = 1, binding = 0) uniform g_Transform
{
    highp mat4 g_ViewMatrix;
    highp mat4 g_ModelMatrix;
	highp vec2 g_CoolPosition;
};

layout(location = 0) in highp vec2 m_Position;
layout(location = 1) in lowp vec4 m_Colour;
layout(location = 2) in highp vec2 m_TexCoord;
layout(location = 3) in highp vec4 m_TexRect;

layout(location = 0) out lowp vec4 v_Colour;
layout(location = 1) out highp vec2 v_TexCoord;
layout(location = 2) out highp vec4 v_TexRect;

void main() {
    v_Colour = m_Colour;
	v_TexCoord = m_TexCoord;
	v_TexRect = m_TexRect;

	vec2 localPosition = m_Position - g_CoolPosition;

	highp vec4 rotatedPos = vec4(g_CoolPosition, 0.0, 0.0) + (g_ModelMatrix * vec4(localPosition, 1.0, 1.0));

    gl_Position = g_ProjMatrix * g_ViewMatrix * rotatedPos;
}

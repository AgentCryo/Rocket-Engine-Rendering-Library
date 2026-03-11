#version 330 core

layout(location = 0) out vec4 FragColor;
layout(location = 1) out vec4 NormalBuffer;

in vec2 TexCoords;

uniform sampler2D uColor;
uniform sampler2D uNormal;
uniform sampler2D uDepth;

void main()
{

    vec3 norm = texture(uNormal, TexCoords).rgb;
    float depth = texture(uDepth, TexCoords).r;
    vec3 color = texture(uColor, TexCoords).rgb;
    
    NormalBuffer = norm;
    FragColor = vec4(color, 1.0);
}

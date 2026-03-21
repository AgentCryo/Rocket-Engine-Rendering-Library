// ======================================================
// Rocket Engine - gbufferSampler.glsl
// ======================================================

in vec2 UV;

uniform sampler2D uColor;
uniform sampler2D uNormal;

vec3 GetColor() {
    return texture(uColor, UV).rgb;
}

vec4 GetNormal() {
    return texture(uNormal, UV).rgba;
}

// gbufferSampler.glsl END

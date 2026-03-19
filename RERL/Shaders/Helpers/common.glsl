// ======================================================
// Rocket Engine - Common.glsl
// ======================================================

// Encode a world-space normal into 0-1 range
vec4 EncodeNormal(vec3 n)
{
    return vec4(n * 0.5 + 0.5, 1.0);
}

// Decode a normal from 0-1 back to -1..1
vec3 DecodeNormal(vec4 packed)
{
    return normalize(packed.xyz * 2.0 - 1.0);
}

// Convert depth buffer value to linear depth
float LinearizeDepth(float depth, float nearPlane, float farPlane)
{
    float z = depth * 2.0 - 1.0;
    return (2.0 * nearPlane * farPlane) /
           (farPlane + nearPlane - z * (farPlane - nearPlane));
}

// Reverse depth linearization
float UnlinearizeDepth(float linearDepth, float near, float far)
{
    float z_ndc = (far + near - (2.0 * near * far) / linearDepth) / (far - near);
    return (z_ndc + 1.0) * 0.5;
}


// Reconstruct world position from depth
vec3 ReconstructWorldPos(vec2 uv, float depth, mat4 invProj, mat4 invView)
{
    vec4 clip = vec4(uv * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
    vec4 view = invProj * clip;
    view /= view.w;
    vec4 world = invView * vec4(view.xyz, 1.0);
    return world.xyz;
}

// Default GBuffer write (fallback)
//void DefaultGBufferWrite(vec3 albedo, vec3 normal)
//{
//    gAlbedo  = vec4(albedo, 1.0);
//    gNormal  = EncodeNormal(normal);
//}

// common.glsl END

void main()
{
    vec4 norm = GetNormal();
    float depth = GetDepth();
    vec3 color = GetColor();
    
    gNormal = norm;
    gAlbedo = vec4(color, 1.0);
}

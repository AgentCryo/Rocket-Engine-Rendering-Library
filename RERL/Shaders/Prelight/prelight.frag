in vec3 vColor;
in vec3 Normal;
in vec3 FragPos;

flat in int materialInstance;

struct Material {
    vec4 baseColor;
};

layout(std430, binding = 0) buffer MaterialBuffer {
    Material materials[];
};

const float ambientLight = 0.2;
const vec3 lightPos = vec3(10, 8, 5);

void main()
{
    gNormal = EncodeNormal(normalize(Normal));
    gAlbedo = vec4(vColor, 1.0);
}

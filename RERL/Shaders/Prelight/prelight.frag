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
    vec3 ambient = ambientLight * vec3(1,1,1);
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * materials[materialInstance].baseColor.rgb;
    gNormal = vec4(norm, 1.0);
    gAlbedo = vec4(vColor * (ambient + diffuse), 1.0);
}

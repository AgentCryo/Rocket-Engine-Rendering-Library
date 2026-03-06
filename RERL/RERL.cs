using OpenTK.Mathematics;

namespace RERL;

public class RERL
{
    struct Vertex
    {
        Vector3 _position;
        Vector3 _normal;
        Vector2 _textureCoord;
    }

    struct Mesh
    {
        Vertex[] _vertices;
        int[] _indices;
    }
}
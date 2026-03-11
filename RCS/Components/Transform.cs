using OpenTK.Mathematics;
using RCS.Components;
using Quaternion = OpenTK.Mathematics.Quaternion;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace RCS;

public class Transform(Vector3 position, Vector3 rotation, Vector3 scale) : IComponent
{
    public Entity Owner { get; set; }
    public Vector3 Position = position;
    public Vector3 Rotation = rotation;
    public Vector3 Scale = scale;

    public Transform? Parent = null;
    public List<Transform> Children = [];
    
    public static Transform Identity =>
        new Transform(Vector3.Zero, Vector3.Zero, Vector3.One);
    
    public Transform SetPosition(Vector3 position)
    {
        Position = position;
        return this;
    }

    public Transform SetRotation(Vector3 rotation)
    {
        Rotation = rotation;
        return this;
    }

    public Transform SetScale(Vector3 scale)
    {
        Scale = scale;
        return this;
    }
    
    public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, Quaternion.FromEulerAngles(Rotation));
    
    public Transform AddChild(Transform child)
    {
        child.Parent = this;
        Children.Add(child);
        return this;
    }
    
    public Matrix4 LocalMatrix
    {
        get
        {
            Matrix4 translation = Matrix4.CreateTranslation(Position);
            Matrix4 rot = Matrix4.CreateRotationX(float.DegreesToRadians(Rotation.X)) *
                          Matrix4.CreateRotationY(float.DegreesToRadians(Rotation.Y)) *
                          Matrix4.CreateRotationZ(float.DegreesToRadians(Rotation.Z));
            Matrix4 scale = Matrix4.CreateScale(Scale);

            return scale * rot * translation;
        }
    }

    public Matrix4 WorldMatrix
    {
        get
        {
            if (Parent != null)
                return LocalMatrix * Parent.WorldMatrix;
            else
                return LocalMatrix;
        }
    }

    public void Load() {}
    public void Update(float deltaTime) {}
    public void OnAdd() {}
}
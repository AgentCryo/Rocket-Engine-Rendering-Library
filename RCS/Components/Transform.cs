using OpenTK.Mathematics;
using RCS.Components;
using Quaternion = OpenTK.Mathematics.Quaternion;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace RCS;

/// <summary>
/// A built‑in component that provides position, rotation, scale,
/// hierarchical parenting, and world/local matrix calculations.
/// </summary>
/// <param name="position">The initial position of the transform.</param>
/// <param name="rotation">The initial rotation of the transform, in degrees.</param>
/// <param name="scale">The initial scale of the transform.</param>
public class Transform(Vector3 position, Vector3 rotation, Vector3 scale) : IComponent
{
    /// <summary>
    /// Gets or sets the entity that owns this component.
    /// </summary>
    public Entity Owner { get; set; }

    /// <summary>
    /// The local position of the transform.
    /// </summary>
    public Vector3 Position = position;

    /// <summary>
    /// The local rotation of the transform, in degrees.
    /// </summary>
    public Vector3 Rotation = rotation;

    /// <summary>
    /// The local scale of the transform.
    /// </summary>
    public Vector3 Scale = scale;

    /// <summary>
    /// The parent transform in the hierarchy, or <c>null</c> if this is a root transform.
    /// </summary>
    public Transform? Parent = null;

    /// <summary>
    /// The list of child transforms attached to this transform.
    /// </summary>
    public List<Transform> Children = [];

    /// <summary>
    /// Returns a transform with zero position and rotation, and a scale of one.
    /// </summary>
    public static Transform Identity =>
        new Transform(Vector3.Zero, Vector3.Zero, Vector3.One);

    /// <summary>
    /// Sets the local position of the transform.
    /// </summary>
    /// <param name="position">The new position.</param>
    /// <returns>The current <see cref="Transform"/> instance.</returns>
    public Transform SetPosition(Vector3 position)
    {
        Position = position;
        return this;
    }

    /// <summary>
    /// Sets the local rotation of the transform, in degrees.
    /// </summary>
    /// <param name="rotation">The new rotation.</param>
    /// <returns>The current <see cref="Transform"/> instance.</returns>
    public Transform SetRotation(Vector3 rotation)
    {
        Rotation = rotation;
        return this;
    }

    /// <summary>
    /// Sets the local scale of the transform.
    /// </summary>
    /// <param name="scale">The new scale.</param>
    /// <returns>The current <see cref="Transform"/> instance.</returns>
    public Transform SetScale(Vector3 scale)
    {
        Scale = scale;
        return this;
    }

    /// <summary>
    /// Gets the forward direction of the transform in world space.
    /// </summary>
    public Vector3 Forward =>
        Vector3.Transform(-Vector3.UnitZ, Quaternion.FromEulerAngles(Rotation));

    /// <summary>
    /// Adds a child transform to this transform.
    /// </summary>
    /// <param name="child">The child transform to add.</param>
    /// <returns>The current <see cref="Transform"/> instance.</returns>
    public Transform AddChild(Transform child)
    {
        child.Parent = this;
        Children.Add(child);
        return this;
    }

    /// <summary>
    /// Gets the local transformation matrix for this transform.
    /// </summary>
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

    /// <summary>
    /// Gets the world transformation matrix for this transform,
    /// including all parent transforms.
    /// </summary>
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

    /// <summary>
    /// Called when the scene is first loaded.
    /// </summary>
    public void Load() {}

    /// <summary>
    /// Called once per frame by the owning entity.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the previous frame.</param>
    public void Update(float deltaTime) {}

    /// <summary>
    /// Called immediately after the component is added to an entity.
    /// </summary>
    public void OnAdd() {}
}

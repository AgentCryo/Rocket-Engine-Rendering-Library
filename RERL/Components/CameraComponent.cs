using OpenTK.Mathematics;
using RCS;
using RCS.Components;

namespace RERL.Components;

/// <summary>
/// A component that controls a RERL camera using the entity's Transform.
/// Automatically updates the camera's position, rotation, and view matrix.
/// </summary>
public class CameraComponent : IComponent
{
    public Entity Owner { get; set; }
    Camera Camera = new Camera();

    public Camera GetCamera() => Camera;

    /// <summary>
    /// Sets the projection matrix using a vertical field of view (in degrees).
    /// </summary>
    /// <param name="aspect">Screen width / Screen Height.</param>
    public void SetProjectionFovYInDegrees(float fovY, float aspect, float near, float far)
        => Camera.SetProjectionFovYInDegrees(fovY, aspect, near, far);

    /// <summary>
    /// Sets the projection matrix using a horizontal field of view (in degrees).
    /// </summary>
    /// <param name="aspect">Screen width / Screen Height.</param>
    public void SetProjectionFovXInDegrees(float fovX, float aspect, float near, float far)
        => Camera.SetProjectionFovXInDegrees(fovX, aspect, near, far);

    /// <summary>
    /// Sets the camera's world position.
    /// </summary>
    public CameraComponent SetPosition(Vector3 position)
    {
        Camera.SetPosition(position);
        return this;
    }

    /// <summary>
    /// Sets the camera's rotation using Euler angles.
    /// </summary>
    public CameraComponent SetRotationInDegrees(Vector3 rotation)
    {
        Camera.SetRotation(Quaternion.FromEulerAngles(rotation));
        return this;
    }
    
    /// <summary>
    /// Sets the camera's rotation using quaternions.
    /// </summary>
    public CameraComponent SetRotation(Quaternion rotation)
    {
        Camera.SetRotation(rotation);
        return this;
    }

    public void Load() {}

    /// <summary>
    /// Updates the camera to match the owner's Transform component.
    /// </summary>
    public void Update(float deltaTime)
    {
        SetPosition(Owner.Transform.Position);
        SetRotation(Owner.Transform.Rotation);
        Camera.UpdateViewMatrix();
    }

    public void OnAdd() {}
}
using OpenTK.Mathematics;
using RCS;
using RCS.Components;

namespace RERL.Objects;

public class CameraComponent : IComponent
{
    public Entity Owner { get; set; }
    Camera Camera = new Camera();
    
    public Camera GetCamera() => Camera;
    
    public void SetProjectionFovYInDegrees(float fovY, float aspect, float near, float far) => Camera.SetProjectionFovYInDegrees(fovY, aspect, near, far);
    public void SetProjectionFovXInDegrees(float fovX, float aspect, float near, float far) => Camera.SetProjectionFovXInDegrees(fovX, aspect, near, far);
    
    public CameraComponent SetPosition(Vector3 position)
    {
        Camera.SetPosition(position);
        return this;
    }

    public CameraComponent SetRotation(Vector3 rotation)
    {
        Camera.SetRotation(Quaternion.FromEulerAngles(rotation));
        return this;
    }

    public void Load() {}

    public void Update(float deltaTime)
    {
        if (!Owner.TryGetComponent<Transform>(out var transform) || transform == null) return;
        SetPosition(transform.Position);
        SetRotation(transform.Rotation);
        Camera.UpdateViewMatrix();
    }
    public void OnAdd() {}
}
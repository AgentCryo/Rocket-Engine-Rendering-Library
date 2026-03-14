using OpenTK.Mathematics;

namespace RERL.Objects;

/// <summary>
/// Represents a 3D camera with position, rotation, and view/projection matrices.
/// </summary>
public class Camera
{
    Vector3    _position;
    Quaternion _rotation;
    
    Matrix4 _view;
    Matrix4 _projection;

    public Matrix4 GetProjection() => _projection;
    public Matrix4 GetView() => _view;

    /// <summary>
    /// Sets the projection matrix using a vertical field of view (in degrees).
    /// </summary>
    public void SetProjectionFovYInDegrees(float fovY, float aspect, float near, float far)
    {
        _projection = Matrix4.CreatePerspectiveFieldOfView(float.DegreesToRadians(fovY), aspect, near, far);
    }

    /// <summary>
    /// Sets the projection matrix using a horizontal field of view (in degrees).
    /// </summary>
    public void SetProjectionFovXInDegrees(float fovX, float aspect, float near, float far)
    {
        float fovY = 2f * MathF.Atan(MathF.Tan(MathHelper.DegreesToRadians(fovX) / 2f) / aspect);
        _projection = Matrix4.CreatePerspectiveFieldOfView(fovY, aspect, near, far);
    }

    /// <summary>
    /// Updates the view matrix based on the camera's position and rotation.
    /// </summary>
    public void UpdateViewMatrix()
    {
        Vector3 forward = _rotation * -Vector3.UnitZ;
        Vector3 up = _rotation * Vector3.UnitY;

        _view = Matrix4.LookAt(_position, _position + forward, up);
    }

    public void SetPosition(Vector3 position) => _position = position;
    public void SetRotation(Quaternion rotation) => _rotation = rotation;
}
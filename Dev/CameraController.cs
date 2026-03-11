using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RERL.Objects;

namespace Dev;

public class CameraController
{
    public Vector3 _position;
    public Vector3 GetPosition() => _position;
    Quaternion _orientation = Quaternion.Identity;
    public Quaternion GetOrientation() => _orientation;
    float _pitch;
    float _yaw;

    const float OriginalSpeed = 7f;
    const float SprintSpeed = OriginalSpeed * 3;
    float _speed = 7f;
    const float Sensitivity = 0.2f;

    Camera _camera;
    KeyboardState _keyboardState;
    MouseState _mouseState;
    GameWindow _window;
    
    public void InitializeCameraController(Camera camera, KeyboardState input, MouseState mouse, GameWindow window)
    {
        _camera = camera;
        _keyboardState = input;
        _mouseState = mouse;
        _window = window;
    }
    
    public void UpdateInput(double deltaTime, bool mouseGrabbedToggle = true)
    {
        #region Mouse

        if (_window.CursorState == CursorState.Grabbed) {
            var delta = _mouseState.Delta;

            _yaw -= delta.X * Sensitivity;
            _pitch -= delta.Y * Sensitivity;

            _pitch = MathHelper.Clamp(_pitch, -89f, 89f);
        }
        
        var yawQ   = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(_yaw));
        var pitchQ = Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(_pitch));

        _orientation = yawQ * pitchQ;
        
        #endregion

        #region Keyboard

        Vector3 front = _orientation * -Vector3.UnitZ;
        Vector3 right = _orientation *  Vector3.UnitX;
        Vector3 up    = _orientation *  Vector3.UnitY;

        _speed = _keyboardState.IsKeyDown(Keys.LeftShift) ? SprintSpeed : OriginalSpeed;

        var dt = (float)deltaTime;
        if (_keyboardState.IsKeyDown(Keys.W))
            _position += front * _speed * dt;
        if (_keyboardState.IsKeyDown(Keys.S))
            _position -= front * _speed * dt;
        if (_keyboardState.IsKeyDown(Keys.A))
            _position -= right * _speed * dt;
        if (_keyboardState.IsKeyDown(Keys.D))
            _position += right * _speed * dt;
        if (_keyboardState.IsKeyDown(Keys.Space))
            _position += up * _speed * dt;
        if (_keyboardState.IsKeyDown(Keys.LeftControl))
            _position -= up * _speed * dt;

        #endregion

        _camera.SetPosition(_position);
        _camera.SetRotation(_orientation);
        _camera.UpdateViewMatrix();
    }
}

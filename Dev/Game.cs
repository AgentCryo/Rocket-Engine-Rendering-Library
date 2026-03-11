using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using RERL;
using RERL.Loaders;
using RERL.Objects;

namespace Dev;

//dotnet publish -p:PublishProfile=Win64

public class Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
    : GameWindow(gameWindowSettings, nativeWindowSettings)
{
    Camera _camera = new Camera();
    CameraController _cameraController = new CameraController();
    
    static Shader _fadeTest;
    
    static MeshRenderer _cube;
    static MeshRenderer _icosahedron;
    static MeshRenderer _sphere;

    protected override void OnLoad()
    {
        base.OnLoad();
        _camera.SetProjectionFovXInDegrees(100, Size.X / (float)Size.Y, 0.1f, 100f);
        CursorState = CursorState.Grabbed;
        _cameraController.InitializeCameraController(_camera, KeyboardState, MouseState, this);
        
        _fadeTest = new Shader().AttachShader(Shader.DefaultVert, "./Shaders/FadeTest/fadeTest.frag");
        RERL_Core.RegisterShader(_fadeTest);
        
        RERL_Core.SetCamera(_camera);
        RERL_Core.SetGameWindow(this);
        RERL_Core.Load();

        _cube = new MeshRenderer().AttachMesh(MeshLoader.CubeMesh).AttachShader(_fadeTest);
        
        _icosahedron = new MeshRenderer().AttachMesh(MeshLoader.IcosahedronMesh).AttachShader(RERL_Core.GetDefaultShader());
        RERL_Core.RegisterRenderable(_icosahedron);

        _sphere = new MeshRenderer().AttachMesh(MeshLoader.UVSphereMesh).AttachShader(RERL_Core.GetDefaultShader());
        RERL_Core.RegisterRenderable(_sphere);
        RERL_Core.RegisterRenderable(_cube);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        _cameraController.UpdateInput(args.Time);
        base.OnUpdateFrame(args);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        RERL_Core.RenderFrame(args);
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        _camera.SetProjectionFovXInDegrees(90, Size.X / (float)Size.Y, 0.1f, 100f);
    }
}
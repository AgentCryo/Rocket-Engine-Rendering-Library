using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using RCS;
using RERL;
using RERL.Components;
using RERL.Loaders;
using RERL.ShaderTypes;

namespace Dev;

// dotnet publish -p:PublishProfile=Win64

public class Game : GameWindow
{
    Camera _camera = new Camera();
    CameraController _cameraController = new CameraController();
    
    static Shader _fadeTest;
    static Shader _mengerSpongeObjectShader = new();
    static PostProcess _testingPostProcess = new();

    public RCS_Core.Scene MainScene;

    public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }
    
    protected override void OnLoad()
    {
        base.OnLoad();
        Logger.Initialize(false, true);
        
        _camera.SetProjectionFovXInDegrees(90, Size.X / (float)Size.Y, 0.1f, 100f);
        CursorState = CursorState.Grabbed;
        _cameraController.InitializeCameraController(_camera, KeyboardState, MouseState, this);
        
        _fadeTest = new Shader().AttachShader(Shader.DefaultVert, "./Shaders/FadeTest/fadeTest.frag");
        RenderPipeline.RegisterShader(_fadeTest);
        
        _mengerSpongeObjectShader = new Shader().AttachShader(Shader.DefaultVert, "./Shaders/MengerSpongeObject/mengerSpongeObject.frag");
        _mengerSpongeObjectShader.RegisterAutoUniform("cameraPos", () => _cameraController.GetPosition());
        _mengerSpongeObjectShader.RegisterAutoUniform("cameraRot", () => _cameraController.GetOrientation());
        _mengerSpongeObjectShader.RegisterAutoUniform("screenSize", () => Size.ToVector2());
        RenderPipeline.RegisterShader(_mengerSpongeObjectShader);
        
        _testingPostProcess = new PostProcess().AttachPostProcessShader("./Shaders/TestingPostProcess/testingPostProcess.frag", this);
        RenderPipeline.RegisterPostProcess(_testingPostProcess);
        
        RERL_Core.SetCamera(_camera);
        RERL_Core.SetGameWindow(this);
        RERL_Core.Load();
        
        
        MainScene = new RCS_Core.Scene("Main");

        // Cube
        {
            var cube = new Entity("Cube")
                .AddComponent(new MeshRenderer()
                    .AttachMesh(MeshLoader.CubeMesh)
                    .AttachShader(_fadeTest))
                .AddComponent<CubeComponent>();

            cube.Transform.Position = new Vector3(0, 0.0f, 0);
            MainScene.AddEntity(cube);
        }

        // Menger Sponge Object
        {
            var menger = new Entity("MengerSpongeObject")
                .AddComponent(new MeshRenderer()
                    .AttachMesh(MeshLoader.CubeMesh)
                    .AttachShader(_mengerSpongeObjectShader));

            menger.Transform.Position = new Vector3(5, 0.0f, 0);
            menger.Transform.Scale = new Vector3(2.5f, 2.5f, 2.5f);
            MainScene.AddEntity(menger);

            _mengerSpongeObjectShader.RegisterAutoUniform("objectPos", () => menger.Transform.Position);
            _mengerSpongeObjectShader.RegisterAutoUniform(
                "objectRot",
                () => menger.Transform.Rotation
            );
            _mengerSpongeObjectShader.RegisterAutoUniform("objectScale", () => menger.Transform.Scale);
        }

        // Icosahedron
        {
            var icosahedron = new Entity("Icosahedron")
                .AddComponent(new PongComponent(5, 5))
                .AddComponent(new MeshRenderer()
                    .AttachMesh(MeshLoader.IcosahedronMesh)
                    .AttachShader(RERL_Core.GetPrelightShader()));

            MainScene.AddEntity(icosahedron);
        }
        
        // Sphere
        {
            var sphere = new Entity("Sphere")
                .AddComponent(new MeshRenderer()
                    .AttachMesh(MeshLoader.UVSphereMesh)
                    .AttachShader(RERL_Core.GetPrelightShader()));

            sphere.Transform.SetPosition((0, 1, 0));
            MainScene.AddEntity(sphere);
        }

        {
            var glbSponzaModels = MeshLoader.ParseMesh("./Models/glbSponza/NewSponza_Main_glTF_003.gltf");
            Dictionary<string, Entity> sponzaEntityLookup = new();

            foreach (var m in glbSponzaModels) {
                var ent = new Entity(m.Name);

                if (m.Model != null) {
                    ent.AddComponent(new ModelRenderer()
                        .AttachModel(m.Model)
                        .AttachShader(RERL_Core.GetPrelightShader()));
                }

                ent.Transform.SetTransform(m.Transform);

                sponzaEntityLookup[m.Name] = ent;
                MainScene.AddEntity(ent);
            }
            foreach (var m in glbSponzaModels.Where(m => !string.IsNullOrWhiteSpace(m.ParrentName))) {
                if (!sponzaEntityLookup.TryGetValue(m.Name, out var child))
                    continue;

                if (!sponzaEntityLookup.TryGetValue(m.ParrentName, out var parent))
                    continue;

                child.Transform.SetParent(parent.Transform);
            }
        }

        RCS_Core.AddScene(MainScene);
        RCS_Core.SetActiveScene("Main");
        RCS_Core.LoadActiveScene();
    }

    float _time;
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        _cameraController.UpdateInput(args.Time);
        RCS_Core.UpdateActiveScene(args.Time);

        var menger = RCS_Core.GetActiveScene().GetEntity("MengerSpongeObject");
        _time += (float)args.Time / 10f;

        menger.Transform.Position.X = float.Sin(_time) * 10f;
        menger.Transform.Position.Y = float.Cos(_time) * 10f;
        menger.Transform.EulerAngles = new Vector3(0, _time * 31f, 0);

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

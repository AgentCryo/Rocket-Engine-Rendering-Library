using System.ComponentModel;
using OpenTK.Mathematics;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RERL.Loaders;
using RERL.Objects;

namespace RERL;

/// <summary>
/// The core rendering system for the Rocket Engine Rendering Layer (RERL).
/// Handles shader registration, mesh batching, G‑Buffer creation,
/// post‑processing, and the main render loop.
/// </summary>
public static class RERL_Core
{
    /// <summary>
    /// Represents a single vertex containing position, normal, and UV coordinates.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex(Vector3 position, Vector3 normal, Vector2 uv)
    {
        public Vector3 Position = position;
        public Vector3 Normal = normal;
        public Vector2 UV = uv;
    }

    /// <summary>
    /// Represents a mesh consisting of vertices and indices.
    /// </summary>
    public struct Mesh(Vertex[] vertices, uint[] indices)
    {
        public Vertex[] Vertices = vertices;
        public uint[] Indices = indices;
    }

    public struct GBuffer
    {
        public int Color, Normal, Depth;
        public int FBO;
        public int GetColor() => Color;
        public int GetNormal() => Normal;
        public int GetDepth() => Depth;
        public int GetFBO() => FBO;
        
        /// <summary>
        /// Creates a new G‑Buffer with color, normal, and depth attachments sized to the screen.
        /// </summary>
        public GBuffer(Vector2i screenSize)
        {
            FBO = GL.GenFramebuffer();
            
            Color = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, Color);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                screenSize.X, screenSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            SetupTexture2D(Color);

            Normal = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, Normal);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f,
                screenSize.X, screenSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            SetupTexture2D(Normal);

            Depth = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, Depth);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent,
                screenSize.X, screenSize.Y, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            SetupTexture2D(Depth);
            
            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                throw new Exception($"GBuffer incomplete: {status}");
        }
        
        void SetupTexture2D(int tex)
        {
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }

        /// <summary>
        /// Clears all G‑Buffer attachments (color, normal, depth).
        /// </summary>
        public void Clear()
        {
            float[] clear = new float[4];
            GL.GetFloat(GetPName.ColorClearValue, clear);

            // Clear COLOR attachment 0 (albedo)
            GL.ClearBuffer(ClearBuffer.Color, 0, clear);

            // Clear COLOR attachment 1 (normal)
            GL.ClearBuffer(ClearBuffer.Color, 1, clear);

            // Clear DEPTH attachment
            float depthClear = 1f;
            GL.ClearBuffer(ClearBuffer.Depth, 0, ref depthClear);
        }

    }
    
    static Shader? _defaultShader;
    public static Shader GetDefaultShader() => _defaultShader!;

    static int _postProcessingQuad_VAO;
    static GBuffer _geometryFrame;
    static readonly List<Shader> Shaders = [];
    static readonly List<PostProcess> PostProcesses = [];
    static readonly Dictionary<int, List<MeshRenderer>> ShaderBatchRendering = new();
    static readonly List<MeshRenderer> Renderables = [];
    static Camera _camera;
    static GameWindow _window;
    
    public static void SetCamera(Camera camera) => _camera = camera;
    public static void SetGameWindow(GameWindow window) => _window = window;
    
    /// <summary>
    /// Initializes the rendering system, loads shaders, creates the G‑Buffer,
    /// and prepares OpenGL state. Must be called before adding renderables.
    /// </summary>
    public static void Load()
    {
        var assembly = AppDomain.CurrentDomain .GetAssemblies() .FirstOrDefault(a => a.GetName().Name == "RERL");

        Console.WriteLine(assembly != null ? $"Library Found: {assembly.FullName}" : "Library not Found");

        GL.ClearColor(Color.FromArgb(255, 20,25,35));
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        _defaultShader = new Shader().AttachShader("./Shaders/Default/default.vert", "./Shaders/Default/default.frag");
        RegisterShader(_defaultShader);
        foreach (var shader in Shaders) {
            shader.RegisterAutoUniform("uView", () => _camera.GetView());
            shader.RegisterAutoUniform("uProjection", () => _camera.GetProjection());
        }

        _geometryFrame = new GBuffer(_window.Size);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _geometryFrame.GetFBO());
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _geometryFrame.Color, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, _geometryFrame.Normal, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _geometryFrame.Depth, 0);
        
        DrawBuffersEnum[] drawBuffers = [DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1];
        GL.DrawBuffers(drawBuffers.Length, drawBuffers);
        
        _postProcessingQuad_VAO = GL.GenVertexArray();
    }
    
    /// <summary>
    /// Renders a single frame, including geometry pass, post‑processing,
    /// and buffer swapping.
    /// </summary>
    public static void RenderFrame(FrameEventArgs args)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, PostProcesses.Count != 0 ? _geometryFrame.GetFBO() : 0);
        _geometryFrame.Clear();
        
        //Render all meshes grouped by shader to minimize shader switches.
        foreach (var kpv in ShaderBatchRendering) {
            //int shaderHandle = kpv.Key;
            List<MeshRenderer> renderables = kpv.Value;
            
            Shader shader = renderables[0].GetShader()!;
            shader.Use();
            shader.ApplyAutoUniforms();

            foreach (var mr in renderables) {
                //Temp Transform Identity, will be replaced with actual transforms later.
                //mr.Render(RenderTransform.Identity);
                //New one
                mr.Render();
            }
        }
        
        if (PostProcesses.Count != 0) {
            GBuffer input = _geometryFrame;
            for (int p = 0; p < PostProcesses.Count; p++) {
                input = PostProcesses[p].RenderPostProcess(input, _postProcessingQuad_VAO, (p == PostProcesses.Count - 1));
            }
        }
        
        _window.SwapBuffers();
    }
    
    static void RegisterToShaderBatch(MeshRenderer renderable)
    {
        int handle = renderable.GetShader()!.GetHandle();
        if (!ShaderBatchRendering.TryGetValue(handle, out var list))
        {
            list = [];
            ShaderBatchRendering[handle] = list;
        }
        list.Add(renderable);
    }

    /// <summary>
    /// Registers a renderable mesh renderer for batched rendering.
    /// </summary>
    public static void RegisterRenderable(MeshRenderer renderable)
    {
        Renderables.Add(renderable);
        RegisterToShaderBatch(renderable);
    }

    /// <summary>
    /// Removes a renderable from the rendering system.
    /// </summary>
    public static void UnregisterRenderable(MeshRenderer renderable)
    {
        Renderables.Remove(renderable);

        int handle = renderable.GetShader()!.GetHandle();
        if (ShaderBatchRendering.TryGetValue(handle, out var list))
        {
            list.Remove(renderable);
            if (list.Count == 0)
                ShaderBatchRendering.Remove(handle);
        }
    }

    /// <summary>
    /// Registers a post‑processing effect to be applied after geometry rendering.
    /// </summary>
    public static void RegisterPostProcess(PostProcess postProcess) => PostProcesses.Add(postProcess);
    /// <summary>
    /// Removes a post‑processing effect from the pipeline.
    /// </summary>
    public static void UnregisterPostProcess(PostProcess postProcess) => PostProcesses.Remove(postProcess);
    
    /// <summary>
    /// Registers a shader with the rendering system and assigns automatic uniforms.
    /// </summary>
    public static void RegisterShader(Shader shader)
    {
        Shaders.Add(shader);
        shader.RegisterAutoUniform("uView", () => _camera.GetView());
        shader.RegisterAutoUniform("uProjection", () => _camera.GetProjection());
    }

    /// <summary>
    /// Removes a shader from the rendering system.
    /// </summary>
    public static void UnregisterShader(Shader shader) => Shaders.Remove(shader);
}
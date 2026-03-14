using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace RERL.Objects;

/// <summary>
/// Represents a post‑processing shader that renders a full‑screen pass
/// using the output of the geometry buffer.
/// </summary>
public class PostProcess
{
    Shader _shader;
    RERL_Core.GBuffer _gbuffer;

    /// <summary>
    /// Loads and attaches the post‑processing shader, and creates an internal G‑Buffer
    /// used to store the output of this post‑process pass.
    /// </summary>
    /// <param name="postProcessFragmentPath">Path to the fragment shader used for post‑processing.</param>
    /// <param name="window">The game window used to size the internal G‑Buffer.</param>
    /// <returns>The current <see cref="PostProcess"/> instance for chaining.</returns>
    public PostProcess AttachPostProcessShader(string postProcessFragmentPath, GameWindow window)
    {
        _gbuffer = new RERL_Core.GBuffer(window.Size);

        _shader = new Shader().AttachShader("./Shaders/DefaultPostProcess/defaultPostProcess.vert",
            postProcessFragmentPath);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _gbuffer.GetFBO());
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, _gbuffer.Color, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
            TextureTarget.Texture2D, _gbuffer.Normal, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, _gbuffer.Depth, 0);
        
        DrawBuffersEnum[] drawBuffers = [DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1];
        GL.DrawBuffers(drawBuffers.Length, drawBuffers);
        
        return this;
    }

    /// <summary>
    /// Executes the post‑processing shader using the provided G‑Buffer as input.
    /// </summary>
    /// <param name="gbuffer">The input G‑Buffer from the geometry pass.</param>
    /// <param name="VAO">The VAO containing a full‑screen triangle.</param>
    /// <param name="renderToScreen">If true, the result is also drawn to the screen.</param>
    /// <returns>The internal G‑Buffer containing the post‑processed output.</returns>
    public RERL_Core.GBuffer RenderPostProcess(RERL_Core.GBuffer gbuffer, int VAO, bool renderToScreen)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _gbuffer.GetFBO());
        _gbuffer.Clear();

        _shader.Use();
        _shader.ApplyAutoUniforms();

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, gbuffer.Color);
        _shader.ApplyUniform("uColor", 0);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, gbuffer.Normal);
        _shader.ApplyUniform("uNormal", 1);

        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.Texture2D, gbuffer.Depth);
        _shader.ApplyUniform("uDepth", 2);

        GL.BindVertexArray(VAO);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
        if (renderToScreen)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindVertexArray(VAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        return _gbuffer;
    }

    /// <summary>
    /// Assigns a value to a uniform variable in the post‑processing shader.
    /// </summary>
    public bool ApplyUniform(string name, object? value, bool silence = false)
    {
        return _shader.ApplyUniform(name, value, silence);
    }

    /// <summary>
    /// Registers a uniform whose value is supplied automatically each frame.
    /// </summary>
    public bool RegisterAutoUniform(string name, Func<object?> getter, bool silence = false)
    {
        return _shader.RegisterAutoUniform(name, getter, silence);
    }

    /// <summary>
    /// Applies all automatically registered uniforms.
    /// </summary>
    public void ApplyAutoUniforms()
    {
        _shader.ApplyAutoUniforms();
    }
}

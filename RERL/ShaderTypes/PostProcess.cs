using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace RERL.Objects;

public class PostProcess
{
    Shader _shader;
    RERL_Core.GBuffer _gbuffer;

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
    /// Assigns a value to a uniform variable in the shader program.
    /// The shader must be bound using <see cref="Use"/> before calling this.
    /// </summary>
    /// <param name="name">The uniform name in the GLSL shader.</param>
    /// <param name="value">The value to upload to the GPU.</param>
    /// <param name="silence">If true, missing uniforms are ignored instead of throwing.</param>
    /// <returns>True if the uniform was successfully applied.</returns>
    public bool ApplyUniform(string name, object? value, bool silence = false)
    {
        return _shader.ApplyUniform(name, value);
    }

    /// <summary>
    /// Registers a uniform whose value is supplied automatically each frame.
    /// The provided getter function is invoked whenever <see cref="ApplyAutoUniforms"/>
    /// is called.
    /// </summary>
    /// <param name="name">The uniform name in the GLSL shader.</param>
    /// <param name="getter">A function that returns the value to assign.</param>
    /// <param name="silence">If true, missing uniforms are ignored.</param>
    /// <returns>True if the uniform was registered successfully.</returns>
    public bool RegisterAutoUniform(string name, Func<object?> getter, bool silence = false)
    {
        return _shader.RegisterAutoUniform(name, getter, silence);
    }

    /// <summary>
    /// Applies all automatically registered uniforms by invoking their getter
    /// functions and uploading the resulting values to the GPU.
    /// </summary>
    public void ApplyAutoUniforms()
    {
        _shader.ApplyAutoUniforms();
    }
}
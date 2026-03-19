using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace RERL;

public struct GBuffer
{
    public int Color, Normal, Depth, DepthBuffer;
    public int FBO;
    public int GetColor() => Color;
    public int GetNormal() => Normal;
    public int GetColorDepth() => Depth;
    public int GetFBO() => FBO;
    
    /// <summary>
    /// Creates a new G‑Buffer with color, normal, and depth attachments sized to the screen.
    /// </summary>
    public GBuffer(Vector2i screenSize)
    {
        FBO = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);

        // --- Color ---
        Color = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Color);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
            screenSize.X, screenSize.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        SetupTexture2D(Color);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Color, 0);

        // --- Normal ---
        Normal = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Normal);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f,
            screenSize.X, screenSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
        SetupTexture2D(Normal);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, Normal, 0);
        
        // --- Depth ---
        Depth = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Depth);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f,
            screenSize.X, screenSize.Y, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
        SetupTexture2D(Depth);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, Depth, 0);

        // --- Hardware Depth ---
        DepthBuffer = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer,
            RenderbufferStorage.DepthComponent24, screenSize.X, screenSize.Y);
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, DepthBuffer);

        // Tell OpenGL which color attachments we are drawing to
        DrawBuffersEnum[] attachments = {
            DrawBuffersEnum.ColorAttachment0,
            DrawBuffersEnum.ColorAttachment1,
            DrawBuffersEnum.ColorAttachment2
        };
        GL.DrawBuffers(attachments.Length, attachments);

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
        
        float depthClear = 1f;
        // Clear COLOR attachment 2 (depth)
        GL.ClearBuffer(ClearBuffer.Color, 2, ref depthClear);
        
        // Clear depth buffer.
        GL.ClearBuffer(ClearBuffer.Depth, 0, ref depthClear);
    }

}
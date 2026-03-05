using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace RERL;

public class MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
    : GameWindow(gameWindowSettings, nativeWindowSettings)
{
    
    protected override void OnLoad()
    {
        base.OnLoad();
        
        var assembly = AppDomain.CurrentDomain .GetAssemblies() .FirstOrDefault(a => a.GetName().Name == "RERL");

        Console.WriteLine(assembly != null ? $"Library Found: {assembly.FullName}" : "Library not Found");

        GL.ClearColor(Color.Green);
    }
    
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        SwapBuffers();
    }
}
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using RERL;

namespace Dev;

class Program
{
    static void Main(string[] args)
    {
        var nativeSettings = new NativeWindowSettings
        {
            Title = "Lighting OpenTK",
            WindowState = WindowState.Fullscreen
        };
        using var window = new MainWindow(GameWindowSettings.Default, nativeSettings);
        window.Run();
    }
}
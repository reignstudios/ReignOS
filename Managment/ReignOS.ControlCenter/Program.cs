using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.ReactiveUI;
using ReignOS.Core;

namespace ReignOS.ControlCenter.Desktop;

enum CompositorMode
{
    Weston,
    Cage,
    X11
}

class Program
{
    public static CompositorMode compositorMode = CompositorMode.Weston;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Log.WriteLine("Installer started: " + VersionInfo.version);

        if (args != null)
        {
            if (args.Contains("-weston")) compositorMode = CompositorMode.Weston;
            else if (args.Contains("-cage")) compositorMode = CompositorMode.Cage;
            else if (args.Contains("-x11")) compositorMode = CompositorMode.X11;
        }

        var options = new X11PlatformOptions();
        options.RenderingMode = new List<X11RenderingMode>() { X11RenderingMode.Software };
        BuildAvaloniaApp().With(options).StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace().UseReactiveUI();
}

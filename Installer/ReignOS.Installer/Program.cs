﻿using System;

using Avalonia;
using Avalonia.ReactiveUI;
using ReignOS.Core;

namespace ReignOS.Installer;

enum CompositorMode
{
    Weston,
    Cage,
    Labwc
}

class Program
{
    public static CompositorMode compositorMode = CompositorMode.Cage;

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
            else if (args.Contains("-labwc")) compositorMode = CompositorMode.Labwc;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace().UseReactiveUI();
}

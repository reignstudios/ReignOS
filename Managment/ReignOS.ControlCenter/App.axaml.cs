using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ReignOS.ControlCenter.Views;
using ReignOS.Core;

namespace ReignOS.ControlCenter;

public partial class App : Application
{
    public static int exitCode;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                //DataContext = new MainViewModel()
            };
            
            desktop.Exit += OnExit;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                //DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        e.ApplicationExitCode = exitCode;
    }
    
    public static bool IsOnline()
    {
        string result = ProcessUtil.Run("ping", "-c 1 google.com", log:false);
        return result.Contains("1 received");
    }
}

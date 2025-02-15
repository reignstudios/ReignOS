using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.IO;
using ReignOS.Core;

namespace ReignOS.ControlCenter.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }
    
    private void GamescopeButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 1;// open Steam in Gamescope
        MainWindow.singleton.Close();
    }
    
    private void CageButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 2;// open Steam in Cage
        MainWindow.singleton.Close();
    }
    
    private void LabwcButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 3;// open Steam in Labwc
        MainWindow.singleton.Close();
    }
    
    private void X11Button_OnClick(object sender, RoutedEventArgs e)
    {
        App.exitCode = 4;// open Steam in X11
        MainWindow.singleton.Close();
    }
    
    private void SleepButton_Click(object sender, RoutedEventArgs e)
    {
        ProcessUtil.Run("systemctl", "suspend", out _, wait:false);
    }
    
    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 10;
        MainWindow.singleton.Close();
    }
    
    private void ShutdownButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 11;
        MainWindow.singleton.Close();
    }
    
    private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 12;// close Managment and launch CheckUpdates.sh
        MainWindow.singleton.Close();
    }
    
    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        App.exitCode = 20;// close Managment and go to virtual terminal
        MainWindow.singleton.Close();
    }

    private void BootApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        const string profileFile = "/home/gamer/.bash_profile";
        string text = File.ReadAllText(profileFile);
        text = text.Replace(" --gamescope", "");
        text = text.Replace(" --cage", "");
        if (boot_Gamescope.IsChecked == true) text = text.Replace("--use-controlcenter", "--use-controlcenter --gamescope");
        else if (boot_Cage.IsChecked == true) text = text.Replace("--use-controlcenter", "--use-controlcenter --cage");
        File.WriteAllText(profileFile, text);
    }
    
    private void RotApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        static void WriteX11Settings(StreamWriter writer, string rotation)
        {
            writer.WriteLine("display=$(xrandr --query | awk '/ connected/ {print $1; exit}' | xargs)");
            writer.WriteLine($"xrandr --output $display --rotate {rotation}");
        }
        
        static void WriteWaylandSettings(StreamWriter writer, string rotation)
        {
            writer.WriteLine("display=$(wlr-randr | awk '/^[^ ]+/{print $1; exit}' | xargs)");
            writer.WriteLine($"wlr-randr --output $display --transform {rotation} --adaptive-sync enabled");
        }
        
        static string GetWaylandDisplay()
        {
            try
            {
                string result = ProcessUtil.Run("wlr-randr", "", out _);
                var lines = result.Split('\n');
                result = lines[0].Split(" ")[0];
                return result;
            }
            catch {}
            return "ERROR";
        }
        
        const string folder = "/home/gamer/ReignOS_Ext";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        string x11File = Path.Combine(folder, "X11_Settings.sh");
        string waylandFile = Path.Combine(folder, "Wayland_Settings.sh");
        if (rot_Default.IsChecked == true)
        {
            using (var writer = new StreamWriter(x11File)) WriteWaylandSettings(writer, "normal");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "normal");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform normal", out _);
        }
        else if (rot_Left.IsChecked == true)
        {
            using (var writer = new StreamWriter(x11File)) WriteWaylandSettings(writer, "left");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "270");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 270", out _);
        }
        else if (rot_Right.IsChecked == true)
        {
            using (var writer = new StreamWriter(x11File)) WriteWaylandSettings(writer, "right");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "90");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()}--transform 90", out _);
        }
        else if (rot_Flip.IsChecked == true)
        {
            using (var writer = new StreamWriter(x11File)) WriteWaylandSettings(writer, "inverted");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "180");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 180", out _);
        }
    }
    
    private void NvidiaApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        // invoke Nvidia driver install script
        if (nvidia_Nouveau.IsChecked == true) App.exitCode = 30;
        else if (nvidia_Proprietary.IsChecked == true) App.exitCode = 31;
        MainWindow.singleton.Close();
    }
}

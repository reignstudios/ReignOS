using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using ReignOS.Core;

namespace ReignOS.ControlCenter.Views;

public partial class MainView : UserControl
{
    private const string settingsFile = "/home/gamer/ReignOS_Ext/Settings.txt";
    
    public MainView()
    {
        InitializeComponent();
        try
        {
            LoadSettings();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    private void LoadSettings()
    {
        if (!File.Exists(settingsFile)) return;
        using (var reader = new StreamReader(settingsFile))
        {
            string line;
            do
            {
                line = reader.ReadLine();
                if (line == null) break;
                
                var parts = line.Split('=');
                if (parts.Length < 2) continue;
                
                if (parts[0] == "Boot")
                {
                    if (parts[1] == "ControlCenter") boot_ControlCenter.IsChecked = true;
                    else if (parts[1] == "Gamescope") boot_Gamescope.IsChecked = true;
                    else if (parts[1] == "Cage") boot_Cage.IsChecked = true;
                }
                else if (parts[0] == "ScreenRotation")
                {
                    if (parts[1] == "Default") rot_Default.IsChecked = true;
                    else if (parts[1] == "Left") rot_Left.IsChecked = true;
                    else if (parts[1] == "Right") rot_Right.IsChecked = true;
                    else if (parts[1] == "Flip") rot_Flip.IsChecked = true;
                }
                else if (parts[0] == "NvidiaDrivers")
                {
                    if (parts[1] == "Nouveau") nvidia_Nouveau.IsChecked = true;
                    else if (parts[1] == "Proprietary") nvidia_Proprietary.IsChecked = true;
                }
            } while (!reader.EndOfStream);
        }
    }
    
    private void SaveSettings()
    {
        using (var writer = new StreamWriter(settingsFile))
        {
            if (boot_ControlCenter.IsChecked == true) writer.WriteLine("Boot=ControlCenter");
            else if (boot_Gamescope.IsChecked == true) writer.WriteLine("Boot=Gamescope");
            else if (boot_Cage.IsChecked == true) writer.WriteLine("Boot=Cage");
            else writer.WriteLine("Boot=ControlCenter");
            
            if (rot_Default.IsChecked == true) writer.WriteLine("ScreenRotation=Default");
            else if (rot_Left.IsChecked == true) writer.WriteLine("ScreenRotation=Left");
            else if (rot_Right.IsChecked == true) writer.WriteLine("ScreenRotation=Right");
            else if (rot_Flip.IsChecked == true) writer.WriteLine("ScreenRotation=Flip");
            else writer.WriteLine("ScreenRotation=Default");
            
            if (nvidia_Nouveau.IsChecked == true) writer.WriteLine("NvidiaDrivers=Nouveau");
            else if (nvidia_Proprietary.IsChecked == true) writer.WriteLine("NvidiaDrivers=Proprietary");
            else writer.WriteLine("NvidiaDrivers=Nouveau");
        }
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
        SaveSettings();
    }
    
    private void RotApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        static void WriteX11Settings(StreamWriter writer, string rotation)
        {
            writer.WriteLine("display=$(xrandr --query | awk '/ connected/ {print $1; exit}')");
            writer.WriteLine($"xrandr --output $display --rotate {rotation}");
        }
        
        static void WriteWaylandSettings(StreamWriter writer, string rotation)
        {
            writer.WriteLine("display=$(wlr-randr | awk '/^[^ ]+/{print $1; exit}')");
            writer.WriteLine($"wlr-randr --output $display --transform {rotation}");// --adaptive-sync enabled (this can break rotation)
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
            using (var writer = new StreamWriter(x11File)) WriteX11Settings(writer, "normal");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "normal");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform normal", out _);
        }
        else if (rot_Left.IsChecked == true)
        {
            using (var writer = new StreamWriter(x11File)) WriteX11Settings(writer, "left");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "270");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 270", out _);
        }
        else if (rot_Right.IsChecked == true)
        {
            using (var writer = new StreamWriter(x11File)) WriteX11Settings(writer, "right");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "90");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()}--transform 90", out _);
        }
        else if (rot_Flip.IsChecked == true)
        {
            using (var writer = new StreamWriter(x11File)) WriteX11Settings(writer, "inverted");
            using (var writer = new StreamWriter(waylandFile)) WriteWaylandSettings(writer, "180");
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 180", out _);
        }
        
        SaveSettings();
    }
    
    private void NvidiaApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        // invoke Nvidia driver install script
        if (nvidia_Nouveau.IsChecked == true) App.exitCode = 30;
        else if (nvidia_Proprietary.IsChecked == true) App.exitCode = 31;
        MainWindow.singleton.Close();
        SaveSettings();
    }

    private void BootManagerButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = false;
        bootManagerGrid.IsVisible = true;

        bootOptionsListBox.Items.Clear();
        string result = ProcessUtil.Run("efibootmgr", "", asAdmin:true);
        var values = result.Split('\n');
        foreach (string value in values)
        {
            if (value.StartsWith("Boot"))
            {
                if (value.StartsWith("BootCurrent:"))
                {
                    var match = Regex.Match(value, @"BootCurrent:\s*(.*)");
                    if (match.Success) bootCurrentText.Text = "Current Boot: " + match.Groups[1].Value;
                    else bootCurrentText.Text = "Current Boot: N/A";
                }
                else if (value.StartsWith("BootOrder:"))
                {
                    var match = Regex.Match(value, @"BootOrder:\s*(.*)");
                    if (match.Success) bootOrderText.Text = "Boot Order: " + match.Groups[1].Value;
                    else bootOrderText.Text = "Boot Order: N/A";
                }
                else
                {
                    var match = Regex.Match(value, @"Boot(\d*)(\*)?\s*(.*)");
                    if (match.Success)
                    {
                        string name = match.Groups[3].Value;
                        const int maxLength = 20;
                        if (name.StartsWith("Windows Boot Manager")) name = "Windows Boot Manager";
                        else if (name.StartsWith("Linux Boot Manager")) name = "Linux Boot Manager";
                        else if (name.Length > maxLength) name = name.Substring(0, maxLength);
                        var item = new ListBoxItem();
                        item.Content = $"({match.Groups[1].Value}{match.Groups[2].Value}): {name}";
                        item.Tag = match.Groups[1].Value;
                        bootOptionsListBox.Items.Add(item);
                    }
                }
            }
        }
    }

    private void BootManagerRebootButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (bootOptionsListBox.SelectedIndex <= -1) return;
        var item = (ListBoxItem)bootOptionsListBox.Items[bootOptionsListBox.SelectedIndex];
        string boot = (string)item.Tag;
        ProcessUtil.Run("efibootmgr", $"-n {boot}", asAdmin:true);
        Thread.Sleep(1000);
        ProcessUtil.Run("reboot", "", asAdmin:false);
    }
    
    private void BootManagerBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        mainGrid.IsVisible = true;
        bootManagerGrid.IsVisible = false;
    }
}

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
        
    }
    
    private void NvidiaApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        // invoke Nvidia driver install script
        if (nvidia_Nouveau.IsChecked == true) App.exitCode = 30;
        else if (nvidia_Proprietary.IsChecked == true) App.exitCode = 31;
        MainWindow.singleton.Close();
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
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
        Environment.ExitCode = 1;// open Steam in Labwc
        MainWindow.singleton.Close();
    }
    
    private void CageButton_OnClick(object sender, RoutedEventArgs e)
    {
        Environment.ExitCode = 2;// open Steam in Labwc
        MainWindow.singleton.Close();
    }
    
    private void LabwcButton_OnClick(object sender, RoutedEventArgs e)
    {
        Environment.ExitCode = 3;// open Steam in Labwc
        MainWindow.singleton.Close();
    }
    
    private void X11Button_OnClick(object sender, RoutedEventArgs e)
    {
        Environment.ExitCode = 4;// open Steam in X11
        MainWindow.singleton.Close();
    }
    
    private void SleepButton_Click(object sender, RoutedEventArgs e)
    {
        ProcessUtil.Run("systemctl", "suspend", out _, wait:false);
    }
    
    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        Environment.ExitCode = 10;
        MainWindow.singleton.Close();
    }
    
    private void ShutdownButton_Click(object sender, RoutedEventArgs e)
    {
        Environment.ExitCode = 11;
        MainWindow.singleton.Close();
    }
    
    private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        Environment.ExitCode = 12;// close Managment and launch CheckUpdates.sh
        MainWindow.singleton.Close();
    }
    
    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Environment.ExitCode = 20;// close Managment and go to virtual terminal
        MainWindow.singleton.Close();
    }
}

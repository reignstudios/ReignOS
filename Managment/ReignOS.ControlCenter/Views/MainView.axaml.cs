using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace ReignOS.ControlCenter.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Environment.ExitCode = 0;// don't shutdown system and just go to terminal
        MainWindow.singleton.Close();
    }
}

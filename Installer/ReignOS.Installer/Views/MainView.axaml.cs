using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.IO;
using Avalonia.Controls.Primitives;
using ReignOS.Core;

namespace ReignOS.Installer.Views;

public partial class MainView : UserControl
{
    private bool isRefreshing;
    private double drivePercentage = 25;
    private ulong driveSize = 512 * 1024 * 1024;
    
    public MainView()
    {
        isRefreshing = true;
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        isRefreshing = false;
    }

    private void UpdateDrivePercentage()
    {
        isRefreshing = true;
        
        drivePercentTextBox.Text = Math.Round(drivePercentage).ToString();
        driveSlider.Value = drivePercentage;
        
        ulong gb = driveSize / 1024 / 1024;
        driveGBTextBox.Text = Math.Round((drivePercentage / 100) * gb).ToString();

        isRefreshing = false;
    }

    private void DrivePercentTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (isRefreshing) return;
        double.TryParse(drivePercentTextBox.Text, out drivePercentage);
        UpdateDrivePercentage();
    }
    
    private void DriveGBTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (isRefreshing) return;
        ulong.TryParse(driveGBTextBox.Text, out ulong size);
        size *= 1024 * 1024;// bytes
        drivePercentage = (size / (double)driveSize) * 100;
        UpdateDrivePercentage();
    }
    
    private void DriveSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (isRefreshing) return;
        drivePercentage = driveSlider.Value;
        UpdateDrivePercentage();
    }
}

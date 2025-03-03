using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using ReignOS.Core;

namespace ReignOS.Installer.Views;

enum InstallerStage
{
    Start,
    Network,
    Drive,
    Install,
    Installing,
    DoneInstalling
}

public partial class MainView : UserControl
{
    private InstallerStage stage;
    private bool isRefreshing = true;
    private double drivePercentage = 25;
    private const ulong driveSize = 512ul * 1024 * 1024 * 1024;

    private System.Timers.Timer connectedTimer;
    private List<string> wlanDevices = new List<string>();
    private string wlanDevice;
    private bool isConnected;
    
    public MainView()
    {
        InitializeComponent();
        InstallUtil.InstallProgress += InstallProgress;
        
        connectedTimer = new System.Timers.Timer(1000 * 5);
        connectedTimer.Elapsed += ConnectedTimer;
        connectedTimer.AutoReset = true;
        connectedTimer.Start();
    }

    private void ConnectedTimer(object sender, ElapsedEventArgs e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            isConnected = App.IsOnline();
            if (stage == InstallerStage.Network) nextButton.IsEnabled = isConnected;
            if (isConnected) isConnectedText.Text = "Network Connected";
            else isConnectedText.Text = "Network Disconnected";
        });
    }

    private void InstallProgress(string task, float progress)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            installText.Text = task;
            installProgressBar.Value = progress;
        });
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        isRefreshing = false;
        UpdateDrivePercentage();
    }

    private void UpdateDrivePercentage()
    {
        isRefreshing = true;
        
        var gb = (double)driveSize / 1024 / 1024 / 1024;
        drivePercentage = Math.Round(drivePercentage);
        
        drivePercentTextBox.Text = Math.Round(drivePercentage).ToString();
        driveGBTextBox.Text = Math.Round((drivePercentage / 100) * gb).ToString() + "gb";
        driveSlider.Value = drivePercentage;

        isRefreshing = false;
    }

    private void DrivePercentTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (isRefreshing) return;
        double.TryParse(drivePercentTextBox.Text, out drivePercentage);
        UpdateDrivePercentage();
    }
    
    private void DriveSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (isRefreshing) return;
        drivePercentage = driveSlider.Value;
        UpdateDrivePercentage();
    }

    private void RotationToggleButton_OnIsCheckedChanged(object sender, RoutedEventArgs e)
    {
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
        
        if (defaultRotRadioButton.IsChecked == true)
        {
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform normal", out _);
        }
        else if (leftRotRadioButton.IsChecked == true)
        {
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 270", out _);
        }
        else if (rightRotRadioButton.IsChecked == true)
        {
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()}--transform 90", out _);
        }
        else if (flipRotRadioButton.IsChecked == true)
        {
            ProcessUtil.Run("wlr-randr", $"--output {GetWaylandDisplay()} --transform 180", out _);
        }
    }

    private void ShutdownButton_OnClick(object sender, RoutedEventArgs e)
    {
        //ProcessUtil.Run("poweroff", "", out _, wait:false);
        MainWindow.singleton.Close();
    }

    private void NextButton_OnClick(object sender, RoutedEventArgs e)
    {
        switch (stage)
        {
            case InstallerStage.Start:
                stage = InstallerStage.Network;
                startPage.IsVisible = false;
                networkSelectPage.IsVisible = true;
                backButton.IsEnabled = true;
                nextButton.IsEnabled = isRefreshing;
                SetupNetworkPage();
                break;
            
            case InstallerStage.Network:
                stage = InstallerStage.Drive;
                networkSelectPage.IsVisible = false;
                drivePage.IsVisible = true;
                SetupDrivePage();
                break;
            
            case InstallerStage.Drive:
                stage = InstallerStage.Install;
                drivePage.IsVisible = false;
                installPage.IsVisible = true;
                nextButton.Content = "Install";
                break;
            
            case InstallerStage.Install:
                stage = InstallerStage.Installing;
                backButton.IsEnabled = false;
                nextButton.IsEnabled = false;
                installProgressBar.IsVisible = true;
                InstallUtil.Install();
                break;
        }
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        switch (stage)
        {
            case InstallerStage.Network:
                stage = InstallerStage.Start;
                startPage.IsVisible = true;
                networkSelectPage.IsVisible = false;
                backButton.IsEnabled = false;
                nextButton.IsEnabled = true;
                break;
            
            case InstallerStage.Drive:
                stage = InstallerStage.Network;
                networkSelectPage.IsVisible = true;
                drivePage.IsVisible = false;
                break;
            
            case InstallerStage.Install:
                stage = InstallerStage.Drive;
                drivePage.IsVisible = true;
                installPage.IsVisible = false;
                nextButton.Content = "Next";
                break;
        }
    }
    
    private void RefreshNetworkButton_OnClick(object sender, RoutedEventArgs e)
    {
        SetupNetworkPage();
    }

    private void SetupNetworkPage()
    {
        // find wlan devices
        void deviceOut(string line)
        {
            if (line.Contains("---") || line.Contains("Name")) return;
            try
            {
                var match = Regex.Match(line, @"\s*(wlan\d)");
                if (match.Success)
                {
                    wlanDevices.Add(match.Groups[1].Value);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        wlanDevices.Clear();
        ProcessUtil.Run("iwctl", "device list", standardOut:deviceOut);
        
        // choose device
        if (wlanDevices.Count == 0) return;
        wlanDevice = wlanDevices[0];
        Console.WriteLine("wlanDevice: " + wlanDevice);
        
        // get SSID
        void ssidOut(string line)
        {
            if (line.Contains("---") || line.Contains("Network name")) return;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    var match = Regex.Match(line, @"\s*\>\s*(\S*)\s?");
                    if (match.Success)
                    {
                        connectionListBox.Items.Add(new ListBoxItem { Content = match.Groups[1].Value + "*" });
                    }
                    else
                    {
                        match = Regex.Match(line, @"\s*(\S*)\s?");
                        if (match.Success)
                        {
                            connectionListBox.Items.Add(new ListBoxItem { Content = match.Groups[1].Value });
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }

        connectionListBox.Items.Clear();
        ProcessUtil.Run("iwctl", $"station {wlanDevice} get-networks", standardOut:ssidOut);
    }
    
    private void NetworkConnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (connectionListBox.SelectedIndex < 0) return;

        void getStandardInput(StreamWriter writer)
        {
            writer.WriteLine(networkPasswordText.Text);
        }
        
        var item = (ListBoxItem)connectionListBox.Items[connectionListBox.SelectedIndex];
        var ssid = (string)item.Content;
        ProcessUtil.Run("iwctl", $"station {wlanDevice} connect {ssid}", getStandardInput:getStandardInput);
    }
    
    private void SetupDrivePage()
    {
        
    }
}

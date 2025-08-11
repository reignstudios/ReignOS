using System;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Threading;
using ReignOS.Core;

namespace ReignOS.Monitor;

public partial class MainWindow : Window
{
    private Timer timer;
    private double cpu, gpu, ram, vram;
    private int fan = -1;

    private bool lastFanEnableValue;
    private double lastFanSpeedValue;
    
    public MainWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;
        Closing += OnClosing;

        lastFanEnableValue = enableFanSpeed.IsChecked == true;
        lastFanSpeedValue = fanSpeed.Value;
        ApplyFanSettings(false, 255);
        
        // start monitor timer
        timer = new Timer(TimerCallback, null, 100, 5000);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        ApplyFanSettings(false, 255);// disable manual fan control
    }

    private void TimerCallback(object? state)
    {
        // get system status
        GetCPUStatus();
        GetGPUStatus();
        GetRAMStatus();
        GetFanStatus();
        
        // update UI
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            cpuPercentage.Value = cpu;
            gpuPercentage.Value = gpu;
            ramPercentage.Value = ram;
            vramPercentage.Value = vram;
            if (fan >= 0) fanRPM.Text = $"RPM: {fan}";
            else fanRPM.Text = "N/A";
            
            // apply fan speed
            ApplyFanSpeed();
        });
    }

    private void GetCPUStatus()
    {
        // needs package: sudo pacman -S sysstat
        string result = ProcessUtil.Run("mpstat", "1 1 | awk '/^Average:/ {print 100 - $NF}'");
        if (double.TryParse(result, out double value))
        {
            cpu = value;
        }
    }
    
    private void GetGPUStatus()
    {
        string result = ProcessUtil.Run("radeontop", "-d - -l 1");
        var values = result.Split(' ');
        for (int i = 0; i != values.Length; i++)
        {
            if (values[i] == "gpu")
            {
                string percentage = values[i + 1].Replace(",", "").Replace("%", "");
                if (double.TryParse(percentage, out double value))
                {
                    gpu = value;
                }
            }
            else if (values[i] == "vram")
            {
                string percentage = values[i + 1].Replace(",", "").Replace("%", "");
                if (double.TryParse(percentage, out double value))
                {
                    vram = value;
                }
            }
        }
    }
    
    private void GetRAMStatus()
    {
        string result = ProcessUtil.Run("awk", "'/MemTotal/{t=$2}/MemAvailable/{a=$2} END{printf \"\"%.2f\\n\"\", 100*(t-a)/t}' /proc/meminfo");
        if (double.TryParse(result, out double value))
        {
            ram = value;
        }
    }

    private void GetFanStatus()
    {
        const string hwPath = "/sys/devices/platform/oxp-platform/hwmon";
        if (Directory.Exists(hwPath))
        {
            foreach (string path in Directory.GetDirectories(hwPath))
            {
                if (!Regex.IsMatch(path, hwPath + "/hwmon")) continue;
                
                string hwPath_rpm = path + "/fan1_input";
                if (!File.Exists(hwPath_rpm)) continue;
                
                string fanValue = File.ReadAllText(hwPath_rpm);
                if (!int.TryParse(fanValue, out fan)) fan = -1;
            }
        }
    }

    private void ApplyFanSpeed()
    {
        if (lastFanEnableValue != enableFanSpeed.IsChecked || lastFanSpeedValue != fanSpeed.Value)
        {
            lastFanEnableValue = enableFanSpeed.IsChecked == true;
            lastFanSpeedValue = fanSpeed.Value;
            byte hardwareFanValue = (byte)Math.Min((lastFanSpeedValue / 100) * 255, 255.0);
            ApplyFanSettings(true, hardwareFanValue);
        }
    }

    private static void ApplyFanSettings(bool enabled, byte speed)
    {
        const string hwPath = "/sys/devices/platform/oxp-platform/hwmon";
        if (Directory.Exists(hwPath))
        {
            foreach (string path in Directory.GetDirectories(hwPath))
            {
                if (!Regex.IsMatch(path, hwPath + "/hwmon")) continue;
                
                string hwPath_enable = path + "/pwm1_enable";
                string hwPath_percentage = path + "/pwm1";
                if (!File.Exists(hwPath_enable) || !File.Exists(hwPath_percentage)) continue;
                
                ProcessUtil.WriteAllTextAdmin(hwPath_enable, enabled ? "1" : "2");
                if (enabled) ProcessUtil.WriteAllTextAdmin(hwPath_percentage, speed.ToString());
            }
        }
    }
}
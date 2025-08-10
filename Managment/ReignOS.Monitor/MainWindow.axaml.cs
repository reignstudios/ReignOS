using System;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Threading;
using ReignOS.Core;

namespace ReignOS.Monitor;

public partial class MainWindow : Window
{
    private Timer timer;
    private double cpu, gpu, ram, vram;
    
    public MainWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode) return;
        
        // start monitor timer
        timer = new Timer(TimerCallback, null, 100, 5000);
    }

    private void TimerCallback(object? state)
    {
        GetCPUStatus();
        GetGPUStatus();
        GetRAMStatus();
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            cpuPercentage.Value = cpu;
            gpuPercentage.Value = gpu;
            ramPercentage.Value = ram;
            vramPercentage.Value = vram;
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
}
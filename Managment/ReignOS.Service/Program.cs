namespace ReignOS.Service;
using ReignOS.Core;
using ReignOS.Service.Hardware;

using System;
using System.Reflection;
using System.Threading;
using HidSharp;

enum HardwareType
{
    Unknown,
    MSI_Claw_A1M,
    MSI_Claw
}

internal class Program
{
    private static bool exit;
    public static HardwareType hardwareType { get; private set; }
    
    static void Main(string[] args)
    {
        Log.WriteLine("Service started");
        Console.CancelKeyPress += ExitEvent;
        LibraryResolver.Init(Assembly.GetExecutingAssembly());
        
        // init virtual gamepad
        VirtualGamepad.Init();
        
        // detect system hardware
        try
        {
            string productName = ProcessUtil.Run("dmidecode", "-s system-product-name");
            Log.WriteLine("Product: " + productName);
            if (productName == "Claw A1M") hardwareType = HardwareType.MSI_Claw_A1M;
            else if (productName.StartsWith("Claw ")) hardwareType = HardwareType.MSI_Claw;
        }
        catch (Exception e)
        {
            Log.WriteLine("Failed to get system hardware");
            Log.WriteLine(e);
        }
        Log.WriteLine("Known hardware detection: " + hardwareType.ToString());

        // detect device & configure hardware
        try
        {
            foreach (var device in DeviceList.Local.GetHidDevices())
            {
                MSI_Claw.Configure(device);
            }
        }
        catch (Exception e)
        {
            Log.WriteLine("Failed to get device hardware");
            Log.WriteLine(e);
        }

        // run events
        var time = DateTime.Now;
        while (!exit)
        {
            // update time
            var lastTime = time;
            time = DateTime.Now;
            var timeSpan = time - lastTime;

            // detect possible resume from sleep
            bool resumeFromSleep = false;
            if (timeSpan.TotalSeconds >= 3) resumeFromSleep = true;

            // update devices
            if (MSI_Claw.isEnabled) MSI_Claw.Update(resumeFromSleep);

            // sleep thread
            Thread.Sleep(1000 / 30);
        }
        
        // shutdown
        MSI_Claw.Dispose();
        VirtualGamepad.Dispose();   
    }

    private static void ExitEvent(object sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        exit = true;
    }
}
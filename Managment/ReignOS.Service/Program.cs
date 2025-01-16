namespace ReignOS.Service;
using ReignOS.Core;
using ReignOS.Core.Hardware;
using HidSharp;

enum HardwareType
{
    Unknown,
    MSI_Claw_A1M,
    MSI_Claw
}

internal class Program
{
    public static HardwareType hardwareType { get; private set; }
    
    static void Main(string[] args)
    {
        Console.WriteLine("ReignOS.Service started");
        
        // detect system hardware
        try
        {
            string productName = ProcessUtil.Run("dmidecode", "-s system-product-name");
            Console.WriteLine("Product: " + productName);
            if (productName == "Claw A1M") hardwareType = HardwareType.MSI_Claw_A1M;
            else if (productName.StartsWith("Claw ")) hardwareType = HardwareType.MSI_Claw;
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to get system hardware");
            Console.Write(e);
        }
        Console.WriteLine("Known hardware detection: " + hardwareType.ToString());

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
            Console.WriteLine("Failed to get device hardware");
            Console.Write(e);
        }

        // run events
        var time = DateTime.Now;
        while (true)
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
            Thread.Sleep(1000);
        }
    }
}
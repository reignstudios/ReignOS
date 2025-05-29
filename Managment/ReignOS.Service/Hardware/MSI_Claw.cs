namespace ReignOS.Service.Hardware;
using ReignOS.Core;
using ReignOS.Service.HardwarePatches;
using ReignOS.Service.OS;
using System;
using System.Threading;

public static class MSI_Claw
{
    enum Mode : byte
    {
        Offline = 0,
        XInput = 1,
        DInput = 2,
        MSI = 3,
        Desktop = 4,
        BIOS = 5,
        Testing = 6
    }

    public static bool isEnabled { get; private set; }
    private static HidDevice device;
    private static bool useInputPlumber;

    public static void Configure(bool useInputPlumber)
    {
        MSI_Claw.useInputPlumber = useInputPlumber;

        // configure after sleep fixes
        AudioPatches.Fix1(true);
        WiFiPatches.Fix1(true);
        WiFiPatches.Fix2(false);

        // configure gamepad
        if (!useInputPlumber)
        {
            device = new HidDevice();
            if (!device.Init(0x0DB0, 0x1901, true) || device.handles.Count == 0)// mode 1
            {
                device.Dispose();
                device = new HidDevice();
                if (!device.Init(0x0DB0, 0x1902, true) || device.handles.Count == 0)// mode 2
                {
                    device.Dispose();
                    device = new HidDevice();
                    if (!device.Init(0x0DB0, 0x1903, true) || device.handles.Count == 0)// mode 3
                    {
                        device.Dispose();
                        device = null;
                        return;
                    }
                } 
            }
        
            Log.WriteLine($"MSI-Claw gamepad found: Handles={device.handles.Count}");
            if (EnableMode(Mode.XInput))
            {
                Program.keyboardInput = new KeyboardInput();
                Program.keyboardInput.Init("AT Translated Set 2 keyboard", true, 0, 0);
            }
        }
    }
    
    public static void Dispose()
    {
        if (device != null)
        {
            device.Dispose();
            device = null;
        }
    }

    private static bool EnableMode(Mode mode)
    {
        int i = 0;
        var buffer = new byte[8];
        buffer[i++] = 15;// report id
        buffer[i++] = 0;
        buffer[i++] = 0;
        buffer[i++] = 60;
        buffer[i++] = 36;// we want to switch mode
        buffer[i++] = (byte)mode;// mode
        buffer[i++] = 0;
        buffer[i++] = 0;
        if (!device.WriteData(buffer, 0, buffer.Length))
        {
            Log.WriteLine("FAILED: To set MSI-Claw gamepad mode");
            return false;
        }

        Thread.Sleep(1000);
        buffer = new byte[256];
        if (device.ReadData(buffer, 0, buffer.Length, out nint sizeRead))
        {
            string hex = BitConverter.ToString(buffer, 0, (int)sizeRead);
            Log.WriteLine($"MSI-Claw gamepad read response: Size={sizeRead} Data:{hex}");
        }
        else
        {
            Log.WriteLine("ERROR: MSI-Claw gamepad failed to read response");
        }

        Log.WriteLine("MSI-Claw gamepad mode set");
        isEnabled = true;
        return true;
    }

    public static void Update(ref DateTime time, bool resumeFromSleep, ushort key, bool keyPressed)
    {
        if (useInputPlumber) return;

        if (resumeFromSleep)
        {
            if (device != null)
            {
                EnableMode(Mode.XInput);
                Thread.Sleep(3000);
                EnableMode(Mode.XInput);// ensure again after delay
                time = DateTime.Now;// reset time
            }
        }
        else if (!keyPressed)
        {
            // relay OEM buttons to virtual gamepad input
            if (key == input.KEY_F15)
            {
                VirtualGamepad.Write_TriggerLeftSteamMenu();
            }
            else if (key == input.KEY_F16)
            {
                VirtualGamepad.Write_TriggerRightSteamMenu();
            }
        }
    }
}
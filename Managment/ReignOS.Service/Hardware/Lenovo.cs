using System;
using System.Threading;
using ReignOS.Core;
using ReignOS.Service.OS;

namespace ReignOS.Service.Hardware;

public static class Lenovo
{
    public static bool isEnabled;
    
    private static HidDevice hidDevice;
    private static byte[] buffer;
    private static ButtonEvent leftMenuButton, rightMenuButton;
    private static int leftButtonIndex, rightButtonIndex;
    private static byte leftButtonValue, rightButtonValue;

    private static BufferDeltaDetector detector = new BufferDeltaDetector();

    public static void Configure()
    {
        bool initHID = false;
        ushort vid = 0, pid = 0;
        if (Program.hardwareType == HardwareType.Lenovo_LegionGo)
        {
            isEnabled = true;
            initHID = true;
            vid = 0x17ef;
            pid = 0x6182;
            leftButtonIndex = 18;
            rightButtonIndex = 18;
            leftButtonValue = 0x80;
            rightButtonValue = 0x40;
        }
        else if (Program.hardwareType == HardwareType.Lenovo_LegionGo2)
        {
            isEnabled = true;
            initHID = true;
            vid = 0x17ef;
            pid = 0x61eb;
            leftButtonIndex = 18;
            rightButtonIndex = 18;
            leftButtonValue = 0x80;
            rightButtonValue = 0x40;
        }
        else if (Program.hardwareType == HardwareType.Lenovo_LegionGoS)
        {
            isEnabled = true;
            initHID = true;
            vid = 0x1a86;
            pid = 0xe310;
            leftButtonIndex = 1;
            rightButtonIndex = 1;
            leftButtonValue = 0x01;
            rightButtonValue = 0x02;
        }

        if (initHID)
        {
            hidDevice = new HidDevice();
            if (hidDevice.Init(vid, pid, true))
            {
                Log.WriteLine("Lenovo HID Device Initialized");
                buffer = new byte[256];
            }
            else
            {
                Log.WriteLine($"Failed to initialize Lenovo HID input device for (VID:{vid.ToString("x4")} PID:{pid.ToString("x4")})");
                hidDevice.Dispose();
                hidDevice = null;
            }
        }
    }

    public static void Dispose()
    {
        if (hidDevice != null)
        {
            hidDevice.Dispose();
            hidDevice = null;
        }
    }

    public static void Update(ref DateTime time, bool resumeFromSleep)
    {
        if (Program.inputMode != InputMode.ReignOS) return;

        // re-init after sleep
        if (resumeFromSleep)
        {
            Thread.Sleep(3000);
            Dispose();
            Configure();
            time = DateTime.Now;// reset time
        }

        // relay OEM buttons to virtual gamepad input
        if (hidDevice != null)
        {
            if (hidDevice.ReadData(buffer, 0, buffer.Length, out var length))
            {
                if (length == 32 && detector.TestDelta(buffer, (int)length))
                {
                    Log.WriteDataAsLine("LEGION: ", buffer, 0, (int)length);
                }
                
                leftMenuButton.Update(buffer[leftButtonIndex] == leftButtonValue);
                rightMenuButton.Update(buffer[rightButtonIndex] == rightButtonValue);
                if (leftMenuButton.down) VirtualGamepad.Write_TriggerLeftSteamMenu();
                else if (rightMenuButton.down) VirtualGamepad.Write_TriggerRightSteamMenu();
            }
        }
    }
}
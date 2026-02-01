using System;
using System.Threading;
using ReignOS.Core;
using ReignOS.Service.OS;

namespace ReignOS.Service.Hardware;

public static class Lenovo
{
    public static bool isEnabled;
    
    private static HidDevice device;
    private static byte[] buffer;
    private static ButtonEvent leftMenuButton, rightMenuButton;
    private static int leftButtonIndex, rightButtonIndex;
    private static byte leftButtonValue, rightButtonValue;

    private static Thread thread;
    private static object locker = new object();
    private static bool threadAlive;

    public static void Configure()
    {
        isEnabled = false;
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
            leftButtonIndex = 0;
            rightButtonIndex = 0;
            leftButtonValue = 0x01;
            rightButtonValue = 0x02;
        }

        if (initHID)
        {
            device = new HidDevice();
            if (device.Init(vid, pid, true, HidDeviceOpenMode.ReadOnly))
            {
                Log.WriteLine($"Lenovo HID Device Initialized (VID:{vid.ToString("x4")} PID:{pid.ToString("x4")} Handles:{device.handles.Count})");
                buffer = new byte[256];
                thread = new Thread(UpdateThread);
                thread.IsBackground = true;
                thread.Start();
            }
            else
            {
                Log.WriteLine($"Failed to initialize Lenovo HID input device for (VID:{vid.ToString("x4")} PID:{pid.ToString("x4")})");
                device.Dispose();
                device = null;
            }
        }
    }

    public static void Dispose()
    {
        threadAlive = false;
        if (device != null)
        {
            device.Dispose();
            device = null;
        }
    }

    public static void Update(ref DateTime time, bool resumeFromSleep)
    {
        if (Program.inputMode != InputMode.ReignOS) return;

        // re-init after sleep
        if (resumeFromSleep)
        {
            lock (locker)
            {
                Thread.Sleep(3000);
                Dispose();
                Configure();
                time = DateTime.Now; // reset time
            }
        }
    }
    
    private static void UpdateThread()
    {
        // relay OEM buttons to virtual gamepad input
        lock (locker)
        {
            if (device != null)
            {
                threadAlive = true;
                while (threadAlive)
                {
                    if (device.ReadData(buffer, 0, buffer.Length, out _, requireReadLength: 32))
                    {
                        leftMenuButton.Update(buffer[leftButtonIndex] == leftButtonValue);
                        rightMenuButton.Update(buffer[rightButtonIndex] == rightButtonValue);
                        if (leftMenuButton.down) VirtualGamepad.Write_TriggerLeftSteamMenu();
                        else if (rightMenuButton.down) VirtualGamepad.Write_TriggerRightSteamMenu();
                    }
                    Thread.Sleep(1);
                }
            }
        }
    }
}
using System;
using ReignOS.Core;
using ReignOS.Service.OS;

namespace ReignOS.Service.Hardware;

public static class Lenovo
{
    public static bool isEnabled;
    
    private static HidDevice hidDevice;
    private static byte[] buffer;
    private static ButtonEvent leftMenuButton, rightMenuButton;

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
        }
        else if (Program.hardwareType == HardwareType.Lenovo_LegionGo2)
        {
            isEnabled = true;
            initHID = true;
            vid = 0x17ef;
            pid = 0x61eb;
        }

        if (initHID)
        {
            hidDevice = new HidDevice();
            if (hidDevice.Init(vid, pid, true))
            {
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

    public static void Update(KeyList keys)
    {
        if (Program.inputMode != InputMode.ReignOS) return;

        // relay OEM buttons to virtual gamepad input
        //if (Program.hardwareType == HardwareType.Lenovo_LegionGo)
        {
            if (hidDevice != null)
            {
                if (hidDevice.ReadData(buffer, 0, buffer.Length, out nint sizeRead))
                {
                    leftMenuButton.Update(buffer[18] == 0x80);
                    rightMenuButton.Update(buffer[18] == 0x40);
                }
                
                if (leftMenuButton.down) VirtualGamepad.Write_TriggerLeftSteamMenu();
                else if (rightMenuButton.down) VirtualGamepad.Write_TriggerRightSteamMenu();
            }
        }
        /*else if (Program.hardwareType == HardwareType.Lenovo_LegionGo2)
        {
            if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_D, true)))
            {
                VirtualGamepad.Write_TriggerLeftSteamMenu();
            }
            else if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTCTRL, true), new KeyEvent(input.KEY_LEFTALT, true)))
            {
                VirtualGamepad.Write_TriggerRightSteamMenu();
            }
        }*/
    }
}
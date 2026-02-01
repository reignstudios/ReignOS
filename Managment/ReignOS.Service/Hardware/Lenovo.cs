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
        isEnabled =
            Program.hardwareType == HardwareType.Lenovo_LegionGo ||
            Program.hardwareType == HardwareType.Lenovo_LegionGo2;
        
        if (isEnabled && Program.hardwareType == HardwareType.Lenovo_LegionGo)
        {
            hidDevice = new HidDevice();
            if (hidDevice.Init(0x17ef, 0x6182, true))
            {
                buffer = new byte[256];
            }
            else
            {
                Log.WriteLine("Failed to initialize Lenovo HID input device for (VID:0x17ef PID:0x6182)");
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
        if (Program.hardwareType == HardwareType.Lenovo_LegionGo)
        {
            if (hidDevice != null)
            {
                if (hidDevice.ReadData(buffer, 0, buffer.Length, out nint sizeRead))
                {
                    //Log.WriteDataAsLine("Levono HID Data:", buffer, 0, (int)sizeRead);
                    leftMenuButton.Update(buffer[18] == 0x80);
                    rightMenuButton.Update(buffer[18] == 0x40);
                }
                
                if (leftMenuButton.down) VirtualGamepad.Write_TriggerLeftSteamMenu();
                else if (rightMenuButton.down) VirtualGamepad.Write_TriggerRightSteamMenu();
            }
        }
        else if (Program.hardwareType == HardwareType.Lenovo_LegionGo2)
        {
            if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_D, true)))
            {
                VirtualGamepad.Write_TriggerLeftSteamMenu();
            }
            else if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTCTRL, true), new KeyEvent(input.KEY_LEFTALT, true)))
            {
                VirtualGamepad.Write_TriggerRightSteamMenu();
            }
        }
    }
}
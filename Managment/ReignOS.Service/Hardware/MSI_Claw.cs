namespace ReignOS.Service.Hardware;
using ReignOS.Core;
using ReignOS.Service.HardwarePatches;
using ReignOS.Service.OS;
using System;
using System.Collections.Generic;
using System.Threading;

public static class MSI_Claw
{
    enum CommandType : byte
    {
        SetMode = 0x24,
        SyncToRom = 0x22
    }

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

    private const byte reportID = 0x0F;

    public static bool isEnabled { get; private set; }
    private static HidDevice device;

    public static void Configure()
    {
        // configure after sleep fixes
        AudioPatches.Fix1(true);
        WiFiPatches.Fix1(true);

        // configure gamepad
        if (Program.inputMode == InputMode.ReignOS)
        {
            device = new HidDevice();
            if (!device.Init(0x0DB0, 0x1901, true) || device.handles.Count == 0)// mode 1 (normal operating mode)
            {
                device.Dispose();
                device = new HidDevice();
                if (!device.Init(0x0DB0, 0x1902, true) || device.handles.Count == 0)// mode 2 (DInput mode)
                {
                    device.Dispose();
                    device = new HidDevice();
                    if (device.Init(0x0DB0, 0x1903, true))// mode 3 (testing mode)
                    {
                        // switch from Testing and reboot
                        DisableTestingMode();
                        ProcessUtil.Run("reboot", "-f", useBash:false);
                    }
                    device.Dispose();
                    device = null;
                    return;
                }

                // switch from DInput and reboot
                EnableMode(Mode.XInput);
                ProcessUtil.Run("reboot", "-f", useBash:false);
                device.Dispose();
                device = null;
                return;
            }

            Log.WriteLine($"MSI-Claw gamepad found: Handles={device.handles.Count}");
            EnableMode(Mode.XInput);
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
        // write
        int i = 0;
        var buffer = new byte[256];
        buffer[i++] = reportID;// report id
        buffer[i++] = 0;
        buffer[i++] = 0;
        buffer[i++] = 0x3C;
        buffer[i++] = (byte)CommandType.SetMode;// we want to switch mode
        buffer[i++] = (byte)mode;// mode
        buffer[i++] = 0;
        buffer[i++] = 0;
        if (!device.WriteData(buffer, 0, 64))// write 64 bytes to match wanted packet size
        {
            Log.WriteLine("FAILED: To set MSI-Claw gamepad mode");
            return false;
        }

        // read
        for (i = 0; i != 8; ++i)
        {
            Thread.Sleep(100);
            Array.Clear(buffer);
            if (device.ReadData(buffer, 0, buffer.Length, out nint sizeRead))
            {
                string hex = BitConverter.ToString(buffer, 0, (int)sizeRead);
                Log.WriteLine($"MSI-Claw gamepad read response {i}: Size={sizeRead} Data:{hex}");
            }
            else
            {
                Log.WriteLine($"MSI-Claw gamepad done reading response");
                break;
            }
        }

        // finish
        Log.WriteLine("MSI-Claw gamepad mode set");
        isEnabled = true;
        return true;
    }

    private static bool SyncToRom()
    {
        // write
        int i = 0;
        var buffer = new byte[256];
        buffer[i++] = reportID;// report id
        buffer[i++] = 0;
        buffer[i++] = 0;
        buffer[i++] = 0x3C;
        buffer[i++] = (byte)CommandType.SyncToRom;// sync to rom
        buffer[i++] = 0;
        buffer[i++] = 0;
        buffer[i++] = 0;
        if (!device.WriteData(buffer, 0, 64))// write 64 bytes to match wanted packet size
        {
            Log.WriteLine("FAILED: To set MSI-Claw sync-to-rom");
            return false;
        }

        // read
        for (i = 0; i != 8; ++i)
        {
            Thread.Sleep(100);
            Array.Clear(buffer);
            if (device.ReadData(buffer, 0, buffer.Length, out nint sizeRead))
            {
                string hex = BitConverter.ToString(buffer, 0, (int)sizeRead);
                Log.WriteLine($"MSI-Claw sync-to-rom read response {i}: Size={sizeRead} Data:{hex}");
            }
            else
            {
                Log.WriteLine($"MSI-Claw sync-to-rom done reading response");
                break;
            }
        }

        // finish
        Log.WriteLine("MSI-Claw sync-to-rom set");
        return true;
    }

    private static bool DisableTestingMode()
    {
        Log.WriteLine("Disabling MSI-Claw testing mode...");

        // write
        int i = 0;
        var buffer = new byte[256];
        buffer[i++] = 0x0f;
        buffer[i++] = 0x00;
        buffer[i++] = 0x00;
        buffer[i++] = 0x3c;
        buffer[i++] = 0x21;// MSI_CLAW_COMMAND_TYPE_WRITE_RGB_STATUS
        buffer[i++] = 0x01;
        buffer[i++] = 0x01;
        buffer[i++] = 0x1f;
        buffer[i++] = 0x05;
        buffer[i++] = 0x01;
        buffer[i++] = 0x00;
        buffer[i++] = 0x00;
        buffer[i++] = 0x12;
        buffer[i++] = 0x00;
        if (!device.WriteData(buffer, 0, 64))// write 64 bytes to match wanted packet size
        {
            Log.WriteLine("FAILED: To disable MSI-Claw testing mode");
            return false;
        }

        // read
        Thread.Sleep(1000);
        Array.Clear(buffer);
        if (device.ReadData(buffer, 0, buffer.Length, out nint sizeRead))
        {
            string hex = BitConverter.ToString(buffer, 0, (int)sizeRead);
            Log.WriteLine($"MSI-Claw gamepad read response: Size={sizeRead} Data:{hex}");
        }
        else
        {
            Log.WriteLine("ERROR: MSI-Claw gamepad failed to read response 1");
        }

        // finish
        Log.WriteLine("MSI-Claw testing mode disable finished (need reboot)");
        return true;
    }

    public static void Update(ref DateTime time, bool resumeFromSleep, KeyList keys)
    {
        if (Program.inputMode != InputMode.ReignOS) return;

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
        else
        {
            // relay OEM buttons to virtual gamepad input
            if (KeyEvent.Pressed(keys, input.KEY_F15))
            {
                VirtualGamepad.Write_TriggerLeftSteamMenu();
            }
            else if (KeyEvent.Pressed(keys, input.KEY_F16))
            {
                VirtualGamepad.Write_TriggerRightSteamMenu();
            }
        }
    }
}
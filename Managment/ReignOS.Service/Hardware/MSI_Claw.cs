namespace ReignOS.Service.Hardware;
using ReignOS.Core;
using ReignOS.Service.OS;
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

    public static void Configure()
    {
        device = new HidDevice();
        if (!device.Init(0x0DB0, 0x1901, true))
        {
            device.Dispose();
            device = null;
            return;
        }
        
        Log.WriteLine("MSI-Claw gamepad found");
        if (EnableMode(Mode.XInput))
        {
            Program.keyboardInput = new KeyboardInput();
            Program.keyboardInput.Init("AT Translated Set 2 keyboard", true, 0, 0);
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
        
        Log.WriteLine("MSI-Claw gamepad mode set");
        isEnabled = true;
        return true;
    }

    public static void Update(bool resumeFromSleep, ushort key, bool keyPressed)
    {
        if (resumeFromSleep)
        {
            if (device != null) EnableMode(Mode.XInput);
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
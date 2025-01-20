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
    private static KeyboardInput keyboardInput;

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
            keyboardInput = new KeyboardInput();
            keyboardInput.Init(0x1, 0x1);
        }
    }
    
    public static void Dispose()
    {
        if (keyboardInput != null)
        {
            keyboardInput.Dispose();
            keyboardInput = null;
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

    public static void Update(bool resumeFromSleep)
    {
        if (resumeFromSleep)
        {
            if (device != null) EnableMode(Mode.XInput);
        }
        else
        {
            // relay OEM buttons to virtual gamepad input
            if (keyboardInput != null && keyboardInput.ReadNextKey(out ushort key, out bool pressed))
            {
                if (key == input.KEY_F15 && !pressed)
                {
                    VirtualGamepad.Write_TriggerLeftSteamMenu();
                }
                else if (key == input.KEY_F16 && !pressed)
                {
                    VirtualGamepad.Write_TriggerRightSteamMenu();
                }
            }
        }
    }
}
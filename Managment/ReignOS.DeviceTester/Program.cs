using ReignOS.Core;

namespace ReignOS.DeviceTester;

enum Mode
{
    Unset,
    HID,
    Keyboard,
    Gamepad
}

class Program
{
    private static Mode mode = Mode.Unset;
    private static ushort vid, pid;
    
    static void Main(string[] args)
    {
        Console.WriteLine("<<< ReignOS Device Tester >>>");
        
        #if DEBUG
        args = new[]
        {
            "-mode=Gamepad",
            //"-vid=0x45e",
            //"-pid=0x2ea"
        };
        #endif
        
        // process args
        if (args == null || args.Length == 0)
        {
            Console.WriteLine("--- HELP ---");
            Console.WriteLine("   Mode: -mode=<HID,Keyboard,Gamepad>");
            Console.WriteLine("   Option: -vid=<HEX,DEC> -pid=<HEX,DEC> (blank opens everything)");
            return;
        }

        foreach (var arg in args)
        {
            if (arg.StartsWith("-mode="))
            {
                var parts = arg.Split('=');
                if (parts[1] == "HID") mode = Mode.HID;
                else if (parts[1] == "Keyboard") mode = Mode.Keyboard;
                else if (parts[1] == "Gamepad") mode = Mode.Gamepad;
            }
            else if (arg.StartsWith("-vid="))
            {
                var parts = arg.Split('=');
                if (ushort.TryParse(parts[1], out ushort value)) vid = value;
                else vid = Convert.ToUInt16(parts[1], 16);
            }
            else if (arg.StartsWith("-pid="))
            {
                var parts = arg.Split('=');
                if (ushort.TryParse(parts[1], out ushort value)) pid = value;
                else pid = Convert.ToUInt16(parts[1], 16);
            }
        }
        
        // run mode
        switch (mode)
        {
            case Mode.HID: Mode_HID(); break;
            case Mode.Keyboard: Mode_Keyboard(); break;
            case Mode.Gamepad: Mode_Gamepad(); break;
            default: throw new NotImplementedException();
        }
    }

    private static void Mode_HID()
    {
        var hidDevice = new HidDevice();
        if (!hidDevice.Init(vid, pid, true, HidDeviceOpenMode.ReadOnly, forceOpenAllEndpoints: vid != 0 && pid != 0))
        {
            Console.WriteLine("ERROR: No HID device found");
            hidDevice.Dispose();
            return;
        }

        var data = new byte[1024];
        while (true)
        {
            if (hidDevice.ReadData(data, 0, data.Length, out nint sizeRead))
            {
                for (nint i = 0; i < sizeRead; i++)
                {
                    string value = data[i].ToString("X2");
                    Console.Write($"0x{value} ");
                }
                Console.WriteLine();
            }
            Thread.Sleep(1);
        }
        
        hidDevice.Dispose();
    }
    
    private static void Mode_Keyboard()
    {
        var keyboardDevice = new KeyboardDevice();
        keyboardDevice.Init(null, false, vid, pid, forceOpenAllEndpoints: vid != 0 && pid != 0);

        while (true)
        {
            if (keyboardDevice.ReadNextKey(out var key))
            {
                string value = key.key.ToString("X2");
                Console.WriteLine($"KEY: 0x{value}");
            }
            Thread.Sleep(1);
        }
        
        keyboardDevice.Dispose();
    }
    
    private static void Mode_Gamepad()
    {
        var gamepadDevice = new GamepadDevice();
        gamepadDevice.Init(vid, pid);

        while (true)
        {
            var gamepads = gamepadDevice.ReadNextInput();
            if (gamepads == null) break;
            
            foreach (var gamepad in gamepads)
            {
                int i = 0;
                foreach (var button in gamepad.buttons)
                {
                    if (button.down) Console.WriteLine($"Gamepad:'{gamepad.name}' Button: {i}");
                    i++;
                }
                
                i = 0;
                foreach (var axis in gamepad.axes)
                {
                    if (axis.value != 0) Console.WriteLine($"Gamepad:'{gamepad.name}' Axis: {i} Value:{axis.value}");
                    i++;
                }
            }
            Thread.Sleep(1);
        }
        
        gamepadDevice.Dispose();
    }
}
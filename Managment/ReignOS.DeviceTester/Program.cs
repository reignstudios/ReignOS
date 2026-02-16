using ReignOS.Core;

namespace ReignOS.DeviceTester;

enum Mode
{
    Unset,
    HID,
    KEY
}

class Program
{
    private static Mode mode = Mode.Unset;
    private static bool listMode;
    private static ushort vid, pid;
    
    private static HidDevice hidDevice;
    private static KeyboardInput keyboardInput;
    
    static void Main(string[] args)
    {
        Console.WriteLine("<<< ReignOS Device Tester >>>");
        
        #if DEBUG
        args = new[]
        {
            "-mode=HID",
            "-vid=0x45e",
            "-pid=0x2ea"
        };
        #endif
        
        // process args
        if (args == null || args.Length == 0)
        {
            Console.WriteLine("--- HELP ---");
            Console.WriteLine("   Mode: -mode=<HID,KEY>");
            Console.WriteLine("   HID Option: -list -vid=<HEX,DEC> -pid=<HEX,DEC>");
            return;
        }

        foreach (var arg in args)
        {
            if (arg.StartsWith("-mode="))
            {
                var parts = arg.Split('=');
                if (parts[1] == "HID") mode = Mode.HID;
                else if (parts[1] == "KEY") mode = Mode.KEY;
            }
            else if (arg.StartsWith("-list"))
            {
                listMode = true;
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
            case Mode.KEY: Mode_KEY(); break;
            default: throw new NotImplementedException();
        }
    }

    private static void Mode_HID()
    {
        hidDevice = new HidDevice();
        if (!hidDevice.Init(vid, pid, true, HidDeviceOpenMode.ReadOnly, debugLog:true))
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
        }
    }
    
    private static void Mode_KEY()
    {
        keyboardInput = new KeyboardInput();
        keyboardInput.Init(null, false, 0, 0, openAll:true);

        while (true)
        {
            if (keyboardInput.ReadNextKey(out var key))
            {
                string keyValue = key.key.ToString("X2");
                Console.WriteLine($"KEY: 0x{keyValue}");
            }
            Thread.Sleep(1);
        }
    }
}
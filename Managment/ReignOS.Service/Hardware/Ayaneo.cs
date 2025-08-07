using ReignOS.Service.HardwarePatches;
using ReignOS.Service.OS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReignOS.Core;

namespace ReignOS.Service.Hardware
{
    public static class Ayaneo
    {
        public static bool isEnabled;

        public static void Configure()
        {
            isEnabled =
                Program.hardwareType == HardwareType.Ayaneo ||
                Program.hardwareType == HardwareType.Ayaneo1 ||
                Program.hardwareType == HardwareType.Ayaneo2 ||
                Program.hardwareType == HardwareType.Ayaneo3 ||
                Program.hardwareType == HardwareType.AyaneoPro ||
                Program.hardwareType == HardwareType.AyaneoPlus ||
                Program.hardwareType == HardwareType.AyaneoFlipDS ||
                Program.hardwareType == HardwareType.AyaneoSlide;

            if (Program.hardwareType == HardwareType.Ayaneo1 || Program.hardwareType == HardwareType.AyaneoPro)
            {
                WiFiPatches.Fix2(true);
            }
            else if (Program.hardwareType == HardwareType.AyaneoSlide)
            {
                bool needsReboot = false;
                if (ForceAcpiStrict("/boot/loader/entries/arch.conf")) needsReboot = true;
                if (ForceAcpiStrict("/boot/loader/entries/chimera.conf")) needsReboot = true;
                if (needsReboot) Program.isRebootMode = true; 
            }
            else if (Program.hardwareType == HardwareType.Ayaneo3)
            {
                // TEST: popped in event
                var device = new HidDevice();
                if (device.Init(7247, 2, true) || device.handles.Count >= 1)
                {
                    var readData = new byte[256];

                    var data = new byte[] {
0x3c,
0x07,
0x21,
0x09,
0x00,
0x00,
0x00,
0x03,
0xff,
0xff,
0xff,
0x03,
0xff,
0xff,
0xff,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x33,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x01,
0x00,
0x00,
0x00,
0x40,
0x64,
0x64,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00
};
                    device.WriteData(data, 0, data.Length);
                    System.Threading.Thread.Sleep(100);
                    device.ReadData(readData, 0, readData.Length, out _);

                    data = new byte[] {
0x00,
0x00,
0x00,
0x0a,
0x01,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00
};
                    device.WriteData(data, 0, data.Length);
                    System.Threading.Thread.Sleep(100);
                    device.ReadData(readData, 0, readData.Length, out _);

                    data = new byte[] {
0x00,
0x00,
0x00,
0x08,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00
};
                    device.WriteData(data, 0, data.Length);
                    System.Threading.Thread.Sleep(100);
                    device.ReadData(readData, 0, readData.Length, out _);

                    data = new byte[] {
0x00,
0x00,
0x00,
0x08,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00
};
                    device.WriteData(data, 0, data.Length);
                    System.Threading.Thread.Sleep(100);
                    device.ReadData(readData, 0, readData.Length, out _);

                // TEST: pop out event
                /*var device = new HidDevice();
                if (device.Init(7247, 2, true) || device.handles.Count >= 1)
                {
                    var readData = new byte[256];
                    var data = new byte[] {
0x3c,
0x07,
0x21,
0x09,
0x00,
0x00,
0x00,
0x03,
0xff,
0xff,
0xff,
0x03,
0xff,
0xff,
0xff,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x33,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x01,
0x00,
0x00,
0x00,
0x40,
0x64,
0x64,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00
};
                    device.WriteData(data, 0, data.Length);
                    System.Threading.Thread.Sleep(100);
                    device.ReadData(readData, 0, readData.Length, out _);

                    data = new byte[] {
0x00,
0x00,
0x00,
0x05,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00
};
                    device.WriteData(data, 0, data.Length);
                    System.Threading.Thread.Sleep(100);
                    device.ReadData(readData, 0, readData.Length, out _);

                    data = new byte[] {
0xb3,
0x07,
0x21,
0x09,
0x00,
0x00,
0x00,
0x03,
0xff,
0xff,
0xff,
0x03,
0xff,
0xff,
0xff,
0x00,
0x00,
0x00,
0x00,
0x77,
0x00,
0x33,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x01,
0x00,
0x00,
0x00,
0x40,
0x64,
0x64,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00
};
                    device.WriteData(data, 0, data.Length);
                    System.Threading.Thread.Sleep(100);
                    device.ReadData(readData, 0, readData.Length, out _);

                    data = new byte[] {
0x00,
0x00,
0x00,
0x08,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00,
0x00
};
                    device.WriteData(data, 0, data.Length);
                    System.Threading.Thread.Sleep(100);
                    device.ReadData(readData, 0, readData.Length, out _);*/
                }
            }
        }

        private static bool ForceAcpiStrict(string conf)
        {
            if (!File.Exists(conf)) return false;

            try
            {
                string settings = File.ReadAllText(conf);
                if (!settings.Contains(" acpi=strict"))
                {
                    settings = settings.Replace(" rw rootwait", " rw rootwait acpi=strict");
                    ProcessUtil.WriteAllTextAdmin(conf, settings);
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(e);
            }
            return false;
        }

        public static void Update(KeyList keys)
        {
            if (Program.inputMode != InputMode.ReignOS) return;

            // relay OEM buttons to virtual gamepad input
            if (Program.hardwareType == HardwareType.Ayaneo1 || Program.hardwareType == HardwareType.Ayaneo)
            {
                if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTMETA, true)))
                {
                    VirtualGamepad.Write_TriggerLeftSteamMenu();
                }
                
                if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_RIGHTALT, true), new KeyEvent(input.KEY_RIGHTCTRL, true), new KeyEvent(input.KEY_DELETE, true)))
                {
                    VirtualGamepad.Write_TriggerRightSteamMenu();
                }
            }

            if (Program.hardwareType == HardwareType.Ayaneo2 || Program.hardwareType == HardwareType.AyaneoFlipDS || Program.hardwareType == HardwareType.Ayaneo)
            {
                if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_RIGHTCTRL, true), new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_F17, true)))
                {
                    VirtualGamepad.Write_TriggerLeftSteamMenu();
                }
            }

            if (Program.hardwareType == HardwareType.Ayaneo3 || Program.hardwareType == HardwareType.Ayaneo)
            {
                if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_F23, true)))
                {
                    VirtualGamepad.Write_TriggerLeftSteamMenu();
                }

                if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_F24, true)))
                {
                    VirtualGamepad.Write_TriggerRightSteamMenu();
                }
            }

            if (Program.hardwareType == HardwareType.AyaneoPro || Program.hardwareType == HardwareType.Ayaneo)
            {
                if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_RIGHTCTRL, true), new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_F12, true)))
                {
                    VirtualGamepad.Write_TriggerLeftSteamMenu();
                }
            }

            if (Program.hardwareType == HardwareType.AyaneoPlus || Program.hardwareType == HardwareType.AyaneoSlide || Program.hardwareType == HardwareType.Ayaneo)
            {
                if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTCTRL, true), new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_F17, true)))
                {
                    VirtualGamepad.Write_TriggerLeftSteamMenu();
                }
            }

            if
            (
                Program.hardwareType == HardwareType.Ayaneo2 ||
                Program.hardwareType == HardwareType.AyaneoPro ||
                Program.hardwareType == HardwareType.AyaneoPlus ||
                Program.hardwareType == HardwareType.AyaneoFlipDS ||
                Program.hardwareType == HardwareType.AyaneoSlide ||
                Program.hardwareType == HardwareType.Ayaneo
            )
            {
                if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_D, true)))
                {
                    VirtualGamepad.Write_TriggerRightSteamMenu();
                }
            }
        }
    }
}

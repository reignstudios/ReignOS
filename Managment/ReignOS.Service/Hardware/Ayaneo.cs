using ReignOS.Service.HardwarePatches;
using ReignOS.Service.OS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

        public static void MagicModule_PopOut()
        {
            Log.WriteLine("MagicModule_PopOut...");
            if (Program.hardwareType != HardwareType.Ayaneo3) return;

            // init hid device
            using var device = new HidDevice();
            if (!device.Init(7247, 2, false, physicalLocation:"input2", physicalLocationIsContains:true) || device.handles.Count == 0) return;
            var data = new byte[256];
            int i;

            // command pattern 1
            //WriteStandardModuleData1(device, data);

            // popout commands
            i = 0;
            Array.Clear(data);
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x05;
            WriteDeviceData(device, data);

            i = 0;
            Array.Clear(data);
            data[i++] = 0xb3;
            data[i++] = 0x07;
            data[i++] = 0x21;
            data[i++] = 0x09;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x03;
            data[i++] = 0xff;
            data[i++] = 0xff;
            data[i++] = 0xff;
            data[i++] = 0x03;
            data[i++] = 0xff;
            data[i++] = 0xff;
            data[i++] = 0xff;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x77;
            data[i++] = 0x00;
            data[i++] = 0x33;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x01;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x40;
            data[i++] = 0x64;
            data[i++] = 0x64;
            WriteDeviceData(device, data);

            // command pattern 2
            //WriteStandardModuleData2(device, data);
            Log.WriteLine("MagicModule_PopOut: Done!");
        }

        public static void MagicModule_PoppedIn()
        {
            Log.WriteLine("MagicModule_PoppedIn...");
            if (Program.hardwareType != HardwareType.Ayaneo3) return;

            // command que pattern
            /*static void QuePattern(HidDevice device, byte[] data)
            {
                for (int l = 0; l != 32; ++l)
                {
                    byte s = 0x00;
                    if (l == 0x16) s = 0x72;
                    else if (l == 0x17) s = 0x73;

                    int i = 0;
                    Array.Clear(data);
                    data[i++] = s;
                    data[i++] = 0x00;
                    data[i++] = 0x0b;
                    data[i++] = 0x07;
                    data[i++] = (byte)l;
                    data[i++] = 0x00;
                    data[i++] = 0x00;
                    data[i++] = 0x00;
                    data[i++] = 0x00;
                    data[i++] = 0x00;
                    data[i++] = 0x00;
                    data[i++] = s;
                    WriteDeviceData(device, data, sleepBeforeRead: 10);
                }

                WriteStandardModuleData2(device, data);
                WriteStandardModuleData1(device, data);
            }*/

            // init hid device
            using (var device = new HidDevice())
            {
                if (!device.Init(7247, 2, false, physicalLocation: "input2", physicalLocationIsContains: true) || device.handles.Count == 0) return;
                var data = new byte[256];
                int i;

                // set xpad mode
                Thread.Sleep(500);
                //QuePattern(device, data);
                i = 0;
                Array.Clear(data);
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x0a;
                data[i++] = 0x01;
                WriteDeviceData(device, data);
                //WriteStandardModuleData2(device, data);
                //WriteStandardModuleData1(device, data);
                //QuePattern(device, data);

                // Ayaneo opens app (device init)
                //QuePattern(device, data);
                i = 0;
                Array.Clear(data);
                data[i++] = 0xc4;
                data[i++] = 0x07;
                data[i++] = 0x21;
                data[i++] = 0x09;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x03;
                data[i++] = 0xff;
                data[i++] = 0xff;
                data[i++] = 0xff;
                data[i++] = 0x03;
                data[i++] = 0xff;
                data[i++] = 0xff;
                data[i++] = 0xff;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x88;
                data[i++] = 0x00;
                data[i++] = 0x33;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x01;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x00;
                data[i++] = 0x40;
                data[i++] = 0x64;
                data[i++] = 0x64;
                WriteDeviceData(device, data);
                //QuePattern(device, data);
            }

            // reset device
            /*Thread.Sleep(100);
            using (var resetDevice = new HidDevice())
            {
                resetDevice.Init(7247, 2, true, resetDevice: true);
            }

            using (var device = new HidDevice())
            {
                if (!device.Init(7247, 2, true, blocking: true) || device.handles.Count == 0) return;
                var data = new byte[256];
                QuePattern(device, data);
            }*/

            // finished
            Log.WriteLine("MagicModule_PoppedIn: Done!");

            // power off
            //Thread.Sleep(500);
            //ProcessUtil.Run("poweroff", "-f", useBash:false);
        }

        /*private static void WriteStandardModuleData1(HidDevice device, byte[] data)
        {
            int i = 0;
            Array.Clear(data);
            data[i++] = 0x3c;
            data[i++] = 0x07;
            data[i++] = 0x21;
            data[i++] = 0x09;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x03;
            data[i++] = 0xff;
            data[i++] = 0xff;
            data[i++] = 0xff;
            data[i++] = 0x03;
            data[i++] = 0xff;
            data[i++] = 0xff;
            data[i++] = 0xff;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x33;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x01;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x40;
            data[i++] = 0x64;
            data[i++] = 0x64;
            WriteDeviceData(device, data);
        }

        private static void WriteStandardModuleData2(HidDevice device, byte[] data)
        {
            int i = 0;
            Array.Clear(data);
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x00;
            data[i++] = 0x08;
            WriteDeviceData(device, data);
        }*/

        private static void WriteDeviceData(HidDevice device, byte[] data, int packetSize = 64, int sleepBeforeRead = 15)
        {
            device.WriteData(data, 0, packetSize);
            Thread.Sleep(sleepBeforeRead);

            //Array.Clear(data, 0, data.Length);
            //device.ReadData(data, 0, data.Length, out _);
            for (int i = 0; i != 8; ++i)
            {
                Thread.Sleep(sleepBeforeRead);
                Array.Clear(data);
                if (device.ReadData(data, 0, data.Length, out nint sizeRead))
                {
                    string hex = BitConverter.ToString(data, 0, (int)sizeRead);
                    Log.WriteLine($"Ayaneo module read response {i}: Size={sizeRead} Data:{hex}");
                }
                else
                {
                    Log.WriteLine($"Ayaneo module done reading response");
                    break;
                }
            }
        }
    }
}

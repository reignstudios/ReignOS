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
                Program.hardwareType == HardwareType.AyaneoPro ||
                Program.hardwareType == HardwareType.AyaneoPlus ||
                Program.hardwareType == HardwareType.AyaneoFlipDS ||
                Program.hardwareType == HardwareType.AyaneoSlide ||
                Program.hardwareType == HardwareType.Ayaneo3;

            if (Program.hardwareType == HardwareType.AyaneoPro)
            {
                WiFiPatches.Fix2(true);
            }
            else if (Program.hardwareType == HardwareType.AyaneoSlide)
            {
                ForceAcpiStrict("/boot/loader/entries/arch.conf");
                ForceAcpiStrict("/boot/loader/entries/chimera.conf");
            }
        }

        private static void ForceAcpiStrict(string conf)
        {
            if (!File.Exists(conf)) return;

            try
            {
                string settings = File.ReadAllText(conf);
                if (!settings.Contains(" acpi=strict"))
                {
                    settings = settings.Replace(" rw rootwait", " rw rootwait acpi=strict");
                    ProcessUtil.WriteAllTextAdmin(conf, settings);
                }
            }
            catch (Exception e)
            {
                Log.WriteLine(e);
            }
        }

        public static void Update(KeyList keys)
        {
            if (Program.inputMode != InputMode.ReignOS) return;

            // relay OEM buttons to virtual gamepad input
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

            if (Program.hardwareType == HardwareType.AyaneoFlipDS || Program.hardwareType == HardwareType.Ayaneo)
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

            if
            (
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

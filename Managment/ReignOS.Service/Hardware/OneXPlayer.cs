using ReignOS.Service.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReignOS.Core;

namespace ReignOS.Service.Hardware
{
    public static class OneXPlayer
    {
        public static bool isEnabled;

        public static void Configure()
        {
            isEnabled = Program.hardwareType == HardwareType.OneXPlayer_Gen1 || Program.hardwareType == HardwareType.OneXPlayer_Gen2;
        }

        public static void Update(KeyList keys)
        {
            if (Program.inputMode != InputMode.ReignOS) return;

            // relay OEM buttons to virtual gamepad input
            if (Program.hardwareType == HardwareType.OneXPlayer_Gen1)
            {
                if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_D, true)))
                {
                    VirtualGamepad.Write_TriggerLeftSteamMenu();
                }
                else if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_RIGHTCTRL, true), new KeyEvent(input.KEY_O, true)))
                {
                    VirtualGamepad.Write_TriggerRightSteamMenu();
                }
            }
            else
            {
                if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_D, true)))
                {
                    VirtualGamepad.Write_TriggerLeftSteamMenu();
                }
                else if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_RIGHTCTRL, true), new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_O, true)))
                {
                    VirtualGamepad.Write_TriggerRightSteamMenu();
                }
            }
        }
    }
}

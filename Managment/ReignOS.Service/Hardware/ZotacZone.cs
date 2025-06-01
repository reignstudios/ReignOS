using ReignOS.Service.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReignOS.Service.Hardware
{
    public static class ZotacZone
    {
        public static bool isEnabled;

        public static void Configure()
        {
            isEnabled = Program.hardwareType == HardwareType.ZotacZone;
        }

        public static void Update(List<KeyEvent> keys)
        {
            // relay OEM buttons to virtual gamepad input
            if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTCTRL, true), new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_F17, true)))
            {
                // VirtualGamepad.Write_TriggerLeftSteamMenu();
                VirtualGamepad.Write_TriggerRightSteamMenu();
            }
            else if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTCTRL, true), new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_F18, true)))
            {
                VirtualGamepad.Write_TriggerRightSteamMenu();
            }
        }
    }
}

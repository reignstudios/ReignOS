using ReignOS.Service.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReignOS.Service.Hardware
{
    public static class LokiZero
    {
        public static bool isEnabled;

        public static void Configure()
        {
            isEnabled = Program.hardwareType == HardwareType.LokiZero;
        }

        public static void Update(KeyList keys)
        {
            if (Program.useInputPlumber) return;

            // relay OEM buttons to virtual gamepad input
            if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTCTRL, true), new KeyEvent(input.KEY_LEFTALT, true), new KeyEvent(input.KEY_LEFTSHIFT, true), new KeyEvent(input.KEY_T, true)))
            {
                VirtualGamepad.Write_TriggerRightSteamMenu();
            }
        }
    }
}
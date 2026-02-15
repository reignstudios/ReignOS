using ReignOS.Core;
using ReignOS.Core.OS;

namespace ReignOS.Service.Hardware;

public static class GPD
{
	public static bool isEnabled;

	public static void Configure()
	{
		isEnabled = Program.hardwareType == HardwareType.GPD_Win5;
	}

	public static void Update(KeyList keys)
	{
		if (Program.inputMode != InputMode.ReignOS) return;

		// relay OEM buttons to virtual gamepad input
        if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_D, true)))
        {
            VirtualGamepad.Write_TriggerLeftSteamMenu();
        }
        else if (KeyEvent.Pressed(keys, new KeyEvent(input.KEY_LEFTCTRL, true), new KeyEvent(input.KEY_LEFTMETA, true), new KeyEvent(input.KEY_O, true)))// Old BIOS
        {
            VirtualGamepad.Write_TriggerRightSteamMenu();
        }
	}
}

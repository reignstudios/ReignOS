using ReignOS.Core.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReignOS.Core;

namespace ReignOS.Service.Hardware
{
    public static class Asus
    {
        public static bool isEnabled;
        private static GamepadDevice gamepadDevice;

        private const int button_A = 0;
        private const int button_B = 1;
        private const int button_X = 2;
        private const int button_Y = 3;
        private const int button_BumperLeft = 4;
        private const int button_BumperRight = 5;
        private const int button_Back = 6;
        private const int button_Menu = 7;
        private const int button_StickLeft = 8;
        private const int button_StickRight = 9;
        
        private const int axis_StickLeftX = 0;
        private const int axis_StickLeftY = 1;
        private const int axis_StickRightX = 3;
        private const int axis_StickRightY = 4;
        private const int axis_TriggerLeft = 2;
        private const int axis_TriggerRight = 5;
        private const int axis_DPadX = 6;
        private const int axis_DPadY = 7;

        public static void Configure()
        {
            isEnabled = false;
            bool initGamepad = false;
            ushort vid = 0, pid = 0;
            if (Program.hardwareType == HardwareType.RogAllyX)
            {
                isEnabled = true;
                initGamepad = true;
                vid = 0x0b05;
                pid = 0x1b4c;
            }
            else if (Program.hardwareType == HardwareType.RogAlly)
            {
                isEnabled = true;
            }

            if (initGamepad)
            {
                Log.Write($"Asus Gamepad init: VID={vid}, PID={pid}");
                gamepadDevice = new GamepadDevice();
                gamepadDevice.Init(vid, pid);
            }
        }

        public static void Dispose()
        {
            if (gamepadDevice != null)
            {
                gamepadDevice.Dispose();
                gamepadDevice = null;
            }
        }

        public static void Update(KeyList keys)
        {
            if (Program.inputMode != InputMode.ReignOS) return;
            
            // relay gamepad to virtual gamepad
            if (gamepadDevice != null)
            {
                var gamepad = gamepadDevice.ReadNextInput().FirstOrDefault();
                if (gamepad != null)
                {
                    var buttons = gamepad.buttons;
                    bool hasButtonUpdates = false;
                    for (int i = 0; i != buttons.Length; i++)
                    {
                        if (buttons[i].hasUpdate)
                        {
                            hasButtonUpdates = true;
                            break;
                        }
                    }
                    
                    var axes = gamepad.axes;
                    bool hasAxisUpdates = false;
                    for (int i = 0; i != axes.Length; i++)
                    {
                        if (axes[i].hasUpdate)
                        {
                            hasAxisUpdates = true;
                            break;
                        }
                    }

                    if (hasButtonUpdates || hasAxisUpdates)
                    {
                        VirtualGamepad.StartWrites();
                        
                        // buttons
                        if (buttons[button_A].hasUpdate) VirtualGamepad.WriteButton(input.BTN_SOUTH, buttons[button_A].on);
                        if (buttons[button_B].hasUpdate) VirtualGamepad.WriteButton(input.BTN_EAST, buttons[button_B].on);
                        if (buttons[button_X].hasUpdate) VirtualGamepad.WriteButton(input.BTN_WEST, buttons[button_X].on);
                        if (buttons[button_Y].hasUpdate) VirtualGamepad.WriteButton(input.BTN_NORTH, buttons[button_Y].on);
                        
                        if (buttons[button_BumperLeft].hasUpdate) VirtualGamepad.WriteButton(input.BTN_TL, buttons[button_BumperLeft].on);
                        if (buttons[button_BumperRight].hasUpdate) VirtualGamepad.WriteButton(input.BTN_TR, buttons[button_BumperRight].on);
                        
                        if (buttons[button_Back].hasUpdate) VirtualGamepad.WriteButton(input.BTN_SELECT, buttons[button_Back].on);
                        if (buttons[button_Menu].hasUpdate) VirtualGamepad.WriteButton(input.BTN_START, buttons[button_Menu].on);
                        
                        if (buttons[button_StickLeft].hasUpdate) VirtualGamepad.WriteButton(input.BTN_THUMBL, buttons[button_StickLeft].on);
                        if (buttons[button_StickRight].hasUpdate) VirtualGamepad.WriteButton(input.BTN_THUMBR, buttons[button_StickRight].on);
                        
                        // axes
                        if (buttons[axis_StickLeftX].hasUpdate) VirtualGamepad.WriteAxis(input.ABS_X, axes[axis_StickLeftX].value);
                        if (buttons[axis_StickLeftY].hasUpdate) VirtualGamepad.WriteAxis(input.ABS_Y, axes[axis_StickLeftY].value);
                        if (buttons[axis_StickRightX].hasUpdate) VirtualGamepad.WriteAxis(input.ABS_RX, axes[axis_StickRightX].value);
                        if (buttons[axis_StickRightY].hasUpdate) VirtualGamepad.WriteAxis(input.ABS_RY, axes[axis_StickRightY].value);
                        
                        if (buttons[axis_TriggerLeft].hasUpdate) VirtualGamepad.WriteAxis(input.ABS_Z, axes[axis_TriggerLeft].value);
                        if (buttons[axis_TriggerRight].hasUpdate) VirtualGamepad.WriteAxis(input.ABS_RZ, axes[axis_TriggerRight].value);
                        
                        if (buttons[axis_DPadX].hasUpdate) VirtualGamepad.WriteAxis(input.ABS_HAT0X, axes[axis_DPadX].value);
                        if (buttons[axis_DPadY].hasUpdate) VirtualGamepad.WriteAxis(input.ABS_HAT0Y, axes[axis_DPadY].value);
                        
                        VirtualGamepad.EndWrites();
                    }
                }
            }

            // relay OEM buttons to virtual gamepad
            if (KeyEvent.Pressed(keys, input.KEY_F16))
            {
                VirtualGamepad.Write_TriggerLeftSteamMenu();
            }
            else if (KeyEvent.Pressed(keys, input.KEY_PROG1))
            {
                VirtualGamepad.Write_TriggerRightSteamMenu();
            }
        }
    }
}

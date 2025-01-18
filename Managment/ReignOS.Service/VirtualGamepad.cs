namespace ReignOS.Service;
using ReignOS.Service.OS;
using ReignOS.Core;

using System;
using System.Text;

public unsafe static class VirtualGamepad
{
    private static int fd;

    public static void Init()
    {
        // open uinput
        uinput.uinput_user_dev uidev;
        byte[] uinputPath = Encoding.UTF8.GetBytes("/dev/uinput");
        fixed (byte* uinputPathPtr = uinputPath) fd = c.open(uinputPathPtr, c.O_WRONLY | c.O_NONBLOCK);
        if (fd < 0)
        {
            Log.WriteLine("Could not open uinput");
            return;
        }
        
        // Setup the uinput device
        c.ioctl(fd, uinput.UI_SET_EVBIT, uinput.EV_KEY);
        c.ioctl(fd, uinput.UI_SET_KEYBIT, uinput.BTN_A);
        c.ioctl(fd, uinput.UI_SET_KEYBIT, uinput.BTN_MODE);
    }

    public static void Dispose()
    {
        
    }
}

/* evtest info for real Xbox-One gamepad
Input driver version is 1.0.1
   Input device ID: bus 0x3 vendor 0x45e product 0x2ea version 0x301
   Input device name: "Microsoft X-Box One S pad"
   Supported events:
     Event type 0 (EV_SYN)
     Event type 1 (EV_KEY)
       Event code 304 (BTN_SOUTH)
       Event code 305 (BTN_EAST)
       Event code 307 (BTN_NORTH)
       Event code 308 (BTN_WEST)
       Event code 310 (BTN_TL)
       Event code 311 (BTN_TR)
       Event code 314 (BTN_SELECT)
       Event code 315 (BTN_START)
       Event code 316 (BTN_MODE)
       Event code 317 (BTN_THUMBL)
       Event code 318 (BTN_THUMBR)
     Event type 3 (EV_ABS)
       Event code 0 (ABS_X)
         Value   3132
         Min   -32768
         Max    32767
         Fuzz      16
         Flat     128
       Event code 1 (ABS_Y)
         Value    116
         Min   -32768
         Max    32767
         Fuzz      16
         Flat     128
       Event code 2 (ABS_Z)
         Value      0
         Min        0
         Max     1023
       Event code 3 (ABS_RX)
         Value   2812
         Min   -32768
         Max    32767
         Fuzz      16
         Flat     128
       Event code 4 (ABS_RY)
         Value  -1119
         Min   -32768
         Max    32767
         Fuzz      16
         Flat     128
       Event code 5 (ABS_RZ)
         Value      0
         Min        0
         Max     1023
       Event code 16 (ABS_HAT0X)
         Value      0
         Min       -1
         Max        1
       Event code 17 (ABS_HAT0Y)
         Value      0
         Min       -1
         Max        1
     Event type 21 (EV_FF)
       Event code 80 (FF_RUMBLE)
       Event code 81 (FF_PERIODIC)
       Event code 88 (FF_SQUARE)
       Event code 89 (FF_TRIANGLE)
       Event code 90 (FF_SINE)
       Event code 96 (FF_GAIN)
*/
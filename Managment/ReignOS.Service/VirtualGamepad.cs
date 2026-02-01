namespace ReignOS.Service;
using ReignOS.Service.OS;
using ReignOS.Core;

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

public unsafe static class VirtualGamepad
{
    private static int handle;
    private static bool UI_DEV_created;
    private static input.input_event e;
    private static object locker = new object();

    public static void Init()
    {
        // open uinput
        byte[] uinputPath = Encoding.UTF8.GetBytes("/dev/uinput");
        fixed (byte* uinputPathPtr = uinputPath) handle = c.open(uinputPathPtr, c.O_WRONLY | c.O_NONBLOCK);
        if (handle < 0)
        {
            Log.WriteLine("Could not open uinput");
            return;
        }
        
        // setup device IDs (matches Xbox-One-S controller [needed by Steam])
        input.uinput_user_dev uidev;
        byte[] name = Encoding.ASCII.GetBytes("ReignOS Virtual Gamepad");
        fixed (byte* namePtr = name) Buffer.MemoryCopy(namePtr, uidev.name, name.Length, name.Length);
        uidev.id.bustype = input.BUS_USB;
        uidev.id.vendor  = 0x45e;
        uidev.id.product = 0x2ea;
        uidev.id.version = 0x301;
        
        // setup device features
        c.ioctl(handle, input.UI_SET_EVBIT, input.EV_KEY);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_MODE);
        
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_A);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_B);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_X);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_Y);
        
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_START);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_SELECT);
        
        c.write(handle, &uidev, (UIntPtr)Marshal.SizeOf<input.uinput_user_dev>());
        if (c.ioctl(handle, input.UI_DEV_CREATE) < 0)
        {
            Log.WriteLine("Error creating uinput device");
            c.close(handle);
            handle = 0;
            return;
        }
        UI_DEV_created = true;
    }

    public static void Dispose()
    {
        if (handle != 0)
        {
            if (UI_DEV_created) c.ioctl(handle, input.UI_DEV_DESTROY);
            c.close(handle);
            handle = 0;
        }
    }
    
    public static void StartWrites()
    {
        var e = new input.input_event();
        c.gettimeofday(&e.time, null);
        VirtualGamepad.e = e;
    }
    
    public static void WriteButton(int button, bool pressed)
    {
        var e = VirtualGamepad.e;
        e.type = input.EV_KEY;
        e.code = (ushort)button;
        e.value = pressed ? 1 : 0;
        c.write(handle, &e, (UIntPtr)Marshal.SizeOf<input.input_event>());
    }
    
    public static void EndWrites()
    {
        var e = VirtualGamepad.e;
        e.type = input.EV_SYN;
        e.code = input.SYN_REPORT;
        c.write(handle, &e, (UIntPtr)Marshal.SizeOf<input.input_event>());
    }

    public static void Write_TriggerLeftSteamMenu()
    {
        lock (locker)
        {
            // press
            StartWrites();
            WriteButton(input.BTN_MODE, true);
            EndWrites();

            // release
            Thread.Sleep(100);
            StartWrites();
            WriteButton(input.BTN_MODE, false);
            EndWrites();
        }
    }

    public static void Write_TriggerRightSteamMenu()
    {
        lock (locker)
        {
            // hold guide
            StartWrites();
            WriteButton(input.BTN_MODE, true);
            EndWrites();

            // tap A
            Thread.Sleep(100);
            StartWrites();
            WriteButton(input.BTN_A, true);
            EndWrites();

            Thread.Sleep(100);
            StartWrites();
            WriteButton(input.BTN_A, false);
            EndWrites();

            // release guide
            StartWrites();
            WriteButton(input.BTN_MODE, false);
            EndWrites();
        }
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
namespace ReignOS.Service;
using System;
using System.Runtime.InteropServices;

using __u16 = UInt16;
using __u32 = UInt32;
using __s16 = Int16;
using __s32 = Int32;

public unsafe static class VirtualGamepad
{
    private const string lib = "libuinput.so";
    
    [DllImport(lib)]
    private static extern int uinput_open();

    public const int UINPUT_MAX_NAME_SIZE = 80;
    public const int ABS_MAX = 0x3f;
    public const int ABS_CNT = (ABS_MAX+1);
    
    [StructLayout(LayoutKind.Sequential)]
    struct input_id
    {
        __u16 bustype;
        __u16 vendor;
        __u16 product;
        __u16 version;
    };

    [StructLayout(LayoutKind.Sequential)]
    struct uinput_user_dev
    {
        public fixed byte name[UINPUT_MAX_NAME_SIZE];
        public input_id id;
        public __u32 ff_effects_max;
        public fixed __s32 absmax[ABS_CNT];
        public fixed __s32 absmin[ABS_CNT];
        public fixed __s32 absfuzz[ABS_CNT];
        public fixed __s32 absflat[ABS_CNT];
    }
    
    public static void Init()
    {
        
    }
}

/*
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
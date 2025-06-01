namespace ReignOS.Service.OS;

using System;
using System.Runtime.InteropServices;

using __u16 = System.UInt16;
using __u32 = System.UInt32;
using __s16 = System.Int16;
using __s32 = System.Int32;

public unsafe static class input
{
    public const int UINPUT_MAX_NAME_SIZE = 80;
    public const uint EVIOCGID = 0x80084502;
    
    public const int BUS_USB = 0x03;
    public const int BUS_HIL = 0x04;
    public const int BUS_BLUETOOTH = 0x05;
    public const int BUS_VIRTUAL = 0x06;
    
    public const int UI_DEV_CREATE = 0x5501;
    public const int UI_SET_EVBIT = 0x40045564;
    public const int UI_DEV_DESTROY = 0x5502;
    public const int UI_SET_KEYBIT = 0x40045565;
    
    public const int EV_SYN = 0x00;
    public const int SYN_REPORT = 0;
    
    public const int EV_MAX = 0x1f;
    public const int EV_KEY = 0x1;
    
    public const int KEY_MAX = 0x2ff;
    public const int KEY_0 = 11;
    public const int KEY_A = 30;
    public const int KEY_D = 32;
    public const int KEY_Z = 44;
    public const int KEY_VOLUMEDOWN = 114;
    public const int KEY_VOLUMEUP = 115;
    public const int KEY_F15 = 185;
    public const int KEY_F16 = 186;
    public const int KEY_RIGHTCTRL = 97;
    public const int KEY_LEFTMETA = 125;

    public const int BTN_MODE = 0x13c;
    public const int BTN_A = 0x130;
    public const int BTN_B = 0x131;
    public const int BTN_X = 0x133;
    public const int BTN_Y = 0x134;
    public const int BTN_START = 0x13b;
    public const int BTN_SELECT = 0x13a;
    public const int BTN_THUMBL = 0x13d;
    public const int BTN_THUMBR = 0x13e;
    public const int BTN_TL = 0x136;
    public const int BTN_TR = 0x137;
    
    public const int ABS_MAX = 0x3f;
    public const int ABS_CNT = (ABS_MAX+1);
    
    [StructLayout(LayoutKind.Sequential)]
    public struct input_id
    {
        public __u16 bustype;
        public __u16 vendor;
        public __u16 product;
        public __u16 version;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct uinput_user_dev
    {
        public fixed byte name[UINPUT_MAX_NAME_SIZE];
        public input_id id;
        public __u32 ff_effects_max;
        public fixed __s32 absmax[ABS_CNT];
        public fixed __s32 absmin[ABS_CNT];
        public fixed __s32 absfuzz[ABS_CNT];
        public fixed __s32 absflat[ABS_CNT];
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct input_event
    {
        public c.timeval time;
        //#define input_event_sec time.tv_sec
        //#define input_event_usec time.tv_usec
        public __u16 type;
        public __u16 code;
        public __s32 value;
    };
}
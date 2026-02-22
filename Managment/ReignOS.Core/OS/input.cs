namespace ReignOS.Core.OS;

using System;
using System.Runtime.InteropServices;

using __u8 = System.Byte;
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
    public const int UI_SET_ABSBIT = 1074025831;
    public const int UI_ABS_SETUP = 1075598596;
    public const int UI_SET_FFBIT = 1074025835;
    
    public const int EV_SYN = 0x00;
    public const int EV_KEY = 0x01;
    public const int EV_REL = 0x02;
    public const int EV_ABS = 0x03;
    public const int EV_FF = 0x15;
    public const int SYN_REPORT = 0;
    
    public const int EV_MAX = 0x1f;
    
    public const int KEY_MAX = 0x2ff;
    public const int KEY_0 = 11;
    public const int KEY_A = 30;
    public const int KEY_D = 32;
    public const int KEY_G = 34;
    public const int KEY_T = 20;
    public const int KEY_O = 24;
    public const int KEY_Z = 44;
    public const int KEY_VOLUMEDOWN = 114;
    public const int KEY_VOLUMEUP = 115;
    public const int KEY_POWER = 116;
    public const int KEY_F12 = 88;
    public const int KEY_F15 = 185;
    public const int KEY_F16 = 186;
    public const int KEY_F17 = 187;
    public const int KEY_F18 = 188;
    public const int KEY_F23 = 193;
    public const int KEY_F24 = 194;
    public const int KEY_LEFTCTRL = 29;
    public const int KEY_RIGHTCTRL = 97;
    public const int KEY_LEFTMETA = 125;
    public const int KEY_LEFTALT = 56;
    public const int KEY_RIGHTALT = 100;
    public const int KEY_LEFTSHIFT = 42;
    public const int KEY_PROG1 = 148;
    public const int KEY_PROG2 = 149;
    public const int KEY_DELETE = 111;

    public const int BTN_SOUTH = 0x130;
    public const int BTN_EAST = 0x131;
    public const int BTN_NORTH = 0x133;
    public const int BTN_WEST = 0x134;
    public const int BTN_START = 0x13b;
    public const int BTN_SELECT = 0x13a;
    public const int BTN_MODE = 0x13c;
    public const int BTN_THUMBL = 0x13d;
    public const int BTN_THUMBR = 0x13e;
    public const int BTN_TL = 0x136;
    public const int BTN_TR = 0x137;
    public const int BTN_TRIGGER_HAPPY5 = 0x2c4;
    public const int BTN_TRIGGER_HAPPY6 = 0x2c5;
    public const int BTN_TRIGGER_HAPPY7 = 0x2c6;
    public const int BTN_TRIGGER_HAPPY8 = 0x2c7;
    
    public const int ABS_X = 0x00;
    public const int ABS_Y = 0x01;
    public const int ABS_Z = 0x02;
    public const int ABS_RX = 0x03;
    public const int ABS_RY = 0x04;
    public const int ABS_RZ = 0x05;
    public const int ABS_HAT0X = 0x10;
    public const int ABS_HAT0Y = 0x11;
    
    public const int FF_RUMBLE = 0x50;
    public const int FF_PERIODIC = 0x51;
    public const int FF_SQUARE = 0x58;
    public const int FF_TRIANGLE = 0x59;
    public const int FF_SINE = 0x5a;
    public const int FF_GAIN = 0x60;
    
    public const int ABS_MAX = 0x3f;
    public const int ABS_CNT = (ABS_MAX+1);

    public const int EV_UINPUT = 0x0101;
    public const int UI_FF_UPLOAD = 1;
    public const int UI_FF_ERASE = 2;

    public const uint UI_BEGIN_FF_UPLOAD = 3228063176;
    public const uint UI_END_FF_UPLOAD = 1080579529;
    public const uint UI_BEGIN_FF_ERASE = 3222033866;
    public const uint UI_END_FF_ERASE = 1074550219;
    
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
    
    [StructLayout(LayoutKind.Sequential)]
    public struct input_absinfo
    {
        public __s32 value;
        public __s32 minimum;
        public __s32 maximum;
        public __s32 fuzz;
        public __s32 flat;
        public __s32 resolution;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct uinput_abs_setup
    {
        public __u16 code; /* axis code */
        /* __u16 filler; */
        public input_absinfo absinfo;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ff_replay
    {
        public __u16 length;
        public __u16 delay;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ff_trigger
    {
        public __u16 button;
        public __u16 interval;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ff_envelope
    {
        public __u16 attack_length;
        public __u16 attack_level;
        public __u16 fade_length;
        public __u16 fade_level;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ff_constant_effect
    {
        public __s16 level;
        public ff_envelope envelope;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ff_ramp_effect
    {
        public __s16 start_level;
        public __s16 end_level;
        public ff_envelope envelope;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ff_periodic_effect
    {
        public __u16 waveform;
        public __u16 period;
        public __s16 magnitude;
        public __s16 offset;
        public __u16 phase;
        public ff_envelope envelope;
        public __u32 custom_len;
        public __s16 *custom_data;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ff_condition_effect
    {
        public __u16 right_saturation;
        public __u16 left_saturation;

        public __s16 right_coeff;
        public __s16 left_coeff;

        public __u16 deadband;
        public __s16 center;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ff_rumble_effect
    {
        public __u16 strong_magnitude;
        public __u16 weak_magnitude;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ff_haptic_effect
    {
        public __u16 hid_usage;
        public __u16 vendor_id;
        public __u8  vendor_waveform_page;
        public __u16 intensity;
        public __u16 repeat_count;
        public __u16 retrigger_period;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ff_effect
    {
        public __u16 type;
        public __s16 id;
        public __u16 direction;
        public ff_trigger trigger;
        public ff_replay replay;

        [StructLayout(LayoutKind.Explicit)]
        public struct U
        {
            [FieldOffset(0)] public ff_constant_effect constant;
            [FieldOffset(0)] public ff_ramp_effect ramp;
            [FieldOffset(0)] public ff_periodic_effect periodic;
            [FieldOffset(0)] public ff_condition_effect conditionX; // One for each axis
            [FieldOffset(1)] public ff_condition_effect conditionY; // One for each axis
            [FieldOffset(0)] public ff_rumble_effect rumble;
            [FieldOffset(0)] public ff_haptic_effect haptic;
        }

        public U u;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct uinput_ff_upload
    {
        public __u32 request_id;
        public __s32 retval;
        public ff_effect effect;
        public ff_effect old;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct uinput_ff_erase
    {
        public __u32 request_id;
        public __s32 retval;
        public __u32 effect_id;
    }
}
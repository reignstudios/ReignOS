using System.Runtime.InteropServices;

using __u8 = System.Byte;
using __u16 = System.UInt16;
using __u32 = System.UInt32;
using __s16 = System.Int16;
using __s32 = System.Int32;

namespace ReignOS.Core.OS;

public unsafe static class joystick
{
    public const int JS_EVENT_BUTTON = 0x01;
    public const int JS_EVENT_AXIS = 0x02;
    
    [StructLayout(LayoutKind.Sequential)]
    public struct js_event
    {
        __u32 time;	/* event timestamp in milliseconds */
        __s16 value;	/* value */
        __u8 type;	/* event type */
        __u8 number;	/* axis/button number */
    }
}
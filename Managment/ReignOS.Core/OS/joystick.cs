using System.Runtime.InteropServices;

using __u8 = System.Byte;
using __u16 = System.UInt16;
using __u32 = System.UInt32;
using __s16 = System.Int16;
using __s32 = System.Int32;

namespace ReignOS.Core.OS;

public unsafe static class joystick
{
    public const uint JSIOCGBUTTONS = 2147576338;
    public const uint JSIOCGAXES = 2147576337;
    
    public const int JS_EVENT_BUTTON = 0x01;
    public const int JS_EVENT_AXIS = 0x02;
    
    [StructLayout(LayoutKind.Sequential)]
    public struct js_event
    {
        public __u32 time;	/* event timestamp in milliseconds */
        public __s16 value;	/* value */
        public __u8 type;	/* event type */
        public __u8 number;	/* axis/button number */
    }
}
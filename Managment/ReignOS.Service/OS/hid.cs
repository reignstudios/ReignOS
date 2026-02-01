namespace ReignOS.Service.OS;
using System.Runtime.InteropServices;

using __u8 = System.Byte;
using __u16 = System.UInt16;
using __u32 = System.UInt32;
using __s16 = System.Int16;
using __s32 = System.Int32;

public unsafe static class hid
{
    public const uint HIDIOCGRAWINFO = 0x80084803;
    public const uint HIDIOCGRDESCSIZE = 0x80044801;
    public const uint HIDIOCGRDESC = 0x90044802;

    public const uint USBDEVFS_RESET = 21780;
    
    public const uint HIDIOCGRAWNAME_256 = 0x81004804;// HIDIOCGRAWNAME(256)
    public const uint HIDIOCGRAWPHYS_256 = 0x81004805;// HIDIOCGRAWPHYS(256)
    
    public const int HID_MAX_DESCRIPTOR_SIZE = 4096;
    
    [StructLayout(LayoutKind.Sequential)]
    public struct hidraw_report_descriptor
    {
        public __u32 size;
        public fixed __u8 value[HID_MAX_DESCRIPTOR_SIZE];
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct hidraw_devinfo
    {
        public __u32 bustype;
        public __u16 vendor;// __s16: force to ushort
        public __u16 product;// __s16: force to ushort
    };
}
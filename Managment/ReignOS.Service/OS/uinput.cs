namespace ReignOS.Service.OS;

using System;
using System.Runtime.InteropServices;

public unsafe static class uinput
{
    public const string lib = "libuinput.so";

    public const int O_WRONLY = 01;
    public const int O_NONBLOCK = 04000;
    
    public const byte UINPUT_IOCTL_BASE = (byte)'U';
    public const uint _IOC_NONE = 0U;
    public const uint _IOC_WRITE = 1U;
    public const uint _IOC_READ = 2U;

    private static uint _IOW<T>(byte type, uint nr)//, int size)
    {
        
    }

    private static uint _IOC<T>(uint dir, byte type, uint nr, UIntPtr size)
    {
        return (((dir) << _IOC_DIRSHIFT) |
         ((type) << _IOC_TYPESHIFT) |
         ((nr) << _IOC_NRSHIFT) |
         ((size) << _IOC_SIZESHIFT));
    }

    private static UIntPtr _IOC_TYPECHECK<T>()
    {
        return (UIntPtr)Marshal.SizeOf<T>();
    }
    
    [DllImport(lib)]
    public static extern int open(char *__file, int __oflag);
    
    [DllImport(lib)]
    public static extern int ioctl(int __fd, UIntPtr __request);
}
namespace ReignOS.Service.OS;

using System;
using System.Runtime.InteropServices;

using __time_t = System.IntPtr;
using __suseconds_t = System.IntPtr;
using ssize_t = System.IntPtr;
using size_t = System.UIntPtr;

public unsafe static class c
{
    public const string lib = "libc.so";

    public const int O_RDONLY = 00;
    public const int O_WRONLY = 01;
    public const int O_RDWR = 02;
    public const int O_NONBLOCK = 04000;
    
    [StructLayout(LayoutKind.Sequential)]
    public struct timeval
    {
        public __time_t tv_sec;		/* Seconds.  */
        public __suseconds_t tv_usec;	/* Microseconds.  */
    };
    
    [DllImport(lib)]
    public static extern int open(byte *__file, int __oflag);
    
    [DllImport(lib)]
    public static extern int close(int __fd);
    
    [DllImport("ReignOS.Service.Native.so", EntryPoint = "ioctl_var_arg0")]
    public static extern int ioctl(int __fd, UIntPtr __request);
    
    [DllImport("ReignOS.Service.Native.so", EntryPoint = "ioctl_var_arg1_int")]
    public static extern int ioctl(int __fd, UIntPtr __request, int var_args);
    
    [DllImport("ReignOS.Service.Native.so", EntryPoint = "ioctl_var_arg1_void")]
    public static extern int ioctl(int __fd, UIntPtr __request, void* var_args);
    
    [DllImport(lib)]
    public static extern int gettimeofday(timeval* __tv, void* __tz);
    
    [DllImport(lib)]
    public static extern ssize_t write(int __fd, void* __buf, size_t __n);
    
    [DllImport(lib)]
    public static extern ssize_t read(int __fd, void *__buf, size_t __nbytes);
}
using System;
using System.Runtime.InteropServices;

namespace ReignOS.Service;
using ReignOS.Service.OS;
using ReignOS.Core;
using System.Text;

public unsafe class KeyboardInput : IDisposable
{
    private int handle;
    
    public void Init(string name, bool useName, ushort vendorID, ushort productID)
    {
        const int bufferSize = 256;
        byte* buffer = stackalloc byte[bufferSize];

        // scan devices
        for (int i = 0; i != 32; ++i)
        {
            // open keyboard
            string path = "/dev/input/event" + i.ToString();
            byte[] pathEncoded = Encoding.UTF8.GetBytes(path);
            fixed (byte* uinputPathPtr = pathEncoded) handle = c.open(uinputPathPtr, c.O_RDONLY);
            if (handle < 0) continue;
            
            // validate hardware
            if (useName)
            {
                string infoPath = $"/sys/class/input/event{i}/device/name";
                byte[] infoPathEncoded = Encoding.UTF8.GetBytes(infoPath);
                int infoHandle;
                fixed (byte* infoPathEncodedPtr = infoPathEncoded) infoHandle = c.open(infoPathEncodedPtr, c.O_RDONLY);
                if (infoHandle < 0) goto CONTINUE;

                NativeUtils.ZeroMemory(buffer, bufferSize);
                if (c.read(infoHandle, buffer, bufferSize - 1) < 0) goto CONTINUE;
                string deviceName = Marshal.PtrToStringAnsi((IntPtr)buffer);
                if (deviceName == name)
                {
                    Log.WriteLine($"Keyboard event device found name:'{name}' path:{path}");
                    break;
                }
            }
            else
            {
                input.input_id id;
                if (c.ioctl(handle, input.EVIOCGID, &id) < 0) goto CONTINUE;
                if (id.vendor == vendorID && id.product == productID)
                {
                    Log.WriteLine($"Keyboard event device found vendorID:{vendorID} productID:{productID} path:{path}");
                    break;
                }
            }
            
            // close
            CONTINUE: c.close(handle);
        }
    }
    
    public void Dispose()
    {
        if (handle != 0)
        {
            c.close(handle);
            handle = 0;
        }
    }
    
    public bool ReadNextKey(out ushort key, out bool pressed)
    {
        var e = new input.input_event();
        if (c.read(handle, &e, (UIntPtr)Marshal.SizeOf<input.input_event>()) < 0)
        {
            key = 0;
            pressed = false;
            return false;
        }
        
        if (e.type == input.EV_KEY)
        {
            key = e.code;
            pressed = e.value == 1;
            return true;
        }
        
        key = 0;
        pressed = false;
        return false;
    }
}
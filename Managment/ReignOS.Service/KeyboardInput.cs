using System;
using System.Runtime.InteropServices;

namespace ReignOS.Service;
using ReignOS.Service.OS;
using ReignOS.Core;
using System.Text;

public unsafe class KeyboardInput : IDisposable
{
    private int handle;
    
    public void Init(ushort vendorID, ushort productID)
    {
        for (int i = 0; i != 32; ++i)
        {
            // open keyboard
            input.uinput_user_dev uidev;
            string path = "/dev/input/event" + i.ToString();
            byte[] uinputPath = Encoding.UTF8.GetBytes(path);
            fixed (byte* uinputPathPtr = uinputPath) handle = c.open(uinputPathPtr, c.O_RDONLY);
            if (handle < 0) continue;
            
            // validate hardware
            input.input_id id;
            if (c.ioctl(handle, input.EVIOCGID, &id) != 0) continue;
            if (id.vendor == vendorID && id.product == productID)
            {
                Log.WriteLine($"Keyboard event device found vendorID:{vendorID} productID:{productID} path:{path}");
                break;
            }
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
        if (c.read(handle, &e, (UIntPtr)Marshal.SizeOf<input.input_event>()) != 0)
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
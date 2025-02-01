using System;
using System.Runtime.InteropServices;

namespace ReignOS.Service;
using ReignOS.Service.OS;
using ReignOS.Core;
using System.Text;

public unsafe class KeyboardInput : IDisposable
{
    private int handle = -1;
    
    public void Init(string name, bool useName, ushort vendorID, ushort productID)
    {
        Log.WriteLine("Searching for media keyboard...");
        
        handle = -1;
        const int bufferSize = 256;
        byte* buffer = stackalloc byte[bufferSize];

        const int BITS_PER_LONG = sizeof(long) * 8;
        const int evbitmaskSize = (input.EV_MAX + BITS_PER_LONG - 1) / BITS_PER_LONG;
        var evbitmask = stackalloc UIntPtr[evbitmaskSize];
        const int EVIOCGBIT_0_evbitmaskSize_ = -2147400416;
        
        const int keybitmaskSize = (input.KEY_MAX + BITS_PER_LONG - 1) / BITS_PER_LONG;
        var keybitmask = stackalloc UIntPtr[keybitmaskSize];
        const int EVIOCGBIT_EV_KEY_keybitmaskSize_ = -2146679519;
        
        // scan devices
        for (int i = 0; i != 32; ++i)
        {
            // open keyboard
            string path = "/dev/input/event" + i.ToString();
            byte[] pathEncoded = Encoding.UTF8.GetBytes(path);
            fixed (byte* uinputPathPtr = pathEncoded) handle = c.open(uinputPathPtr, c.O_RDONLY | c.O_NONBLOCK);
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
                if (c.read(infoHandle, buffer, bufferSize - 1) < 0)
                {
                    c.close(infoHandle);
                    goto CONTINUE;
                }
                
                string deviceName = Marshal.PtrToStringAnsi((IntPtr)buffer).TrimEnd();
                c.close(infoHandle);
                if (deviceName == name)
                {
                    Log.WriteLine($"Keyboard event device found name:'{name}' path:{path}");
                    break;
                }
            }
            else
            {
                if (vendorID == 0 && productID == 0)
                {
                    static UIntPtr TestBit(int bit, UIntPtr* array) => ((array[bit / BITS_PER_LONG] >> (bit % BITS_PER_LONG)) & 1);
                    Log.WriteLine("KeyboardPath test1: " + path);
                    NativeUtils.ZeroMemory(evbitmask, evbitmaskSize);
                    if (c.ioctl(handle, unchecked((UIntPtr)EVIOCGBIT_0_evbitmaskSize_), evbitmask) < 0) goto CONTINUE;
                    Log.WriteLine("KeyboardPath test2: " + path);
                    if (TestBit(input.EV_KEY, evbitmask) != UIntPtr.Zero)
                    {
                        Log.WriteLine("KeyboardPath test3: " + path);
                        NativeUtils.ZeroMemory(keybitmask, keybitmaskSize);
                        if (c.ioctl(handle, unchecked((UIntPtr)EVIOCGBIT_EV_KEY_keybitmaskSize_), keybitmask) < 0) goto CONTINUE;
                        Log.WriteLine("KeyboardPath test4: " + path);
                        if (TestBit(input.KEY_VOLUMEDOWN, keybitmask) != 0 || TestBit(input.KEY_VOLUMEUP, keybitmask) != 0)
                        {
                            Log.WriteLine($"Media Keyboard device found path:{path}");
                            break;
                        }
                        /*else if (TestBit(input.KEY_A, keybitmask) != 0 && TestBit(input.KEY_Z, keybitmask) != 0)
                        {
                            Log.WriteLine($"Normal Keyboard device found path:{path}");
                            break;
                        }*/
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
            }
            
            // close
            CONTINUE: c.close(handle);
        }
    }
    
    public void Dispose()
    {
        if (handle >= 0)
        {
            c.close(handle);
            handle = -1;
        }
    }
    
    public bool ReadNextKey(out ushort key, out bool pressed)
    {
        if (handle < 0)
        {
            key = 0;
            pressed = false;
            return false;
        }
        
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
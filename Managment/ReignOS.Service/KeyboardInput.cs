namespace ReignOS.Service;
using ReignOS.Service.OS;
using ReignOS.Core;
using System.Text;

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public unsafe class KeyboardInput : IDisposable
{
    private List<int> handles;
    
    public void Init(string name, bool useName, ushort vendorID, ushort productID)
    {
        Log.WriteLine("Searching for media keyboard...");
        
        handles = new List<int>();
        const int bufferSize = 256;
        byte* buffer = stackalloc byte[bufferSize];

        int NBITS(int x) => (x + 7) / 8;
        
        int ev_bitsSize = NBITS(input.EV_MAX);
        var ev_bits = stackalloc byte[ev_bitsSize];
        const nint EVIOCGBIT_EV_MAX_ev_bitsSize_ = -2147203808;
        
        int key_bitsSize = NBITS(input.KEY_MAX);
        var key_bits = stackalloc byte[key_bitsSize];
        const nint EVIOCGBIT_EV_KEY_key_bitsSize_ = -2141174495;
        
        // scan devices
        for (int i = 0; i != 32; ++i)
        {
            // open keyboard
            string path = "/dev/input/event" + i.ToString();
            byte[] pathEncoded = Encoding.UTF8.GetBytes(path);
            int handle;
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
                    handles.Add(handle);
                    break;
                }
            }
            else
            {
                if (vendorID == 0 && productID == 0)
                {
                    int TestBit(int bit, byte* array) => array[bit / 8] & (1 << (bit % 8));
                    
                    NativeUtils.ZeroMemory(ev_bits, ev_bitsSize);
                    if (c.ioctl(handle, unchecked((UIntPtr)EVIOCGBIT_EV_MAX_ev_bitsSize_), ev_bits) < 0) goto CONTINUE;
                    
                    if (TestBit(input.EV_KEY, ev_bits) != 0)
                    {
                        NativeUtils.ZeroMemory(key_bits, key_bitsSize);
                        if (c.ioctl(handle, unchecked((UIntPtr)EVIOCGBIT_EV_KEY_key_bitsSize_), key_bits) < 0) goto CONTINUE;
                        
                        if (TestBit(input.KEY_VOLUMEDOWN, key_bits) != 0 && TestBit(input.KEY_VOLUMEUP, key_bits) != 0)
                        {
                            Log.WriteLine($"Media Keyboard device found path:{path}");
                            handles.Add(handle);
                        }
                        else if (TestBit(input.KEY_A, key_bits) != 0 && TestBit(input.KEY_Z, key_bits) != 0)
                        {
                            Log.WriteLine($"Keyboard device found path:{path}");
                            handles.Add(handle);
                        }
                    }
                }
                else
                {
                    input.input_id id;
                    if (c.ioctl(handle, input.EVIOCGID, &id) < 0) goto CONTINUE;
                    if (id.vendor == vendorID && id.product == productID)
                    {
                        Log.WriteLine($"Keyboard event device found vendorID:{vendorID} productID:{productID} path:{path}");
                        handles.Add(handle);
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
        if (handles != null)
        {
            foreach (var handle in handles) c.close(handle);
            handles = null;
        }
    }
    
    public bool ReadNextKey(out ushort key, out bool pressed)
    {
        key = 0;
        pressed = false;
        if (handles == null || handles.Count == 0) return false;
        
        bool success = false;
        foreach (var handle in handles)
        {
            var e = new input.input_event();
            if (c.read(handle, &e, (UIntPtr)Marshal.SizeOf<input.input_event>()) >= 0)
            {
                if (e.type == input.EV_KEY)
                {
                    Log.WriteLine("EV_KEY: " + e.type);
                    key = e.code;
                    pressed = e.value == 1;
                    success = true;
                }
            }
        }
        
        return success;
    }
}
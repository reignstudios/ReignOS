namespace ReignOS.Service;
using ReignOS.Core.OS;
using ReignOS.Core;
using System.Text;

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public struct KeyEvent
{
    public ushort key;
    public bool pressed, held;

    public KeyEvent(ushort key, bool pressed, bool held = false)
    {
        this.key = key;
        this.pressed = pressed;
        this.held = held;
    }

    public static bool Pressed(KeyList keyEvents, bool includeHeld = false)
    {
        if (!keyEvents.ready) return false;
        for (int i = 0; i < keyEvents.count; ++i)
        {
            ref var key = ref keyEvents.keys[i];
            if (key.pressed || (includeHeld && key.held)) return true;
        }
        return false;
    }

    public static bool Pressed(KeyList keyEvents, ushort key, bool includeHeld = false)
    {
        if (!keyEvents.ready) return false;
        for (int i = 0; i < keyEvents.count; ++i)
        {
            ref var e = ref keyEvents.keys[i];
            if ((e.pressed || (includeHeld && e.held)) && e.key == key) return true;
        }
        return false;
    }

    public static bool Pressed(KeyList keyEvents, params KeyEvent[] keys)
    {
        if (!keyEvents.ready) return false;
        if (keyEvents.count != 0 && keyEvents.count >= keys.Length)
        {
            for (int i = 0; i != keys.Length; i++)
            {
                var k1 = keyEvents.keys[i];
                var k2 = keys[i];
                if (k1.key != k2.key || k1.pressed != k2.pressed)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }
}

public class KeyList
{
    public KeyEvent[] keys;
    public int count;
    public bool ready;

    public KeyList(int maxCount)
    {
        keys = new KeyEvent[maxCount];
    }

    public void Clear()
    {
        count = 0;
        ready = false;
    }

    public void Add(KeyEvent key)
    {
        if (count >= keys.Length) return;
        keys[count] = key;
        count++;
    }
}

public unsafe class KeyboardInput : IDisposable
{
    private List<int> handles;

    private KeyList keyList = new KeyList(32);
    private int keyListWaitCount;

    public void Init(string name, bool useName, ushort vendorID, ushort productID)
    {
        Log.WriteLine("Searching for media keyboard...");
        
        handles = new List<int>();
        const int bufferSize = 256;
        byte* buffer = stackalloc byte[bufferSize];

        static int NBITS(int x) => (x + 7) / 8;
        
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
                        
                        if (TestBit(input.KEY_VOLUMEDOWN, key_bits) != 0 || TestBit(input.KEY_VOLUMEUP, key_bits) != 0)
                        {
                            Log.WriteLine($"Media Keyboard device found path:{path}");
                            handles.Add(handle);
                            continue;
                        }
                        else if (TestBit(input.KEY_A, key_bits) != 0 || TestBit(input.KEY_Z, key_bits) != 0)
                        {
                            Log.WriteLine($"Keyboard device found path:{path}");
                            handles.Add(handle);
                            continue;
                        }
                        else if (TestBit(input.KEY_POWER, key_bits) != 0)
                        {
                            Log.WriteLine($"PowerButton device found path:{path}");
                            handles.Add(handle);
                            continue;
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
    
    public bool ReadNextKey(out KeyEvent key)
    {
        key = new KeyEvent();
        if (handles == null || handles.Count == 0) return false;
        
        bool hasEvent = false;
        foreach (var handle in handles)
        {
            var e = new input.input_event();
            if (c.read(handle, &e, (UIntPtr)Marshal.SizeOf<input.input_event>()) >= 0)
            {
                if (e.type == input.EV_KEY)
                {
                    key.key = e.code;
                    key.pressed = e.value == 1;
                    key.held = e.value == 2;
                    hasEvent = true;
                }
            }
        }
        
        return hasEvent;
    }

    public bool ReadNextKeys(out KeyList keys, int waitCount)
    {
        keys = keyList;
        if (handles == null || handles.Count == 0) return false;

        if (keys.ready)
        {
            keyListWaitCount = 0;
            keys.Clear();
        }

        foreach (var handle in handles)
        {
            var e = new input.input_event();
            while (c.read(handle, &e, (UIntPtr)Marshal.SizeOf<input.input_event>()) >= 0)
            {
                if (e.type == input.EV_KEY)
                {
                    keys.Add(new KeyEvent(e.code, e.value == 1, e.value == 2));
                }
            }
        }

        keyListWaitCount++;
        if (keyListWaitCount >= waitCount)
        {
            keys.ready = true;
            return keys.count != 0;
        }
        
        return false;
    }

    public void ClearKeys()
    {
        keyList.Clear();
    }
}
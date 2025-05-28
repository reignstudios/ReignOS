namespace ReignOS.Service;
using ReignOS.Service.OS;
using ReignOS.Core;
using System.Text;
using System;
using System.Collections.Generic;

public unsafe class HidDevice
{
    public List<int> handles;
    
    public bool Init(ushort vendorID, ushort productID, bool openAll)
    {
        const int bufferSize = 256;
        byte* buffer = stackalloc byte[bufferSize];
        handles = new List<int>();
        
        // scan devices
        for (int i = 0; i != 32; ++i)
        {
            // open keyboard
            string path = "/dev/hidraw" + i.ToString();
            byte[] uinputPath = Encoding.UTF8.GetBytes(path);
            int handle;
            fixed (byte* uinputPathPtr = uinputPath) handle = c.open(uinputPathPtr, c.O_RDWR | c.O_NONBLOCK);
            if (handle < 0) continue;
            
            // validate hardware
            hid.hidraw_devinfo info;
            if (c.ioctl(handle, hid.HIDIOCGRAWINFO, &info) < 0) goto CONTINUE;
            if (info.vendor == vendorID && info.product == productID)
            {
                Log.WriteLine($"HID device found type:{BusType(info.bustype)} vendorID:{vendorID} productID:{productID} path:{path}");
                handles.Add(handle);
            
                // get name
                NativeUtils.ZeroMemory(buffer, bufferSize);
                if (c.ioctl(handle, hid.HIDIOCGRAWNAME_256, buffer) < 0)
                {
                    Log.WriteLine("Failed: HIDIOCGRAWNAME");
                }
                else
                {
                    Log.WriteLine("HID Name: ", buffer);
                }

                // get physical location
                NativeUtils.ZeroMemory(buffer, bufferSize);
                if (c.ioctl(handle, hid.HIDIOCGRAWPHYS_256, buffer) < 0)
                {
                    Log.WriteLine("Failed: HIDIOCGRAWPHYS");
                }
                else
                {
                    Log.WriteLine("HID Physical Location: ", buffer);
                }

                if (!openAll) return true;
                continue;// don't close this handle
            }
            
            // close
            CONTINUE: c.close(handle);
        }

        return openAll;
    }
    
    public void Dispose()
    {
        if (handles != null)
        {
            foreach (int handle in handles)
            {
                if (handle >= 0) c.close(handle);
            }
            handles = null;
        }
    }
    
    private string BusType(uint bus)
    {
        switch (bus)
        {
            case input.BUS_USB: return "USB";
            case input.BUS_HIL: return "HIL";
            case input.BUS_BLUETOOTH: return "Bluetooth";
            case input.BUS_VIRTUAL: return "Virtual";  
            default: return "Other";
        }
    }

    public bool WriteData(byte[] data, int offset, int size)
    {
        bool success = false;
        foreach (int handle in handles)
        {
            fixed (byte* dataPtr = data)
            {
                if (c.write(handle, dataPtr + offset, (UIntPtr)size) >= 0)
                {
                    success = true;// success if any write
                }
            }
        }
        
        return success;
    }
    
    public bool ReadData(byte[] data, int offset, int size, out nint sizeRead)
    {
        foreach (int handle in handles)
        {
            fixed (byte* dataPtr = data)
            {
                sizeRead = c.read(handle, dataPtr + offset, (UIntPtr)size);
                if (sizeRead >= 0)
                {
                    return true;// success on first read
                }
            }
        }

        sizeRead = -1;
        return false;
    }
}
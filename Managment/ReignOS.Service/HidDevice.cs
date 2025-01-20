namespace ReignOS.Service;
using ReignOS.Service.OS;
using ReignOS.Core;
using System.Text;
using System;
using System.Collections.Generic;

public unsafe class HidDevice
{
    private List<int> handles;
    
    public bool Init(ushort vendorID, ushort productID, bool openAll)
    {
        const int bufferSize = 256;
        byte* buffer = stackalloc byte[bufferSize];
        handles = new List<int>();
        
        // scan devices
        for (int i = 0; i != 32; ++i)
        {
            // open keyboard
            input.uinput_user_dev uidev;
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
                break;
            }
            
            // get Report Descriptor Size
            int descSize = 0;
            if (c.ioctl(handle, hid.HIDIOCGRDESCSIZE, &descSize) < 0)
            {
                descSize = 0;
                Log.WriteLine("Failed: HIDIOCGRDESCSIZE");
            }
            else
            {
                Log.WriteLine($"Report Descriptor Size: {descSize}");
            }

            // get Report Descriptor
            if (descSize > 0)
            {
                var reportDesc = new hid.hidraw_report_descriptor();
                reportDesc.size = (uint)descSize;
                if (c.ioctl(handle, hid.HIDIOCGRDESC, &reportDesc) < 0)
                {
                    Log.WriteLine("Failed: HIDIOCGRDESC");
                }
                else
                {
                    Log.WriteLine("Report Descriptor:");
                    for (i = 0; i < reportDesc.size; i++) Log.Write(reportDesc.value[i].ToString("x"));
                }
            }
            
            // get name
            ZeroMemory(buffer, bufferSize);
            if (c.ioctl(handle, hid.HIDIOCGRAWNAME_256, buffer) < 0)
            {
                Log.WriteLine("Failed: HIDIOCGRAWNAME");
            }
            else
            {
                Log.WriteLine("HID Name: ", buffer);
            }

            // get physical location
            ZeroMemory(buffer, bufferSize);
            if (c.ioctl(handle, hid.HIDIOCGRAWPHYS_256, buffer) < 0)
            {
                Log.WriteLine("Failed: HIDIOCGRAWPHYS");
            }
            else
            {
                Log.WriteLine("HID PhysicalLocation: ", buffer);
            }
            
            // device found
            handles.Add(handle);
            if (!openAll) return true;
            
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
                if (handle != 0) c.close(handle);
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

    private static void ZeroMemory(byte* buffer, int size)
    {
        for (int i = 0; i < size; ++i) buffer[i] = 0;
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
    
    public bool ReadData(byte[] data, int offset, int size)
    {
        foreach (int handle in handles)
        {
            fixed (byte* dataPtr = data)
            {
                if (c.read(handle, dataPtr + offset, (UIntPtr)size) >= 0)
                {
                    return true;// success on first read
                }
            }
        }
        
        return false;
    }
}
using ReignOS.Core.OS;
using ReignOS.Core;
using System.Text;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ReignOS.Core;

// HID devices on Hub: sudo udevadm info -a -n /dev/hidraw1
// Watch Events: sudo hid-recorder /dev/hidrawX

public enum HidDeviceOpenMode
{
    ReadOnly = c.O_RDONLY,
    WriteOnly = c.O_WRONLY,
    ReadWrite = c.O_RDWR
}

public unsafe class HidDevice : IDisposable
{
    public List<int> handles;
    private byte[] readBuffer;
    
    public bool Init
    (
        ushort vendorID, ushort productID, bool openAll, HidDeviceOpenMode mode, bool forceOpenAllEndpoints = false,
        string name = null, bool nameIsContains = false,
        string physicalLocation = null, bool physicalLocationIsContains = false,
        bool blocking = false, bool resetDevice = false,
        bool debugLog = false
    )
    {
        const int bufferSize = 256;
        byte* buffer = stackalloc byte[bufferSize];
        handles = new List<int>();
        
        // scan devices
        for (int i = 0; i != 32; ++i)
        {
            // open keyboard
            string path = $"/dev/hidraw{i}";
            //string path = $"/dev/input/event{i}";
            if (debugLog) Log.WriteLine($"HID: Path '{path}'");
            byte[] uinputPath = Encoding.UTF8.GetBytes(path);
            int handle;
            int blockFlag = blocking ? 0 : c.O_NONBLOCK;
            if (resetDevice) fixed (byte* uinputPathPtr = uinputPath) handle = c.open(uinputPathPtr, c.O_WRONLY | blockFlag);
            else fixed (byte* uinputPathPtr = uinputPath) handle = c.open(uinputPathPtr, (int)mode | blockFlag);
            if (handle < 0) continue;
            
            // force open all endpoints
            if (forceOpenAllEndpoints)
            {
                Log.WriteLine($"Force open All found:{path}");
                handles.Add(handle);
                continue;
            }
            
            // validate hardware
            hid.hidraw_devinfo info;
            if (resetDevice)
            {
                Log.WriteLine($"Reseting device handle for vendorID:{vendorID} productID:{productID} path:{path}");
                c.ioctl(handle, hid.USBDEVFS_RESET, null);
                c.close(handle);// always close handle in reset mode
                if (!openAll) return true;// stop if we're done
                else goto CONTINUE;
            }
            else
            {
                if (debugLog) Log.WriteLine("HID: 'HIDIOCGRAWINFO' reached");
                if (c.ioctl(handle, hid.HIDIOCGRAWINFO, &info) < 0)
                {
                    if (debugLog) Log.WriteLine("HID: 'HIDIOCGRAWINFO' FAILED");
                    goto CONTINUE;
                }
                if (debugLog) Log.WriteLine($"HID: 'HIDIOCGRAWINFO' Info, VID:0x{info.vendor.ToString("X4")} PID:0x{info.product.ToString("X4")}");
            }

            if (info.vendor == vendorID && info.product == productID)
            {
                if (debugLog) Log.WriteLine($"HID: matching reached VID:0x{vendorID.ToString("X4")} PID:0x{productID.ToString("X4")}");
                
                // get name
                string deviceName = null;
                NativeUtils.ZeroMemory(buffer, bufferSize);
                if (c.ioctl(handle, hid.HIDIOCGRAWNAME_256, buffer) >= 0)
                {
                    deviceName = Marshal.PtrToStringUTF8((IntPtr)buffer);
                }

                // get physical location
                string devicePhysicalLocation = null;
                NativeUtils.ZeroMemory(buffer, bufferSize);
                if (c.ioctl(handle, hid.HIDIOCGRAWPHYS_256, buffer) >= 0)
                {
                    devicePhysicalLocation = Marshal.PtrToStringUTF8((IntPtr)buffer);
                }

                // add handle
                if (name != null)
                {
                    if (nameIsContains && deviceName.Contains(name)) handles.Add(handle);
                    else if (deviceName == name) handles.Add(handle);
                    else goto CONTINUE;
                }
                else if (physicalLocation != null)
                {
                    if (physicalLocationIsContains && devicePhysicalLocation.Contains(physicalLocation)) handles.Add(handle);
                    else if (devicePhysicalLocation == physicalLocation) handles.Add(handle);
                    else goto CONTINUE;
                }
                else
                {
                    handles.Add(handle);
                }

                // log
                Log.WriteLine($"HID device found type:{BusType(info.bustype)} vendorID:{vendorID} productID:{productID} path:{path} deviceName:{deviceName} devicePhysicalLocation:{devicePhysicalLocation}");

                // stop if we're done
                if (!openAll) return true;

                // don't close this handle
                continue;
            }

            // close
            CONTINUE: c.close(handle);
        }

        return openAll && handles.Count > 0;
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
    
    public bool ReadData(byte[] data, int offset, int size, out nint sizeRead, int requireReadLength = -1)
    {
        // only allocate whats needed
        if (readBuffer == null) readBuffer = new byte[size];
        else if (readBuffer.Length < size) Array.Resize(ref readBuffer, size);
        
        // read all handles
        sizeRead = -1;
        bool success = false;
        fixed (byte* readBufferPtr = readBuffer)
        {
            foreach (int handle in handles)
            {
                nint readBufferSizeRead = c.read(handle, readBufferPtr, (UIntPtr)size);
                if (readBufferSizeRead >= 1 && !success)// only copy to first successful
                {
                    if (requireReadLength >= 1 && requireReadLength != readBufferSizeRead) continue;
                    sizeRead = readBufferSizeRead;
                    Array.Copy(readBuffer, 0, data, offset, readBufferSizeRead);
                    success = true;// success on first read
                }
            }
        }

        return success;
    }
}
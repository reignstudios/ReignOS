using ReignOS.Core.OS;
using ReignOS.Core;

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace ReignOS.Core;

public unsafe static class VirtualGamepad
{
    private static int handle;
    private static bool UI_DEV_created;
    private static input.input_event e;

    public static void Init()
    {
        // open uinput
        byte[] uinputPath = Encoding.UTF8.GetBytes("/dev/uinput");
        fixed (byte* uinputPathPtr = uinputPath) handle = c.open(uinputPathPtr, c.O_WRONLY | c.O_NONBLOCK);
        if (handle < 0)
        {
            Log.WriteLine("VirtualGamepad: Could not open uinput");
            return;
        }
        
        // setup device IDs (matches "Microsoft X-Box One Elite 2" controller)
        input.uinput_user_dev uidev;
        byte[] name = Encoding.ASCII.GetBytes("ReignOS Virtual Gamepad");
        fixed (byte* namePtr = name) Buffer.MemoryCopy(namePtr, uidev.name, name.Length, name.Length);
        uidev.id.bustype = input.BUS_USB;
        uidev.id.vendor  = 0x45e;
        uidev.id.product = 0xb00;
        uidev.id.version = 0x515;
        
        // setup button features
        c.ioctl(handle, input.UI_SET_EVBIT, input.EV_SYN);
        c.ioctl(handle, input.UI_SET_EVBIT, input.EV_KEY);
        
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_SOUTH);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_EAST);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_NORTH);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_WEST);
        
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_TL);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_TR);
        
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_START);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_SELECT);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_MODE);
        
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_THUMBL);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_THUMBR);
        
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_TRIGGER_HAPPY5);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_TRIGGER_HAPPY6);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_TRIGGER_HAPPY7);
        c.ioctl(handle, input.UI_SET_KEYBIT, input.BTN_TRIGGER_HAPPY8);
        
        // setup axis features
        c.ioctl(handle, input.UI_SET_EVBIT, input.EV_ABS);
        var abs_setup = new input.uinput_abs_setup();
        
        c.ioctl(handle, input.UI_SET_ABSBIT, input.ABS_X);
        abs_setup.code = input.ABS_X;
        abs_setup.absinfo.minimum = -32768;
        abs_setup.absinfo.maximum = 32767;
        abs_setup.absinfo.fuzz = 16;
        abs_setup.absinfo.flat = 128;
        c.ioctl(handle, input.UI_ABS_SETUP, &abs_setup);
        
        c.ioctl(handle, input.UI_SET_ABSBIT, input.ABS_Y);
        abs_setup.code = input.ABS_Y;
        abs_setup.absinfo.minimum = -32768;
        abs_setup.absinfo.maximum = 32767;
        abs_setup.absinfo.fuzz = 16;
        abs_setup.absinfo.flat = 128;
        c.ioctl(handle, input.UI_ABS_SETUP, &abs_setup);
        
        c.ioctl(handle, input.UI_SET_ABSBIT, input.ABS_Z);
        abs_setup.code = input.ABS_Z;
        abs_setup.absinfo.minimum = 0;
        abs_setup.absinfo.maximum = 1023;
        c.ioctl(handle, input.UI_ABS_SETUP, &abs_setup);
        
        c.ioctl(handle, input.UI_SET_ABSBIT, input.ABS_RX);
        abs_setup.code = input.ABS_RX;
        abs_setup.absinfo.minimum = -32768;
        abs_setup.absinfo.maximum = 32767;
        abs_setup.absinfo.fuzz = 16;
        abs_setup.absinfo.flat = 128;
        c.ioctl(handle, input.UI_ABS_SETUP, &abs_setup);
        
        c.ioctl(handle, input.UI_SET_ABSBIT, input.ABS_RY);
        abs_setup.code = input.ABS_RY;
        abs_setup.absinfo.minimum = -32768;
        abs_setup.absinfo.maximum = 32767;
        abs_setup.absinfo.fuzz = 16;
        abs_setup.absinfo.flat = 128;
        c.ioctl(handle, input.UI_ABS_SETUP, &abs_setup);
        
        c.ioctl(handle, input.UI_SET_ABSBIT, input.ABS_RZ);
        abs_setup.code = input.ABS_RZ;
        abs_setup.absinfo.minimum = 0;
        abs_setup.absinfo.maximum = 1023;
        c.ioctl(handle, input.UI_ABS_SETUP, &abs_setup);
        
        c.ioctl(handle, input.UI_SET_ABSBIT, input.ABS_HAT0X);
        abs_setup.code = input.ABS_HAT0X;
        abs_setup.absinfo.minimum = -1;
        abs_setup.absinfo.maximum = 1;
        c.ioctl(handle, input.UI_ABS_SETUP, &abs_setup);
        
        c.ioctl(handle, input.UI_SET_ABSBIT, input.ABS_HAT0Y);
        abs_setup.code = input.ABS_HAT0Y;
        abs_setup.absinfo.minimum = -1;
        abs_setup.absinfo.maximum = 1;
        c.ioctl(handle, input.UI_ABS_SETUP, &abs_setup);
        
        // setup force-feedback features
        /*c.ioctl(handle, input.UI_SET_EVBIT, input.EV_FF);
        
        c.ioctl(handle, input.UI_SET_FFBIT, input.FF_RUMBLE);
        c.ioctl(handle, input.UI_SET_FFBIT, input.FF_PERIODIC);
        c.ioctl(handle, input.UI_SET_FFBIT, input.FF_SQUARE);
        c.ioctl(handle, input.UI_SET_FFBIT, input.FF_TRIANGLE);
        c.ioctl(handle, input.UI_SET_FFBIT, input.FF_SINE);
        c.ioctl(handle, input.UI_SET_FFBIT, input.FF_GAIN);*/
        
        // create device
        c.write(handle, &uidev, (UIntPtr)Marshal.SizeOf<input.uinput_user_dev>());
        int errorValue = c.ioctl(handle, input.UI_DEV_CREATE);
        if (errorValue < 0)
        {
            byte* errorPtr = c.strerror(errorValue);
            int len = 0;
            while (errorPtr[len] != 0) len++;
            string error = Encoding.UTF8.GetString(errorPtr, len);
            
            Log.WriteLine("VirtualGamepad: Error creating uinput device: " + error);
            c.close(handle);
            handle = 0;
            return;
        }
        UI_DEV_created = true;
    }

    public static void Dispose()
    {
        if (handle != 0)
        {
            if (UI_DEV_created) c.ioctl(handle, input.UI_DEV_DESTROY);
            c.close(handle);
            handle = 0;
        }
    }

    public delegate void ReadForceFeedbackCallback(in input.input_event e);
    public static void ReadForceFeedback(ReadForceFeedbackCallback callback)
    {
        if (!UI_DEV_created) return;
        var e = new input.input_event();
        var size = (IntPtr)Marshal.SizeOf<input.input_event>();
        while (c.read(handle, &e, (UIntPtr)size) == size)
        {
            if (e.type == input.EV_UINPUT)
            {
                if (e.code == input.UI_FF_UPLOAD)
                {
                    var upload = new input.uinput_ff_upload();
                    upload.request_id = (uint)e.value;
                    if (c.ioctl(handle, input.UI_BEGIN_FF_UPLOAD, &upload) < 0) continue;
                    
                    /*input.ff_effect *effect &upload.effect;// NOTE: relevent value reference
                    if (effect->type == input.FF_RUMBLE)
                    {
                        //input.ff_rumble_effect *rumble = &effect->u.rumble;
                        //rumble->strong_magnitude;
                        //rumble->weak_magnitude;
                        //effect->replay.length;
                        //effect->replay.delay;
                    }
                    else if (effect->type == input.FF_PERIODIC)
                    {
                        //effect->u.periodic.waveform;
                        //effect->u.periodic.magnitude;
                        //effect->u.periodic.period;
                    }*/
                    
                    // tell kernel success
                    c.ioctl(handle, input.UI_END_FF_UPLOAD, &upload);
                    callback?.Invoke(e);
                }
                else if (e.code == input.UI_FF_ERASE)
                {
                    var erase = new input.uinput_ff_erase();
                    erase.request_id = (uint)e.value;
                    if (c.ioctl(handle, input.UI_BEGIN_FF_ERASE, &erase) < 0) continue;
                    
                    // tell kernel success
                    erase.retval = 0;
                    c.ioctl(handle, input.UI_END_FF_ERASE, &erase);
                    callback?.Invoke(e);
                }
            }
        }
    }
    
    public static void StartWrites()
    {
        if (!UI_DEV_created) return;
        var e = new input.input_event();
        c.gettimeofday(&e.time, null);
        VirtualGamepad.e = e;
    }
    
    public static void WriteButton(int button, bool pressed)
    {
        if (!UI_DEV_created) return;
        var e = VirtualGamepad.e;
        e.type = input.EV_KEY;
        e.code = (ushort)button;
        e.value = pressed ? 1 : 0;
        c.write(handle, &e, (UIntPtr)Marshal.SizeOf<input.input_event>());
    }
    
    public static void EndWrites()
    {
        if (!UI_DEV_created) return;
        var e = VirtualGamepad.e;
        e.type = input.EV_SYN;
        e.code = input.SYN_REPORT;
        c.write(handle, &e, (UIntPtr)Marshal.SizeOf<input.input_event>());
    }

    public static void Write_TriggerLeftSteamMenu()
    {
        // press
        StartWrites();
        WriteButton(input.BTN_MODE, true);
        EndWrites();

        // release
        Thread.Sleep(100);
        StartWrites();
        WriteButton(input.BTN_MODE, false);
        EndWrites();
    }

    public static void Write_TriggerRightSteamMenu()
    {
        // hold guide
        StartWrites();
        WriteButton(input.BTN_MODE, true);
        EndWrites();

        // tap A
        Thread.Sleep(100);
        StartWrites();
        WriteButton(input.BTN_SOUTH, true);
        EndWrites();

        Thread.Sleep(100);
        StartWrites();
        WriteButton(input.BTN_SOUTH, false);
        EndWrites();

        // release guide
        StartWrites();
        WriteButton(input.BTN_MODE, false);
        EndWrites();
    }
}

/*Input driver version is 1.0.1
   Input device ID: bus 0x3 vendor 0x45e product 0xb00 version 0x515
   Input device name: "Microsoft X-Box One Elite 2 pad"
   Supported events:
     Event type 0 (EV_SYN)
     Event type 1 (EV_KEY)
       Event code 304 (BTN_SOUTH)
       Event code 305 (BTN_EAST)
       Event code 307 (BTN_NORTH)
       Event code 308 (BTN_WEST)
       Event code 310 (BTN_TL)
       Event code 311 (BTN_TR)
       Event code 314 (BTN_SELECT)
       Event code 315 (BTN_START)
       Event code 316 (BTN_MODE)
       Event code 317 (BTN_THUMBL)
       Event code 318 (BTN_THUMBR)
       Event code 708 (BTN_TRIGGER_HAPPY5)
       Event code 709 (BTN_TRIGGER_HAPPY6)
       Event code 710 (BTN_TRIGGER_HAPPY7)
       Event code 711 (BTN_TRIGGER_HAPPY8)
     Event type 3 (EV_ABS)
       Event code 0 (ABS_X)
         Value  -2653
         Min   -32768
         Max    32767
         Fuzz      16
         Flat     128
       Event code 1 (ABS_Y)
         Value   -718
         Min   -32768
         Max    32767
         Fuzz      16
         Flat     128
       Event code 2 (ABS_Z)
         Value      0
         Min        0
         Max     1023
       Event code 3 (ABS_RX)
         Value  -1171
         Min   -32768
         Max    32767
         Fuzz      16
         Flat     128
       Event code 4 (ABS_RY)
         Value   1679
         Min   -32768
         Max    32767
         Fuzz      16
         Flat     128
       Event code 5 (ABS_RZ)
         Value      0
         Min        0
         Max     1023
       Event code 16 (ABS_HAT0X)
         Value      0
         Min       -1
         Max        1
       Event code 17 (ABS_HAT0Y)
         Value      0
         Min       -1
         Max        1
     Event type 21 (EV_FF)
       Event code 80 (FF_RUMBLE)
       Event code 81 (FF_PERIODIC)
       Event code 88 (FF_SQUARE)
       Event code 89 (FF_TRIANGLE)
       Event code 90 (FF_SINE)
       Event code 96 (FF_GAIN)
   Properties:*/
   
/* evtest info for real Xbox-One gamepad
Input driver version is 1.0.1
Input device ID: bus 0x3 vendor 0x45e product 0x2ea version 0x301
Input device name: "Microsoft X-Box One S pad"
Supported events:
  Event type 0 (EV_SYN)
  Event type 1 (EV_KEY)
    Event code 304 (BTN_SOUTH)
    Event code 305 (BTN_EAST)
    Event code 307 (BTN_NORTH)
    Event code 308 (BTN_WEST)
    Event code 310 (BTN_TL)
    Event code 311 (BTN_TR)
    Event code 314 (BTN_SELECT)
    Event code 315 (BTN_START)
    Event code 316 (BTN_MODE)
    Event code 317 (BTN_THUMBL)
    Event code 318 (BTN_THUMBR)
  Event type 3 (EV_ABS)
    Event code 0 (ABS_X)
      Value   3132
      Min   -32768
      Max    32767
      Fuzz      16
      Flat     128
    Event code 1 (ABS_Y)
      Value    116
      Min   -32768
      Max    32767
      Fuzz      16
      Flat     128
    Event code 2 (ABS_Z)
      Value      0
      Min        0
      Max     1023
    Event code 3 (ABS_RX)
      Value   2812
      Min   -32768
      Max    32767
      Fuzz      16
      Flat     128
    Event code 4 (ABS_RY)
      Value  -1119
      Min   -32768
      Max    32767
      Fuzz      16
      Flat     128
    Event code 5 (ABS_RZ)
      Value      0
      Min        0
      Max     1023
    Event code 16 (ABS_HAT0X)
      Value      0
      Min       -1
      Max        1
    Event code 17 (ABS_HAT0Y)
      Value      0
      Min       -1
      Max        1
  Event type 21 (EV_FF)
    Event code 80 (FF_RUMBLE)
    Event code 81 (FF_PERIODIC)
    Event code 88 (FF_SQUARE)
    Event code 89 (FF_TRIANGLE)
    Event code 90 (FF_SINE)
    Event code 96 (FF_GAIN)
*/
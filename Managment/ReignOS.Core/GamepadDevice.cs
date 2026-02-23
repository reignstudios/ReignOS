using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Linq;
using ReignOS.Core.OS;

namespace ReignOS.Core;

public struct GamepadButton
{
    public bool on, down, up;
    public bool hasUpdate;

    public void Update(bool on)
    {
        down = false;
        up = false;
        hasUpdate = false;
        if (this.on != on)
        {
            down = on;
            up = !on;
            this.on = on;
            hasUpdate = true;
        }
    }
}

public struct GamepadAxis
{
    public float value;
    public bool hasUpdate;

    public void Update(float value)
    {
        if (MathF.Abs(value) < 0.1f) value = 0;
        hasUpdate = false;
        if (this.value != value)
        {
            this.value = value;
            hasUpdate = true;
        }
    }
}

public class Gamepad
{
    public int handle;
    public string name;
    public ushort vid, pid;
    public GamepadButton[] buttons;
    public GamepadAxis[] axes;
    
    public Gamepad(int handle, string name, ushort vid, ushort pid)
    {
        this.handle = handle;
        this.name = name;
        this.vid = vid;
        this.pid = pid;
    }

    public void Dispose()
    {
        if (handle >= 0)
        {
            c.close(handle);
            handle = -1;
        }
    }
}

public unsafe class GamepadDevice : IDisposable
{
    private List<Gamepad> gamepads;
    
    public void Init(ushort vendorID, ushort productID)
    {
        gamepads = new List<Gamepad>();
        const int bufferSize = 256;
        byte* buffer = stackalloc byte[bufferSize];
        
        for (int i = 0; i != 16; ++i)
        {
            // open device
            string path = "/dev/input/js" + i.ToString();
            byte[] pathEncoded = Encoding.UTF8.GetBytes(path);
            int handle;
            fixed (byte* uinputPathPtr = pathEncoded) handle = c.open(uinputPathPtr, c.O_RDONLY | c.O_NONBLOCK);
            if (handle < 0) continue;
            
            // get device name
            byte[] infoPathEncoded = Encoding.UTF8.GetBytes($"/sys/class/input/js{i}/device/name");
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
            
            // validate hardware
            if (vendorID == 0 && productID == 0)
            {
                string vendorValue = File.ReadAllText($"/sys/class/input/js{i}/device/id/vendor").TrimEnd();
                string productValue = File.ReadAllText($"/sys/class/input/js{i}/device/id/product").TrimEnd();
                ushort vendor = Convert.ToUInt16(vendorValue, 16);
                ushort product = Convert.ToUInt16(productValue, 16);
                Log.WriteLine($"Gamepad device found Name:'{deviceName}' vendorID:{vendor} productID:{product} path:{path}");
                gamepads.Add(new Gamepad(handle, deviceName, vendor, product));
                continue;
            }
            else
            {
                string vendorValue = File.ReadAllText($"/sys/class/input/js{i}/device/id/vendor").TrimEnd();
                string productValue = File.ReadAllText($"/sys/class/input/js{i}/device/id/product").TrimEnd();
                ushort vendor = Convert.ToUInt16(vendorValue, 16);
                ushort product = Convert.ToUInt16(productValue, 16);
                if (vendor == vendorID && product == productID)
                {
                    Log.WriteLine($"Gamepad device found Name:'{deviceName}' vendorID:{vendor} productID:{product} path:{path}");
                    gamepads.Add(new Gamepad(handle, deviceName, vendor, product));
                    continue;
                }
            }
            
            // close
            CONTINUE: c.close(handle);
        }
        
        // alloc elements
        foreach (var gamepad in gamepads)
        {
            byte count = 0;
            if (c.ioctl(gamepad.handle, joystick.JSIOCGBUTTONS, &count) >= 0)
            {
                gamepad.buttons = new GamepadButton[count];
            }
            
            count = 0;
            if (c.ioctl(gamepad.handle, joystick.JSIOCGAXES, &count) >= 0)
            {
                gamepad.axes = new GamepadAxis[count];
            }
        }
    }
    
    public void Dispose()
    {
        if (gamepads != null)
        {
            foreach (var gamepad in gamepads) gamepad.Dispose();
            gamepads = null;
        }
    }
    
    public List<Gamepad> ReadNextInput()
    {
        if (gamepads == null || gamepads.Count == 0) return null;
        
        var buttonsPressed = stackalloc bool[64];
        var axesValues = stackalloc float[32];
        foreach (var gamepad in gamepads)
        {
            // clear input
            for (int i = 0; i != gamepad.buttons.Length; ++i) buttonsPressed[i] = false;
            for (int i = 0; i != gamepad.axes.Length; ++i) axesValues[i] = 0;
            
            // gather input
            var e = new joystick.js_event();
            while (true)
            {
                if (c.read(gamepad.handle, &e, (UIntPtr)Marshal.SizeOf<joystick.js_event>()) >= 0)
                {
                    if (e.type == joystick.JS_EVENT_BUTTON)
                    {
                        buttonsPressed[e.number] = e.value != 0;
                    }
                    else if (e.type == joystick.JS_EVENT_AXIS)
                    {
                        axesValues[e.number] = e.value / (float)short.MaxValue;
                    }
                }
                else
                {
                    break;
                }
            }
            
            // update input
            for (int i = 0; i != gamepad.buttons.Length; ++i)
            {
                gamepad.buttons[i].Update(buttonsPressed[i]);
            }
            
            for (int i = 0; i != gamepad.axes.Length; ++i)
            {
                gamepad.axes[i].Update(axesValues[i]);
            }
        }
        
        return gamepads;
    }
}
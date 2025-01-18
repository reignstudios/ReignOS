namespace ReignOS.Service;
using ReignOS.Service.OS;
using ReignOS.Core;

using System.Text;

public unsafe static class KeyboardInput
{
    private static int handle;
    
    public static void Init()
    {
        // open keyboard
        uinput.uinput_user_dev uidev;
        byte[] uinputPath = Encoding.UTF8.GetBytes("/dev/input/event2");
        fixed (byte* uinputPathPtr = uinputPath) handle = c.open(uinputPathPtr, c.O_WRONLY | c.O_NONBLOCK);
        if (handle < 0)
        {
            Log.WriteLine("Could not open keyboard events");
            return;
        }
    }
    
    public static void Dispose()
    {
        
    }
    
    public static void Update()
    {
        
    }
}
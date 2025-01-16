namespace ReignOS.Core.Hardware;

using HidSharp;

public static class MSI_Claw
{
    enum Mode : byte
    {
        Offline = 0,
        XInput = 1,
        DInput = 2,
        MSI = 3,
        Desktop = 4,
        BIOS = 5,
        Testing = 6
    }

    public static bool isEnabled { get; private set; }
    private static HidDevice? device;

    public static void Configure(HidDevice device)
    {
        if (device.VendorID != 0x0DB0 || device.ProductID != 0x1901) return;
        if (EnableMode(device, Mode.XInput)) MSI_Claw.device = device;
    }

    private static bool EnableMode(HidDevice device, Mode mode)
    {
        if (!device.TryOpen(out var hidStream)) return false;
        Log.WriteLine("MSI-Claw gamepad found");
			
        using (hidStream)
        {
            try
            {
                int i = 0;
                var writeBuf = new byte[8];
                writeBuf[i++] = 15;// report id
                writeBuf[i++] = 0;
                writeBuf[i++] = 0;
                writeBuf[i++] = 60;
                writeBuf[i++] = 36;// we want to switch mode
                writeBuf[i++] = (byte)mode;// mode
                writeBuf[i++] = 0;
                writeBuf[i++] = 0;
                hidStream.Write(writeBuf);
                Log.WriteLine("MSI-Claw gamepad mode set");
                isEnabled = true;
                return true;
            }
            catch { }
        }

        return false;
    }

    public static void Update(bool resumeFromSleep)
    {
        if (resumeFromSleep)
        {
            if (device != null) EnableMode(device, Mode.XInput);
        }
    }
}
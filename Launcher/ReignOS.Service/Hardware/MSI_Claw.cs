namespace ReignOS.Core.Hardware;

using HidSharp;

public static class MSI_Claw
{
    public static void Configure(HidDevice device)
    {
        if (device.VendorID != 0x0DB0 || device.ProductID != 0x1901) return;

        if (!device.TryOpen(out var hidStream)) return;
        Console.WriteLine("MSI-Claw gamepad found");
			
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
                writeBuf[i++] = 1;// xpad mode
                writeBuf[i++] = 0;
                writeBuf[i++] = 0;
                hidStream.Write(writeBuf);
                Console.WriteLine("MSI-Claw gamepad mode set");
            }
            catch { }
        }
    }
}
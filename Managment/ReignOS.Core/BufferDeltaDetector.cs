using System;

namespace ReignOS.Core;

public class BufferDeltaDetector
{
    private byte[] data;
    private int waitFrame;

    public bool TestDelta(byte[] data, int length)
    {
        // init on first pass or change
        if (this.data == null || this.data.Length != data.Length)
        {
            this.data = new byte[data.Length];
            Array.Copy(data, this.data, data.Length);
            waitFrame = 0;
            return true;
        }

        // wait 10 frames before we start matching
        waitFrame++;
        if (waitFrame < 10) return false;
        
        // check for any changes
        for (int i = 0; i < length; i++)
        {
            if (this.data[i] != data[i])
            {
                return true;
            }
        }

        return false;
    }
}
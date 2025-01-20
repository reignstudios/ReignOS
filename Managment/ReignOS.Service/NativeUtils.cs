using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReignOS.Service
{
    static unsafe class NativeUtils
    {
        public static void ZeroMemory(void* buffer, int size)
        {
            byte* data = (byte*)buffer;
            for (int i = 0; i < size; ++i) data[i] = 0;
        }
    }
}

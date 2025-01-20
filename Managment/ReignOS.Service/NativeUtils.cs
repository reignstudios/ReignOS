using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReignOS.Service
{
    static unsafe class NativeUtils
    {
        public static void ZeroMemory(byte* buffer, int size)
        {
            for (int i = 0; i < size; ++i) buffer[i] = 0;
        }
    }
}

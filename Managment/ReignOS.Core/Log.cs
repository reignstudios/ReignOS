namespace ReignOS.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Log
{
    public static void Write(string message)
    {
        Console.Write(message);
    }

    public static void Write(object o)
    {
        if (o != null) Write(o.ToString());
    }
    
    public static void WriteLine(string message)
    {
        Console.WriteLine("ReignOS: " + message);
    }

    public static void WriteLine(object o)
    {
        if (o != null) WriteLine(o.ToString());
    }
    
    public unsafe static void WriteLine(string header, byte* nativeText)
    {
        Console.Write("ReignOS: " + header);
        int i = 0;
        byte c = nativeText[0];
        while (c != '\0')
        {
            Console.Write(c);
            i++;
            c = nativeText[i];
        }
        Console.WriteLine();
    }
}
namespace ReignOS.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Log
{
    public static object lockObj = new object();
    public static string prefix = "ReignOS: ";

    public static void Write(string message)
    {
        lock (lockObj) Console.Write(prefix + message);
    }

    public static void Write(object o)
    {
        if (o != null) Write(o.ToString());
    }
    
    public static void WriteLine(string message)
    {
        lock (lockObj) Console.WriteLine(prefix + message);
    }

    public static void WriteLine(object o)
    {
        if (o != null) WriteLine(o.ToString());
    }
    
    public unsafe static void WriteLine(string header, byte* nativeText)
    {
        lock (lockObj)
        {
            Console.Write(prefix + header);
            int i = 0;
            char c = (char)nativeText[0];
            while (c != '\0')
            {
                Console.Write(c);
                i++;
                c = (char)nativeText[i];
            }
            Console.WriteLine();
        }
    }
}
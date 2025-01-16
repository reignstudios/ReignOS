namespace ReignOS.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Log
{
    public static void WriteLine(string? message)
    {
        Console.WriteLine("ReignOS: " + message);
    }

    public static void WriteLine(object? o)
    {
        if (o != null) WriteLine(o.ToString());
    }
}
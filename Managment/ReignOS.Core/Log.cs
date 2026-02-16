namespace ReignOS.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

public static class Log
{
    public static object lockObj = new object();
    private static string prefix = "ReignOS: ";
    
    private static FileStream stream;
    private static StreamWriter writer;

    public static void Init(string prefix)
    {
        try
        {
            const string path = "/home/gamer/ReignOS_Ext";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            string filename = Path.Combine(path, prefix + ".log");
            stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
            writer = new StreamWriter(stream, Encoding.UTF8);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        Log.prefix = prefix + ": ";
    }

    public static void Close()
    {
        lock (lockObj)
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }

            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }

    public static void Write(string message)
    {
        lock (lockObj)
        {
            if (writer == null)
            {
                Console.Write(prefix + message);
                return;
            }
            
            try
            {
                writer.Write(prefix + message);
                writer.Flush();
                stream.Flush();
            }
            catch { }
        }
    }

    public static void Write(object o)
    {
        if (o != null) Write(o.ToString());
    }
    
    public static void WriteLine(string message)
    {
        lock (lockObj)
        {
            if (writer == null)
            {
                Console.WriteLine(prefix + message);
                return;
            }
            
            try
            {
                writer.WriteLine(prefix + message);
                writer.Flush();
                stream.Flush();
            }
            catch { }
        }
    }

    public static void WriteLine(object o)
    {
        if (o != null) WriteLine(o.ToString());
    }
    
    public unsafe static void WriteLine(string header, byte* nativeText)
    {
        lock (lockObj)
        {
            if (writer == null)
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
                return;
            }
            
            try
            {
                writer.Write(prefix + header);
                int i = 0;
                char c = (char)nativeText[0];
                while (c != '\0')
                {
                    writer.Write(c);
                    i++;
                    c = (char)nativeText[i];
                }
                writer.WriteLine();
                writer.Flush();
                stream.Flush();
            }
            catch { }
        }
    }
    
    public static void WriteData(string header, byte[] data, int offset, int length)
    {
        lock (lockObj)
        {
            if (writer == null)
            {
                Console.WriteLine(header);
                for (int i = offset; i < length; i++)
                {
                    Console.WriteLine(" " + data[i].ToString("x4"));
                }
                return;
            }
            
            writer.WriteLine(header);
            for (int i = offset; i < length; i++)
            {
                writer.WriteLine(" " + data[i].ToString("x4"));
            }
        }
    }
    
    public static void WriteDataAsLine(string header, byte[] data, int offset, int length)
    {
        lock (lockObj)
        {
            if (writer == null)
            {
                Console.Write(header);
                for (int i = offset; i < length; i++)
                {
                    Console.Write(data[i].ToString("x4") + " ");
                }
                Console.WriteLine();
                return;
            }
            
            writer.Write(header);
            for (int i = offset; i < length; i++)
            {
                writer.Write(data[i].ToString("x4") + " ");
            }
            writer.WriteLine();
        }
    }
}
namespace ReignOS.Service;
using ReignOS.Core;
using System;
using System.Text.RegularExpressions;
using System.Diagnostics;

static class DbusMonitor
{
    public static void Init()
    {
        using var serviceProcess = new Process();
        try
        {
            serviceProcess.StartInfo.UseShellExecute = false;
            serviceProcess.StartInfo.FileName = "dbus-monitor";
            serviceProcess.StartInfo.Arguments = "--system";
            serviceProcess.StartInfo.RedirectStandardOutput = true;
            serviceProcess.StartInfo.RedirectStandardError = true;
            serviceProcess.OutputDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    ProcessLine(args.Data);
                }
            };
            serviceProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    ProcessLine(args.Data);
                }
            };
            serviceProcess.Start();
            
            serviceProcess.BeginOutputReadLine();
            serviceProcess.BeginErrorReadLine();
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
    }
    
    public static void Shutdown()
    {
        // TODO: make process a field so we close it
    }
    
    private static void ProcessLine(string line)
    {
        lock (Log.lockObj) Console.WriteLine(line);
        var match = Regex.Match(line, @"method call time=.*? sender=.*? -> destination=org.freedesktop.DBus serial=.*? path=/org/freedesktop/DBus; interface=org.freedesktop.DBus; member=Hello");
        if (match.Success)
        {
            // TODO: keep track of line progress and trigger after valid
        }
    }
}
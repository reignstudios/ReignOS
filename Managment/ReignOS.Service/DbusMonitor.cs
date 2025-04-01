namespace ReignOS.Service;
using ReignOS.Core;
using System;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Generic;

static class DbusMonitor
{
    private static Process process;
    public static bool? isRebootMode;

    public static void Init()
    {
        // start inhibiter
        ProcessUtil.Run("systemd-inhibit", "--what=shutdown --who=\"ReignOS\" --why=\"Clean Shutdown\" -- sleep infinity", out _, wait:false);

        // start watchdog
        try
        {
            process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = "dbus-monitor";
            process.StartInfo.Arguments = "--system";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.OutputDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    ProcessLine(args.Data);
                }
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    ProcessLine(args.Data);
                }
            };
            process.Start();
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
    }
    
    public static void Shutdown()
    {
        ProcessUtil.KillHard("systemd-inhibit", false, out _);
        if (process != null)
        {
            process.Dispose();
            process = null;
        }
    }
    
    private static void ProcessLine(string line)
    {
        //lock (Log.lockObj) Console.WriteLine(line);// NOTE: only log for testing
        
        if (line.Contains("member=PowerOff"))
        {
            isRebootMode = false;
        }
        else if (line.Contains("member=Reboot"))
        {
            isRebootMode = true;
        }
        else if (line.Contains("Access denied due to active block inhibitor"))// || line.Contains("member=ListInhibitors"))
        {
            PreShutdown();
        }
    }

    private static void PreShutdown()
    {
        Log.WriteLine("Shutting down steam...");
        ProcessUtil.Run("sudo", "-u gamer -- steam -shutdown");
        ProcessUtil.Wait("steam", 20);
        ProcessUtil.KillHard("systemd-inhibit", false, out _);
        Program.exit = true;
    }
}
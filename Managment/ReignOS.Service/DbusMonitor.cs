namespace ReignOS.Service;
using ReignOS.Core;
using System;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Generic;

static class DbusMonitor
{
    enum Stage
    {
        None,

        Signal_Step1,
        Signal_Step2,

        PwrBtn_Step1,
        PwrBtn_Step2,
        PwrBtn_Step3,

        Shutdown
    }

    private static Process process;

    private static Stage stage;
    private static int lineToleranceCount, arrayOpen, structOpen;
    private static bool hasShutdownBlockedFlag, hasSpecifierFlag, isRebootMode;

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
        if (line.Contains("member=ListInhibitors") || line.Contains("Access denied due to active block inhibitor"))
        {
            PreShutdown();
        }
        
        /*lock (Log.lockObj) Console.WriteLine(line);// NOTE: only log for testing

        // detect
        if (stage == Stage.None)
        {
            var match = Regex.Match(line, @"method call time=.*? sender=.*? -> destination=org.freedesktop.DBus serial=.*? path=/org/freedesktop/DBus; interface=org.freedesktop.DBus; member=Hello");
            if (match.Success)
            {
                stage = Stage.Signal_Step1;
            }
            else
            {
                match = Regex.Match(line, @"signal time=.*? sender=.*? -> destination=(null destination) serial=.*? path=/org/freedesktop/login1; interface=org.freedesktop.login1.Manager; member=PrepareForShutdown");
                if (match.Success)
                {
                    isRebootMode = false;
                    stage = Stage.PwrBtn_Step1;
                }
                else
                {
                    match = Regex.Match(line, @"signal time=.*? sender=.*? -> destination=(null destination) serial=.*? path=/org/freedesktop/login1; interface=org.freedesktop.login1.Manager; member=PrepareForReboot");
                    if (match.Success)
                    {
                        isRebootMode = true;
                        stage = Stage.PwrBtn_Step1;
                    }
                }
            }
        }

        // poweroff/reboot signal
        else if (stage == Stage.Signal_Step1)
        {
            var match = Regex.Match(line, @"method call time=.*? sender=.*? -> destination=org.freedesktop.login1 serial=.*? path=/org/freedesktop/login1; interface=org.freedesktop.login1.Manager; member=ListInhibitors");
            if (match.Success)
            {
                stage = Stage.Signal_Step2;
            }
            else// continue to scan
            {
                lineToleranceCount++;
                if (lineToleranceCount >= 20)
                {
                    Reset();
                }
            }
        }
        else if (stage == Stage.Signal_Step2)
        {
            if (line.Contains("array [")) arrayOpen++;
            else if (line.Contains("]")) arrayOpen--;
            else if (line.Contains("struct {")) structOpen++;
            else if (line.Contains("}")) structOpen--;
            else if (line.Contains("string \"shutdown\"")) hasShutdownBlockedFlag = true;
            else if (line.Contains("string \"ReignOS\"")) hasSpecifierFlag = true;

            if (arrayOpen == 0 && structOpen == 0 && hasShutdownBlockedFlag && hasSpecifierFlag)
            {
                isRebootMode = false;
                stage = Stage.Shutdown;
                PreShutdown();
            }
        }

        // power button
        else if (stage == Stage.PwrBtn_Step1)
        {
            if (line.Contains("boolean true")) stage = Stage.PwrBtn_Step2;
        }
        else if (stage == Stage.PwrBtn_Step2)
        {
            var match = Regex.Match(line, @"signal time=.*? sender=.*? -> destination=(null destination) serial=.*? path=/org/freedesktop/login1; interface=org.freedesktop.DBus.Properties; member=PropertiesChanged");
            if (match.Success)
            {
                stage = Stage.PwrBtn_Step3;
            }
            else// continue to scan
            {
                lineToleranceCount++;
                if (lineToleranceCount >= 20)
                {
                    Reset();
                }
            }
        }
        else if (stage == Stage.PwrBtn_Step3)
        {
            if (line.Contains("array [")) arrayOpen++;
            else if (line.Contains("]")) arrayOpen--;
            else if (line.Contains("dict entry(")) structOpen++;
            else if (line.Contains(")")) structOpen--;
            else if (line.Contains("string \"DelayInhibited\"")) hasShutdownBlockedFlag = true;
            else if (line.Contains("string \"sleep\"")) hasSpecifierFlag = true;

            if (arrayOpen == 0 && structOpen == 0 && hasShutdownBlockedFlag && hasSpecifierFlag)
            {
                stage = Stage.Shutdown;
                PreShutdown();
            }
        }

        // reset
        else if (stage != Stage.Shutdown)
        {
            Reset();
        }*/
    }

    private static void Reset()
    {
        stage = Stage.None;
        lineToleranceCount = 0;
        arrayOpen = 0;
        structOpen = 0;
        hasShutdownBlockedFlag = false;
        hasSpecifierFlag = false;
        isRebootMode = false;
    }

    private static void PreShutdown()
    {
        Log.WriteLine("Shutting down steam...");
        ProcessUtil.Run("sudo", "-u gamer -- steam -shutdown", out _);
        //ProcessUtil.Run("steam", "-shutdown", out _);
        ProcessUtil.Wait("steam", 20);
        ProcessUtil.KillHard("systemd-inhibit", false, out _);
        //if (isRebootMode) ProcessUtil.Run("reboot", "", out _);
        //else ProcessUtil.Run("poweroff", "", out _);
    }
}
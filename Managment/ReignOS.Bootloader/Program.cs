namespace ReignOS.Bootloader;
using ReignOS.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

enum Compositor
{
    None,
    Cage,
    Gamescope,
    Labwc
}

internal class Program
{
    static void Main(string[] args)
    {
        Log.prefix = "ReignOS.Bootloader: ";
        Log.WriteLine("Bootloader started");
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        LibraryResolver.Init(Assembly.GetExecutingAssembly());

        // start auto mounting service
        ProcessUtil.KillHard("udiskie", true, out _);
        ProcessUtil.Run("udiskie", "--no-tray", out _, wait:false);

        // kill service if its currently running
        ProcessUtil.KillHard("ReignOS.Service", true, out _);
        
        // start service
        using var serviceProcess = new Process();
        try
        {
            serviceProcess.StartInfo.UseShellExecute = false;
            serviceProcess.StartInfo.FileName = "sudo";
            serviceProcess.StartInfo.Arguments = "-S ./ReignOS.Service";
            serviceProcess.StartInfo.RedirectStandardInput = true;
            serviceProcess.StartInfo.RedirectStandardOutput = true;
            serviceProcess.StartInfo.RedirectStandardError = true;
            serviceProcess.OutputDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    string value = args.Data;
                    if (value.Contains("[sudo] password for"))
                    {
                        serviceProcess.StandardInput.WriteLine("gamer");
                        serviceProcess.StandardInput.Flush();
                    }
                    else if (value.Contains("SET_VOLUME_DOWN"))
                    {
                        ProcessUtil.Run("amixer", "set Master 5%-", out _);
                        //ProcessUtil.Run("beep", "", out _);
                    }
                    else if (value.Contains("SET_VOLUME_UP"))
                    {
                        ProcessUtil.Run("amixer", "set Master 5%+", out _);
                        //ProcessUtil.Run("beep", "", out _);
                    }
                    lock (Log.lockObj) Console.WriteLine(value);
                }
            };
            serviceProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    string value = args.Data;
                    if (value.Contains("[sudo] password for"))
                    {
                        serviceProcess.StandardInput.WriteLine("gamer");
                        serviceProcess.StandardInput.Flush();
                    }
                    lock (Log.lockObj) Console.WriteLine(value);
                }
            };
            serviceProcess.Start();
            
            serviceProcess.StandardInput.WriteLine("gamer");
            serviceProcess.StandardInput.Flush();
            
            serviceProcess.BeginOutputReadLine();
            serviceProcess.BeginErrorReadLine();
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
            goto SHUTDOWN;
        }
        Thread.Sleep(1000);// give service a sec to config anything needed before launching compositor

        // start compositor
        var compositor = Compositor.None;
        foreach (string arg in args)
        {
            if (arg == "--cage") compositor = Compositor.Cage;
            else if (arg == "--gamescope") compositor = Compositor.Gamescope;
            else if (arg == "--labwc") compositor = Compositor.Labwc;
        }

        try
        {
            switch (compositor)
            {
                case Compositor.None:
                    Log.WriteLine("No Compositor specified (sleeping)");
                    Thread.Sleep(6000);
                    break;// sleep for 6 seconds to allow for service bootup testing

                case Compositor.Cage: StartCompositor_Cage(); break;
                case Compositor.Gamescope: StartCompositor_Gamescope(); break;
                case Compositor.Labwc: StartCompositor_Labwc(); break;
            }
        }
        catch (Exception e)
        {
            Log.WriteLine("Failed to start compositor");
            Log.WriteLine(e);
        }

        // stop service
        SHUTDOWN:;
        ProcessUtil.KillHard("udiskie", true, out _);
        if (serviceProcess != null && !serviceProcess.HasExited)
        {
            Log.WriteLine("Soft Killing service");
            ProcessUtil.KillSoft("ReignOS.Service", true, out int exitCode);
            Thread.Sleep(1000);

            Log.WriteLine("Hard Killing service (just in case)");
            serviceProcess.Kill();
            ProcessUtil.KillHard("ReignOS.Service", true, out exitCode);
        }
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e != null) Log.WriteLine($"Unhandled exception: {e}");
        else Log.WriteLine("Unhandled exception: Unknown");
    }

    private static void StartCompositor_Cage()
    {
        ProcessUtil.Run("chmod", "+x ./Start_Cage.sh", out _, wait:true);
        string result = ProcessUtil.Run("cage", "-d -s -- ./Start_Cage.sh", out _, enviromentVars:null, wait:true);// start Cage with Steam in console mode
        Log.WriteLine(result);
    }

    private static void StartCompositor_Gamescope()
    {
        ProcessUtil.Run("chmod", "+x ./Start_Gamescope.sh", out _, wait:true);
        string result = ProcessUtil.Run("gamescope", "-e -f --adaptive-sync -- ./Start_Gamescope.sh", out _, enviromentVars:null, wait:true);// start Gamescope with Steam in console mode, VRR
        Log.WriteLine(result);
    }

    private static void StartCompositor_Labwc()
    {
        string result = ProcessUtil.Run("labwc", "--session steam", out _, enviromentVars:null, wait:true);// start Labwc with Steam in desktop mode
        Log.WriteLine(result);
    }
}
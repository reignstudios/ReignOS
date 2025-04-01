namespace ReignOS.Bootloader;
using ReignOS.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.IO;

enum Compositor
{
    None,
    Gamescope,
    Cage,
    Labwc,
    X11
}

internal class Program
{
    static void Main(string[] args)
    {
        int exitCode = 0;

        Log.prefix = "ReignOS.Bootloader: ";
        Log.WriteLine("Bootloader started: " + VersionInfo.version);
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        LibraryResolver.Init(Assembly.GetExecutingAssembly());

        // ensure permissions
        ProcessUtil.Run("chmod", "+x ./Launch.sh");
        ProcessUtil.Run("chmod", "+x ./Update.sh");
        ProcessUtil.Run("chmod", "+x ./PostKill.sh");
        
        ProcessUtil.Run("chmod", "+x ./Nvidia_Install_Nouveau.sh");
        ProcessUtil.Run("chmod", "+x ./Nvidia_Install_Proprietary.sh");

        ProcessUtil.Run("chmod", "+x ./Start_Gamescope.sh");
        ProcessUtil.Run("chmod", "+x ./Start_Cage.sh");
        ProcessUtil.Run("chmod", "+x ./Start_Labwc.sh");
        ProcessUtil.Run("chmod", "+x ./Start_X11.sh");
        
        // configure X11
        const string x11ConfigFile = "/home/gamer/.xinitrc";
        using (var writer = new StreamWriter(x11ConfigFile))
        {
            writer.WriteLine("#!/bin/bash");
            writer.WriteLine("/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Start_X11.sh");
        }
        ProcessUtil.Run("chmod", "+x " + x11ConfigFile);

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
            serviceProcess.StartInfo.Arguments = "./ReignOS.Service";
            serviceProcess.StartInfo.RedirectStandardOutput = true;
            serviceProcess.StartInfo.RedirectStandardError = true;
            serviceProcess.OutputDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    string value = args.Data;
                    if (value.Contains("SET_VOLUME_DOWN"))
                    {
                        ProcessUtil.Run("amixer", "set Master 5%-");
                    }
                    else if (value.Contains("SET_VOLUME_UP"))
                    {
                        ProcessUtil.Run("amixer", "set Master 5%+");
                    }
                    lock (Log.lockObj) Console.WriteLine(value);
                }
            };
            serviceProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    string value = args.Data;
                    lock (Log.lockObj) Console.WriteLine(value);
                }
            };
            serviceProcess.Start();
            
            serviceProcess.BeginOutputReadLine();
            serviceProcess.BeginErrorReadLine();
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
            goto SHUTDOWN;
        }
        Thread.Sleep(1000);// give service a sec to config anything needed before launching compositor

        // process args
        var compositor = Compositor.None;
        bool useControlCenter = false;
        foreach (string arg in args)
        {
            if (arg == "--gamescope") compositor = Compositor.Gamescope;
            else if (arg == "--cage") compositor = Compositor.Cage;
            else if (arg == "--labwc") compositor = Compositor.Labwc;
            else if (arg == "--x11") compositor = Compositor.X11;
            else if (arg == "--use-controlcenter") useControlCenter = true;
        }

        // manage interfaces
        while (true)
        {
            bool compositorRan = true;
            try
            {
                switch (compositor)
                {
                    case Compositor.None:
                        if (!useControlCenter)
                        {
                            Log.WriteLine("No Compositor specified (sleeping)");
                            Thread.Sleep(6000);// sleep for 6 seconds to allow for service bootup testing
                        }
                        compositorRan = false;
                        break;

                    case Compositor.Gamescope: StartCompositor_Gamescope(); break;
                    case Compositor.Cage: StartCompositor_Cage(); break;
                    case Compositor.Labwc: StartCompositor_Labwc(); break;
                    case Compositor.X11: StartCompositor_X11(); break;
                }
            }
            catch (Exception e)
            {
                Log.WriteLine("Failed to start compositor");
                Log.WriteLine(e);
            }
            if (compositor == Compositor.None && !useControlCenter) break;
            
            // wait and check if service closed
            if (compositorRan)
            {
                Log.WriteLine("Waiting...");
                Thread.Sleep(2000);
                if (serviceProcess.HasExited)
                {
                    Log.WriteLine("Service has exited on its own");
                    break;
                }
            }

            // start control center
            if (useControlCenter)
            {
                Log.WriteLine("Starting Cage with ReignOS.ControlCenter...");
                string result = ProcessUtil.Run("cage", "./ReignOS.ControlCenter", out exitCode);// start ControlCenter
                Console.WriteLine(result);
                if (exitCode == 0) break;
                else if (exitCode == 1) compositor = Compositor.Gamescope;
                else if (exitCode == 2) compositor = Compositor.Cage;
                else if (exitCode == 3) compositor = Compositor.Labwc;
                else if (exitCode == 4) compositor = Compositor.X11;
                else break;// exit with control-center exit-code

                // reset things for new compositor
                exitCode = 0;
                ProcessUtil.Wait("cage", 6);// wait for cage
                ProcessUtil.KillHard("cage", true, out _);// kill cage in case its stuck
            }
        }

        // stop service
        SHUTDOWN:;
        ProcessUtil.KillHard("udiskie", true, out _);
        if (serviceProcess != null)
        {
            if (!serviceProcess.HasExited)
            {
                Log.WriteLine("Soft Killing service");
                ProcessUtil.KillSoft("ReignOS.Service", true, out _);
                Thread.Sleep(1000);
            }

            if (!serviceProcess.HasExited)
            {
                Log.WriteLine("Hard Killing service (just in case)");
                serviceProcess.Kill();
                ProcessUtil.KillHard("ReignOS.Service", true, out _);
            }
            
            int serviceExitCode = serviceProcess.ExitCode;
            Log.WriteLine("Service ExitCode: " + serviceExitCode.ToString());
            if (exitCode == 0) exitCode = serviceExitCode;// if control-center has no exit code, we use service one
        }

        Environment.ExitCode = exitCode;
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e != null) Log.WriteLine($"Unhandled exception: {e}");
        else Log.WriteLine("Unhandled exception: Unknown");
    }

    private static void StartCompositor_Gamescope()
    {
        Log.WriteLine("Starting Gamescope with Steam...");
        string result = ProcessUtil.Run("gamescope", "-e -f --adaptive-sync --hdr-enabled --framerate-limit -- ./Start_Gamescope.sh");// start Gamescope with Steam in console mode, VRR
        Log.WriteLine(result);
    }

    private static void StartCompositor_Cage()
    {
        Log.WriteLine("Starting Cage with Steam...");
        string result = ProcessUtil.Run("cage", "-d -s -- ./Start_Cage.sh");// start Cage with Steam in console mode
        Log.WriteLine(result);
    }

    private static void StartCompositor_Labwc()
    {
        Log.WriteLine("Starting Labwc with Steam...");
        string result = ProcessUtil.Run("labwc", "--startup ./Start_Labwc.sh");// start Labwc with Steam in desktop mode
        Log.WriteLine(result);
    }

    private static void StartCompositor_X11()
    {
        Log.WriteLine("Starting X11 with Steam...");
        string result = ProcessUtil.Run("startx", "");// start X11 with Steam in console mode
        Log.WriteLine(result);
    }
}
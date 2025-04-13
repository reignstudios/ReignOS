﻿namespace ReignOS.Bootloader;
using ReignOS.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Linq;

enum Compositor
{
    None,
    Gamescope,
    Weston,
    WestonWindowed,
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
        ProcessUtil.Run("chmod", "+x ./Launch.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Update.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./InstallingMissingPackages.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./PostKill.sh", useBash:false);
        
        ProcessUtil.Run("chmod", "+x ./Nvidia_Install_Nouveau.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Nvidia_Install_Proprietary.sh", useBash:false);

        ProcessUtil.Run("chmod", "+x ./Start_ControlCenter.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Start_Gamescope.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Start_Weston.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Start_Cage.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Start_Labwc.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Start_X11.sh", useBash:false);
        
        // detect if system needs package updates
        if (PackageUpdates.NeedsUpdate() && IsOnline())
        {
            Log.WriteLine("Missing packages...");
            Environment.ExitCode = 100;
            return;
        }
        
        // configure X11
        const string x11ConfigFile = "/home/gamer/.xinitrc";
        using (var writer = new StreamWriter(x11ConfigFile))
        {
            writer.WriteLine("#!/bin/bash");
            writer.WriteLine("exec /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Start_X11.sh");
        }
        ProcessUtil.Run("chmod", "+x " + x11ConfigFile, useBash:false);

        // start auto mounting service
        ProcessUtil.KillHard("udiskie", true, out _);
        ProcessUtil.Run("udiskie", "--no-tray", out _, wait:false, useBash:false);

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
                    else if (value.StartsWith("ReignOS.Service.COMMAND: "))// service wants to run user-space command
                    {
                        string cmd = value.Replace("ReignOS.Service.COMMAND: ", "");
                        ProcessUtil.Run("bash", $"-c \"{cmd}\"", useBash:false);
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
        bool useMangoHub = false;
        bool vrr = false;
        bool hdr = false;
        int gpu = 0;
        foreach (string arg in args)
        {
            if (arg == "--gamescope") compositor = Compositor.Gamescope;
            else if (arg == "--weston") compositor = Compositor.Weston;
            else if (arg == "--weston-windowed") compositor = Compositor.WestonWindowed;
            else if (arg == "--cage") compositor = Compositor.Cage;
            else if (arg == "--labwc") compositor = Compositor.Labwc;
            else if (arg == "--x11") compositor = Compositor.X11;
            else if (arg == "--use-controlcenter") useControlCenter = true;

            else if (arg.StartsWith("--gpu-"))
            {
                string value = arg.Substring("--gpu-".Length);
                if (!int.TryParse(value, out gpu)) gpu = 0;
            }

            else if (arg == "--use-mangohub") useMangoHub = true;
            else if (arg == "--vrr") vrr = true;
            else if (arg == "--hdr") hdr = true;
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

                    case Compositor.Gamescope: StartCompositor_Gamescope(useMangoHub, vrr, hdr, gpu); break;
                    case Compositor.Weston: StartCompositor_Weston(useMangoHub, false, gpu); break;
                    case Compositor.WestonWindowed: StartCompositor_Weston(useMangoHub, true, gpu); break;
                    case Compositor.Cage: StartCompositor_Cage(useMangoHub, gpu); break;
                    case Compositor.Labwc: StartCompositor_Labwc(gpu); break;
                    case Compositor.X11: StartCompositor_X11(gpu); break;
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
                //Log.WriteLine("Starting Cage with ReignOS.ControlCenter...");
                //string result = ProcessUtil.Run("cage", "./Start_ControlCenter.sh", out exitCode, useBash:false);// start ControlCenter
                Log.WriteLine("Starting Weston with ReignOS.ControlCenter...");
                string result = ProcessUtil.Run("weston", "--shell=kiosk-shell.so --xwayland -- ./Start_ControlCenter.sh", out exitCode, useBash:false);// start ControlCenter
                Console.WriteLine(result);

                var resultValues = result.Split('\n');
                var exitCodeValue = resultValues.FirstOrDefault(x => x.Contains("EXIT_CODE: "));// get ControlCenter exit code (Weston doesn't pass this back like Cage)
                if (exitCodeValue != null)
                {
                    if (!int.TryParse(exitCodeValue.Replace("EXIT_CODE: ", ""), out exitCode)) exitCode = 0;
                }

                if (exitCode == 0) break;
                else if (exitCode == 1) compositor = Compositor.Gamescope;
                else if (exitCode == 2) compositor = Compositor.Weston;
                else if (exitCode == 3) compositor = Compositor.WestonWindowed;
                else if (exitCode == 4) compositor = Compositor.Cage;
                else if (exitCode == 5) compositor = Compositor.Labwc;
                else if (exitCode == 6) compositor = Compositor.X11;
                else break;// exit with control-center exit-code

                // reset things for new compositor
                exitCode = 0;
                //ProcessUtil.Wait("cage", 6);// wait for cage
                //ProcessUtil.KillHard("cage", true, out _);// kill cage in case its stuck
                ProcessUtil.Wait("weston", 6);// wait for cage
                ProcessUtil.KillHard("weston", true, out _);// kill cage in case its stuck
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

    private static string GetGPUArg(int gpu)
    {
        return "VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/nvidia_icd.json prime-run ";
        //return gpu >= 1 ? $"DRI_PRIME={gpu} WLR_DRM_DEVICES=/dev/dri/card{gpu} prime-run " : "";//__NV_PRIME_RENDER_OFFLOAD={gpu} __GLX_VENDOR_LIBRARY_NAME=nvidia __VK_LAYER_NV_optimus=NVIDIA_only VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/nvidia_icd.json
    }

    private static void StartCompositor_Gamescope(bool useMangoHub, bool vrr, bool hdr, int gpu)
    {
        Log.WriteLine("Starting Gamescope with Steam...");
        string useMangoHubArg = useMangoHub ? "MANGOHUD=1 " : "";
        string useMangoHubArg2 = useMangoHub ? " --mangoapp" : "";
        string vrrArg = vrr ? " --adaptive-sync" : "";
        string hdrArg = hdr ? " --hdr-enabled" : "";
        string gpuArg = GetGPUArg(gpu);
        string result = ProcessUtil.Run($"{gpuArg}{useMangoHubArg}gamescope", $"-e -f{useMangoHubArg2}{vrrArg}{hdrArg} -- ./Start_Gamescope.sh", useBash:true);// --framerate-limit
        Log.WriteLine(result);
    }

    private static void StartCompositor_Weston(bool useMangoHub, bool windowedMode, int gpu)
    {
        Log.WriteLine("Starting Weston with Steam...");
        string useMangoHubArg = useMangoHub ? " --use-mangohub" : "";
        string windowedModeArg = !windowedMode ? "--shell=kiosk-shell.so " : "";
        string windowedModeArg2 = windowedMode ? " --windowed-mode" : "";
        string gpuArg = GetGPUArg(gpu);
        string gpuArg2 = "";//gpu >= 1 ? $"--drm-device=card{gpu} " : "";
        string result = ProcessUtil.Run($"{gpuArg}weston", $"{gpuArg2}{windowedModeArg}--xwayland -- ./Start_Weston.sh{useMangoHubArg}{windowedModeArg2}", useBash:true);
        Log.WriteLine(result);
    }
    
    private static void StartCompositor_Cage(bool useMangoHub, int gpu)
    {
        Log.WriteLine("Starting Cage with Steam...");
        string useMangoHubArg = useMangoHub ? " --use-mangohub" : "";
        string gpuArg = GetGPUArg(gpu);
        string result = ProcessUtil.Run($"{gpuArg}cage", $"-d -s -- ./Start_Cage.sh{useMangoHubArg}", useBash:true);
        Log.WriteLine(result);
    }

    private static void StartCompositor_Labwc(int gpu)
    {
        Log.WriteLine("Starting Labwc with Steam...");
        string gpuArg = GetGPUArg(gpu);
        string result = ProcessUtil.Run($"{gpuArg}labwc", "--startup ./Start_Labwc.sh", useBash:true);
        Log.WriteLine(result);
    }

    private static void StartCompositor_X11(int gpu)
    {
        Log.WriteLine("Starting X11 with Steam...");
        string gpuArg = GetGPUArg(gpu);
        string result = ProcessUtil.Run($"{gpuArg}startx", "", useBash:true);
        Log.WriteLine(result);
    }

    public static bool IsOnline()
    {
        string result = ProcessUtil.Run("ping", "-c 1 google.com", consoleLogOut:false, useBash:false);
        return result.Contains("1 received");
    }

    //List card infos
    //ls /dev/dri # cards (card0, card1 etc)
    //lspci | grep -E "VGA|3D" # card names
    //ls -l /sys/class/drm/card*/device/driver # driver names

    //nvidia-settings # open nvidia control-panel

    //DRI_PRIME=1 WLR_DRM_DEVICES=/dev/dri/card1 weston # mesa
    //weston --drm-device=card1 # other weston option
    //prime-run weston #proprietary

    //__NV_PRIME_RENDER_OFFLOAD=1 __GLX_VENDOR_LIBRARY_NAME=nvidia __VK_LAYER_NV_optimus=NVIDIA_only # same as prime-run
}
namespace ReignOS.Bootloader;
using ReignOS.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Linq;

enum ControlCenterCompositor
{
    Weston,
    Cage,
    X11,
    KDE_G
}

enum Compositor
{
    None,
    Gamescope,
    Weston,
    WestonWindowed,
    Cage,
    Labwc,
    X11,
    KDE,
    KDE_X11,
    KDE_G
}

internal class Program
{
    private static void Main(string[] args)
    {
        int exitCode = 0;

        Log.Init("ReignOS.Bootloader");
        Log.WriteLine("Bootloader started: " + VersionInfo.version);
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        LibraryResolver.Init(Assembly.GetExecutingAssembly());
        
        // process args
        var controlCenterCompositor = ControlCenterCompositor.Weston;
        var compositor = Compositor.None;
        bool useControlCenter = false;
        bool useMangoHub = false;
        bool vrr = false;
        bool hdr = false;
        bool disableSteamGPU = false;
        bool disableSteamDeck = false;
        var inputMode = InputMode.ReignOS;
        int gpu = 0;
        var screenRotation = ScreenRotation.Unset;
        bool forceControlCenter = false;
        int displayIndex = 0;
        int displayWidth = 0, displayHeight = 0;
        foreach (string arg in args)
        {
            if (arg == "--use-controlcenter") useControlCenter = true;
            else if (arg == "--controlcenter-weston") controlCenterCompositor = ControlCenterCompositor.Weston;
            else if (arg == "--controlcenter-cage") controlCenterCompositor = ControlCenterCompositor.Cage;
            else if (arg == "--controlcenter-x11") controlCenterCompositor = ControlCenterCompositor.X11;
            else if (arg == "--controlcenter-kde-g") controlCenterCompositor = ControlCenterCompositor.KDE_G;

            else if (arg == "--gamescope") compositor = Compositor.Gamescope;
            else if (arg == "--weston") compositor = Compositor.Weston;
            else if (arg == "--weston-windowed") compositor = Compositor.WestonWindowed;
            else if (arg == "--cage") compositor = Compositor.Cage;
            else if (arg == "--labwc") compositor = Compositor.Labwc;
            else if (arg == "--x11") compositor = Compositor.X11;
            else if (arg == "--kde") compositor = Compositor.KDE;
            else if (arg == "--kde-x11") compositor = Compositor.KDE_X11;
            else if (arg == "--kde-g") compositor = Compositor.KDE_G;

            else if (arg.StartsWith("--gpu-"))
            {
                string value = arg.Substring("--gpu-".Length);
                if (!int.TryParse(value, out gpu)) gpu = 0;
            }

            else if (arg == "--use-mangohub") useMangoHub = true;
            else if (arg == "--vrr") vrr = true;
            else if (arg == "--hdr") hdr = true;
            else if (arg == "--disable-steam-gpu") disableSteamGPU = true;
            else if (arg == "--disable-steam-deck") disableSteamDeck = true;

            else if (arg == "--rotation-default") screenRotation = ScreenRotation.Default;
            else if (arg == "--rotation-left") screenRotation = ScreenRotation.Left;
            else if (arg == "--rotation-right") screenRotation = ScreenRotation.Right;
            else if (arg == "--rotation-flip") screenRotation = ScreenRotation.Flip;

            else if (arg == "--input-reignos") inputMode = InputMode.ReignOS;
            else if (arg == "--input-inputplumber") inputMode = InputMode.InputPlumber;
            else if (arg == "--input-disable") inputMode = InputMode.Disabled;
            
            else if (arg.StartsWith("--display-index="))
            {
                var parts = arg.Split('=');
                if (parts.Length == 2 && !int.TryParse(parts[1], out displayIndex)) displayIndex = 0;
            }
            else if (arg.StartsWith("--resolution"))
            {
                var parts = arg.Split('=');
                if (parts.Length == 2)
                {
                    parts = parts[1].Split('x');
                    if (!int.TryParse(parts[0], out displayWidth)) displayWidth = 0;
                    if (!int.TryParse(parts[1], out displayHeight)) displayHeight = 0;
                }
            }
            
            else if (arg == "--force-controlcenter") forceControlCenter = true;
        }
        
        if (forceControlCenter) compositor = Compositor.None;

        // ensure permissions
        ProcessUtil.Run("chmod", "+x ./Launch.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Update.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./InstallingMissingPackages.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./PostKill.sh", useBash:false);
        
        ProcessUtil.Run("chmod", "+x ./Nvidia_Install_Nouveau.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Nvidia_Install_Proprietary.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./AMD_Install_Mesa.sh", useBash: false);
        ProcessUtil.Run("chmod", "+x ./AMD_Install_AMDVLK.sh", useBash: false);
        ProcessUtil.Run("chmod", "+x ./AMD_Install_Proprietary.sh", useBash: false);

        ProcessUtil.Run("chmod", "+x ./Start_ControlCenter.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Start_Gamescope.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Start_Weston.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Start_Cage.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Start_Labwc.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Start_X11.sh", useBash:false);
        ProcessUtil.Run("chmod", "+x ./Start_KDE-G.sh", useBash: false);

        // detect if system needs package updates
        if (PackageUpdates.CheckUpdates() && IsOnline())
        {
            Log.WriteLine("Missing packages...");
            Environment.ExitCode = 100;
            return;
        }

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
            string inputArg;
            switch (inputMode)
            {
                case InputMode.InputPlumber: inputArg = " --input-inputplumber"; break;
                case InputMode.Disabled: inputArg = " --input-disable"; break;
                default: inputArg = " --input-reignos"; break;
            }
            serviceProcess.StartInfo.Arguments = $"-- ./ReignOS.Service{inputArg}";
            serviceProcess.StartInfo.RedirectStandardOutput = true;
            serviceProcess.StartInfo.RedirectStandardError = true;
            serviceProcess.StartInfo.RedirectStandardInput = true;
            serviceProcess.OutputDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    string value = args.Data;
                    if (value.Contains("SET_VOLUME_DOWN"))
                    {
                        //ProcessUtil.Run("amixer", "set Master 5%-");
                        ProcessUtil.Run("pactl", "set-sink-volume @DEFAULT_SINK@ -5%");
                    }
                    else if (value.Contains("SET_VOLUME_UP"))
                    {
                        //ProcessUtil.Run("amixer", "set Master 5%+");
                        ProcessUtil.Run("pactl", "set-sink-volume @DEFAULT_SINK@ +5%");
                    }
                    else if (value.StartsWith("ReignOS.Service.COMMAND: "))// service wants to run user-space command
                    {
                        string cmd = value.Replace("ReignOS.Service.COMMAND: ", "");
                        ProcessUtil.Run("bash", $"-c \"{cmd}\"", useBash:false);
                    }
                }
            };
            serviceProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    string value = args.Data;
                    Log.WriteLine(value);
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
        Log.WriteLine("test"); return;
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

                    case Compositor.Gamescope: StartCompositor_Gamescope(useMangoHub, vrr, hdr, disableSteamGPU, disableSteamDeck, gpu, displayWidth, displayHeight, screenRotation); break;
                    case Compositor.Weston: StartCompositor_Weston(useMangoHub, false, disableSteamGPU, disableSteamDeck, gpu); break;
                    case Compositor.WestonWindowed: StartCompositor_Weston(useMangoHub, true, disableSteamGPU, disableSteamDeck, gpu); break;
                    case Compositor.Cage: StartCompositor_Cage(useMangoHub, disableSteamGPU, disableSteamDeck, gpu); break;
                    case Compositor.Labwc: StartCompositor_Labwc(disableSteamGPU, gpu); break;
                    case Compositor.X11: StartCompositor_X11(useMangoHub, disableSteamGPU, disableSteamDeck, gpu); break;
                    case Compositor.KDE: StartCompositor_KDE(useMangoHub, disableSteamGPU, disableSteamDeck, gpu, false, false, serviceProcess); break;
                    case Compositor.KDE_X11: StartCompositor_KDE(useMangoHub, disableSteamGPU, disableSteamDeck, gpu, true, false, serviceProcess); break;
                    case Compositor.KDE_G: StartCompositor_KDE(useMangoHub, disableSteamGPU, disableSteamDeck, gpu, false, true, serviceProcess); break;
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
                Thread.Sleep(5000);
                if (serviceProcess.HasExited)
                {
                    Log.WriteLine("Service has exited on its own");
                    break;
                }
            }

            // start control center
            if (useControlCenter)
            {
                string result = string.Empty;
                if (controlCenterCompositor == ControlCenterCompositor.Weston)
                {
                    Log.WriteLine("Starting Weston with ReignOS.ControlCenter...");
                    result = ProcessUtil.Run("weston", "--shell=kiosk-shell.so --xwayland -- ./Start_ControlCenter.sh -weston", out exitCode, useBash:true);// start ControlCenter
                }
                else if (controlCenterCompositor == ControlCenterCompositor.Cage)
                {
                    Log.WriteLine("Starting Cage with ReignOS.ControlCenter...");
                    result = ProcessUtil.Run("cage", "-d -s -- ./Start_ControlCenter.sh -cage", out exitCode, useBash:true);// start ControlCenter
                }
                else if (controlCenterCompositor == ControlCenterCompositor.X11)
                {
                    Log.WriteLine("Starting X11 with ReignOS.ControlCenter...");
                    ConfigureX11("/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Start_ControlCenter.sh -x11");
                    result = ProcessUtil.Run("startx", "", useBash:false);// start ControlCenter
                }
                else if (controlCenterCompositor == ControlCenterCompositor.KDE_G)
                {
                    Log.WriteLine("Starting KDE-G with ReignOS.ControlCenter...");
                    result = ProcessUtil.Run("Start_ControlCenter.sh", "-kde-g", out exitCode, useBash: true);// start ControlCenter
                }

                var resultValues = result.Split('\n');
                var exitCodeValue = resultValues.FirstOrDefault(x => x.Contains("EXIT_CODE: "));// get ControlCenter exit code (Weston doesn't pass this back like Cage)
                if (exitCodeValue != null)
                {
                    if (!int.TryParse(exitCodeValue.Replace("EXIT_CODE: ", ""), out exitCode)) exitCode = 0;
                }

                bool exitLoop = false;
                if (exitCode == 0) exitLoop = true;
                else if (exitCode == 1) compositor = Compositor.Gamescope;
                else if (exitCode == 2) compositor = Compositor.Weston;
                else if (exitCode == 3) compositor = Compositor.WestonWindowed;
                else if (exitCode == 4) compositor = Compositor.Cage;
                else if (exitCode == 5) compositor = Compositor.Labwc;
                else if (exitCode == 6) compositor = Compositor.X11;
                else if (exitCode == 7) compositor = Compositor.KDE;
                else if (exitCode == 8) compositor = Compositor.KDE_X11;
                else if (exitCode == 9) compositor = Compositor.KDE_G;
                else exitLoop = true;// exit with control-center exit-code

                // wait for soft shutdown
                if (controlCenterCompositor == ControlCenterCompositor.Weston)
                {
                    ProcessUtil.Wait("weston", 6);// wait for cage
                    ProcessUtil.KillHard("weston", true, out _);// kill cage in case its stuck
                }
                else if (controlCenterCompositor == ControlCenterCompositor.Cage)
                {
                    ProcessUtil.Wait("cage", 6);// wait for cage
                    ProcessUtil.KillHard("cage", true, out _);// kill cage in case its stuck
                }
                else if (controlCenterCompositor == ControlCenterCompositor.X11)
                {
                    Thread.Sleep(5);// just wait a bit
                }

                // reset things for new compositor
                if (exitLoop)
                {
                    break;
                }
                else
                {
                    exitCode = 0;
                    Thread.Sleep(5000);// wait a little before loading a new compositor
                }
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
        Log.Close();
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e != null) Log.WriteLine($"Unhandled exception: {e}");
        else Log.WriteLine("Unhandled exception: Unknown");
    }

    private static string GetGPUArg(int gpu)
    {
        if (gpu == 100)
        {
            return "prime-run ";
        }

        return "";
    }

    private static void StartCompositor_Gamescope(bool useMangoHub, bool vrr, bool hdr, bool disableSteamGPU, bool disableSteamDeck, int gpu, int displayWidth, int displayHeight, ScreenRotation screenRotation)
    {
        Log.WriteLine("Starting Gamescope with Steam...");
        DisableX11();
        string useMangoHubArg = useMangoHub ? " --mangoapp" : "";
        string vrrArg = vrr ? " --adaptive-sync" : "";
        string hdrArg = hdr ? " --hdr-enabled" : "";
        string steamGPUArg = disableSteamGPU ? " --disable-steam-gpu" : "";
        string steamDeckArg = disableSteamDeck ? " --disable-steam-deck" : "";
        string gpuArg = GetGPUArg(gpu);
        string rotArg = "";
        switch (screenRotation)
        {
            case ScreenRotation.Default: rotArg = " --force-orientation normal"; break;
            case ScreenRotation.Left: rotArg = " --force-orientation left"; break;
            case ScreenRotation.Right: rotArg = " --force-orientation right"; break;
            case ScreenRotation.Flip: rotArg = " --force-orientation upsidedown"; break;
        }

        string displayRezArg = (displayWidth > 0 && displayHeight > 0) ? $" -W {displayWidth} -H {displayHeight}" : "";
        ProcessUtil.Run($"{gpuArg}gamescope", $"-e -f{useMangoHubArg}{vrrArg}{hdrArg}{rotArg}{displayRezArg} -- ./Start_Gamescope.sh{steamGPUArg}{steamDeckArg}", useBash:true, verboseLog:true);// --framerate-limit
    }

    private static void StartCompositor_Weston(bool useMangoHub, bool windowedMode, bool disableSteamGPU, bool disableSteamDeck, int gpu)
    {
        Log.WriteLine("Starting Weston with Steam...");
        DisableX11();
        string useMangoHubArg = useMangoHub ? " --use-mangohub" : "";
        string windowedModeArg = !windowedMode ? "--shell=kiosk-shell.so " : "";
        string windowedModeArg2 = windowedMode ? " --windowed-mode" : "";
        string steamGPUArg = disableSteamGPU ? " --disable-steam-gpu" : "";
        string steamDeckArg = disableSteamDeck ? " --disable-steam-deck" : "";
        string gpuArg = GetGPUArg(gpu);
        ProcessUtil.Run($"{gpuArg}weston", $"{windowedModeArg}--xwayland -- ./Start_Weston.sh{useMangoHubArg}{windowedModeArg2}{steamGPUArg}{steamDeckArg}", useBash:true, verboseLog: true);
    }
    
    private static void StartCompositor_Cage(bool useMangoHub, bool disableSteamGPU, bool disableSteamDeck, int gpu)
    {
        Log.WriteLine("Starting Cage with Steam...");
        DisableX11();
        string useMangoHubArg = useMangoHub ? " --use-mangohub" : "";
        string steamGPUArg = disableSteamGPU ? " --disable-steam-gpu" : "";
        string steamDeckArg = disableSteamDeck ? " --disable-steam-deck" : "";
        string gpuArg = GetGPUArg(gpu);
        ProcessUtil.Run($"{gpuArg}cage", $"-d -s -- ./Start_Cage.sh{useMangoHubArg}{steamGPUArg}{steamDeckArg}", useBash:true, verboseLog: true);
    }

    private static void StartCompositor_Labwc(bool disableSteamGPU, int gpu)
    {
        Log.WriteLine("Starting Labwc with Steam...");
        DisableX11();
        string steamGPUArg = disableSteamGPU ? " --disable-steam-gpu" : "";
        string gpuArg = GetGPUArg(gpu);
        ProcessUtil.Run($"{gpuArg}labwc", $"--startup ./Start_Labwc.sh{steamGPUArg}", useBash:true, verboseLog: true);
    }

    private static void StartCompositor_X11(bool useMangoHub, bool disableSteamGPU, bool disableSteamDeck, int gpu)
    {
        Log.WriteLine("Starting X11 with Steam...");

        string useMangoHubArg = useMangoHub ? " --use-mangohub" : "";
        string steamGPUArg = disableSteamGPU ? " --disable-steam-gpu" : "";
        string steamDeckArg = disableSteamDeck ? " --disable-steam-deck" : "";
        string gpuArg = GetGPUArg(gpu);
        ConfigureX11($"{gpuArg}/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Start_X11.sh{useMangoHubArg}{steamGPUArg}{steamDeckArg}");
        ProcessUtil.Run("startx", "", useBash:false, verboseLog: true);
    }

    private static void StartCompositor_KDE(bool useMangoHub, bool disableSteamGPU, bool disableSteamDeck, int gpu, bool useX11, bool useGMode, Process serviceProcess)
    {
        Log.WriteLine("Starting KDE...");
        string gpuArg = GetGPUArg(gpu);
        if (useGMode)
        {
            DisableX11();
            string useMangoHubArg = useMangoHub ? " --use-mangohub" : "";
            string steamGPUArg = disableSteamGPU ? " --disable-steam-gpu" : "";
            string steamDeckArg = disableSteamDeck ? " --disable-steam-deck" : "";
            ProcessUtil.Run($"{gpuArg}./Start_KDE-G.sh{useMangoHubArg}{steamGPUArg}{steamDeckArg}", "", useBash:true, verboseLog:true);
        }
        else if (useX11)
        {
            serviceProcess.StandardInput.WriteLine("stop-inhibit");
            ConfigureX11($"{gpuArg}startplasma-x11");
            ProcessUtil.Run("startx", "", useBash:false, verboseLog:true);
            serviceProcess.StandardInput.WriteLine("start-inhibit");
        }
        else
        {
            serviceProcess.StandardInput.WriteLine("stop-inhibit");
            DisableX11();
            ProcessUtil.Run($"{gpuArg}startplasma-wayland", "", useBash:true, verboseLog:true);
            serviceProcess.StandardInput.WriteLine("start-inhibit");
        }
    }

    public static bool IsOnline()
    {
        string result = ProcessUtil.Run("ping", "-c 1 -W 4 google.com", log:false, useBash:false);
        return result.Contains("1 received");
    }

    private static void ConfigureX11(string launch)
    {
        const string x11ConfigFile = "/home/gamer/.xinitrc";
        using (var writer = new StreamWriter(x11ConfigFile))
        {
            writer.WriteLine("#!/bin/bash");
            writer.WriteLine();
            writer.WriteLine(launch);
        }
        ProcessUtil.Run("chmod", "+x " + x11ConfigFile, useBash:false);
    }

    private static void DisableX11()
    {
        const string x11ConfigFile = "/home/gamer/.xinitrc";
        using (var writer = new StreamWriter(x11ConfigFile))
        {
            writer.WriteLine("");
        }
        ProcessUtil.Run("chmod", "+x " + x11ConfigFile, useBash: false);
    }
}
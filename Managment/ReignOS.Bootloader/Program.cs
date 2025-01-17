namespace ReignOS.Bootloader;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ReignOS.Core;

enum Compositor
{
    Cage,
    Gamescope
}

internal class Program
{
    static void Main(string[] args)
    {
        Log.WriteLine("Bootloader started");

        // kill service if its currently running
        ProcessUtil.Kill("ReignOS.Service", true);
        
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
                    if (args.Data.Contains("[sudo] password for"))
                    {
                        serviceProcess.StandardInput.WriteLine("gamer");
                        serviceProcess.StandardInput.Flush();
                    }
                    Log.WriteLine(args.Data);
                }
            };
            serviceProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args != null && args.Data != null)
                {
                    if (args.Data.Contains("[sudo] password for"))
                    {
                        serviceProcess.StandardInput.WriteLine("gamer");
                        serviceProcess.StandardInput.Flush();
                    }
                    Log.WriteLine(args.Data);
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

        // start compositor
        var compositor = Compositor.Cage;
        foreach (string arg in args)
        {
            if (arg == "--cage")
            {
                compositor = Compositor.Cage;
            }
            else if (arg == "--gamescope")
            {
                compositor = Compositor.Gamescope;
            }
        }

        try
        {
            switch (compositor)
            {
                case Compositor.Cage: StartCompositor_Cage(); break;
                case Compositor.Gamescope: StartCompositor_Gamescope(); break;
            }
        }
        catch (Exception e)
        {
            Log.WriteLine("Failed to start compositor");
            Log.WriteLine(e);
        }

        // stop service
        SHUTDOWN:;
        if (serviceProcess != null && !serviceProcess.HasExited)
        {
            Log.WriteLine("Killing service");
            serviceProcess.Kill();
        }
    }
    
    private static void StartCompositor_Cage()
    {
        var envVars = new Dictionary<string, string>()
        {
            { "CUSTOM_REFRESH_RATES", "30,60,120" },
            { "STEAM_DISPLAY_REFRESH_LIMITS", "30,60,120" }
        };

        string launchArg = "steam -bigpicture -steamdeck";
        launchArg += " & unclutter -idle 3";// hide mouse after 3 seconds
        //launchArg += " & wlr-randr --output eDP-1 --transform 90 --adaptive-sync enabled";// TODO: rotate screen or enable VRR
        string result = ProcessUtil.Run("cage", "-- " + launchArg, enviromentVars:envVars, wait:true);// start Cage with Steam in console mode
        Log.WriteLine(result);
    }

    private static void StartCompositor_Gamescope()
    {
        var envVars = new Dictionary<string, string>()
        {
            { "CUSTOM_REFRESH_RATES", "30,60,120" },
            { "STEAM_DISPLAY_REFRESH_LIMITS", "30,60,120" }
        };
        string result = ProcessUtil.Run("gamescope", "-e -f --adaptive-sync -- steam -bigpicture -steamdeck", enviromentVars:envVars, wait:true);// start Gamescope with Steam in console mode, VRR
        Log.WriteLine(result);
    }
}
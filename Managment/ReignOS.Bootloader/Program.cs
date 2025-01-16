namespace ReignOS.Bootloader;
using ReignOS.Core;
using System.Diagnostics;

enum Compositor
{
    Cage,
    Gamescope
}

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("ReignOS.Bootloader started");

        // kill service if its currently running
        ProcessUtil.Kill("ReignOS.Service");
        
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
                    Console.WriteLine(args.Data);
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
                    Console.WriteLine(args.Data);
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
            Console.WriteLine(e);
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
            Console.WriteLine("Failed to start compositor");
            Console.WriteLine(e);
        }

        // stop service
        SHUTDOWN:;
        if (serviceProcess != null && !serviceProcess.HasExited)
        {
            serviceProcess.Kill();
        }
    }
    
    private static void StartCompositor_Cage()
    {
        /*var envVars = new Dictionary<string, string>()
        {
            { "CUSTOM_REFRESH_RATES", "30,60,120" },
            { "STEAM_DISPLAY_REFRESH_LIMITS", "30,60,120" }
        };*/

        string result = ProcessUtil.Run("cage", "-- steam -bigpicture -steamdeck", enviromentVars:null, wait:true);// start Cage with Steam in console mode
        Console.WriteLine(result);
        //ProcessUtil.Run("wlr-randr", "--output eDP-1 --transform 90", wait:true);// tell wayland/cage to rotate screen
        //ProcessUtil.Run("unclutter", "-idle 3", wait:false);// hide cursor after 3 seconds
    }

    private static void StartCompositor_Gamescope()
    {
        var envVars = new Dictionary<string, string>()
        {
            { "CUSTOM_REFRESH_RATES", "30,60,120" },
            { "STEAM_DISPLAY_REFRESH_LIMITS", "30,60,120" }
        };
        string result = ProcessUtil.Run("gamescope", "-e -f --adaptive-sync -- steam -bigpicture -steamdeck", enviromentVars:envVars, wait:true);// start Gamescope with Steam in console mode, VRR
        Console.WriteLine(result);
    }
}
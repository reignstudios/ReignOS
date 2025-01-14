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
    private static Process serviceProcess;
    
    static void Main(string[] args)
    {
        Console.WriteLine("ReignOS.Bootloader started");
        
        // start service
        serviceProcess = new Process();
        try
        {
            serviceProcess.StartInfo.UseShellExecute = false;
            serviceProcess.StartInfo.FileName = "sudo";
            serviceProcess.StartInfo.Arguments = "-S ReignOS.Service";
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
            
            serviceProcess.WaitForExit();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
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

        serviceProcess.Dispose();
    }
    
    private static void StartCompositor_Cage()
    {
        //Environment.GetEnvironmentVariables();

        /*var envVars = new Dictionary<string, string>()
        {
            { "CUSTOM_REFRESH_RATES", "30,60,120" },
            { "STEAM_DISPLAY_REFRESH_LIMITS", "30,60,120" }
        };*/

        string result = ProcessUtil.Run("cage", "steam -bigpicture -steamdeck", enviromentVars:null, wait:true);// start Cage with Steam in console mode
        Console.WriteLine(result);

        //const string launchCmd = "cage -- steam -bigpicture -steamdeck";
        //const string launchUserCmd = $"su - gamer -c \"{launchCmd}\"";
        //string result = ProcessUtil.Run("bash", $"-c \"{launchUserCmd}\"", enviromentVars:null, wait:false);// start Cage with Steam in console mode
        //Console.WriteLine(result);

        /*string gamerUserVars = ProcessUtil.Run("su", "- gamer -c \"printenv\"");
        var gamerUserVarsValues = gamerUserVars.Split(Environment.NewLine);
        foreach (var v in gamerUserVarsValues)
        {
            var values = v.Split('=');
            if (values.Length >= 2) envVars.Add(values[0], values[1]);
        }

        const string launchCmd = "cage -- steam -bigpicture -steamdeck";
        //const string launchUserCmd = $"-c \"{launchCmd}\"";
        string result = ProcessUtil.Run("su", $"- gamer -c \"{launchCmd}\"", enviromentVars:envVars, wait:true);// start Cage with Steam in console mode
        Console.WriteLine(result);*/

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
        const string launchCmd = "runuser -u gamer -- gamescope -e -f --adaptive-sync -- steam -bigpicture -steamdeck";
        ProcessUtil.Run("bash", $"-c \"{launchCmd}\"", enviromentVars:envVars, wait:true);// start Gamescope with Steam in console mode, VRR
    }
}
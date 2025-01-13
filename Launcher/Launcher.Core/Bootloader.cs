﻿using HidSharp;
using Launcher.Core.Hardware;

namespace Launcher.Core
{
    public enum HardwareType
    {
        Unknown,
        MSI_Claw_A1M,
        MSI_Claw
    }

    public enum Compositor
    {
        Cage,
        Gamescope
    }

    public static class Bootloader
    {
        public static void Start(Compositor compositor)
        {
            // start compositor
            switch (compositor)
            {
                case Compositor.Cage: StartCompositor_Cage(); break;
                case Compositor.Gamescope: StartCompositor_Gamescope(); break;
                default: throw new NotImplementedException();
            }
        }

        private static void StartCompositor_Cage()
        {
            //Environment.GetEnvironmentVariables();

            var envVars = new Dictionary<string, string>()
            {
                { "CUSTOM_REFRESH_RATES", "30,60,120" },
                { "STEAM_DISPLAY_REFRESH_LIMITS", "30,60,120" }
            };

            //const string launchCmd = "cage -- steam -bigpicture -steamdeck";
            //const string launchUserCmd = $"su - gamer -c \"{launchCmd}\"";
            //string result = ProcessUtil.Run("bash", $"-c \"{launchUserCmd}\"", enviromentVars:null, wait:false);// start Cage with Steam in console mode
            //Console.WriteLine(result);

            string gamerUserVars = ProcessUtil.Run("su", "- gamer -c \"printenv\"");
            var gamerUserVarsValues = gamerUserVars.Split(Environment.NewLine);
            foreach (var v in gamerUserVarsValues)
            {
                var values = v.Split('=');
                if (values.Length >= 2) envVars.Add(values[0], values[1]);
            }

            const string launchCmd = "cage -- steam -bigpicture -steamdeck";
            //const string launchUserCmd = $"-c \"{launchCmd}\"";
            string result = ProcessUtil.Run("su", $"- gamer -c \"{launchCmd}\"", enviromentVars:envVars, wait:true);// start Cage with Steam in console mode
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
            const string launchCmd = "runuser -u gamer -- gamescope -e -f --adaptive-sync -- steam -bigpicture -steamdeck";
            ProcessUtil.Run("bash", $"-c \"{launchCmd}\"", enviromentVars:envVars, wait:false);// start Gamescope with Steam in console mode, VRR
        }
    }
}

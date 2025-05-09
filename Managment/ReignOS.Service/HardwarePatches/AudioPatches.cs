﻿using ReignOS.Core;
using System.IO;
using System.Text;

namespace ReignOS.Service.HardwarePatches
{
	static class AudioPatches
	{
        /// <summary>
        /// Fixes audio after sleep on hardware: MSI-Claw
        /// </summary>
		public static void Fix1(bool apply)
		{
			string path = "/home/gamer/.config/wireplumber/wireplumber.conf.d";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, "80-alsa-headroom.conf");
            if (File.Exists(path))
            {
                if (apply) return;// already patched

                // remove
                File.Delete(path);
                return;
            }

            const string config =
@"monitor.alsa.rules = [
  {
    matches = [
      {
        node.name = ""alsa_output.pci-0000_00_1f.3-platform-skl_hda_dsp_generic.HiFi__Speaker__sink""
      }
    ]
    actions = {
      update-props = {
        api.alsa.headroom = 1024
      }
    }
  }
]";
            File.WriteAllText(path, config);
            Program.RunUserCmd("chown -R $USER " + path);
            Program.RunUserCmd("systemctl --user restart wireplumber");
		}
	}
}

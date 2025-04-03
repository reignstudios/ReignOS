using ReignOS.Core;
using System.IO;
using System.Text;

namespace ReignOS.Service.HardwarePatches
{
	static class AudioPatches
	{
        /// <summary>
        /// Fixes audio after sleep on hardware: MSI-Claw
        /// </summary>
		public static void Patch1()
		{
			string audioPath = "/home/gamer/.config/wireplumber/wireplumber.conf.d";
            if (!Directory.Exists(audioPath)) Directory.CreateDirectory(audioPath);
            audioPath = Path.Combine(audioPath, "80-alsa-headroom.conf");
            if (File.Exists(audioPath)) return;// already patched

            const string audioConfig =
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
            File.WriteAllText(audioPath, audioConfig);
            Program.RunUserCmd("chown -R $USER " + audioPath);
            Program.RunUserCmd("systemctl --user restart wireplumber");
		}
	}
}

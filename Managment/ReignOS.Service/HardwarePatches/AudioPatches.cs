using ReignOS.Core;
using System;
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

        public static bool InstallFirmware_aw87559()
        {
            const string rootFirmwareFolder = "/usr/lib/firmware";
            const string firmwareFolder = rootFirmwareFolder + "/aw87559";
            const string firmwareFile = firmwareFolder + "/awinic_smartk_acf.bin";
            const string firmwareFileSysLink = rootFirmwareFolder + "/aw87xxx_acf.bin";
            if (File.Exists(firmwareFile) && File.Exists(firmwareFileSysLink)) return false;

            try
            {
                Directory.CreateDirectory(firmwareFolder);
                File.Copy("/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Firmware/aw87559/awinic_smartk_acf.bin", firmwareFile, true);
                ProcessUtil.Run("ln", $"-s {firmwareFile} {firmwareFileSysLink}", useBash:false);
                return true;
            }
            catch (Exception e)
            {
                Log.WriteLine(e);
            }

            return false;
        }
    }
}

using System.IO;

namespace ReignOS.Service.HardwarePatches
{
	static class WiFiPatches
	{
		/// <summary>
        /// Fixes wifi after sleep on hardware: MSI-Claw
        /// </summary>
		public static void Fix1()
		{
			string path = "/usr/lib/systemd/system-sleep";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, "iwlwifi-sleep.sh");
            if (File.Exists(path)) return;// already patched

            const string audioConfig =
@"#!/bin/bash

case ""$1"" in
  pre)
    /usr/sbin/modprobe -r iwlmvm iwlwifi
    ;;
  post)
    /usr/sbin/modprobe iwlwifi iwlmvm
    ;;
esac";
            File.WriteAllText(path, audioConfig);
            Program.RunUserCmd("chmod +x " + path);
		}
	}
}

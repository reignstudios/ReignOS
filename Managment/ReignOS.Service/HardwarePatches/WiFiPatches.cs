using ReignOS.Core;
using System.IO;

namespace ReignOS.Service.HardwarePatches
{
	static class WiFiPatches
	{
		/// <summary>
        /// Fixes wifi after sleep on hardware: MSI-Claw
        /// </summary>
		public static void Fix1(bool apply)
		{
			string path = "/usr/lib/systemd/system-sleep";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, "iwlwifi-sleep.sh");
            if (File.Exists(path))
            {
                if (apply) return;// already patched

                // remove
                File.Delete(path);
                return;
            }

            const string config =
@"#!/bin/bash

case ""$1"" in
  pre)
    /usr/sbin/modprobe -r iwlmvm iwlwifi
    ;;
  post)
    /usr/sbin/modprobe iwlwifi iwlmvm
    ;;
esac";
            File.WriteAllText(path, config);
            ProcessUtil.Run("chmod", "+x " + path, useBash:false);
		}

        /// <summary>
        /// Fixes wifi after sleep on hardware: MSI-Claw
        /// </summary>
		public static void Fix2(bool apply)
        {
            string path = "/etc/NetworkManager/conf.d";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, "wifi-powersave.conf");
            if (File.Exists(path))
            {
                if (apply) return;// already patched

                // remove
                File.Delete(path);
                ProcessUtil.Run("systemctl", "restart NetworkManager", useBash:false);
                return;
            }

            const string config =
@"[connection]
wifi.powersave=2";
            File.WriteAllText(path, config);
            ProcessUtil.Run("systemctl", "restart NetworkManager", useBash:false);
        }
	}
}

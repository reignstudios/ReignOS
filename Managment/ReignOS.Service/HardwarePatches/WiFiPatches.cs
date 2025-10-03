using ReignOS.Core;
using System.IO;

// sudo lsmod
// sudo dmesg | grep -i wifi
// sudo modinfo iwlwifi | grep depends

namespace ReignOS.Service.HardwarePatches
{
	static class WiFiPatches
	{
        /// <summary>
        /// Fixes wifi after sleep (reboot iwlmld iwlmvm iwlwifi)
        /// </summary>
        public static void Fix1(bool apply)
		{
			string path = "/usr/lib/systemd/system-sleep";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, "wifi-sleep.sh");
            if (!apply && File.Exists(path))
            {
                // remove
                File.Delete(path);
                return;
            }

            const string config =
@"#!/bin/bash

case ""$1"" in
  pre)
    /usr/sbin/modprobe -r iwlmld iwlmvm iwlwifi
    ;;
  post)
    /usr/sbin/modprobe iwlwifi iwlmvm iwlmld
    ;;
esac";
            File.WriteAllText(path, config);
            ProcessUtil.Run("chmod", "+x " + path, useBash:false);
		}

        /// <summary>
        /// Fixes wifi after sleep (reboot iwlmvm mt7921e)
        /// </summary>
		public static void Fix2(bool apply)
        {
            string path = "/usr/lib/systemd/system-sleep";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, "wifi-sleep.sh");
            if (!apply && File.Exists(path))
            {
                // remove
                File.Delete(path);
                return;
            }

            const string config =
@"#!/bin/bash

case ""$1"" in
  pre)
    /usr/sbin/modprobe -r iwlmvm mt7921e
    ;;
  post)
    /usr/sbin/modprobe mt7921e iwlmvm
    ;;
esac";
            File.WriteAllText(path, config);
            ProcessUtil.Run("chmod", "+x " + path, useBash: false);
        }

        /// <summary>
        /// Fixes wifi after sleep (restart NetworkManager)
        /// </summary>
		public static void Fix_PowerState(bool apply)
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

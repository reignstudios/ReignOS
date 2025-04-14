using ReignOS.Core;
using System;
using System.IO;

namespace ReignOS.Bootloader;

static class PackageUpdates
{
    private static bool PackageExits(string package)
    {
        string result = ProcessUtil.Run("pacman", $"-Qs {package}");
        return result != null && result.Length != 0;
    }


    public static bool CheckUpdates()
    {
        if (CheckBadConfigs()) return true;

        // check old packages
        // nothing yet...

        // check for missing packages
        if (!PackageExits("wayland-utils")) return true;
        if (!PackageExits("weston")) return true;

        if (!PackageExits("vulkan-tools")) return true;
        if (!PackageExits("vulkan-mesa-layers")) return true;
        if (!PackageExits("lib32-vulkan-mesa-layers")) return true;

        if (!PackageExits("bluez")) return true;
        if (!PackageExits("bluez-utils")) return true;

        return false;
    }

    private static bool CheckBadConfigs()
    {
        try
        {
            const string path = "/etc/hostname";
            string hostname = File.ReadAllText(path).Trim();
            if (hostname == "reignos")
            {
                hostname = $"reignos_{Guid.NewGuid()}";
                void getStandardInput_hostname(StreamWriter writer)
                {
                    writer.WriteLine(hostname);
                    writer.Flush();
                    writer.Close();
                }
                ProcessUtil.Run("tee", path, asAdmin:true, getStandardInput:getStandardInput_hostname);

                return true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
    }
}
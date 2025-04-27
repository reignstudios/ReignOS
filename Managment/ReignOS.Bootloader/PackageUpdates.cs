using ReignOS.Core;
using System;
using System.IO;

namespace ReignOS.Bootloader;

static class PackageUpdates
{
    private static bool PackageExits(string package)
    {
        string result = ProcessUtil.Run("pacman", $"-Q {package}");
        return result != null && !result.StartsWith("error:");
    }

    public static bool CheckUpdates()
    {
        bool badConfig = false;
        if (CheckBadHostname()) badConfig = true;
        if (CheckBadKernelSettings()) badConfig = true;
        if (CheckBadDriverSettings()) badConfig = true;
        if (badConfig) return true;

        // check old packages
        // nothing yet...

        // check for missing packages
        if (!PackageExits("linux-headers")) return true;

        if (!PackageExits("wayland-utils")) return true;
        if (!PackageExits("weston")) return true;
        if (!PackageExits("openbox")) return true;
        if (!PackageExits("xdg-desktop-portal")) return true;
        if (!PackageExits("xdg-desktop-portal-wlr")) return true;

        if (!PackageExits("vulkan-tools")) return true;
        if (!PackageExits("vulkan-mesa-layers")) return true;
        if (!PackageExits("lib32-vulkan-mesa-layers")) return true;

        if (!PackageExits("bluez")) return true;
        if (!PackageExits("bluez-utils")) return true;

        if (!PackageExits("flatpak")) return true;

        if (!PackageExits("yay")) return true;
        if (!PackageExits("supergfxctl")) return true;
        if (!PackageExits("ttf-ms-fonts")) return true;
        if (!PackageExits("steamcmd")) return true;
        if (!PackageExits("proton-ge-custom")) return true;

        if (!PackageExits("fwupd")) return true;

        return false;
    }

    private static bool CheckBadHostname()
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

    private static bool CheckBadKernelSettings()
    {
        try
        {
            const string path = "/boot/loader/entries/arch.conf";
            string settings = File.ReadAllText(path);
            if
            (
                settings.Contains("acpi_osi=Linux") ||
                settings.Contains("i915.enable_dc=2") ||
                settings.Contains("i915.enable_psr=1") ||
                settings.Contains("amdgpu.dpm=1") ||
                settings.Contains("amdgpu.ppfeaturemask=0xffffffff") ||
                settings.Contains("amdgpu.dc=1 nouveau.pstate=1") ||
                settings.Contains("nouveau.perflvl=N") ||
                settings.Contains("nouveau.perflvl_wr=7777") ||
                settings.Contains("nouveau.config=NvGspRm=1") ||
                settings.Contains("nvidia_drm.modeset=1")
            )
            {
                // remove bad args
                settings = settings.Replace("acpi_osi=Linux", "");
                settings = settings.Replace("i915.enable_dc=2", "");
                settings = settings.Replace("i915.enable_psr=1", "");
                settings = settings.Replace("amdgpu.dpm=1", "");
                settings = settings.Replace("amdgpu.ppfeaturemask=0xffffffff", "");
                settings = settings.Replace("amdgpu.dc=1 nouveau.pstate=1", "");
                settings = settings.Replace("nouveau.perflvl=N", "");
                settings = settings.Replace("nouveau.perflvl_wr=7777", "");
                settings = settings.Replace("nouveau.config=NvGspRm=1", "");
                settings = settings.Replace("nvidia_drm.modeset=1", "");

                // add good args
                settings = settings.Replace(" rw", " rw pci=realloc");
                settings = settings.TrimEnd();

                // update conf
                ProcessUtil.WriteAllTextAdmin(path, settings);

                return true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
    }

    private static bool CheckBadDriverSettings()
    {
        try
        {
            bool found = false;

            const string nvidia = "/etc/modprobe.d/nvidia.conf";
            if (File.Exists(nvidia))
            {
                ProcessUtil.DeleteFileAdmin(nvidia);
                found = true;
            }

            const string nvidia99 = "/etc/modprobe.d/99-nvidia.conf";
            if (File.Exists(nvidia99))
            {
                ProcessUtil.DeleteFileAdmin(nvidia99);
                found = true;
            }

            if (found) return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
    }
}
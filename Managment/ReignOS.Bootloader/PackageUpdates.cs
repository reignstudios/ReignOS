using ReignOS.Core;
using System;
using System.IO;
using System.Text;

namespace ReignOS.Bootloader;

// sudo cpupower idle-info

// mem_sleep_default=deep or s2idle
// intel_idle.max_cstate=1 # intel
// amd_pstate=disable processor.max_cstate=1 # amd
// nouveau.pstate=1 nouveau.runpm=0 # nvidia

// /etc/modprobe.d/nvidia.conf
//options nvidia NVreg_EnableS0ixPowerManagement=1
//options nvidia NVreg_PreserveVideoMemoryAllocations=1
//options nvidia NVreg_DynamicPowerManagement=0x02

// to decode edid
// cat /sys/class/drm/card0-HDMI-A-1/edid | edid-decode

static class PackageUpdates
{
    private static bool PackageExits(string package)
    {
        string result = ProcessUtil.Run("pacman", $"-Q {package}", useBash:false);
        return result != null && !result.StartsWith("error:");
    }

    public static bool CheckUpdates()
    {
        // non-restart changes
        AddLaunchScript();
        FixOSName();
        
        // check bad configs
        bool badConfig = false;
        if (CheckBadHostname()) badConfig = true;
        //if (CheckBadKernelSettings()) badConfig = true;
        //if (CheckBadDriverSettings()) badConfig = true;
        if (badConfig) return true;

        // check old packages
        // nothing yet...

        // check for missing packages
        if (!PackageExits("linux-headers")) return true;
        if (!PackageExits("cpupower")) return true;

        if (!PackageExits("wayland-utils")) return true;
        if (!PackageExits("weston")) return true;
        if (!PackageExits("openbox")) return true;
        if (!PackageExits("xdg-desktop-portal")) return true;
        if (!PackageExits("xdg-desktop-portal-wlr")) return true;
        if (!PackageExits("xdg-desktop-portal-kde")) return true;
        if (!PackageExits("xdg-desktop-portal-gtk")) return true;

        if (!PackageExits("vulkan-tools")) return true;
        if (!PackageExits("vulkan-mesa-layers")) return true;
        if (!PackageExits("lib32-vulkan-mesa-layers")) return true;

        if (!PackageExits("bluez")) return true;
        if (!PackageExits("bluez-utils")) return true;

        if (!PackageExits("plasma-desktop")) return true;
        if (!PackageExits("konsole")) return true;
        if (!PackageExits("dolphin")) return true;
        if (!PackageExits("kate")) return true;
        if (!PackageExits("ark")) return true;
        if (!PackageExits("exfatprogs")) return true;
        if (!PackageExits("dosfstools")) return true;
        if (!PackageExits("partitionmanager")) return true;

        if (!PackageExits("gparted")) return true;
        if (!PackageExits("flatpak")) return true;

        if (!PackageExits("yay")) return true;
        if (!PackageExits("supergfxctl")) return true;
        if (!PackageExits("ttf-ms-fonts")) return true;
        if (!PackageExits("steamcmd")) return true;
        if (!PackageExits("proton-ge-custom")) return true;

        if (!PackageExits("fwupd")) return true;

        return false;
    }

    private static void AddLaunchScript()
    {
        try
        {
            const string bash = "/home/gamer/.bash_profile";
            const string launch = "/home/gamer/ReignOS_Launch.sh";
            if (!File.Exists(launch))
            {
                string bashText = File.ReadAllText(bash);
                var lines = bashText.Split('\n');
                string reignOSLaunchLine = null;
                foreach (string line in lines)
                {
                    if (line == "chmod +x /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Launch.sh")
                    {
                        bashText = bashText.Replace(line, "chmod +x /home/gamer/ReignOS_Launch.sh\n/home/gamer/ReignOS_Launch.sh");
                    }
                    else if (line.StartsWith("/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Launch.sh"))
                    {
                        reignOSLaunchLine = line;
                        bashText = bashText.Replace(line, "");
                    }
                }

                if (reignOSLaunchLine != null)
                {
                    File.WriteAllText(bash, bashText);

                    var builder = new StringBuilder();
                    builder.AppendLine("chmod +x /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Launch.sh");
                    builder.AppendLine(reignOSLaunchLine);
                    File.WriteAllText(launch, builder.ToString());
                }
                else
                {
                    Log.WriteLine("Failed to find ReignOS Launcher line in bash profile");
                }
            }
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
    }

    private static void FixOSName()
    {
        try
        {
            const string path = "/etc/lsb-release";
            string text = File.ReadAllText(path);
            if (text.Contains("DISTRIB_ID=\"Arch\""))
            {
                text = text.Replace("DISTRIB_ID=\"Arch\"", "DISTRIB_ID=\"ReignOS\"");
                text = text.Replace("DISTRIB_DESCRIPTION=\"Arch Linux\"", "DISTRIB_DESCRIPTION=\"ReignOS\"");
                void getStandardInput(StreamWriter writer)
                {
                    writer.WriteLine(text);
                    writer.Flush();
                    writer.Close();
                }
                ProcessUtil.Run("tee", path, asAdmin:true, getStandardInput:getStandardInput);
            }
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
        
        try
        {
            const string path = "/etc/os-release";
            string text = File.ReadAllText(path);
            if (text.Contains("NAME=\"Arch Linux\""))
            {
                text = text.Replace("NAME=\"Arch Linux\"", "NAME=\"ReignOS\"");
                text = text.Replace("PRETTY_NAME=\"Arch Linux\"", "PRETTY_NAME=\"ReignOS\"");
                text = text.Replace("HOME_URL=\"https://archlinux.org/\"", "HOME_URL=\"http://reign-os.com/\"");
                text = text.Replace("ID=arch", "ID=reignos");
                void getStandardInput(StreamWriter writer)
                {
                    writer.WriteLine(text);
                    writer.Flush();
                    writer.Close();
                }
                ProcessUtil.Run("tee", path, asAdmin:true, getStandardInput:getStandardInput);
            }
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
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
            Log.WriteLine(e);
        }

        return false;
    }

    /*private static bool CheckBadKernelSettings()
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
                settings.Contains("amdgpu.dc=1") ||
                settings.Contains("nouveau.pstate=1") ||
                settings.Contains("nouveau.perflvl=N") ||
                settings.Contains("nouveau.perflvl_wr=7777") ||
                settings.Contains("nouveau.config=NvGspRm=1") ||
                settings.Contains("nvidia-drm.modeset=1") ||
                settings.Contains("nvidia_drm.fbdev=0") ||
                settings.Contains("nvidia.NVreg_PreserveVideoMemoryAllocations=0")
            )
            {
                // remove bad args
                settings = settings.Replace("acpi_osi=Linux", "");
                settings = settings.Replace("i915.enable_dc=2", "");
                settings = settings.Replace("i915.enable_psr=1", "");
                settings = settings.Replace("amdgpu.dpm=1", "");
                settings = settings.Replace("amdgpu.ppfeaturemask=0xffffffff", "");
                settings = settings.Replace("amdgpu.dc=1", "");
                settings = settings.Replace("nouveau.pstate=1", "");
                settings = settings.Replace("nouveau.perflvl=N", "");
                settings = settings.Replace("nouveau.perflvl_wr=7777", "");
                settings = settings.Replace("nouveau.config=NvGspRm=1", "");
                settings = settings.Replace("nvidia-drm.modeset=1", "");
                settings = settings.Replace("nvidia_drm.fbdev=0", "");
                settings = settings.Replace("nvidia.NVreg_PreserveVideoMemoryAllocations=0", "");

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
            Log.WriteLine(e);
        }

        return false;
    }*/

    /*private static bool CheckBadDriverSettings()
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
            Log.WriteLine(e);
        }

        return false;
    }*/
}
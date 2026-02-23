using ReignOS.Core;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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

// maybe wanted late packages: asusctl
// needed for HIP support: hip-runtime-amd via pacman
// needed for oneAPI support: intel-oneapi-basekit via pacman

static class PackageUpdates
{
    public static bool CheckUpdates()
    {
        // non-restart changes
        //AddLaunchScript();
        //FixOSName();
        IgnorePackages();
        //ReconfigureAutoLogin();
        //AddPermissions();

        // check bad configs (do them all at once then reboot)
        /*bool badConfig = false;
        //if (CheckBadHostname()) badConfig = true;
        //if (CheckBadKernelSettings()) badConfig = true;
        //if (CheckBadDriverSettings()) badConfig = true;
        //if (CheckNonArchKernelDefault()) badConfig = true;
        if (badConfig) return true;*/

        // read last version
        const string versionPath = "/home/gamer/ReignOS_Version";
        string lastVersion = VersionInfo.version;
        if (File.Exists(versionPath)) lastVersion = File.ReadAllText(versionPath);

        // write current version
        File.WriteAllText(versionPath, VersionInfo.version);

        return versionPath != VersionInfo.version;
    }

    private static void IgnorePackages()
    {
        try
        {
            const string pacmanConf = "/etc/pacman.conf";
            string text = File.ReadAllText(pacmanConf);
            var lines = text.Split('\n');
            foreach (string line in lines)
            {
                if ((line.StartsWith("#IgnorePkg ") || line.StartsWith("# IgnorePkg ") || line.StartsWith("IgnorePkg ")) && !line.Contains(" jack2"))
                {
                    text = text.Replace(line, "IgnorePkg = jack2");
                    break;
                }
            }
            ProcessUtil.WriteAllTextAdmin(pacmanConf, text);
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
    }

    /*private static void ReconfigureAutoLogin()
    {
        try
        {
            const string bashProfile = "/home/gamer/.bash_profile";
            string text = File.ReadAllText(bashProfile);
            if (!text.Contains("if [[ \"$(tty)\" == \"/dev/tty1\" && -n \"$XDG_VTNR\" && \"$XDG_VTNR\" -eq 1 ]]; then"))
            {
                text = text.Replace("[[ -f ~/.bashrc ]] && . ~/.bashrc", "[[ -f ~/.bashrc ]] && . ~/.bashrc\n\nif [[ \"$(tty)\" == \"/dev/tty1\" && -n \"$XDG_VTNR\" && \"$XDG_VTNR\" -eq 1 ]]; then");
                text = text + "\nfi";
            }
            File.WriteAllText(bashProfile, text);
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
    }*/

    /*private static void AddPermissions()
    {
        //Run("usermod", "-aG wheel,audio,video,storage gamer");
        //Run("usermod", "-aG wheel,audio,video,storage,input,games,gamemode gamer");
        try
        {
            string result = ProcessUtil.Run("id", "-nG", useBash: false);
            if (!result.Contains(" input") || !result.Contains(" games") || !result.Contains(" gamemode"))
            {
                ProcessUtil.Run("usermod", "-aG wheel,audio,video,storage,input,games,gamemode gamer", useBash: true, asAdmin: true);// set user permissions
            }
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
    }*/

    /*private static void AddLaunchScript()
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
    }*/

    /*private static void FixOSName()
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
    }*/

    /*private static bool CheckBadHostname()
    {
        try
        {
            const string path = "/etc/hostname";
            string hostname = File.ReadAllText(path).Trim();
            if (hostname == "reignos" || hostname.StartsWith("reignos_"))
            {
                string id = Guid.NewGuid().ToString();
                id = id.Split('-')[0];
                hostname = $"reignos-{id}";
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
    }*/

    /*private static bool CheckBadKernelSettings()
    {
        try
        {
            const string path = "/boot/loader/entries/arch.conf";
            string settings = File.ReadAllText(path);
            if
            (
                !settings.Contains("root=UUID=")
            )
            {
                // remove bad args
                settings = settings.Replace(" pci=realloc", "");

                // add good args
                if (!settings.Contains(" rootwait")) settings = settings.Replace(" rw", " rw rootwait");
                
                // use partition ID path
                var match = Regex.Match(settings, @" root=(\S*) rw");
                if (match.Success)
                {
                    string partitionInfoResult = ProcessUtil.Run("blkid", match.Groups[1].Value, asAdmin: true, useBash: false);
                    match = Regex.Match(partitionInfoResult, @".*?UUID=""(.*?)""");
                    if (match.Success)
                    {
                        var match2 = Regex.Match(settings, @" (root=.*?) rw");
                        settings = settings.Replace(match2.Groups[1].Value, $"root=UUID={match.Groups[1].Value}");

                        // update conf
                        settings = settings.TrimEnd();
                        ProcessUtil.WriteAllTextAdmin(path, settings);
                    }
                }

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

    /*private static bool CheckNonArchKernelDefault()
    {
        try
        {
            string loader = File.ReadAllText("/boot/loader/loader.conf");
            var match = Regex.Match(loader, @"(default [^\n]*)");
            if (!match.Success) loader += "\ndefault arch.conf";
            ProcessUtil.WriteAllTextAdmin("/boot/loader/loader.conf", loader);
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }

        return false;
    }*/
}
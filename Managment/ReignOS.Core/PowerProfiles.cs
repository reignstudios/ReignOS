using System;
using System.IO;
using System.Threading;

namespace ReignOS.Core;

public static class PowerProfiles
{
    public static void Apply(bool asAdmin)
    {
        if (!File.Exists("/home/gamer/ReignOS_Ext/PowerProfileSettings.txt")) return;
        try
        {
            using (var stream = new FileStream("/home/gamer/ReignOS_Ext/PowerProfileSettings.txt", FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var parts = line.Split(' ');
                    if (parts == null || parts.Length == 0) continue;
                    if (parts[0].StartsWith("Profile="))
                    {
                        if (PackageUtils.PackageExits("power-profiles-daemon"))
                        {
                            var subParts = parts[0].Split('=');
                            string powerProfile = subParts[1];
                            ProcessUtil.Run("powerprofilesctl", "set " + powerProfile, asAdmin:asAdmin, useBash:false);
                            Thread.Sleep(500);// wait a sec before setting other values
                        }
                    }
                    else if (parts[0].StartsWith("IntelTurboBoost="))
                    {
                        var subParts = parts[0].Split('=');
                        bool intelTurboBoost = subParts[1] == "True";
                        const string turboBoostPath = "/sys/devices/system/cpu/intel_pstate/no_turbo";
                        if (File.Exists(turboBoostPath))
                        {
                            if (asAdmin) ProcessUtil.WriteAllTextAdmin(turboBoostPath, intelTurboBoost ? "0" : "1");
                            else File.WriteAllText(turboBoostPath, intelTurboBoost ? "0" : "1");
                        }
                    }
                    else if (parts[0].StartsWith("CPU="))
                    {
                        string name = null;
                        int minFreq = -1, maxFreq = -1;
                        bool? boost = null;
                        foreach (var part in parts)
                        {
                            var subParts = part.Split('=');
                            switch (subParts[0])
                            {
                                case "CPU": name = subParts[1]; break;
                                case "MinFreq": if (int.TryParse(subParts[1], out int minFreqValue)) minFreq = minFreqValue; break;
                                case "MaxFreq": if (int.TryParse(subParts[1], out int maxFreqValue)) maxFreq = maxFreqValue; break;
                                case "Boost": boost = subParts[1] == "True"; break;
                            }
                        }

                        if (name == null || minFreq < 0 || maxFreq < 0) continue;
                        string cpuPath = Path.Combine("/sys/devices/system/cpu", name, "cpufreq");
                        if (asAdmin)
                        {
                            ProcessUtil.WriteAllTextAdmin(Path.Combine(cpuPath, "scaling_min_freq"), minFreq.ToString());
                            ProcessUtil.WriteAllTextAdmin(Path.Combine(cpuPath, "scaling_max_freq"), maxFreq.ToString());
                            if (boost != null) ProcessUtil.WriteAllTextAdmin(Path.Combine(cpuPath, "boost"), boost == true ? "1" : "0");
                        }
                        else
                        {
                            File.WriteAllText(Path.Combine(cpuPath, "scaling_min_freq"), minFreq.ToString());
                            File.WriteAllText(Path.Combine(cpuPath, "scaling_max_freq"), maxFreq.ToString());
                            if (boost != null) File.WriteAllText(Path.Combine(cpuPath, "boost"), boost == true ? "1" : "0");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
        }
    }
}
using System;
using System.IO;

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
                        var subParts = parts[0].Split('=');
                        string powerProfile = subParts[1];
                    }
                    else if (parts[0].StartsWith("IntelTurboBoost="))
                    {
                        var subParts = parts[0].Split('=');
                        bool intelTurboBoost = subParts[1] == "True";
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
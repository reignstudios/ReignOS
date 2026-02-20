using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReignOS.Core
{
    public static class PackageUtils
    {
        public static bool PackageExits(string package)
        {
            string result = ProcessUtil.Run("pacman", $"-Q {package}", useBash: false);
            bool value = result != null && !result.StartsWith("error:");
            Log.WriteLine($"Package '{package}' Exists: {value}");
            return value;
        }
        
        public static bool ServiceEnabled(string service, bool user, bool asAdmin)
        {
            string userArg = user ? "--user " : "";
            string result = ProcessUtil.Run("systemctl", $"{userArg}status {service}", useBash: false, asAdmin: asAdmin);
            bool value = result != null && result.Contains("Active: active");
            Log.WriteLine($"Service '{service}' Active: {value}");
            return value;
        }
    }
}

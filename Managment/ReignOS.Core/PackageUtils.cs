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
            return result != null && !result.StartsWith("error:");
        }
    }
}

using ReignOS.Core;

namespace ReignOS.Bootloader;

static class PackageUpdates
{
    private static bool PackageExits(string package)
    {
        string result = ProcessUtil.Run("pacman", $"-Qs {package}");
        return result != null && result.Length != 0;
    }


    public static bool NeedsUpdate()
    {
        // check old packages
        // nothing yet...

        // check for missing packages
        if (!PackageExits("wayland-utils")) return true;
        if (!PackageExits("weston")) return true;

        return false;
    }
}
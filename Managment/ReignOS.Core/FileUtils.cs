namespace ReignOS.Core;
using System;
using System.IO;

public static class FileUtils
{
    public static bool InstallService(string srcPath, string dstPath)
    {
        Log.WriteLine("Installing service: " + dstPath);
        try
        {
            string path = Path.GetDirectoryName(dstPath);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            File.Copy(srcPath, dstPath, true);
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
            return false;
        }
        return true;
    }

    public static bool InstallScript(string srcPath, string dstPath)
    {
        Log.WriteLine("Installing script: " + dstPath);
        try
        {
            string path = Path.GetDirectoryName(dstPath);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            File.Copy(srcPath, dstPath, true);
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
            return false;
        }

        ProcessUtil.Run("chmod", $"+x {dstPath}", out _, wait:true, asAdmin:false);
        return true;
    }

    public static bool SafeCopy(string srcPath, string dstPath)
    {
        try
        {
            File.Copy(srcPath, dstPath, true);
        }
        catch (Exception e)
        {
            Log.WriteLine(e);
            return false;
        }
        return true;
    }
}
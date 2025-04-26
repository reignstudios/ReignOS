namespace ReignOS.Core;
using System;
using System.IO;

public static class FileUtils
{
    public static void InstallService(string srcPath, string dstPath)
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
            Log.WriteLine(e.Message);
        }
    }

    public static void InstallScript(string srcPath, string dstPath)
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
            Log.WriteLine(e.Message);
        }

        ProcessUtil.Run("chmod", $"+x {dstPath}", out _, wait:true);
    }
}
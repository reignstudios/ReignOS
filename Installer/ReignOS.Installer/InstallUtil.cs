namespace ReignOS.Installer;

static class InstallUtil
{
    public delegate void InstallProgressDelegate(string task, float progress);
    public static event InstallProgressDelegate InstallProgress;

    private static Thread installThread;
    
    public static void Install()
    {
        installThread = new Thread(InstallThread);
        installThread.Start();
    }

    private static void InstallThread()
    {
        InstallProgress?.Invoke("Partitioning drive...", 0);
        PartitionDrive();
        InstallProgress?.Invoke("Installing base Arch...", 10);
        InstallBaseArch();
        InstallProgress?.Invoke("Installing Arch packages...", 30);
        InstallArchPackages();
        InstallProgress?.Invoke("Configuring Arch...", 90);
        ConfigureArch();
        InstallProgress?.Invoke("Done", 100);
    }

    private static void PartitionDrive()
    {
        
    }
    
    private static void InstallBaseArch()
    {
        
    }
    
    private static void InstallArchPackages()
    {
        
    }
    
    private static void ConfigureArch()
    {
        
    }
}
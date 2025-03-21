using System.Text;
using System.Diagnostics;
using ReignOS.Core;

namespace ReignOS.Installer;

class Partition
{
    public Drive drive;
    public int number;
    public ulong start, end, size;
    public string fileSystem;
    public string name;
    public string flags;
    
    public string path => drive.disk + "p" + number;

    public Partition(Drive drive)
    {
        this.drive = drive;
    }
}

class Drive
{
    public string model;
    public string disk;
    public ulong size;
    public List<Partition> partitions = new List<Partition>();
}

static class InstallUtil
{
    public delegate void InstallProgressDelegate(string task, float progress);
    public static event InstallProgressDelegate InstallProgress;

    private static Process archRootProcess;
    private static Partition efiPartition, ext4Partition;
    private static Thread installThread;
    private static float progress;
    private static string progressTask;

    private static void UpdateProgress(int progress)
    {
        InstallUtil.progress = progress;
        InstallProgress?.Invoke(progressTask, progress);
    }

    private static void Run(string name, string args)
    {
        if (archRootProcess == null)
        {
            ProcessUtil.Run(name, args, asAdmin:true);
        }
        else
        {
            archRootProcess.StandardInput.WriteLine($"{name} {args}");
            archRootProcess.StandardInput.Flush();
        }
    }

    private static void StartArchRootProcess()
    {
        archRootProcess = new Process();
        archRootProcess.StartInfo.UseShellExecute = false;
        archRootProcess.StartInfo.FileName = "sudo";
        archRootProcess.StartInfo.Arguments = "-S -- arch-chroot /mnt";
        archRootProcess.StartInfo.RedirectStandardInput = true;
        archRootProcess.StartInfo.RedirectStandardOutput = true;
        archRootProcess.StartInfo.RedirectStandardError = true;
        archRootProcess.Start();
        
        archRootProcess.StandardInput.WriteLine("gamer");
        archRootProcess.StandardInput.Flush();

        void ReadLine(object sender, DataReceivedEventArgs args)
        {
            if (args != null && args.Data != null)
            {
                Console.WriteLine(args.Data);
            }
        }
        
        archRootProcess.OutputDataReceived += ReadLine;
        archRootProcess.BeginOutputReadLine();

        archRootProcess.ErrorDataReceived += ReadLine;
        archRootProcess.BeginErrorReadLine();
    }
    
    public static void Install(Partition efiPartition, Partition ext4Partition)
    {
        InstallUtil.efiPartition = efiPartition;
        InstallUtil.ext4Partition = ext4Partition;
        installThread = new Thread(InstallThread);
        installThread.Start();
    }

    private static void InstallThread()
    {
        progress = 0;
        try
        {
            InstallBaseArch();
            InstallArchPackages();
            ConfigureArch();
            InstallProgress?.Invoke("Done", progress);
        }
        catch (Exception e)
        {
            InstallProgress?.Invoke("Failed", progress);
            Console.WriteLine(e);
        }

        if (archRootProcess != null)
        {
            archRootProcess.StandardInput.WriteLine("exit");
            Thread.Sleep(1000);
            archRootProcess.Kill(true);
            archRootProcess.Dispose();
            archRootProcess = null;
            Run("umount", "-R /mnt");
        }
    }
    
    private static void InstallBaseArch()
    {
        progressTask = "Installing base Arch...";
        UpdateProgress(0);

        Run("mount", $"{ext4Partition.path} /mnt");
        Run("mkdir", "-p /mnt/boot");
        Run("mount", $"{efiPartition.path} /mnt/boot");
        UpdateProgress(5);
        
        Run("pacstrap", "/mnt base linux linux-firmware systemd");
        Run("genfstab", "-U /mnt >> /mnt/etc/fstab");
        StartArchRootProcess();
        UpdateProgress(20);

        string path = "/mnt/etc/pacman.conf";
        string fileText = File.ReadAllText(path);
        fileText = fileText.Replace("# [multilib]", "[multilib]");
        fileText = fileText.Replace("# Include = /etc/pacman.d/mirrorlist", "Include = /etc/pacman.d/mirrorlist");
        File.WriteAllText(path, fileText);
        Run("pacman", "-Syy");
        UpdateProgress(25);

        Run("pacman", "-S nano");
        Run("pacman", "-S evtest");
        UpdateProgress(30);
        
        Run("pacman", "-S dhcpcd dhclient networkmanager iwd netctl iproute2 wireless_tools dialog");
        Run("pacman", "-S network-manager-applet nm-connection-editor");
        Run("systemctl", "enable dhcpcd NetworkManager iwd");
        UpdateProgress(35);

        Run("ln", "-sf /usr/share/zoneinfo/Region/City /etc/localtime");
        Run("hwclock", "--systohc");
        Run("locale-gen", "");
        Run("echo", "\"LANG=en_US.UTF-8\" > /etc/locale.conf");
        UpdateProgress(40);

        Run("echo", "\"reignos\" > /etc/hostname");
        path = "/mnt/etc/hosts";
        var fileBuilder = new StringBuilder(File.ReadAllText(path));
        fileBuilder.AppendLine("127.0.0.1 localhost");
        fileBuilder.AppendLine("::1 localhost");
        fileBuilder.AppendLine("127.0.1.1 reignos.localdomain reignos");
        File.WriteAllText(path, fileBuilder.ToString());
        UpdateProgress(45);
        
        Run("bootctl", "install");
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("title ReignOS");
        fileBuilder.AppendLine("linux /vmlinuz-linux");
        fileBuilder.AppendLine("initrd /initramfs-linux.img");
        fileBuilder.AppendLine("options root=/dev/nvme0n1p2 rw");
        File.WriteAllText("/mnt/boot/loader/entries/arch.conf", fileBuilder.ToString());
        Run("systemctl", "enable systemd-networkd systemd-resolved");
        UpdateProgress(50);

        Run("passwd", "root");
        archRootProcess.StandardInput.WriteLine("gamer");
        UpdateProgress(51);

        Run("useradd", "-m -G users -s /bin/bash gamer");
        Run("passwd", "gamer");
        archRootProcess.StandardInput.WriteLine("gamer");
        Run("usermod", "-aG wheel,audio,video,storage gamer");
        UpdateProgress(55);

        Run("pacman", "-S sudo");
        path = "/mnt/etc/sudoers";
        fileText = File.ReadAllText(path);
        fileText = fileText.Replace("# %wheel ALL=(ALL:ALL) ALL", "%wheel ALL=(ALL:ALL) ALL");
        File.WriteAllText(path, fileText);
        UpdateProgress(60);
    }
    
    private static void InstallArchPackages()
    {
        progressTask = "Installing Arch packages...";
        UpdateProgress(90);
    }
    
    private static void ConfigureArch()
    {
        progressTask = "Configuring Arch...";
        UpdateProgress(100);
    }
}
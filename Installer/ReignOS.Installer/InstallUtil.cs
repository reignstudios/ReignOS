using System.Text;
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
    public List<Partition> partitions;
}

static class InstallUtil
{
    public delegate void InstallProgressDelegate(string task, float progress);
    public static event InstallProgressDelegate InstallProgress;

    private static Partition efiPartition, ext4Partition;
    private static Thread installThread;
    private static float progress;
    private static string progressTask;

    private static void UpdateProgress(int progress)
    {
        InstallUtil.progress = progress;
        InstallProgress?.Invoke(progressTask, progress);
    }

    private static bool archChrootEnable;
    private static void Run(string name, string args)
    {
        if (archChrootEnable) ProcessUtil.Run("", $"/mnt {name} {args}");
        else ProcessUtil.Run(name, args);
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
        archChrootEnable = false;
        InstallBaseArch();
        InstallArchPackages();
        ConfigureArch();
        InstallProgress?.Invoke("Done", progress);
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
        archChrootEnable = true;
        UpdateProgress(20);
        
        string fileText = File.ReadAllText("/etc/pacman.conf");
        fileText = fileText.Replace("# [multilib]", "[multilib]");
        fileText = fileText.Replace("# Include = /etc/pacman.d/mirrorlist", "Include = /etc/pacman.d/mirrorlist");
        File.WriteAllText("/etc/pacman.conf", fileText);
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
        var fileBuilder = new StringBuilder(File.ReadAllText("/etc/hosts"));
        fileBuilder.AppendLine("127.0.0.1 localhost");
        fileBuilder.AppendLine("::1 localhost");
        fileBuilder.AppendLine("127.0.1.1 reignos.localdomain reignos");
        File.WriteAllText("/etc/hosts", fileBuilder.ToString());
        UpdateProgress(45);
        
        Run("bootctl", "install");
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("title ReignOS");
        fileBuilder.AppendLine("linux /vmlinuz-linux");
        fileBuilder.AppendLine("initrd /initramfs-linux.img");
        fileBuilder.AppendLine("options root=/dev/nvme0n1p2 rw");
        File.WriteAllText("/boot/loader/entries/arch.conf", fileBuilder.ToString());
        Run("systemctl", "enable systemd-networkd systemd-resolved");
        UpdateProgress(50);
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
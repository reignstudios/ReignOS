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

    public string path
    {
        get
        {
            if (drive.PartitionsUseP()) return drive.disk + "p" + number.ToString();
            return drive.disk + number.ToString();
        }
    }

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

    public bool PartitionsUseP()
    {
        return disk.StartsWith("/dev/nvme") || disk.StartsWith("/dev/mmcblk");
    }
}

static class InstallUtil
{
    public delegate void InstallProgressDelegate(string task, float progress);
    public static event InstallProgressDelegate InstallProgress;

    private static bool archRootMode;
    private static Partition efiPartition, ext4Partition;
    private static Thread installThread;
    private static float progress;
    private static string progressTask;

    private static void UpdateProgress(int progress)
    {
        InstallUtil.progress = progress;
        InstallProgress?.Invoke(progressTask, progress);
    }

    private static void Run(string name, string args, ProcessUtil.ProcessInputDelegate getStandardInput = null)
    {
        if (!archRootMode)
        {
            ProcessUtil.Run(name, args, asAdmin:true, enterAdminPass:false, getStandardInput:getStandardInput);
        }
        else
        {
            Console.WriteLine($"Arch-Chroot.Run: {name} {args}");
            ProcessUtil.Run("arch-chroot", $"/mnt bash -c \\\"{name} {args}\\\"", asAdmin:true, enterAdminPass:false, getStandardInput:getStandardInput);
        }
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
        archRootMode = false;
        ProcessUtil.KillHard("arch-chroot", true, out _);
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

        archRootMode = false;
        ProcessUtil.KillHard("arch-chroot", true, out _);
        Run("umount", "-R /mnt/boot");
        Run("umount", "-R /mnt");
    }
    
    private static void InstallBaseArch()
    {
        progressTask = "Installing base...";
        UpdateProgress(0);
        
        Run("umount", "-R /mnt/boot");
        Run("umount", "-R /mnt");
        UpdateProgress(5);

        Run("mount", $"{ext4Partition.path} /mnt");
        Run("rm", "-rf /mnt/*");
        Run("mkdir", "-p /mnt/boot");
        Run("mount", $"{efiPartition.path} /mnt/boot");
        Run("rm", "-rf /mnt/boot/*");
        UpdateProgress(10);
        
        Run("pacstrap", "/mnt base linux linux-firmware systemd");
        Run("genfstab", "-U /mnt >> /mnt/etc/fstab");
        archRootMode = true;
        UpdateProgress(20);

        string path = "/mnt/etc/pacman.conf";
        string fileText = File.ReadAllText(path);
        fileText = fileText.Replace("#[multilib]\n#Include = /etc/pacman.d/mirrorlist", "[multilib]\nInclude = /etc/pacman.d/mirrorlist");
        void getStandardInput_pacman_conf(StreamWriter writer)
        {
            writer.WriteLine(fileText);
            writer.Flush();
            writer.Close();
        }
        ProcessUtil.Run("tee", path, asAdmin:true, getStandardInput:getStandardInput_pacman_conf);
        Run("pacman", "-Syy --noconfirm");
        UpdateProgress(25);

        Run("pacman", "-S --noconfirm nano");
        Run("pacman", "-S --noconfirm evtest");
        UpdateProgress(30);
        
        Run("pacman", "-S --noconfirm dhcpcd dhclient networkmanager iwd netctl iproute2 wireless_tools dialog");
        Run("pacman", "-S --noconfirm network-manager-applet nm-connection-editor");
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
        void getStandardInput_hostname(StreamWriter writer)
        {
            writer.WriteLine(fileBuilder);
            writer.Flush();
            writer.Close();
        }
        ProcessUtil.Run("tee", path, asAdmin:true, getStandardInput:getStandardInput_hostname);
        UpdateProgress(45);
        
        Run("bootctl", "install");
        path = "/mnt/boot/loader/entries/arch.conf";
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("title ReignOS");
        fileBuilder.AppendLine("linux /vmlinuz-linux");
        fileBuilder.AppendLine("initrd /initramfs-linux.img");
        fileBuilder.AppendLine($"options root={ext4Partition.path} rw");
        void getStandardInput_arch_conf(StreamWriter writer)
        {
            writer.WriteLine(fileBuilder);
            writer.Flush();
            writer.Close();
        }
        ProcessUtil.Run("tee", path, asAdmin:true, getStandardInput:getStandardInput_arch_conf);
        Run("systemctl", "enable systemd-networkd systemd-resolved");
        UpdateProgress(50);

        Run("echo", "'root:gamer' | chpasswd");
        UpdateProgress(51);

        Run("pacman", "-S --noconfirm sudo pam");
        Run("pacman", "-Qs sudo");
        UpdateProgress(55);
        
        Run("useradd", "-m -G users -s /bin/bash gamer");
        Run("echo", "'gamer:gamer' | chpasswd");
        Run("usermod", "-aG wheel,audio,video,storage gamer");
        UpdateProgress(58);

        path = "/mnt/etc/sudoers";
        fileText = ProcessUtil.ReadAllTextAdmin(path);
        fileText = fileText.Replace("# %wheel ALL=(ALL:ALL) ALL", "%wheel ALL=(ALL:ALL) ALL\ngamer ALL=(ALL) NOPASSWD:ALL");
        void getStandardInput_sudoers(StreamWriter writer)
        {
            writer.WriteLine(fileText);
            writer.Flush();
            writer.Close();
        }
        ProcessUtil.Run("tee", path, asAdmin:true, getStandardInput:getStandardInput_sudoers);
        UpdateProgress(60);
    }
    
    private static void InstallArchPackages()
    {
        progressTask = "Installing packages...";
        UpdateProgress(90);
    }
    
    private static void ConfigureArch()
    {
        progressTask = "Configuring...";
        UpdateProgress(100);
    }
}
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
        
        // unmount conflicting mounts
        Run("umount", "-R /mnt/boot");
        Run("umount", "-R /mnt");
        UpdateProgress(5);

        // mount partitions
        Run("mount", $"{ext4Partition.path} /mnt");
        Run("rm", "-rf /mnt/*");
        Run("mkdir", "-p /mnt/boot");
        Run("mount", $"{efiPartition.path} /mnt/boot");
        Run("rm", "-rf /mnt/boot/*");
        UpdateProgress(10);
        
        // install arch base
        Run("pacstrap", "/mnt base linux linux-firmware systemd");
        Run("genfstab", "-U /mnt >> /mnt/etc/fstab");
        archRootMode = true;
        UpdateProgress(20);

        // configure pacman
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
        
        // install network support
        Run("pacman", "-S --noconfirm dhcpcd dhclient networkmanager iwd netctl iproute2 wireless_tools dialog");
        Run("pacman", "-S --noconfirm network-manager-applet nm-connection-editor");
        Run("systemctl", "enable dhcpcd NetworkManager iwd");
        UpdateProgress(30);

        // install BT support
        // TODO

        // configure time and lang
        Run("ln", "-sf /usr/share/zoneinfo/Region/City /etc/localtime");
        Run("hwclock", "--systohc");
        Run("locale-gen", "");
        Run("echo", "\"LANG=en_US.UTF-8\" > /etc/locale.conf");
        UpdateProgress(40);

        // configure hosts file
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
        
        // configure systemd-boot
        Run("bootctl", "install");
        path = "/mnt/boot/loader/entries/arch.conf";
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("title ReignOS");
        fileBuilder.AppendLine("linux /vmlinuz-linux");
        fileBuilder.AppendLine("initrd /initramfs-linux.img");
        fileBuilder.AppendLine($"options root={ext4Partition.path} rw acpi_osi=Linux i915.enable_dc=2 i915.enable_psr=1 amdgpu.dpm=1 amdgpu.ppfeaturemask=0xffffffff amdgpu.dc=1 nouveau.pstate=1 nouveau.perflvl=N nouveau.perflvl_wr=7777 nouveau.config=NvGspRm=1 nvidia_drm.modeset=1");
        void getStandardInput_arch_conf(StreamWriter writer)
        {
            writer.WriteLine(fileBuilder);
            writer.Flush();
            writer.Close();
        }
        ProcessUtil.Run("tee", path, asAdmin:true, getStandardInput:getStandardInput_arch_conf);
        Run("systemctl", "enable systemd-networkd systemd-resolved");
        UpdateProgress(50);

        // configure nvidia settings
        path = "/etc/modprobe.d/";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "nvidia.conf"), "options nvidia-drm modeset=1");
        File.WriteAllText(Path.Combine(path, "99-nvidia.conf"), "options nvidia NVreg_DynamicPowerManagement=0x02");
        UpdateProgress(51);

        // configure root pass
        Run("echo", "'root:gamer' | chpasswd");
        UpdateProgress(53);

        // install sudo
        Run("pacman", "-S --noconfirm sudo");
        Run("pacman", "-Qs sudo");
        UpdateProgress(55);
        
        // configure gamer user
        Run("useradd", "-m -G users -s /bin/bash gamer");
        Run("echo", "'gamer:gamer' | chpasswd");
        Run("usermod", "-aG wheel,audio,video,storage gamer");
        UpdateProgress(58);

        // make gamer user a sudo user without needing pass
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

        // make gamer user auto login
        path = "/etc/systemd/system/getty@tty1.service.d/";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("[Service]");
        fileBuilder.AppendLine("ExecStart=");
        fileBuilder.AppendLine("ExecStart=-/usr/bin/agetty --autologin gamer --noclear %I $TERM");
        File.WriteAllText(Path.Combine(path, "autologin.conf"), fileBuilder.ToString());
        UpdateProgress(61);

        // auto invoke launch after login
        path = "/home/gamer/.bash_profile";
        if (File.Exists(path)) fileText = File.ReadAllText(path);
        else fileText = "";
        fileBuilder = new StringBuilder(fileText);
        fileBuilder.AppendLine();
        fileBuilder.AppendLine("/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Launch.sh --use-controlcenter");
        File.WriteAllText(path, fileBuilder.ToString());
        UpdateProgress(62);

        // configure splash image
        // TODO
    }
    
    private static void InstallArchPackages()
    {
        progressTask = "Installing packages...";

        // install apps
        Run("pacman", "-S --noconfirm nano evtest");
        Run("pacman", "-S --noconfirm dmidecode udev python");
        UpdateProgress(70);

        // install wayland
        Run("pacman", "-S --noconfirm xorg-server-xwayland wayland wayland-protocols");
        Run("pacman", "-S --noconfirm xorg-xev xbindkeys xorg-xinput xorg-xmodmap");
        UpdateProgress(75);

        // install x11
        Run("pacman", "-S --noconfirm xorg xorg-server xorg-xinit xterm");
        UpdateProgress(80);

        // install wayland graphics drivers
        Run("pacman", "-S --noconfirm mesa lib32-mesa");
        Run("pacman", "-S --noconfirm libva-intel-driver intel-media-driver intel-ucode vulkan-intel lib32-vulkan-intel intel-gpu-tools");// Intel
        Run("pacman", "-S --noconfirm libva-mesa-driver lib32-libva-mesa-driver amd-ucode vulkan-radeon lib32-vulkan-radeon radeontop");// AMD
        Run("pacman", "-S --noconfirm vulkan-nouveau lib32-vulkan-nouveau");// Nvida
        Run("pacman", "-S --noconfirm vulkan-icd-loader lib32-vulkan-icd-loader lib32-libglvnd");
        Run("pacman", "-S --noconfirm vulkan-tools vulkan-mesa-layers lib32-vulkan-mesa-layers");
        Run("pacman", "-S --noconfirm egl-wayland");
        UpdateProgress(85);

        // install x11 graphics drivers
        Run("pacman", "-S --noconfirm xf86-video-intel xf86-video-amdgpu xf86-video-nouveau");
        Run("pacman", "-S --noconfirm glxinfo");
        UpdateProgress(90);

        // install compositors
        Run("pacman", "-S --noconfirm wlr-randr wlroots gamescope cage labwc");
        UpdateProgress(92);

        // install audio
        Run("pacman", "-S --noconfirm alsa-utils alsa-plugins");
        Run("pacman", "-S --noconfirm sof-firmware");
        Run("pacman", "-S --noconfirm pipewire pipewire-pulse pipewire-alsa pipewire-jack");
        Run("systemctl", "--user enable pipewire pipewire-pulse");
        UpdateProgress(93);

        // install power
        Run("pacman", "-S --noconfirm acpi acpid powertop power-profiles-daemon");
        Run("pacman", "-S --noconfirm python-gobject");
        Run("systemctl", "enable acpid power-profiles-daemon");
        UpdateProgress(94);

        // install auto-mount drives
        Run("pacman", "-S --noconfirm udiskie udisks2");
        string path = "/etc/udev/rules.d/";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        path = Path.Combine(path, "99-automount.rules");
        string fileText;
        if (File.Exists(path)) fileText = File.ReadAllText(path);
        else fileText = "";
        var fileBuilder = new StringBuilder(fileText);
        fileBuilder.AppendLine();
        fileBuilder.AppendLine("ACTION==\"add\", SUBSYSTEM==\"block\", ENV{ID_FS_TYPE}!=\"\", RUN+=\"/usr/bin/udisksctl mount -b $env{DEVNAME}\"");
        File.WriteAllText(path, fileBuilder.ToString());
        Run("udevadm", "control --reload-rules");
        Run("systemctl", "enable udisks2");
        UpdateProgress(94);

        // install compiler tools
        Run("pacman", "-S --noconfirm base-devel dotnet-sdk-8.0 git git-lfs");
        UpdateProgress(95);

        // install steam
        Run("pacman", "-S --noconfirm libxcomposite lib32-libxcomposite libxrandr lib32-libxrandr libgcrypt lib32-libgcrypt lib32-pipewire libpulse lib32-libpulse gtk2 lib32-gtk2");
        Run("pacman", "-S --noconfirm gnutls lib32-gnutls openal lib32-openal sqlite lib32-sqlite libcurl-compat lib32-libcurl-compat");
        Run("pacman", "-S --noconfirm xdg-desktop-portal xdg-desktop-portal-gtk");
        Run("pacman", "-S --noconfirm mangohud");
        Run("pacman", "-S --noconfirm steam");// TODO: how to accept correct defaults?
        UpdateProgress(98);

        // install wayland mouse util
        Run("pacman", "-S --noconfirm unclutter");
        UpdateProgress(100);
    }
    
    private static void ConfigureArch()
    {
        progressTask = "Configuring...";
        UpdateProgress(100);
    }
}
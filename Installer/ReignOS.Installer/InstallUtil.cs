using System.Text;
using System.Diagnostics;
using ReignOS.Core;

namespace ReignOS.Installer;

public class Partition
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

public class Drive
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

    private static void Run(string name, string args, ProcessUtil.ProcessOutputDelegate standardOut = null, ProcessUtil.ProcessInputDelegate getStandardInput = null, string workingDir = null)
    {
        if (!archRootMode)
        {
            ProcessUtil.Run(name, args, asAdmin:true, enterAdminPass:false, standardOut:standardOut, getStandardInput:getStandardInput, workingDir:workingDir);
        }
        else
        {
            string l = $"Arch-Chroot.Run: {name} {args}";
            Console.WriteLine(l);
            Views.MainView.ProcessOutput(l);
            ProcessUtil.Run("arch-chroot", $"/mnt bash -c \\\"{name} {args}\\\"", asAdmin:true, enterAdminPass:false, standardOut:standardOut, getStandardInput:getStandardInput, workingDir:workingDir);
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
        ProcessUtil.ProcessOutput += Views.MainView.ProcessOutput;
        progress = 0;
        archRootMode = false;
        ProcessUtil.KillHard("arch-chroot", true, out _);
        try
        {
            InstallBaseArch();
            InstallArchPackages();
            InstallReignOSRepo();
            InstallProgress?.Invoke("Done", progress);
        }
        catch (Exception e)
        {
            InstallProgress?.Invoke("Failed", progress);
            Console.WriteLine(e);
        }

        archRootMode = false;
        ProcessUtil.ProcessOutput -= Views.MainView.ProcessOutput;
        ProcessUtil.KillHard("arch-chroot", true, out _);
        Run("umount", "-R /var/cache/pacman/pkg");
        Run("umount", "-R /root/.nuget");
        Run("umount", "-R /mnt/boot");
        Run("umount", "-R /mnt");
    }

    private static void InstallBaseArch()
    {
        progressTask = "Installing base...";
        UpdateProgress(0);
        
        // unmount conflicting mounts
        Run("umount", "-R /var/cache/pacman/pkg");
        Run("umount", "-R /root/.nuget");
        Run("umount", "-R /mnt/boot");
        Run("umount", "-R /mnt");
        UpdateProgress(1);

        // make sure we re-format drives before installing
        Views.MainView.FormatExistingPartitions(efiPartition, ext4Partition);
        UpdateProgress(5);

        // mount partitions
        Run("mount", $"{ext4Partition.path} /mnt");
        Run("rm", "-rf /mnt/*");
        Run("mkdir", "-p /mnt/boot");
        Run("mount", $"{efiPartition.path} /mnt/boot");
        Run("rm", "-rf /mnt/boot/*");
        UpdateProgress(6);
        
        // store package cache on install drive
        Run("mkdir", "-p /mnt/var/cache/pacman/pkg");
        Run("mount", "--bind /mnt/var/cache/pacman/pkg /var/cache/pacman/pkg");
        UpdateProgress(10);
        
        // map nuget cache path to use install drive
        Run("mkdir", "-p /mnt/root/.nuget");
        Run("mount", "--bind /mnt/root/.nuget /root/.nuget");
        UpdateProgress(11);
        
        // install arch base
        Run("pacstrap", "/mnt base linux linux-headers linux-firmware systemd");
        Run("genfstab", "-U /mnt >> /mnt/etc/fstab");
        archRootMode = true;
        UpdateProgress(15);

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
        UpdateProgress(16);
        
        // install lib32-systemd
        Run("pacman", "-S --noconfirm lib32-systemd");
        UpdateProgress(17);
        
        // install network support
        Run("pacman", "-S --noconfirm networkmanager iwd iw iproute2 wireless_tools");
        path = "/mnt/etc/NetworkManager/";
        if (!Directory.Exists(path)) ProcessUtil.CreateDirectoryAdmin(path);
        path = Path.Combine(path, "NetworkManager.conf");
        var fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("[device]");
        fileBuilder.AppendLine("wifi.backend=iwd");
        void getStandardInput_NetworkManager(StreamWriter writer)
        {
            writer.WriteLine(fileBuilder);
            writer.Flush();
            writer.Close();
        }
        ProcessUtil.Run("tee", path, asAdmin:true, getStandardInput:getStandardInput_NetworkManager);
        Run("systemctl", "enable NetworkManager iwd");
        UpdateProgress(18);

        // install BT support
        Run("pacman", "-S --noconfirm bluez bluez-utils");
        Run("systemctl", "enable bluetooth");
        UpdateProgress(19);

        // configure time and lang
        Run("ln", "-sf /usr/share/zoneinfo/Region/City /etc/localtime");
        Run("hwclock", "--systohc");
        Run("locale-gen", "");
        Run("echo", "\"LANG=en_US.UTF-8\" > /etc/locale.conf");
        Run("pacman", "-S --noconfirm noto-fonts");
        Run("pacman", "-S --noconfirm noto-fonts-cjk");
        Run("pacman", "-S --noconfirm noto-fonts-extra");
        Run("pacman", "-S --noconfirm noto-fonts-emoji");
        Run("pacman", "-S --noconfirm ttf-dejavu ttf-liberation");
        Run("fc-cache", "-fv");
        UpdateProgress(20);

        // configure hosts file
        string hostname = $"reignos_{Guid.NewGuid()}";
        Run("echo", $"\"{hostname}\" > /etc/hostname");
        path = "/mnt/etc/hosts";
        fileBuilder = new StringBuilder(File.ReadAllText(path));
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
        UpdateProgress(21);
        
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
        UpdateProgress(22);

        // configure nvidia settings
        path = "/mnt/etc/modprobe.d/";
        if (!Directory.Exists(path)) ProcessUtil.CreateDirectoryAdmin(path);
        void getStandardInput_nvidia_conf(StreamWriter writer)
        {
            writer.WriteLine("options nvidia-drm modeset=1");
            writer.Flush();
            writer.Close();
        }
        ProcessUtil.Run("tee", Path.Combine(path, "nvidia.conf"), asAdmin:true, getStandardInput:getStandardInput_nvidia_conf);
        
        void getStandardInput_99nvidia_conf(StreamWriter writer)
        {
            writer.WriteLine("options nvidia NVreg_DynamicPowerManagement=0x02");
            writer.Flush();
            writer.Close();
        }
        ProcessUtil.Run("tee", Path.Combine(path, "99-nvidia.conf"), asAdmin:true, getStandardInput:getStandardInput_99nvidia_conf);
        UpdateProgress(23);

        // configure root pass
        Run("echo", "'root:gamer' | chpasswd");
        UpdateProgress(24);

        // install sudo
        Run("pacman", "-S --noconfirm sudo");
        Run("pacman", "-Qs sudo");
        UpdateProgress(25);
        
        // configure gamer user
        Run("useradd", "-m -G users -s /bin/bash gamer");
        Run("echo", "'gamer:gamer' | chpasswd");
        Run("usermod", "-aG wheel,audio,video,storage gamer");
        UpdateProgress(26);

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
        UpdateProgress(27);

        // make gamer user auto login
        path = "/mnt/etc/systemd/system/getty@tty1.service.d/";
        if (!Directory.Exists(path)) ProcessUtil.CreateDirectoryAdmin(path);
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("[Service]");
        fileBuilder.AppendLine("ExecStart=");
        fileBuilder.AppendLine("ExecStart=-/usr/bin/agetty --autologin gamer --noclear %I $TERM");
        void getStandardInput_autologin_conf(StreamWriter writer)
        {
            writer.WriteLine(fileBuilder.ToString());
            writer.Flush();
            writer.Close();
        }
        ProcessUtil.Run("tee", Path.Combine(path, "autologin.conf"), asAdmin:true, getStandardInput:getStandardInput_autologin_conf);
        Run("systemctl", "daemon-reload");
        Run("systemctl", "restart getty@tty1.service");
        UpdateProgress(28);

        // create post-install first-run install tasks
        path = "/mnt/home/gamer/FirstRun.sh";
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("#!/bin/bash");
        fileBuilder.AppendLine("sudo chown -R $USER /home/gamer/FirstRun_Invoke.sh");
        fileBuilder.AppendLine("sudo chmod +x /home/gamer/FirstRun_Invoke.sh");
        fileBuilder.AppendLine("/home/gamer/FirstRun_Invoke.sh");
        File.WriteAllText(path, fileBuilder.ToString());

        path = "/mnt/home/gamer/FirstRun_Invoke.sh";// create extra first-run backup of tasks in case user needs to run again
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("#!/bin/bash");
        fileBuilder.AppendLine("echo \"ReignOS: Running FirstRun post-install tasks...\"");
        fileBuilder.AppendLine();// make sure we have network still or install needs to fail until it does
        fileBuilder.AppendLine("NetworkUp=false");
        fileBuilder.AppendLine("for i in $(seq 1 30); do");
        fileBuilder.AppendLine("    # Try to ping Google's DNS server");
        fileBuilder.AppendLine("    if ping -c 1 -W 1 8.8.8.8 &> /dev/null; then");
        fileBuilder.AppendLine("        echo \"FirstRun: Network is up!\"");
        fileBuilder.AppendLine("        NetworkUp=true");
        fileBuilder.AppendLine("        sleep 1");
        fileBuilder.AppendLine("        break");
        fileBuilder.AppendLine("    else");
        fileBuilder.AppendLine("        echo \"FirstRun: Waiting for network... $i\"");
        fileBuilder.AppendLine("        sleep 1");
        fileBuilder.AppendLine("    fi");
        fileBuilder.AppendLine("done");
        fileBuilder.AppendLine();
        fileBuilder.AppendLine("if [ \"$NetworkUp\" = \"false\" ]; then");
        fileBuilder.AppendLine("    echo \"Network is required for FirstRun to complete. FAILED!\"");
        fileBuilder.AppendLine("    sleep infinity");
        fileBuilder.AppendLine("fi");

        fileBuilder.AppendLine();// install yay
        fileBuilder.AppendLine("echo \"Installing yay support...\"");
        fileBuilder.AppendLine("cd /home/gamer");
        fileBuilder.AppendLine("git clone https://aur.archlinux.org/yay.git");
        fileBuilder.AppendLine("cd /home/gamer/yay");
        fileBuilder.AppendLine("makepkg -si --noconfirm");
        fileBuilder.AppendLine("yay -Syy --noconfirm");

        fileBuilder.AppendLine();// install NUX support
        fileBuilder.AppendLine("echo \"Installing NUX support...\"");
        fileBuilder.AppendLine("yay -S supergfxctl --noconfirm");
        fileBuilder.AppendLine("sudo systemctl enable supergfxd.service");

        fileBuilder.AppendLine();// disable FirstRun
        fileBuilder.AppendLine("echo \"rebooting...\"");
        fileBuilder.AppendLine("echo -n > /home/gamer/FirstRun.sh");
        fileBuilder.AppendLine("reboot");
        File.WriteAllText(path, fileBuilder.ToString());
        UpdateProgress(29);

        // auto invoke launch after login
        path = "/mnt/home/gamer/.bash_profile";
        if (File.Exists(path)) fileText = File.ReadAllText(path);
        else fileText = "";
        fileBuilder = new StringBuilder(fileText);
        fileBuilder.AppendLine();
        fileBuilder.AppendLine("sudo chown -R $USER /home/gamer/FirstRun.sh");
        fileBuilder.AppendLine("sudo chmod +x /home/gamer/FirstRun.sh");
        fileBuilder.AppendLine("/home/gamer/FirstRun.sh");
        fileBuilder.AppendLine("sudo chown -R $USER /root/.nuget");
        fileBuilder.AppendLine("sudo chown -R $USER /home/gamer/ReignOS");
        fileBuilder.AppendLine("chmod +x /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Launch.sh");
        fileBuilder.AppendLine("/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Launch.sh --use-controlcenter");
        File.WriteAllText(path, fileBuilder.ToString());
        UpdateProgress(30);
    }
    
    private static void InstallArchPackages()
    {
        progressTask = "Installing packages...";
        archRootMode = true;

        // install misc apps
        Run("pacman", "-S --noconfirm nano evtest efibootmgr");
        Run("pacman", "-S --noconfirm dmidecode udev python");
        UpdateProgress(31);

        // install wayland
        Run("pacman", "-S --noconfirm xorg-server-xwayland wayland wayland-protocols wayland-utils");
        Run("pacman", "-S --noconfirm xorg-xev xbindkeys xorg-xinput xorg-xmodmap");
        UpdateProgress(32);

        // install x11
        Run("pacman", "-S --noconfirm xorg xorg-server xorg-xinit xf86-input-libinput xterm");
        UpdateProgress(33);

        // install wayland graphics drivers
        Run("pacman", "-S --noconfirm mesa lib32-mesa");
        Run("pacman", "-S --noconfirm libva-intel-driver intel-media-driver intel-ucode vulkan-intel lib32-vulkan-intel intel-gpu-tools");// Intel
        Run("pacman", "-S --noconfirm libva-mesa-driver lib32-libva-mesa-driver amd-ucode vulkan-radeon lib32-vulkan-radeon radeontop");// AMD
        Run("pacman", "-S --noconfirm vulkan-nouveau lib32-vulkan-nouveau");// Nvida
        Run("pacman", "-S --noconfirm vulkan-icd-loader lib32-vulkan-icd-loader lib32-libglvnd");
        Run("pacman", "-S --noconfirm vulkan-tools vulkan-mesa-layers lib32-vulkan-mesa-layers");
        Run("pacman", "-S --noconfirm egl-wayland");
        UpdateProgress(40);

        // install x11 graphics drivers
        Run("pacman", "-S --noconfirm xf86-video-intel xf86-video-amdgpu xf86-video-nouveau");
        Run("pacman", "-S --noconfirm glxinfo");
        UpdateProgress(46);

        // install compositors
        Run("pacman", "-S --noconfirm wlr-randr wlroots gamescope cage labwc weston");
        Run("pacman", "-S --noconfirm openbox");
        UpdateProgress(47);

        // install audio
        Run("pacman", "-S --noconfirm alsa-utils alsa-plugins");
        Run("pacman", "-S --noconfirm sof-firmware");
        Run("pacman", "-S --noconfirm pipewire pipewire-pulse pipewire-alsa pipewire-jack wireplumber");
        Run("systemctl", "--user enable pipewire pipewire-pulse wireplumber");
        UpdateProgress(50);

        // install power
        Run("pacman", "-S --noconfirm acpi acpid powertop power-profiles-daemon");
        Run("pacman", "-S --noconfirm python-gobject");
        Run("systemctl", "enable acpid power-profiles-daemon");
        UpdateProgress(51);

        // install auto-mount drives
        Run("pacman", "-S --noconfirm udiskie udisks2");
        string path = "/mnt/etc/udev/rules.d/";
        if (!Directory.Exists(path)) ProcessUtil.CreateDirectoryAdmin(path);
        path = Path.Combine(path, "99-automount.rules");
        string fileText;
        if (File.Exists(path)) fileText = File.ReadAllText(path);
        else fileText = "";
        var fileBuilder = new StringBuilder(fileText);
        fileBuilder.AppendLine();
        fileBuilder.AppendLine("ACTION==\"add\", SUBSYSTEM==\"block\", ENV{ID_FS_TYPE}!=\"\", RUN+=\"/usr/bin/udisksctl mount -b $env{DEVNAME}\"");
        void getStandardInput_99automount_rules(StreamWriter writer)
        {
            writer.WriteLine(fileBuilder.ToString());
            writer.Flush();
            writer.Close();
        }
        ProcessUtil.Run("tee", path, asAdmin:true, getStandardInput:getStandardInput_99automount_rules);
        Run("udevadm", "control --reload-rules");
        Run("systemctl", "enable udisks2");
        UpdateProgress(52);

        // install compiler tools
        Run("pacman", "-S --noconfirm base-devel dotnet-sdk-8.0 git git-lfs");
        UpdateProgress(55);

        // install steam
        Run("pacman", "-S --noconfirm libxcomposite lib32-libxcomposite libxrandr lib32-libxrandr libgcrypt lib32-libgcrypt lib32-pipewire libpulse lib32-libpulse gtk2 lib32-gtk2 gtk3 lib32-gtk3 nss lib32-nss glib2 lib32-glib2");
        Run("pacman", "-S --noconfirm libxss lib32-libxss libva lib32-libva libvdpau lib32-libvdpau");
        Run("pacman", "-S --noconfirm gnutls lib32-gnutls openal lib32-openal sqlite lib32-sqlite libcurl-compat lib32-libcurl-compat");
        Run("pacman", "-S --noconfirm xdg-desktop-portal xdg-desktop-portal-gtk");
        Run("pacman", "-S --noconfirm mangohud lib32-mangohud gamemode lib32-gamemode");
        Run("pacman", "-S --noconfirm glibc lib32-glibc");// needed by cef
        Run("pacman", "-S --noconfirm fontconfig lib32-fontconfig");// needed for fonts
        Run("pacman", "-S --noconfirm vulkan-dzn vulkan-gfxstream vulkan-intel vulkan-nouveau vulkan-radeon vulkan-swrast vulkan-virtio");// all steam options
        Run("pacman", "-S --noconfirm lib32-vulkan-dzn lib32-vulkan-gfxstream lib32-vulkan-intel lib32-vulkan-nouveau lib32-vulkan-radeon lib32-vulkan-swrast lib32-vulkan-virtio");// all steam options
        Run("pacman", "-S --noconfirm steam");//steam-native-runtime (use Arch libs)
        UpdateProgress(70);

        // remove AMD driver defaults steam might try to install
        Run("pacman", "-R amdvlk");
        Run("pacman", "-R lib32-amdvlk");
        Run("pacman", "-R amdvlk-pro");
        Run("pacman", "-R amdvlk-git");
        UpdateProgress(71);

        // remove Nvidia driver defaults steam might try to install
        Run("pacman", "-R nvidia-utils");
        Run("pacman", "-R lib32-nvidia-utils");
        UpdateProgress(72);

        // install wayland mouse util
        Run("pacman", "-S --noconfirm unclutter");
        UpdateProgress(73);
    }
    
    private static void InstallReignOSRepo()
    {
        progressTask = "Installing ReignOS Repo...";
        archRootMode = true;

        // clear package cache
        Run("rm", "-rf /var/cache/pacman/pkg/*");
        archRootMode = false;
        
        // clone ReignOS repo
        Run("git", "clone https://github.com/reignstudios/ReignOS.git /mnt/home/gamer/ReignOS");
        Run("NUGET_PACKAGES=/mnt/root/.nuget dotnet", "publish -r linux-x64 -c Release", workingDir:"/mnt/home/gamer/ReignOS/Managment");
        UpdateProgress(80);

        // copy wifi settings
        Run("mkdir", "-p /mnt/var/lib/iwd/");
        Run("cp", "-r /var/lib/iwd/* /mnt/var/lib/iwd/");
        
        UpdateProgress(100);
    }
}
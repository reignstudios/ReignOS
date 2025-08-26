using System.Text;
using System.Text.RegularExpressions;
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

public class FailIfError : IDisposable
{
    private bool failIfError;

    public FailIfError(bool shouldFailIfError)
    {
        failIfError = InstallUtil.failIfError;
        InstallUtil.failIfError = shouldFailIfError;
    }

    public void Dispose()
    {
        InstallUtil.failIfError = failIfError;
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
    public static bool cancel, failIfError = true;
    private static bool refreshIntegrity;

    private static void UpdateProgress(int progress)
    {
        InstallUtil.progress = progress;
        InstallProgress?.Invoke(progressTask, progress);
    }

    private static void Run(string name, string args, ProcessUtil.ProcessOutputDelegate standardOut = null, ProcessUtil.ProcessInputDelegate getStandardInput = null, string workingDir = null)
    {
        if (cancel) throw new Exception("Install Cancelled");

        if (!archRootMode)
        {
            ProcessUtil.Run(name, args, asAdmin:true, enterAdminPass:false, standardOut:standardOut, getStandardInput:getStandardInput, workingDir:workingDir, verboseLog:true);
        }
        else
        {
            const int retryMax = 5;
            int retryCount = 0;
            while (retryCount < retryMax)
            {
                ProcessUtil.Run("arch-chroot", $"/mnt bash -c \\\"{name} {args}\\\"", asAdmin:true, enterAdminPass:false, standardOut:standardOut, getStandardInput:getStandardInput, workingDir:workingDir, verboseLog:true);
                if (!cancel) break;// if it didn't cancel its success

                retryCount++;
                Log.WriteLine($"Attempt failed {retryCount} of {retryMax}. Retry in 5 seconds...");
                Thread.Sleep(5);
            }
        }
    }
    
    public static void Install(Partition efiPartition, Partition ext4Partition, bool refreshIntegrity)
    {
        InstallUtil.efiPartition = efiPartition;
        InstallUtil.ext4Partition = ext4Partition;
        InstallUtil.refreshIntegrity = refreshIntegrity;
        installThread = new Thread(InstallThread);
        installThread.Start();
    }

    private static void InstallThread()
    {
        cancel = false;
        failIfError = true;
        ProcessUtil.ProcessOutput += Views.MainView.ProcessOutput;
        progress = 0;
        archRootMode = false;
        ProcessUtil.KillHard("arch-chroot", true, out _);
        try
        {
            RefreshingInstallerIntegrity(refreshIntegrity);
            InstallBaseArch();
            InstallArchPackages();
            InstallReignOSRepo();
            InstallProgress?.Invoke("Done", progress);
        }
        catch (Exception e)
        {
            InstallProgress?.Invoke("Failed", progress);
            Log.WriteLine(e);
            Views.MainView.ProcessOutput(e.ToString());
        }

        cancel = false;// reset canceled to avoid exceptions
        failIfError = false;// do not trigger fails in cleanup
        archRootMode = false;
        ProcessUtil.KillHard("arch-chroot", true, out _);
        Run("umount", "-R /var/cache/pacman/pkg");
        Run("umount", "-R /root/.nuget");
        Run("umount", "-R /mnt/boot");
        Run("umount", "-R /mnt");
        ProcessUtil.ProcessOutput -= Views.MainView.ProcessOutput;
    }

    private static void RefreshingInstallerIntegrity(bool fullRefresh)
    {
        progressTask = "Refreshing Integrity (can take 10-15 min, please wait)...";
        UpdateProgress(0);
        archRootMode = false;

        static void standardOut(string line)
        {
            // do nothing: just used to keep output read
        }

        using (new FailIfError(false))
        {
            // update time
            Run("timedatectl", "set-ntp true");
            Run("hwclock", "--systohc");
            Thread.Sleep(1000);
            Run("timedatectl", "");// log time

            // update mirror list to use newer versions
            string countryCode = ProcessUtil.Run("curl", "-s https://ifconfig.co/country-iso", useBash:false).Trim();
            ProcessUtil.Run("reflector", $"--country {countryCode} --latest 50 --protocol https --sort rate --save /etc/pacman.d/mirrorlist", useBash: false, asAdmin: true);
            Run("pacman", "-Syyu --noconfirm");// always run after reflector

            // update keyring
            if (fullRefresh)
            {
                Run("pacman", "-Sy archlinux-keyring --noconfirm");
                Run("pacman-key", "--init", standardOut: standardOut);
                Run("pacman-key", "--populate archlinux", standardOut:standardOut);
                Run("pacman-key", "--refresh-keys", standardOut: standardOut);
                Run("pacman-key", "--updatedb", standardOut: standardOut);
            }
        }
    }

    private static void InstallBaseArch()
    {
        progressTask = "Installing base...";
        UpdateProgress(0);

        // setup for install
        using (new FailIfError(false))
        {
            // unmount conflicting mounts
            Run("umount", "-R /var/cache/pacman/pkg");
            Run("umount", "-R /root/.nuget");
            Run("umount", "-R /mnt/boot");
            Run("umount", "-R /mnt");
            UpdateProgress(1);

            // init pacman keyring
            static void standardOut(string line)
            {
                // do nothing: just used to keep output read
            }
            Run("pacman-key", "--init", standardOut: standardOut);
        }

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
        ProcessUtil.WriteAllTextAdmin(path, fileText);
        Run("pacman", "-Syy --noconfirm");
        UpdateProgress(16);
        
        // install lib32-systemd
        Run("pacman", "-S --noconfirm --needed lib32-systemd");
        UpdateProgress(17);
        
        // install network support
        Run("pacman", "-S --noconfirm --needed networkmanager iwd iw iproute2 wireless_tools");
        path = "/mnt/etc/NetworkManager/";
        if (!Directory.Exists(path)) ProcessUtil.CreateDirectoryAdmin(path);
        path = Path.Combine(path, "NetworkManager.conf");
        var fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("[device]");
        fileBuilder.AppendLine("wifi.backend=iwd");
        ProcessUtil.WriteAllTextAdmin(path, fileBuilder);
        Run("systemctl", "enable NetworkManager iwd");
        UpdateProgress(18);

        // install BT support
        Run("pacman", "-S --noconfirm --needed bluez bluez-utils");
        Run("systemctl", "enable bluetooth");
        UpdateProgress(19);

        // install thunderbold support (needed for eGPUs)
        Run("pacman", "-S --noconfirm --needed bolt");
        Run("systemctl", "enable bolt.service");
        UpdateProgress(19);

        // configure time and lang
        Run("ln", "-sf /usr/share/zoneinfo/Region/City /etc/localtime");
        Run("hwclock", "--systohc");
        Run("echo", "'LANG=en_US.UTF-8' > /etc/locale.conf");
        Run("echo", "'en_US.UTF-8 UTF-8' > /etc/locale.gen");
        Run("locale-gen", "");
        Run("pacman", "-S --noconfirm --needed noto-fonts");
        Run("pacman", "-S --noconfirm --needed noto-fonts-cjk");
        Run("pacman", "-S --noconfirm --needed noto-fonts-extra");
        Run("pacman", "-S --noconfirm --needed noto-fonts-emoji");
        Run("pacman", "-S --noconfirm --needed ttf-dejavu");
        Run("pacman", "-S --noconfirm --needed ttf-liberation");
        Run("fc-cache", "-fv");
        UpdateProgress(20);

        // configure hosts file
        string id = Guid.NewGuid().ToString();
        id = id.Split('-')[0];
        string hostname = $"reignos-{id}";
        Run("echo", $"\"{hostname}\" > /etc/hostname");
        path = "/mnt/etc/hosts";
        fileBuilder = new StringBuilder(File.ReadAllText(path));
        fileBuilder.AppendLine("127.0.0.1 localhost");
        fileBuilder.AppendLine("::1 localhost");
        fileBuilder.AppendLine("127.0.1.1 reignos.localdomain reignos");
        ProcessUtil.WriteAllTextAdmin(path, fileBuilder);
        UpdateProgress(21);
        
        // configure systemd-boot
        Run("bootctl", "install");
        path = "/mnt/boot/loader/entries/arch.conf";
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("title ReignOS");
        fileBuilder.AppendLine("linux /vmlinuz-linux");
        fileBuilder.AppendLine("initrd /initramfs-linux.img");
        string partitionInfoResult = ProcessUtil.Run("blkid", ext4Partition.path, asAdmin:true, useBash:false);
        var match = Regex.Match(partitionInfoResult, @".*?PARTUUID=""(.*?)""");
        if (match.Success) fileBuilder.AppendLine($"options root=PARTUUID={match.Groups[1].Value} rw rootwait");
        else fileBuilder.AppendLine($"options root={ext4Partition.path} rw rootwait");
        ProcessUtil.WriteAllTextAdmin(path, fileBuilder);
        Run("systemctl", "enable systemd-networkd systemd-resolved");
        UpdateProgress(22);

        // ensure systemd-boot defaults to arch kernel and not chimera
        path = "/mnt/boot/loader/loader.conf";
        fileText = "";
        if (File.Exists(path)) fileText = ProcessUtil.ReadAllTextAdmin(path);
        fileText += "\ndefault arch.conf";
        ProcessUtil.WriteAllTextAdmin(path, fileText);
        UpdateProgress(23);

        // configure root pass
        Run("echo", "'root:gamer' | chpasswd");
        UpdateProgress(24);

        // install sudo
        Run("pacman", "-S --noconfirm --needed sudo");
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
        ProcessUtil.WriteAllTextAdmin(path, fileText);
        UpdateProgress(27);

        // make gamer user auto login
        path = "/mnt/etc/systemd/system/getty@tty1.service.d/";
        if (!Directory.Exists(path)) ProcessUtil.CreateDirectoryAdmin(path);
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("[Service]");
        fileBuilder.AppendLine("ExecStart=");
        fileBuilder.AppendLine("ExecStart=-/usr/bin/agetty --autologin gamer --noclear %I $TERM");
        ProcessUtil.WriteAllTextAdmin(Path.Combine(path, "autologin.conf"), fileBuilder);
        Run("systemctl", "daemon-reload");
        Run("systemctl", "restart getty@tty1.service");
        UpdateProgress(28);

        // create post-install first-run install tasks
        path = "/mnt/home/gamer/FirstRun.sh";
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("#!/bin/bash");
        fileBuilder.AppendLine("set -e");
        fileBuilder.AppendLine("sudo chown -R $USER /home/gamer/FirstRun_Invoke.sh");
        fileBuilder.AppendLine("chmod +x /home/gamer/FirstRun_Invoke.sh");
        fileBuilder.AppendLine("/home/gamer/FirstRun_Invoke.sh");
        fileBuilder.AppendLine("reboot");
        File.WriteAllText(path, fileBuilder.ToString());

        path = "/mnt/home/gamer/FirstRun_Invoke.sh";// create extra first-run backup of tasks in case user needs to run again
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("#!/bin/bash");
        fileBuilder.AppendLine("echo \"ReignOS: Running FirstRun post-install tasks...\"");
        fileBuilder.AppendLine("echo \"NOTE: This will take some time, let it finish!\"");
        fileBuilder.AppendLine("sleep 5");

        fileBuilder.AppendLine();// add retry install methods
        fileBuilder.AppendLine("pacman_retry() {");
        fileBuilder.AppendLine("    local tries=5");
        fileBuilder.AppendLine("    for ((i=1; i<=tries; i++)); do");
        fileBuilder.AppendLine("        sudo pacman --noconfirm --needed \"$@\" && return 0");
        fileBuilder.AppendLine("        echo \"Retry $i/$tries failed... retrying in 5s\"");
        fileBuilder.AppendLine("        sleep 5");
        fileBuilder.AppendLine("    done");
        fileBuilder.AppendLine("    return 1");
        fileBuilder.AppendLine("}");
        fileBuilder.AppendLine();
        fileBuilder.AppendLine("yay_retry() {");
        fileBuilder.AppendLine("    local tries=5");
        fileBuilder.AppendLine("    for ((i=1; i<=tries; i++)); do");
        fileBuilder.AppendLine("        yay --noconfirm --needed \"$@\" && return 0");
        fileBuilder.AppendLine("        echo \"Retry $i/$tries failed... retrying in 5s\"");
        fileBuilder.AppendLine("        sleep 5");
        fileBuilder.AppendLine("    done");
        fileBuilder.AppendLine("    return 1");
        fileBuilder.AppendLine("}");

        fileBuilder.AppendLine();// make sure we have network still or install needs to fail until it does
        fileBuilder.AppendLine("NetworkUp=false");
        fileBuilder.AppendLine("for i in $(seq 1 30); do");
        fileBuilder.AppendLine("    # Try to ping Google's DNS server");
        fileBuilder.AppendLine("    if ping -c 1 -W 2 google.com &> /dev/null; then");
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

        fileBuilder.AppendLine();// correct managment user
        fileBuilder.AppendLine("sudo chown -R $USER /root/.nuget");
        fileBuilder.AppendLine("sudo chown -R $USER /home/gamer/ReignOS");

        fileBuilder.AppendLine();// update time
        fileBuilder.AppendLine("echo 'sync time...'");
        fileBuilder.AppendLine("sudo timedatectl set-ntp true");
        fileBuilder.AppendLine("sudo hwclock --systohc");
        fileBuilder.AppendLine("sleep 1");
        fileBuilder.AppendLine("timedatectl");// log time
        fileBuilder.AppendLine("sleep 1");

        fileBuilder.AppendLine();// update mirror list to use newer versions
        fileBuilder.AppendLine("echo 'refresh mirror list...'");
        fileBuilder.AppendLine("COUNTRY=$(curl -s https://ifconfig.co/country-iso)");
        fileBuilder.AppendLine("echo 'Country = $COUNTRY'");
        fileBuilder.AppendLine("echo 'Running: sudo reflector --country $COUNTRY --latest 50 --protocol https --sort rate --save /etc/pacman.d/mirrorlist'");
        fileBuilder.AppendLine("sudo reflector --country $COUNTRY --latest 50 --protocol https --sort rate --save /etc/pacman.d/mirrorlist");
        fileBuilder.AppendLine("sleep 1");

        /*fileBuilder.AppendLine();// update keyring
        fileBuilder.AppendLine("echo 'refresh keyring...'");
        fileBuilder.AppendLine("sudo pacman -Sy archlinux-keyring --noconfirm");
        fileBuilder.AppendLine("sudo pacman-key --init");
        fileBuilder.AppendLine("sudo pacman-key --populate archlinux");
        fileBuilder.AppendLine("sudo pacman-key --refresh-keys");
        fileBuilder.AppendLine("sudo pacman-key --updatedb");
        fileBuilder.AppendLine("sleep 5");*/

        fileBuilder.AppendLine();// update pacman
        fileBuilder.AppendLine("sudo pacman -Syyu --noconfirm");
        fileBuilder.AppendLine("sleep 1");

        fileBuilder.AppendLine();// make sure git-lfs is hooked up
        fileBuilder.AppendLine("echo \"Hook up git-lfs...\"");
        fileBuilder.AppendLine("git lfs install");

        fileBuilder.AppendLine();// stop on any error
        fileBuilder.AppendLine("set -e");

        fileBuilder.AppendLine();// install yay
        fileBuilder.AppendLine("echo \"Installing yay support...\"");
        fileBuilder.AppendLine("if [ ! -d \"/home/gamer/yay\" ]; then");
        fileBuilder.AppendLine("    cd /home/gamer");
        fileBuilder.AppendLine("echo 'Running: git clone https://aur.archlinux.org/yay.git'");
        fileBuilder.AppendLine("    git clone https://aur.archlinux.org/yay.git");
        fileBuilder.AppendLine("    cd /home/gamer/yay");
        fileBuilder.AppendLine("    makepkg -si --noconfirm");
        fileBuilder.AppendLine("fi");
        fileBuilder.AppendLine("sleep 1");

        fileBuilder.AppendLine();// update yay
        fileBuilder.AppendLine("set +e");// disable errors
        fileBuilder.AppendLine("yay -Syu --noconfirm");
        fileBuilder.AppendLine("set -e");// enabled errors
        fileBuilder.AppendLine("sleep 1");

        fileBuilder.AppendLine();// install MUX support
        fileBuilder.AppendLine("echo \"Installing NUX support...\"");
        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed supergfxctl");

        fileBuilder.AppendLine();// install extra fonts
        fileBuilder.AppendLine("echo \"Installing extra fonts...\"");
        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed ttf-ms-fonts");
        fileBuilder.AppendLine("fc-cache -fv");

        fileBuilder.AppendLine();// install steamcmd
        fileBuilder.AppendLine("echo \"Installing steamcmd...\"");
        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed steamcmd");

        fileBuilder.AppendLine();// install ProtonGE
        fileBuilder.AppendLine("echo \"Installing ProtonGE...\"");
        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed proton-ge-custom-bin");

        fileBuilder.AppendLine();// install DeckyLoader
        fileBuilder.AppendLine("echo \"Installing DeckyLoader...\"");
        fileBuilder.AppendLine("sudo pacman_retry -S --noconfirm --needed jq");
        fileBuilder.AppendLine("curl -L https://github.com/SteamDeckHomebrew/decky-installer/releases/latest/download/install_release.sh | sh");

        fileBuilder.AppendLine();// install misc drivers
        fileBuilder.AppendLine("echo \"Installing Misc Drivers...\"");
        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed bcm20702a1-firmware");

        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed ayaneo-platform-dkms-git");
        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed ayn-platform-dkms-git");

        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed rtl8812au-dkms-git");
        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed rtl8814au-dkms-git");
        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed rtl88x2bu-dkms-git");
        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed rtl8821au-dkms-git");
        
        fileBuilder.AppendLine("yay_retry -S --noconfirm --needed ryzenadj");

        fileBuilder.AppendLine();// disable stop on any error
        fileBuilder.AppendLine("set +e");

        fileBuilder.AppendLine();// set volume to 100%
        fileBuilder.AppendLine("echo \"Setting volume to 100%...\"");
        fileBuilder.AppendLine("pactl set-sink-volume @DEFAULT_SINK@ 100%");

        fileBuilder.AppendLine();// disable FirstRun
        fileBuilder.AppendLine("echo \"disable FirstRun...\"");
        fileBuilder.AppendLine("echo -n > /home/gamer/FirstRun.sh");

        fileBuilder.AppendLine();// run main updates scripts at least once
        fileBuilder.AppendLine("echo \"Running main update script...\"");
        fileBuilder.AppendLine("sudo chown -R $USER /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Update.sh");
        fileBuilder.AppendLine("chmod +x /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Update.sh");
        fileBuilder.AppendLine("/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Update.sh");
        fileBuilder.AppendLine("reboot");
        File.WriteAllText(path, fileBuilder.ToString());
        UpdateProgress(29);

        // add login launch script
        path = "/mnt/home/gamer/ReignOS_Launch.sh";
        fileBuilder = new StringBuilder();
        fileBuilder.AppendLine("#!/bin/bash");
        fileBuilder.AppendLine("set -e");
        fileBuilder.AppendLine("sudo chown -R $USER /home/gamer/FirstRun.sh");
        fileBuilder.AppendLine("chmod +x /home/gamer/FirstRun.sh");
        fileBuilder.AppendLine("/home/gamer/FirstRun.sh");
        fileBuilder.AppendLine("chmod +x /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Launch.sh");
        fileBuilder.AppendLine("/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Launch.sh --use-controlcenter");
        File.WriteAllText(path, fileBuilder.ToString());
        UpdateProgress(30);

        // auto invoke launch after login
        path = "/mnt/home/gamer/.bash_profile";
        if (File.Exists(path)) fileText = File.ReadAllText(path);
        else fileText = "";
        fileBuilder = new StringBuilder(fileText);
        fileBuilder.AppendLine();
        fileBuilder.AppendLine("if [[ \"$(tty)\" == \"/dev/tty1\" && -n \"$XDG_VTNR\" && \"$XDG_VTNR\" -eq 1 ]]; then");
        fileBuilder.AppendLine("    sudo chown -R $USER /home/gamer/ReignOS_Launch.sh");
        fileBuilder.AppendLine("    chmod +x /home/gamer/ReignOS_Launch.sh");
        fileBuilder.AppendLine("    /home/gamer/ReignOS_Launch.sh");
        fileBuilder.AppendLine("fi");
        File.WriteAllText(path, fileBuilder.ToString());
        UpdateProgress(31);
    }
    
    private static void InstallArchPackages()
    {
        progressTask = "Installing packages...";
        archRootMode = true;

        // install misc apps
        Run("pacman", "-S --noconfirm --needed nano evtest efibootmgr");
        Run("pacman", "-S --noconfirm --needed dmidecode hwinfo sysstat udev curl");
        Run("pacman", "-S --noconfirm --needed python hidapi python-hidapi libusb usbutils");
        Run("pacman", "-S --noconfirm --needed rsync");
        Run("pacman", "-S --noconfirm --needed wget");
        Run("pacman", "-S --noconfirm --needed reflector");
        UpdateProgress(32);

        // install firmware update support
        Run("pacman", "-S --noconfirm --needed fwupd");
        Run("pacman", "-S --noconfirm --needed dkms");
        UpdateProgress(33);

        // install wayland
        Run("pacman", "-S --noconfirm --needed xorg-server-xwayland wayland lib32-wayland wayland-protocols wayland-utils");
        Run("pacman", "-S --noconfirm --needed xorg-xev xbindkeys xorg-xinput xorg-xmodmap");
        UpdateProgress(34);

        // install x11
        Run("pacman", "-S --noconfirm --needed xorg xorg-server xorg-xinit xf86-input-libinput xterm");
        UpdateProgress(35);

        // install wayland graphics drivers
        Run("pacman", "-S --noconfirm --needed mesa lib32-mesa");
        Run("pacman", "-S --noconfirm --needed libva-intel-driver intel-media-driver intel-ucode vulkan-intel lib32-vulkan-intel intel-gpu-tools");// Intel
        Run("pacman", "-S --noconfirm --needed libva-mesa-driver lib32-libva-mesa-driver amd-ucode vulkan-radeon lib32-vulkan-radeon radeontop");// AMD
        Run("pacman", "-S --noconfirm --needed vulkan-nouveau lib32-vulkan-nouveau");// Nvida
        Run("pacman", "-S --noconfirm --needed vulkan-icd-loader lib32-vulkan-icd-loader libglvnd lib32-libglvnd");
        Run("pacman", "-S --noconfirm --needed vulkan-tools vulkan-mesa-layers lib32-vulkan-mesa-layers");
        Run("pacman", "-S --noconfirm --needed egl-wayland");
        Run("pacman", "-S --noconfirm --needed eglexternalplatform");
        UpdateProgress(40);
        
        // codex / misc
        Run("pacman", "-S --noconfirm --needed vdpauinfo");
        Run("pacman", "-S --noconfirm --needed ffmpeg gstreamer gst-plugins-base gst-plugins-good gst-plugins-bad gst-plugins-ugly gst-libav");
        Run("pacman", "-S --noconfirm --needed libva libva-utils gstreamer-vaapi");
        Run("pacman", "-S --noconfirm --needed libvdpau-va-gl mesa-vdpau");
        Run("pacman", "-S --noconfirm --needed libdvdread libdvdnav libdvdcss libbluray");
        UpdateProgress(54);

        // install x11 graphics drivers
        Run("pacman", "-S --noconfirm --needed xf86-video-intel xf86-video-amdgpu xf86-video-nouveau");
        Run("pacman", "-S --noconfirm --needed glxinfo");
        UpdateProgress(56);

        // install compositors
        Run("pacman", "-S --noconfirm --needed wlr-randr gamescope cage labwc weston");
        Run("pacman", "-S --noconfirm --needed openbox");
        UpdateProgress(57);

        // install desktop portal
        Run("pacman", "-S --noconfirm --needed xdg-desktop-portal xdg-desktop-portal-wlr xdg-desktop-portal-kde xdg-desktop-portal-gtk");
        UpdateProgress(58);

        // install audio
        Run("pacman", "-S --noconfirm --needed alsa-firmware alsa-utils alsa-plugins alsa-ucm-conf");
        Run("pacman", "-S --noconfirm --needed sof-firmware");
        using (new FailIfError(false)) Run("pacman", "-Rdd --noconfirm jack2");// force remove jack2 let pipewire-jack install instead (installed from ffmpeg)
        Run("pacman", "-S --noconfirm --needed pipewire pipewire-pulse pipewire-alsa pipewire-jack wireplumber");
        Run("systemctl", "--user enable pipewire pipewire-pulse wireplumber");
        Run("systemctl", "--user enable pipewire.socket pipewire.service pipewire-pulse.socket pipewire-pulse.service");
        UpdateProgress(60);

        // install power
        Run("pacman", "-S --noconfirm --needed acpi powertop power-profiles-daemon");
        Run("pacman", "-S --noconfirm --needed python-gobject");
        Run("systemctl", "enable power-profiles-daemon");
        UpdateProgress(61);

        // install ssh
        Run("pacman", "-S --noconfirm --needed net-tools openssh");
        Run("systemctl", "enable sshd");

        // install auto-mount drives
        Run("pacman", "-S --noconfirm --needed udiskie udisks2");
        string path = "/mnt/etc/udev/rules.d/";
        if (!Directory.Exists(path)) ProcessUtil.CreateDirectoryAdmin(path);
        path = Path.Combine(path, "99-automount.rules");
        string fileText;
        if (File.Exists(path)) fileText = File.ReadAllText(path);
        else fileText = "";
        var fileBuilder = new StringBuilder(fileText);
        fileBuilder.AppendLine();
        fileBuilder.AppendLine("ACTION==\"add\", SUBSYSTEM==\"block\", ENV{ID_FS_TYPE}!=\"\", RUN+=\"/usr/bin/udisksctl mount -b $env{DEVNAME}\"");
        ProcessUtil.WriteAllTextAdmin(path, fileBuilder);
        Run("udevadm", "control --reload-rules");
        Run("systemctl", "enable udisks2");
        UpdateProgress(62);

        // install compiler tools
        Run("pacman", "-S --noconfirm --needed base-devel dotnet-sdk-8.0 git git-lfs");
        UpdateProgress(65);

        // install steam
        Run("pacman", "-S --noconfirm --needed libxcomposite lib32-libxcomposite libxrandr lib32-libxrandr libgcrypt lib32-libgcrypt lib32-pipewire libpulse lib32-libpulse nss lib32-nss glib2 lib32-glib2");
        Run("pacman", "-S --noconfirm --needed gtk2 lib32-gtk2 gtk3 lib32-gtk3 gtk4");
        Run("pacman", "-S --noconfirm --needed libxss lib32-libxss libva lib32-libva libvdpau lib32-libvdpau");
        Run("pacman", "-S --noconfirm --needed gnutls lib32-gnutls openal lib32-openal sqlite lib32-sqlite libcurl-compat lib32-libcurl-compat");
        Run("pacman", "-S --noconfirm --needed mangohud lib32-mangohud gamemode lib32-gamemode");
        Run("pacman", "-S --noconfirm --needed glibc lib32-glibc");// needed by cef
        Run("pacman", "-S --noconfirm --needed fontconfig lib32-fontconfig");// needed for fonts
        Run("pacman", "-S --noconfirm --needed vulkan-dzn vulkan-gfxstream vulkan-intel vulkan-nouveau vulkan-radeon vulkan-swrast vulkan-virtio");// all steam options
        Run("pacman", "-S --noconfirm --needed lib32-vulkan-dzn lib32-vulkan-gfxstream lib32-vulkan-intel lib32-vulkan-nouveau lib32-vulkan-radeon lib32-vulkan-swrast lib32-vulkan-virtio");// all steam options
        Run("pacman", "-S --noconfirm --needed steam");//steam-native-runtime (use Arch libs)
        UpdateProgress(80);

        using (new FailIfError(false))
        {
            // remove AMD driver defaults steam might try to install
            Run("pacman", "-R amdvlk");
            Run("pacman", "-R lib32-amdvlk");
            Run("pacman", "-R amdvlk-pro");
            Run("pacman", "-R amdvlk-git");
            UpdateProgress(81);

            // remove Nvidia driver defaults steam might try to install
            Run("pacman", "-R nvidia-utils");
            Run("pacman", "-R lib32-nvidia-utils");
            UpdateProgress(82);
        }

        // install wayland mouse util
        Run("pacman", "-S --noconfirm --needed unclutter");
        UpdateProgress(83);

        // install flatpak
        Run("pacman", "-S --noconfirm --needed flatpak");
        UpdateProgress(84);

        // install kde
        Run("pacman", "-S --noconfirm --needed zip unzip gzip bzip2 7zip xz");
        Run("pacman", "-S --noconfirm --needed plasma konsole dolphin kate ark exfatprogs dosfstools partitionmanager");
        Run("pacman", "-S --noconfirm --needed btrfs-progs ntfs-3g");
        Run("pacman", "-S --noconfirm --needed maliit-keyboard");
        Run("pacman", "-S --noconfirm --needed qt5-wayland qt6-wayland");
        Run("pacman", "-S --noconfirm --needed wmctrl");
        UpdateProgress(85);

        // install gparted
        Run("pacman", "-S --noconfirm --needed gparted");
        UpdateProgress(86);

        // install linux-tools
        Run("pacman", "-S --noconfirm --needed linux-tools");
        UpdateProgress(87);
    }
    
    private static void InstallReignOSRepo()
    {
        progressTask = "Installing ReignOS Repo...";
        archRootMode = true;

        // enable timezone
        Run("systemctl", "enable systemd-timesyncd");
        
        // enable time sync
        Run("timedatectl", "set-ntp true");
        Run("hwclock", "--systohc");

        // clear package cache
        Run("rm", "-rf /var/cache/pacman/pkg/*");
        archRootMode = false;
        
        // clone ReignOS repo
        Run("git", "clone https://github.com/reignstudios/ReignOS.git /mnt/home/gamer/ReignOS");
        Run("NUGET_PACKAGES=/mnt/root/.nuget dotnet", "publish -r linux-x64 -c Release", workingDir:"/mnt/home/gamer/ReignOS/Managment");
        UpdateProgress(90);

        // configure reignos names
        string path = "/mnt/etc/lsb-release";
        string text =
@"DISTRIB_ID=""ReignOS""
DISTRIB_RELEASE=""rolling""
DISTRIB_DESCRIPTION=""ReignOS""";
        ProcessUtil.WriteAllTextAdmin(path, text);

        path = "/mnt/etc/os-release";
        text =
@"NAME=""ReignOS""
PRETTY_NAME=""ReignOS""
ID=reignos
BUILD_ID=rolling
ANSI_COLOR=""38;2;23;147;209""
HOME_URL=""http://reign-os.com/""
DOCUMENTATION_URL=""https://wiki.archlinux.org/""
SUPPORT_URL=""https://bbs.archlinux.org/""
BUG_REPORT_URL=""https://gitlab.archlinux.org/groups/archlinux/-/issues""
PRIVACY_POLICY_URL=""https://terms.archlinux.org/docs/privacy-policy/""
LOGO=archlinux-logo";
        ProcessUtil.WriteAllTextAdmin(path, text);
        UpdateProgress(95);

        // copy wifi settings
        Run("mkdir", "-p /mnt/var/lib/iwd/");
        Run("cp", "-r /var/lib/iwd/* /mnt/var/lib/iwd/");
        UpdateProgress(100);
    }
}
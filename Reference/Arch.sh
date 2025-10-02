# Gamescope args: https://github.com/ValveSoftware/gamescope/blob/f1f105b3a95b4fec5c92e8a10e6927cbb76fe804/src/main.cpp#L209

# to install installer apps on existing Arch install
sudo pacman -S arch-install-scripts

# to fix missing install
# boot back into USB image
lsblk
mount /dev/nvme0n1p2 /mnt
mount /dev/nvme0n1p1 /mnt/boot
arch-chroot /mnt

# connect to network
iwctl device list
iwctl station wlan0 scan
iwctl station wlan0 get-networks
iwctl station wlan0 connect <SSID>
iwctl station wlan0 show

# create partitions
fdisk -l
fdisk /dev/nvme0n1
# TODO: create partition table
#((512 * 1024 * 1024) / 512) + 2048 = 1050624 [first partition offset size at 512mb in sections]
# When creating EFI /dev/nvme0n1p1 partition. Hit 't' then '1' to mark as EFI partition (this is needed for some tools)
# when done configuring partion hit 'w' to write changes

parted
(parted) print devices
(parted) select <device>
(parted) mklabel gpt
(parted) mkpart ESP fat32 1MiB 513MiB #1mb header, 513mb ensured 512mb
(parted) set 1 boot on
(parted) set 1 esp on
(parted) mkpart primary ext4 513MiB 100%
(parted) quit

# format partitions
mkfs.fat -F32 /dev/nvme0n1p1
mkfs.ext4 /dev/nvme0n1p2

# mount partitions
mount /dev/nvme0n1p2 /mnt
mkdir -p /mnt/boot
mount /dev/nvme0n1p1 /mnt/boot

#install linux firmware
pacstrap /mnt base linux linux-firmware systemd

# Generate fstab
genfstab -U /mnt >> /mnt/etc/fstab

# Chroot into the Installed System
arch-chroot /mnt

# install apps
pacman -S nano
pacman -S evtest

# add mirror list
nano /etc/pacman.conf
# uncomment existing lines...
# [multilib]
# Include = /etc/pacman.d/mirrorlist
pacman -Syy

# install network
pacman -S networkmanager iwd netctl iproute2
systemctl enable NetworkManager iwd

# configure timezone
ln -sf /usr/share/zoneinfo/Region/City /etc/localtime
hwclock --systohc
locale-gen
echo "LANG=en_US.UTF-8" > /etc/locale.conf

# setup hostname
echo "reignos" > /etc/hostname
nano /etc/hosts
# add these lines...
# 127.0.0.1   localhost
# ::1         localhost
# 127.0.1.1   reignos.localdomain reignos

# install systemd
bootctl install
nano /boot/loader/entries/arch.conf
# add these lines...
# title ReignOS
# linux /vmlinuz-linux
# initrd /initramfs-linux.img
# options root=/dev/nvme0n1p2 rw

# enable systemd
systemctl enable systemd-networkd
systemctl enable systemd-resolved

# set root password
passwd root
# use "gamer" as password

# create local user
useradd -m -G users -s /bin/bash gamer
passwd gamer
usermod -aG wheel,audio,video,storage gamer

# add new user to sudo
pacman -S sudo
nano /etc/sudoers
# %wheel ALL=(ALL:ALL) ALL # uncomment
#gamer ALL=(ALL) NOPASSWD:ALL # add line (disable the need for sudo pass)

# finish install
exit
umount -R /mnt
reboot

# login as root after reboot
# mkinitcpio -P
# chmod 666 /dev/dri/*



#auto login
mkdir /etc/systemd/system/getty@tty1.service.d/
nano autologin.conf # add lines below
#[Service]
#ExecStart=
#ExecStart=-/usr/bin/agetty --autologin gamer --noclear %I $TERM
sudo systemctl daemon-reload
sudo systemctl restart getty@tty1.service

# auto start ReignOS launch.sh
nano /home/gamer/.bash_profile # add lines below
#/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Launch.sh --use-controlcenter

# ReignOS Splashscreen
pacman -S plymouth
nano /etc/mkinitcpio.conf # add 'plymouth' after 'base'
#HOOKS=(base plymouth ...)
sudo mkinitcpio -P # reconfigure
nano /boot/loader/entries/arch.conf # add 'quiet splash' after 'rw'
#options root=<path> rw quiet splash

mkdir /usr/share/plymouth/themes/reignos
cp /home/gamer/ReignOS/Splash/ReignOS.png /usr/share/plymouth/themes/reignos
nano /usr/share/plymouth/themes/reignos/reignos.plymouth # copy stuff from Splash folder
nano /usr/share/plymouth/themes/reignos/script # copy stuff from Splash folder
plymouth-set-default-theme -R reignos

# install apps/tools
pacman -S dmidecode udev python

# install Wayland and XWayland support
pacman -S xorg-server-xwayland wayland wayland-protocols
pacman -S xorg-xev xbindkeys xorg-xinput xorg-xmodmap

# install X11
pacman -S xorg xorg-server xorg-xinit xf86-input-libinput xterm

nano ~/.xinitrc
# add lines:
#!/bin/bash
#xrandr --output default --rotate left
#xsetroot -cursor_name left_ptr
#MESA_GL_VERSION_OVERRIDE=1.3 steam -bigpicture
chmod +x ~/.xinitrc

# install Wayland graphics drivers
pacman -S mesa lib32-mesa
pacman -S libva-intel-driver intel-media-driver intel-ucode vulkan-intel lib32-vulkan-intel intel-gpu-tools
pacman -S libva-mesa-driver lib32-libva-mesa-driver amd-ucode vulkan-radeon lib32-vulkan-radeon radeontop
pacman -S vulkan-nouveau lib32-vulkan-nouveau
#pacman -S nvidia nvidia-utils lib32-nvidia-utils nvidia-settings nvidia-prime
pacman -S vulkan-icd-loader lib32-vulkan-icd-loader lib32-libglvnd
pacman -S vulkan-tools vulkan-mesa-layers lib32-vulkan-mesa-layers
pacman -S egl-wayland

# install X11 drivers
pacman -S xf86-video-intel xf86-video-amdgpu xf86-video-nouveau
pacman -S glxinfo

#install compositors
pacman -S wlr-randr
pacman -S wlroots
pacman -S labwc
pacman -S cage
pacman -S gamescope

# install audio
pacman -S alsa-utils alsa-plugins
pacman -S sof-firmware
pacman -S pipewire pipewire-pulse pipewire-alsa pipewire-jack
# NOTE: "amixer set Master 5%+" or "amixer set Master 5%-" needs to be called by C# app using libinput
#sudo pacman -S beep # call "beep to play current volume level"

systemctl --user enable pipewire
systemctl --user start pipewire

systemctl --user enable pipewire-pulse
systemctl --user start pipewire-pulse

# install power managment
pacman -S acpi acpid powertop power-profiles-daemon # tlp (other power option)
pacman -S python-gobject

systemctl enable acpid
systemctl start acpid

systemctl enable power-profiles-daemon
systemctl start power-profiles-daemon

#sudo systemctl enable tlp
#sudo systemctl start tlp

# install GPU power suspend states (are these needed? IDK if they're)
nano /boot/loader/entries/arch.conf
# add to the end of 'options'
i915.enable_dc=2 i915.enable_psr=1
amdgpu.dpm=1 amdgpu.ppfeaturemask=0xffffffff amdgpu.dc=1
nouveau.pstate=1 nouveau.perflvl=N nouveau.perflvl_wr=7777 nouveau.config=NvGspRm=1
nvidia_drm.modeset=1
# mem_sleep_default=deep (this can cause some systems to fail to wake)
acpi_osi=Linux

# /etc/modprobe.d/nvidia.conf
options nvidia-drm modeset=1

#/etc/modprobe.d/99-nvidia.conf
options nvidia NVreg_DynamicPowerManagement=0x02

# auto mount drives
sudo pacman -S udiskie udisks2
sudo nano /etc/udev/rules.d/99-automount.rules
# ADD LINE: ACTION=="add", SUBSYSTEM=="block", ENV{ID_FS_TYPE}!="", RUN+="/usr/bin/udisksctl mount -b $env{DEVNAME}"

# start auto mount
sudo udevadm control --reload-rules
sudo systemctl enable udisks2
sudo systemctl start udisks2
udiskie --no-tray & # run this in Bootloader
# NOTE: After auto-mounting a new drive run: sudo chown -R gamer:gamer /run/media/gamer/<disk-label>

# install steam
pacman -S libxcomposite lib32-libxcomposite libxrandr lib32-libxrandr libgcrypt lib32-libgcrypt lib32-pipewire libpulse lib32-libpulse gtk2 lib32-gtk2
pacman -S gnutls lib32-gnutls openal lib32-openal sqlite lib32-sqlite libcurl-compat lib32-libcurl-compat
pacman -S xdg-desktop-portal xdg-desktop-portal-gtk
pacman -S mangohud
pacman -S steam
# select 1: gnu-free-fonts
# select 3: vulkan-intel (or what the GPU is)
# select 3: vulkan-intel 32-bit (or what the GPU is)

# dev tools
pacman -S base-devel dotnet-sdk-8.0 git git-lfs
#dotnet publish -r linux-x64 -c Release

# reboot and login into gamer/gamer user

# launch steam Cage
pacman -S unclutter
```start.sh
unclutter -idle 3 & #NOTE: makes cursor go away after 3 seconds
wlr-randr --output eDP-1 --transform 90 --adaptive-sync enabled
# optional enable overlay: MANGOHUD=1 steam -bigpicture -steamdeck
steam -bigpicture -steamdeck
```
cage start.sh

# launch steam Gamescope
gamescope -e --fullscreen --adaptive-sync --steam -- steam -steamdeck

NOTE: -bigpicture is older UI while -tenfoot is newer supposadly

# launch Labwc (used to launch steam in windowed mode and close on exit)
labwc --session steam

# TODO: send key events over dbus
xdotool # for X11
ydotool key 42:1 15:1 15:0 42:0

export DISPLAY:=0
export STEAM_RUNTIME=1
exec steam
#sudo usermod -aG video $USER (is this needed for X11?)








# ====================================
# Installer stuff
# ====================================

# configure for archiso
pacman -S archiso
mkdir ~/ReignOS
cd ~/ReignOS
cp -r /usr/share/archiso/configs/releng/* .

# update archiso
cd ~/ReignOS
cp -r /usr/share/archiso/configs/releng/* .

airootfs/etc/motd # "To install ReignOS Linux follow the installation guide:" "http://reign-os.com/"
airootfs/etc/passwd # "gamer:x:0:0:gamer:/home/gamer:/usr/bin/bash"
airootfs/etc/systemd/system/getty@tty1.service.d/autologin.conf # "ExecStart=-/sbin/agetty -o '-p -f -- \\u' --noclear --autologin gamer - $TERM"
packages.x86_64 # add lines below from "nano packages.x86_64"
profiledef.sh
{
    #iso_name="reignos"
    #iso_label="REIGNOS_$(date --date="@${SOURCE_DATE_EPOCH:-$(date +%s)}" +%Y%m)"
    #iso_publisher="ReignOS <http://reign-studios.com>"
    #iso_application="ReignOS Installer"
}
syslinux/archiso_head.cfg # "MENU TITLE ReignOS"
syslinux/archiso_pxe-linux.cfg
{
    #Boot the ReignOS install medium using NBD.
    #It allows you to install ReignOS or perform system maintenance.
    #MENU LABEL ReignOS install medium (x86_64, NBD)
    #MENU LABEL ReignOS install medium (x86_64, HTTP)
}
syslinux/archiso_sys-linux.cfg
{
    #Boot the ReignOS install medium on BIOS.
    #It allows you to install ReignOS or perform system maintenance.
    #Boot the ReignOS install medium on BIOS with speakup screen reader.
    #It allows you to install ReignOS or perform system maintenance with speech feedback.
    #MENU LABEL ReignOS install medium (x86_64, BIOS) with ^speech
}
efiboot/loader/entries/<config-files>.conf

# add packages needed in live USB (like dotnet)
nano packages.x86_64

bash
parted
gparted
ntfs-3g
git
git-lfs
gcc
dotnet-sdk-8.0
weston
cage
labwc
openbox
wlr-randr
dmidecode
udev
python
xorg-server-xwayland
wayland
wayland-protocols
wayland-utils
xorg
xorg-server
xorg-xinit
xorg-xev
xf86-input-libinput
xterm
xbindkeys
xorg-xinput
xorg-xmodmap
mesa
libdrm
libva-intel-driver
intel-media-driver
intel-ucode
vulkan-intel
intel-gpu-tools
libva-mesa-driver
amd-ucode
vulkan-radeon
radeontop
vulkan-nouveau
vulkan-icd-loader
vulkan-tools
vulkan-mesa-layers
egl-wayland
xf86-video-intel
xf86-video-amdgpu
xf86-video-nouveau
glxinfo

# allow installer to access mirror list
nano pacman.conf # uncomment files below
#[multilib]
#Include = /etc/pacman.d/mirrorlist

# configure root pass
echo "root:gamer" | chpasswd -R airootfs/
nano airootfs/etc/passwd # change to (or .bashrc isn't called): root:x:0:0:root:/root:/usr/bin/bash
# gamer:x:0:0:gamer:/home/gamer:/usr/bin/bash # add this line

# configure gamer user
nano airootfs/root/customize_airootfs.sh
#useradd -m -G wheel -s /bin/bash gamer
#passwd -d gamer

nano /etc/sudoers.d/archiso # add this folder and file
#Defaults env_reset
#Defaults mail_badpass
#Defaults secure_path="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
#%wheel ALL=(ALL) ALL
#%wheel ALL=(ALL:ALL) ALL
#gamer ALL=(ALL) NOPASSWD:ALL # add line (disable the need for sudo pass)

# edit ReignOS metadata
nano profiledef.sh

# configure auto boot of installer
nano airootfs/home/gamer/install.sh # add whats needed here
chmod +x airootfs/home/gamer/install.sh
nano airootfs/home/gamer/.bashrc # add lines below
# chmod +x /usr/local/bin/install.sh
# /usr/local/bin/install.sh
nano airootfs/home/gamer/.bash_profile # add lines below
# ./bashrc

# build iso
mkarchiso -v .

# rebuild iso
rm -rf work/ out/
rm -rf work/ out/ /var/cache/pacman/pkg/* # to clear all packages
mkarchiso -v .

# copy ISO from VirtualBox to host
In VirtualBox, go to settings and Shared Folders
Select Host path and name "Folder Name" and "Mount point" to "share"
Boot up system

In Arch install:
pacman -S virtualbox-guest-utils linux-headers base-devel
modprobe vboxguest
modprobe vboxsf
modprobe vboxvideo
systemctl enable vboxservice.service
reboot

Create mount point: mkdir -p /mnt/share
Mount host folder: mount -t vboxsf share /mnt/share
OR just access via: /media/sf_VirtualBox

# mount ISO to inspect files
mount -o loop out/<file-name>.iso /mnt
umount -R /mnt

# KeyCode ref
https://elixir.bootlin.com/linux/v6.11.4/source/include/uapi/linux/input-event-codes.h

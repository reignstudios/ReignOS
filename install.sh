# Test Dual booting on MSI-Claw using MicroSD with https://neosmart.net/EasyBCD/

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

# format partitions
sudo mkfs.vfat -F 32 /dev/nvme0n1p1
sudo mkfs.ext4 /dev/nvme0n1p2

# mount partitions
mount /dev/nvme0n1p2 /mnt
mount /mnt/boot /mnt
mount /dev/nvme0n1p1 /mnt/boot

#install linux firmware
pacstrap /mnt base linux linux-firmware systemd

# Generate fstab
genfstab -U /mnt >> /mnt/etc/fstab

# Chroot into the Installed System
arch-chroot /mnt

# install GPU power suspend states (are these needed? IDK if they're)
#sudo nano /boot/loader/entries/arch.conf
# add to the end of 'options'
i915.enable_dc=2 i915.enable_psr=1
# amdgpu.dpm=1 amdgpu.ppfeaturemask=0xffffffff amdgpu.dc=1
# nouveau.pstate=1
# mem_sleep_default=deep (this can cause some systems to fail to wake)

# install apps/tools
pacman -S nano
pacman -S dmidecode udev
pacman -S python

# install Wayland and XWayland support
pacman -S xorg-server-xwayland wayland wayland-protocols
pacman -S xorg-xev xbindkeys xorg-xinput xorg-xmodmap

# install Wayland graphics drivers
pacman -S mesa
pacman -S lib32-mesa
pacman -S libva-intel-driver intel-media-driver
pacman -S intel-ucode
pacman -S vulkan-intel
pacman -S lib32-vulkan-icd-loader lib32-libglvnd

pacman -S intel-gpu-tools # amdgpu-tools nvidia-smi

# install X11
pacman -S xorg xorg-server xorg-xinit xorg-xterm

# install X11 drivers
pacman -S xf86-video-intel

#install compositors
pacman -S wlr-randr
pacman -S wlroots
pacman -S labwc
pacman -S cage
pacman -S gamescope

# install network
pacman -S dhcpcd dhclient networkmanager iwd netctl iproute2 wireless_tools wpa_supplicant dialog
pacman -S network-manager-applet nm-connection-editor

# install audio
pacman -S alsa-utils alsa-plugins
pacman -S sof-firmware
pacman -S pipewire pipewire-pulse pipewire-alsa pipewire-jack
# NOTE: "amixer set Master 5%+" or "amixer set Master 5%-" needs to be called by C# app using libinput
sudo pacman -S beep # call "beep to play current volume level"

pactl list sinks short
pactl set-default-sink <sink_name>

# install power managment
pacman -S acpi acpid powertop tlp #power-profiles-daemon (less feature complete)
pacman -S python-gobject

# auto mount drives
sudo pacman -S udiskie udisks2
sudo nano /etc/udev/rules.d/99-automount.rules
# ADD LINE: ACTION=="add", SUBSYSTEM=="block", ENV{ID_FS_TYPE}!="", RUN+="/usr/bin/udisksctl mount -b $env{DEVNAME}"

# install steam
nano /etc/pacman.conf
# uncomment existing lines...
# [multilib]
# Include = /etc/pacman.d/mirrorlist
pacman -Syy
pacman -S steam
pacman -S mangohud
# select 1: gnu-free-fonts
# select 3: vulkan-intel (or what the GPU is)
# select 3: vulkan-intel 32-bit (or what the GPU is)
pacman -S lib32-libxcomposite lib32-libxrandr lib32-libgcrypt lib32-libpulse lib32-gtk2
pacman -S vulkan-tools
pacman -S egl-wayland
pacman -S xdg-desktop-portal xdg-desktop-portal-gtk

# configure timezone
ln -sf /usr/share/zoneinfo/Region/City /etc/localtime
hwclock --systohc
locale-gen
echo "LANG=en_US.UTF-8" > /etc/locale.conf

# setup hostname
echo "gamestation" > /etc/hostname
nano /etc/hosts
# add these lines...
# 127.0.0.1   localhost
# ::1         localhost
# 127.0.1.1   gamestation.localdomain gamestation

# install systemd
bootctl install
nano /boot/loader/entries/arch.conf
# add these lines...
# title Game Station
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
# uncomment
# %wheel ALL=(ALL:ALL) ALL

# finish install
exit
umount -R /mnt
reboot

# login as root after reboot
# mkinitcpio -P
# chmod 666 /dev/dri/*

# start network service
systemctl enable iwd
systemctl start iwd

systemctl enable dhcpcd
systemctl start dhcpcd

systemctl enable NetworkManager
systemctl start NetworkManager

systemctl enable wpa_supplicant
systemctl start wpa_supplicant

# start audio service
systemctl --user enable pipewire
systemctl --user start pipewire

systemctl --user enable pipewire-pulse
systemctl --user start pipewire-pulse

# start power services
systemctl enable acpid
systemctl start acpid

#systemctl enable power-profiles-daemon
#systemctl start power-profiles-daemon

sudo systemctl enable tlp
sudo systemctl start tlp

# start auto mount
sudo udevadm control --reload-rules
sudo systemctl start udisks2
sudo systemctl enable udisks2
udiskie --no-tray & # run this in Bootloader
# NOTE: After auto-mounting a new drive run: sudo chown -R gamer:gamer /run/media/gamer/<disk-label>

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
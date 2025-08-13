#!/bin/bash

# remove old packages
# nothing yet...

# add core pacman packages
sudo pacman -S --noconfirm --needed linux-headers linux-tools

# make sure yay installed
if [ ! -d "/home/gamer/yay" ]; then
	cd /home/gamer
	git clone https://aur.archlinux.org/yay.git
	cd /home/gamer/yay
	makepkg -si --noconfirm
	yay -Syy --noconfirm
fi

# add pacman packages
sudo pacman -S --noconfirm --needed jq
sudo pacman -S --noconfirm --needed hwinfo
sudo pacman -S --noconfirm --needed sysstat
sudo pacman -S --noconfirm --needed rsync
sudo pacman -S --noconfirm --needed reflector

sudo pacman -S --noconfirm --needed wayland-utils
sudo pacman -S --noconfirm --needed wlr-randr gamescope cage labwc weston
sudo pacman -S --noconfirm --needed openbox
sudo pacman -S --noconfirm --needed xdg-desktop-portal xdg-desktop-portal-wlr xdg-desktop-portal-kde xdg-desktop-portal-gtk

sudo pacman -S --noconfirm --needed vulkan-tools vulkan-mesa-layers lib32-vulkan-mesa-layers

sudo pacman -S --noconfirm --needed alsa-firmware alsa-ucm-conf

sudo pacman -Rdd --noconfirm jack2
sudo pacman -S --noconfirm --needed pipewire pipewire-pulse pipewire-alsa pipewire-jack wireplumber

sudo pacman -S --noconfirm --needed bluez bluez-utils
sudo systemctl enable bluetooth
yay -S --noconfirm --needed bcm20702a1-firmware

sudo pacman -S --noconfirm --needed bolt
sudo systemctl enable bolt.service

sudo pacman -S --noconfirm --needed plasma konsole dolphin kate ark exfatprogs dosfstools partitionmanager
sudo pacman -S --noconfirm --needed btrfs-progs ntfs-3g
sudo pacman -S --noconfirm --needed maliit-keyboard
sudo pacman -S --noconfirm --needed qt5-wayland qt6-wayland
sudo pacman -S --noconfirm --needed wmctrl

sudo pacman -S --noconfirm --needed wget
sudo pacman -S --noconfirm --needed gparted
sudo pacman -S --noconfirm --needed flatpak
sudo pacman -S --noconfirm --needed zip unzip gzip bzip2 7zip xz

yay -S --noconfirm --needed supergfxctl

yay -S --noconfirm --needed ttf-ms-fonts
fc-cache -fv

yay -S --noconfirm --needed steamcmd
yay -S --noconfirm --needed proton-ge-custom

sudo pacman -S --noconfirm --needed dkms
sudo pacman -S --noconfirm --needed fwupd

sudo pacman -S --noconfirm --needed vdpauinfo
sudo pacman -S --noconfirm --needed ffmpeg gstreamer gst-plugins-base gst-plugins-good gst-plugins-bad gst-plugins-ugly gst-libav
sudo pacman -S --noconfirm --needed libva libva-utils gstreamer-vaapi
sudo pacman -S --noconfirm --needed libvdpau-va-gl mesa-vdpau
sudo pacman -S --noconfirm --needed libdvdread libdvdnav libdvdcss libbluray

yay -S --noconfirm --needed ayaneo-platform-dkms-git
yay -S --noconfirm --needed ayn-platform-dkms-git

yay -S --noconfirm --needed rtl8812au-dkms-git
yay -S --noconfirm --needed rtl8814au-dkms-git
yay -S --noconfirm --needed rtl88x2bu-dkms-git
yay -S --noconfirm --needed rtl8821au-dkms-git

yay -S --noconfirm --needed ryzenadj

sudo systemctl stop acpid
sudo systemctl disable acpid
sudo pacman -R --noconfirm acpid

# enable missing audio services
systemctl --user enable pipewire.socket pipewire.service pipewire-pulse.socket pipewire-pulse.service
#!/bin/bash

# remove old packages
# nothing yet...

# add pacman packages
sudo pacman -S --noconfirm linux-headers linux-tools

sudo pacman -S --noconfirm wayland-utils
sudo pacman -S --noconfirm weston
sudo pacman -S --noconfirm openbox
sudo pacman -S --noconfirm xdg-desktop-portal xdg-desktop-portal-wlr xdg-desktop-portal-kde xdg-desktop-portal-gtk

sudo pacman -S --noconfirm vulkan-tools vulkan-mesa-layers lib32-vulkan-mesa-layers

sudo pacman -S --noconfirm bluez bluez-utils
sudo systemctl enable bluetooth

sudo pacman -S --noconfirm bolt
sudo systemctl enable bolt.service

sudo pacman -S --noconfirm plasma konsole dolphin kate ark exfatprogs dosfstools partitionmanager

sudo pacman -S --noconfirm gparted
sudo pacman -S --noconfirm flatpak

# add yay packages
if [ ! -d "/home/gamer/yay" ]; then
	cd /home/gamer
	git clone https://aur.archlinux.org/yay.git
	cd /home/gamer/yay
	makepkg -si --noconfirm
fi

yay -S --noconfirm supergfxctl

yay -S --noconfirm ttf-ms-fonts
fc-cache -fv

yay -S --noconfirm steamcmd
yay -S --noconfirm proton-ge-custom

sudo pacman -S --noconfirm fwupd
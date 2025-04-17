#!/bin/bash

# remove old packages
# nothing yet...

# add pacman packages
sudo pacman -S --noconfirm linux-headers

sudo pacman -S --noconfirm wayland-utils
sudo pacman -S --noconfirm weston
sudo pacman -S --noconfirm openbox

sudo pacman -S --noconfirm vulkan-tools vulkan-mesa-layers lib32-vulkan-mesa-layers

sudo pacman -S --noconfirm bluez bluez-utils
sudo systemctl enable bluetooth

sudo pacman -S --noconfirm bolt
sudo systemctl enable bolt.service

# add yay packages
if [ ! -d "/home/gamer/yay" ]; then
	cd /home/gamer
	git clone https://aur.archlinux.org/yay.git
	cd /home/gamer/yay
	makepkg -si --noconfirm
fi

yay -S supergfxctl --noconfirm
sudo systemctl enable supergfxd.service

sudo pacman -S --noconfirm fwupd
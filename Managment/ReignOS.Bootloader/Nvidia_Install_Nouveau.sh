#!/bin/bash

sudo pacman -Syu --noconfirm
yay -Syu --noconfirm

cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./Nvidia_Uninstall.sh

echo "Installing Nvidia Nouveau drivers"
set -e
sudo pacman -S --noconfirm vulkan-nouveau lib32-vulkan-nouveau
sudo pacman -S --noconfirm vulkan-icd-loader lib32-vulkan-icd-loader lib32-libglvnd
sudo pacman -S --noconfirm xf86-video-nouveau
sudo mkinitcpio -P

sudo reboot -f
exit 0
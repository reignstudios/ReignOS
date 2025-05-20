#!/bin/bash

sudo pacman -Syu --noconfirm

echo "Uninstalling Nvidia Proprietary drivers"
sudo systemctl disable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service
sudo pacman -R --noconfirm nvidia nvidia-utils lib32-nvidia-utils nvidia-settings nvidia-prime
sudo pacman -R --noconfirm nvidia-lts

echo "Installing Nvidia Nouveau drivers"
sudo pacman -S --noconfirm vulkan-nouveau lib32-vulkan-nouveau
sudo pacman -S --noconfirm vulkan-icd-loader lib32-vulkan-icd-loader lib32-libglvnd
sudo pacman -S --noconfirm xf86-video-nouveau
sudo mkinitcpio -P

sudo reboot -f
exit 0
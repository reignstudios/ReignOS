#!/bin/bash

sudo pacman -Syu

echo "Uninstalling Nvidia Nouveau drivers"
sudo pacman -R --noconfirm vulkan-nouveau lib32-vulkan-nouveau
sudo pacman -R --noconfirm xf86-video-nouveau

echo "Installing Nvidia Proprietary drivers"
sudo pacman -S --noconfirm nvidia nvidia-utils lib32-nvidia-utils nvidia-settings nvidia-prime egl-wayland
sudo pacman -S --noconfirm nvidia-lts
sudo mkinitcpio -P
sudo systemctl enable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service

sudo reboot -f
exit 0
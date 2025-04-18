#!/bin/bash

sudo pacman -Syu

echo "Uninstalling Nvidia Nouveau drivers"
sudo pacman -R --noconfirm vulkan-nouveau lib32-vulkan-nouveau

echo "Installing Nvidia Proprietary drivers"
sudo pacman -S --noconfirm nvidia nvidia-utils lib32-nvidia-utils nvidia-settings nvidia-prime egl-wayland
sudo mkinitcpio -P

sudo reboot -f
exit 0
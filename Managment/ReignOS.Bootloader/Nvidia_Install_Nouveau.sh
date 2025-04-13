#!/bin/bash

sudo pacman -Syu

echo "Uninstalling Nvidia Proprietary drivers"
sudo pacman -R --noconfirm nvidia
sudo pacman -R --noconfirm nvidia-utils
sudo pacman -R --noconfirm lib32-nvidia-utils
sudo pacman -R --noconfirm nvidia-settings
sudo pacman -R --noconfirm nvidia-prime

echo "Installing Nvidia Nouveau drivers"
sudo pacman -S --noconfirm vulkan-nouveau lib32-vulkan-nouveau

reboot
exit 0
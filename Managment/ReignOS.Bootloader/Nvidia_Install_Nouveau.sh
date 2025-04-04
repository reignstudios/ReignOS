#!/bin/bash

sudo pacman -Syu

echo "Uninstalling Nvidia Proprietary drivers"
sudo pacman -R nvidia nvidia-utils lib32-nvidia-utils nvidia-settings nvidia-prime --noconfirm

echo "Installing Nvidia Nouveau drivers"
sudo pacman -S vulkan-nouveau lib32-vulkan-nouveau --noconfirm

reboot
exit 0
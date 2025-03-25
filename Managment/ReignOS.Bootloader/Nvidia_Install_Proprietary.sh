#!/bin/bash

echo "Uninstalling Nvidia Nouveau drivers"
sudo pacman -R vulkan-nouveau lib32-vulkan-nouveau --noconfirm

echo "Installing Nvidia Proprietary drivers"
sudo pacman -S nvidia nvidia-utils lib32-nvidia-utils nvidia-settings nvidia-prime --noconfirm

reboot
exit 0
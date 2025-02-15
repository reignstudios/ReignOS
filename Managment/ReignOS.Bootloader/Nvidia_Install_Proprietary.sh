#!/bin/bash

echo "Uninstalling Nvidia Nouveau drivers"
echo "gamer" | sudo -S pacman -R vulkan-nouveau lib32-vulkan-nouveau --noconfirm

echo "Installing Nvidia Proprietary drivers"
echo "gamer" | sudo -S pacman -S nvidia nvidia-utils lib32-nvidia-utils nvidia-settings nvidia-prime --noconfirm

reboot
exit 0
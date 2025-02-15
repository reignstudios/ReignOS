#!/bin/bash

echo "Uninstalling Nvidia Proprietary drivers"
echo "gamer" | sudo -S pacman -R nvidia nvidia-utils lib32-nvidia-utils nvidia-settings nvidia-prime --noconfirm

echo "Installing Nvidia Nouveau drivers"
echo "gamer" | sudo -S pacman -S vulkan-nouveau lib32-vulkan-nouveau --noconfirm

reboot
exit 0
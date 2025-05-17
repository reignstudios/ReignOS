#!/bin/bash

sudo pacman -Syu --noconfirm

echo "Uninstalling AMD MESA drivers"
sudo pacman -R --noconfirm vulkan-radeon lib32-vulkan-radeon radeontop

echo "Installing AMD Proprietary drivers"
sudo pacman -S --noconfirm amdvlk lib32-amdvlk

sudo reboot -f
exit 0
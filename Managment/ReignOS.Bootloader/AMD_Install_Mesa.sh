#!/bin/bash

sudo pacman -Syu --noconfirm

echo "Uninstalling AMD Proprietary drivers"
sudo pacman -R --noconfirm amdvlk lib32-amdvlk

echo "Installing AMD MESA drivers"
sudo pacman -S --noconfirm vulkan-radeon lib32-vulkan-radeon

sudo reboot -f
exit 0
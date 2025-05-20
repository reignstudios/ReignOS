#!/bin/bash

sudo pacman -Syu --noconfirm

echo "Uninstalling AMD NVK drivers"
sudo pacman -R --noconfirm amdvlk lib32-amdvlk

echo "Uninstalling AMD MESA drivers"
sudo pacman -R --noconfirm vulkan-radeon lib32-vulkan-radeon

echo "Installing AMD Proprietary drivers"
yay -S --noconfirm amf-amdgpu-pro amdgpu-pro-oglp lib32-amdgpu-pro-oglp vulkan-amdgpu-pro lib32-vulkan-amdgpu-pro
# NOTE: amdgpu-pro-installer not found with yay (so installing its packages manually)

sudo reboot -f
exit 0
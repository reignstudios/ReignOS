#!/bin/bash

sudo pacman -Syu --noconfirm

echo "Uninstalling AMD Proprietary drivers"
yay -R --noconfirm amf-amdgpu-pro amdgpu-pro-oglp lib32-amdgpu-pro-oglp vulkan-amdgpu-pro lib32-vulkan-amdgpu-pro

echo "Uninstalling AMD VLK drivers"
sudo pacman -R --noconfirm amdvlk lib32-amdvlk

echo "Installing AMD MESA drivers"
sudo pacman -S --noconfirm vulkan-radeon lib32-vulkan-radeon

sudo reboot -f
exit 0
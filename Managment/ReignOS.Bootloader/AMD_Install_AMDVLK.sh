#!/bin/bash

sudo pacman -Syu --noconfirm

echo "Uninstalling AMD Proprietary drivers"
yay -Rns --noconfirm amf-amdgpu-pro amdgpu-pro-oglp lib32-amdgpu-pro-oglp vulkan-amdgpu-pro lib32-vulkan-amdgpu-pro

echo "Uninstalling AMD MESA drivers"
sudo pacman -Rns --noconfirm vulkan-radeon lib32-vulkan-radeon

echo "Installing AMD VLK drivers"
sudo pacman -S --noconfirm amdvlk lib32-amdvlk

sudo reboot -f
exit 0
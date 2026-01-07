#!/bin/bash

echo "Uninstalling AMD Proprietary drivers"
yay -R --noconfirm amf-amdgpu-pro amdgpu-pro-oglp lib32-amdgpu-pro-oglp vulkan-amdgpu-pro lib32-vulkan-amdgpu-pro
yay -R --noconfirm amf-amdgpu-pro
yay -R --noconfirm amdgpu-pro-oglp
yay -R --noconfirm lib32-amdgpu-pro-oglp
yay -R --noconfirm vulkan-amdgpu-pro
yay -R --noconfirm lib32-vulkan-amdgpu-pro

echo "Uninstalling AMD VLK drivers"
sudo pacman -R --noconfirm amdvlk lib32-amdvlk
sudo pacman -R --noconfirm amdvlk
sudo pacman -R --noconfirm lib32-amdvlk

echo "Uninstalling AMD MESA drivers"
sudo pacman -R --noconfirm vulkan-radeon lib32-vulkan-radeon
sudo pacman -R --noconfirm vulkan-radeon
sudo pacman -R --noconfirm lib32-vulkan-radeon

exit 0
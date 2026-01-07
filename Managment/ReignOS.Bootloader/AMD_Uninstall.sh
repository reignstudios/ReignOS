#!/bin/bash

echo "Uninstalling AMD Proprietary drivers"
yay -Rdd --noconfirm amf-amdgpu-pro amdgpu-pro-oglp lib32-amdgpu-pro-oglp vulkan-amdgpu-pro lib32-vulkan-amdgpu-pro

echo "Uninstalling AMD VLK drivers"
sudo pacman -Rdd --noconfirm amdvlk lib32-amdvlk

echo "Uninstalling AMD MESA drivers"
sudo pacman -Rdd --noconfirm vulkan-radeon lib32-vulkan-radeon

exit 0
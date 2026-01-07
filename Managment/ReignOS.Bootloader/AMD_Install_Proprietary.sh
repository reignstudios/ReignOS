#!/bin/bash

sudo pacman -Syu --noconfirm

cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./AMD_Uninstall.sh

echo "Installing AMD Proprietary drivers"
yay -S --noconfirm amf-amdgpu-pro amdgpu-pro-oglp lib32-amdgpu-pro-oglp vulkan-amdgpu-pro lib32-vulkan-amdgpu-pro
# NOTE: amdgpu-pro-installer not found with yay (so installing its packages manually)

sudo reboot -f
exit 0
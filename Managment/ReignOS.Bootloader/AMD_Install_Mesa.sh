#!/bin/bash

sudo pacman -Syu --noconfirm
yay -Syu --noconfirm

cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./AMD_Uninstall.sh

echo "Installing AMD MESA drivers"
set -e
sudo pacman -S --noconfirm vulkan-radeon lib32-vulkan-radeon

sudo reboot -f
exit 0
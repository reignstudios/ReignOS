#!/bin/bash

sudo pacman -Syu --noconfirm

cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./AMD_Uninstall.sh

echo "Installing AMD VLK drivers"
sudo pacman -S --noconfirm amdvlk lib32-amdvlk

sudo reboot -f
exit 0
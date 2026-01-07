#!/bin/bash

sudo pacman -Syu --noconfirm
yay -Syu --noconfirm

cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./Nvidia_Uninstall.sh

echo "Installing Nvidia Proprietary drivers"
sudo pacman -S --noconfirm nvidia nvidia-utils lib32-nvidia-utils nvidia-settings
#sudo pacman -S --noconfirm nvidia-lts #Only needed if we add LTS kernel support
sudo pacman -S --noconfirm nvidia-prime
sudo pacman -S --noconfirm egl-gbm egl-wayland
sudo mkinitcpio -P
sudo systemctl enable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service

sudo reboot -f
exit 0
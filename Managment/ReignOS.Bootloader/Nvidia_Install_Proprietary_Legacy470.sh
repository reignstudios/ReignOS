#!/bin/bash

sudo pacman -Syu --noconfirm
yay -Syu --noconfirm

cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./Nvidia_Uninstall.sh

echo "Installing Nvidia Proprietary drivers"
yay -S --noconfirm nvidia-470xx-dkms nvidia-470xx-utils lib32-nvidia-470xx-utils nvidia-470xx-settings
#sudo pacman -S --noconfirm nvidia-prime #NOTE: because there is no specific package for this, let other dependencies pull wanted
sudo pacman -S --noconfirm egl-gbm egl-wayland
sudo mkinitcpio -P
sudo systemctl enable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service

sudo reboot -f
exit 0
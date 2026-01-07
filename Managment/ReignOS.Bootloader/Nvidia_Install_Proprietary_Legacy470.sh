#!/bin/bash

sudo pacman -Syu --noconfirm
yay -Syu --noconfirm

cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./Nvidia_Uninstall.sh

echo "Installing Nvidia Proprietary drivers"
set -e
yay -S --noconfirm nvidia-470xx-dkms nvidia-470xx-utils lib32-nvidia-470xx-utils nvidia-470xx-settings egl-wayland egl-gbm #nvidia-prime
sudo mkinitcpio -P
sudo systemctl enable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service

sudo reboot -f
exit 0
#!/bin/bash

sudo pacman -Syu

echo "Uninstalling Nvidia Proprietary drivers"
sudo systemctl disable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service
sudo pacman -R --noconfirm nvidia nvidia-utils lib32-nvidia-utils nvidia-settings nvidia-prime

echo "Installing Nvidia Nouveau drivers"
sudo pacman -S --noconfirm vulkan-nouveau lib32-vulkan-nouveau
sudo mkinitcpio -P

sudo reboot -f
exit 0
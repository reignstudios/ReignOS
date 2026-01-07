#!/bin/bash

echo "Uninstalling Nvidia Proprietary drivers"
sudo systemctl disable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service
sudo pacman -R --noconfirm nvidia-340xx nvidia-340xx-utils lib32-nvidia-340xx-utils nvidia-340xx-settings

echo "Uninstalling Nvidia Proprietary drivers"
sudo systemctl disable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service
sudo pacman -R --noconfirm nvidia-470xx nvidia-470xx-utils lib32-nvidia-470xx-utils nvidia-470xx-settings

echo "Uninstalling Nvidia Proprietary drivers"
sudo systemctl disable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service
sudo pacman -R --noconfirm nvidia-580xx nvidia-580xx-utils lib32-nvidia-580xx-utils nvidia-580xx-settings

echo "Uninstalling Nvidia Proprietary drivers"
sudo systemctl disable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service
sudo pacman -R --noconfirm nvidia nvidia-utils lib32-nvidia-utils nvidia-settings
sudo pacman -R --noconfirm nvidia-lts
sudo pacman -R --noconfirm nvidia-prime
sudo pacman -R --noconfirm egl-gbm
sudo pacman -R --noconfirm egl-wayland

echo "Uninstalling Nvidia Nouveau drivers"
sudo pacman -R --noconfirm vulkan-nouveau lib32-vulkan-nouveau
sudo pacman -R --noconfirm xf86-video-nouveau

exit 0
#!/bin/bash

sudo systemctl disable nvidia-suspend.service nvidia-hibernate.service nvidia-resume.service
sudo pacman -Rdd --noconfirm nvidia-open # this can block others if not uninstalled first

echo "Uninstalling Nvidia Proprietary drivers"
sudo pacman -Rdd --noconfirm nvidia-340xx nvidia-340xx-utils lib32-nvidia-340xx-utils nvidia-340xx-settings

echo "Uninstalling Nvidia Proprietary drivers"
sudo pacman -Rdd --noconfirm nvidia-470xx nvidia-470xx-utils lib32-nvidia-470xx-utils nvidia-470xx-settings

echo "Uninstalling Nvidia Proprietary drivers"
sudo pacman -Rdd --noconfirm nvidia-580xx nvidia-580xx-utils lib32-nvidia-580xx-utils nvidia-580xx-settings

echo "Uninstalling Nvidia Proprietary drivers"
sudo pacman -Rdd --noconfirm nvidia nvidia-utils lib32-nvidia-utils nvidia-settings nvidia-open
sudo pacman -Rdd --noconfirm nvidia-lts
sudo pacman -Rdd --noconfirm nvidia-prime
sudo pacman -Rdd --noconfirm egl-gbm
sudo pacman -Rdd --noconfirm egl-wayland

echo "Uninstalling Nvidia Nouveau drivers"
sudo pacman -Rdd --noconfirm vulkan-nouveau lib32-vulkan-nouveau
sudo pacman -Rdd --noconfirm xf86-video-nouveau

exit 0
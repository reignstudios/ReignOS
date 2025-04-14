#!/bin/bash

# remove old packages
# nothing yet...

# add new packages
sudo pacman -S --noconfirm wayland-utils
sudo pacman -S --noconfirm weston

sudo pacman -S --noconfirm vulkan-tools vulkan-mesa-layers lib32-vulkan-mesa-layers

sudo pacman -S --noconfirm bluez bluez-utils
sudo systemctl enable bluetoothd
#!/bin/bash

sudo pacman -Syu --noconfirm

# remove old packages
# nothing yet...

# add new packages
sudo pacman -S wayland-utils --noconfirm
sudo pacman -S weston --noconfirm
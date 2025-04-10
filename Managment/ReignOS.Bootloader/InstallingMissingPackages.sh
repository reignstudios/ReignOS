#!/bin/bash

sudo pacman -Syu --noconfirm

# remove old packages
# nothing yet...

# add new packages
sudo pacman -S --noconfirm wayland-utils
sudo pacman -S --noconfirm weston
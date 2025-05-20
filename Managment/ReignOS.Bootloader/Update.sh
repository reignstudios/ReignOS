#!/bin/bash

# check if pacman updates exist
echo ""
echo "ReignOS Checking 'pacman' for updates..."
sudo pacman -Sy
HAS_UPDATES=false
if pacman -Qu &> /dev/null; then
    echo "Updates are available under pacman"
    HAS_UPDATES=true
fi

# check if yay updates exist
if [ "$HAS_UPDATES" = "false" ]; then
  echo ""
  echo "ReignOS Checking 'yay' for updates..."
  sudo yay -Sy
  HAS_UPDATES=false
  if yay -Qu &> /dev/null; then
      echo "Updates are available under yay"
      HAS_UPDATES=true
  fi
fi

# 
if [ "$HAS_UPDATES" = "false" ]; then
  echo ""
  echo "ReignOS Checking 'flatpak' for updates..."
  if [ -n "$(flatpak remote-ls --updates)" ]; then
      echo "Updates are available under flatpak"
      HAS_UPDATES=true
  fi
fi

# update Arch
arch_exit_code=0
yay_exit_code=0
if [ "$HAS_UPDATES" = "true" ]; then
  echo ""
  echo "ReignOS Updating Arch..."
  sleep 2

  # pacman
  echo "ReignOS Updating pacman pacages..."
  sudo pacman -Syu --noconfirm
  arch_exit_code=$?

  # yay
  echo "ReignOS Updating yay pacages..."
  yay -Syu --noconfirm
  yay_exit_code=$?

  # flatpaks
  echo "ReignOS Updating flatpak pacages..."
  flatpak update --noninteractive

  # firmware
  echo "ReignOS Updating fwupdmgr firmware..."
  sudo fwupdmgr refresh -y
  sudo fwupdmgr update -y

  if [ $arch_exit_code -eq 0 ]; then
    reboot
    exit 0
  fi
  
  if [ $yay_exit_code -eq 0 ]; then
    reboot
    exit 0
  fi
fi

# update ReignOS Git package
echo ""
echo "ReignOS Updating Git packages..."
cd /home/gamer/ReignOS
git pull
cd /home/gamer/ReignOS/Managment
echo "ReignOS Building packages..."
dotnet publish -r linux-x64 -c Release
sleep 1

# just stop everything if Arch fails to update (but allow ReignOS git to update before this)
if [ $arch_exit_code -ne 0 ]; then
    echo "ReignOS Updating Arch failed: $arch_exit_code 'hit Ctrl+C to stop boot'"
    sleep 5
fi

if [ $yay_exit_code -ne 0 ]; then
    echo "ReignOS Updating Yay failed: $yay_exit_code 'hit Ctrl+C to stop boot'"
    sleep 5
fi

exit 0
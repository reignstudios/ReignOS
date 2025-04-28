#!/bin/bash

# check if Arch updates exist
echo ""
echo "ReignOS Checking Arch for updates..."
sudo pacman -Sy
HAS_UPDATES=false
if pacman -Qu &> /dev/null; then
    echo "Updates are available"
    HAS_UPDATES=true
fi

# update Arch
arch_exit_code=0
yay_exit_code=0
if [ "$HAS_UPDATES" = "true" ]; then
  echo ""
  echo "ReignOS Updating Arch..."
  sleep 2

  # pacman
  sudo pacman -Syu --noconfirm
  arch_exit_code=$?

  # yay
  yay -Syu --noconfirm
  yay_exit_code=$?

  # firmware
  sudo fwupdmgr refresh --noconfirm
  sudo fwupdmgr update --noconfirm

  if [ $arch_exit_code -eq 0 ]; then
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
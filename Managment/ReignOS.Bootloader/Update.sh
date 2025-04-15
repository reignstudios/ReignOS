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
if [ "$HAS_UPDATES" = "true" ]; then
  echo ""
  echo "ReignOS Updating Arch..."
  sleep 2
  sudo pacman -Syu --noconfirm
  if [ $? -ne 0 ]; then
    echo "ReignOS Updating Arch failed: $?"
    exit 100
  else
    reboot
  fi
  exit 0
fi

# update ReignOS Git package
echo ""
echo "ReignOS Updating Git packages..."
cd /home/gamer/ReignOS
git reset --hard
git pull
cd /home/gamer/ReignOS/Managment
echo "ReignOS Building packages..."
dotnet publish -r linux-x64 -c Release
sleep 1

exit 0
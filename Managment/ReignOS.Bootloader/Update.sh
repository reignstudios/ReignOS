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
  sudo pacman -Syu --noconfirm
  reboot
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
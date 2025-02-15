#!/bin/bash

# check if Arch updates exist
echo ""
echo "ReignOS Checking Arch for updates..."
echo "gamer" | sudo -S pacman -Sy
HAS_UPDATES=false
if pacman -Qu &> /dev/null; then
    echo "Updates are available"
    HAS_UPDATES=true
fi

# update Arch
if [ "$HAS_UPDATES" = "true" ]; then
  echo ""
  echo "ReignOS Updating Arch..."
  echo "gamer" | sudo -S pacman -Syu --noconfirm
  reboot
  exit 0
fi

# update ReignOS Git package
echo ""
echo "ReignOS Updating Git packages..."
cd /home/gamer/ReignOS
git reset --hard
if git pull | grep -q "Already up to date."; then
  echo "ReignOS repo already up to date (skipping...)"
else
  cd /home/gamer/ReignOS/Managment
  echo "ReignOS Building packages..."
  dotnet publish -r linux-x64 -c Release --no-incremental
fi
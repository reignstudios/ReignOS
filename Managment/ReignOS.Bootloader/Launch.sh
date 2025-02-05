#!/bin/bash

# wait for network
for i in $(seq 1 10); do
    # Try to ping Google's DNS server
    if ping -c 1 -W 1 8.8.8.8 &> /dev/null; then
        echo "Network is up!"
        break
    else
        echo "Waiting for network... $i/$timeout"
        sleep 1
    fi
done

# check if Arch updates exist
echo ""
echo "ReignOS Checking Arch for updates..."
echo "gamer" | sudo pacman -Sy
HAS_UPDATES=false
if echo "gamer" | sudo pacman -Qu &> /dev/null; then
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
git pull
cd Managment
echo "ReignOS Building packages..."
dotnet publish -r linux-x64 -c Release

# run bootloader
cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./ReignOS.Bootloader $@
exit_code=$?

# shutdown
if [ $exit_code -eq 1 ]; then
  echo ""
  echo "ReignOS (shutting down)..."
  poweroff
  exit 0
fi

# reboot
if [ $exit_code -eq 2 ]; then
  echo ""
  echo "ReignOS (rebooting)..."
  reboot
  exit 0
fi
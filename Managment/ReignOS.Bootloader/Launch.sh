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

# update Arch
echo ""
echo "ReignOS Updating Arch..."
echo "gamer" | sudo -S pacman -Syu --noconfirm

# update ReignOS Git package
echo ""
echo "ReignOS Updating Git packages..."
cd ~/ReignOS
git reset --hard
git pull
cd Managment
echo "ReignOS Building packages..."
dotnet publish -r linux-x64 -c Release

# run bootloader
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
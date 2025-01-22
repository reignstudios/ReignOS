#!/bin/bash

# make sure ReignOS Managment stuff isn't running
echo "Killing ReignOS Managment..."
echo "gamer" | sudo pkill "ReignOS.ControlCenter"
echo "gamer" | sudo pkill "ReignOS.Service"
echo "gamer" | sudo pkill "ReignOS.Bootloader"

# make sure steam is shutdown
echo "Shutting down Steam..."
steam -Shutdown
counter=0
while pgrep -x "steam" > /dev/null; do
    echo "Waiting for Steam to close..."
    sleep 1
    counter=$((counter + 1))
    if [ "$counter" -ge "10" ]; then
        echo "Timeout reached. Force closing Steam..."
        echo "gamer" | sudo pkill "steam"
        break
    fi
done

# update Arch
echo ""
echo "ReignOS Updating Arch..."
echo "gamer" | sudo pacman -Syu --noconfirm

# update ReignOS Git package
echo ""
echo "ReignOS Updating Git packages..."
cd ~/ReignOS
git reset --hard
git pull
cd Managment
echo "ReignOS Building packages..."
dotnet publish -r linux-x64 -c Release

# shutdown
echo ""
echo "ReignOS Pre-Shutdown done (shutting down)..."
poweroff
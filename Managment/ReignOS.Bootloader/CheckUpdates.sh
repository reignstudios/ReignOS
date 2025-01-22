#!/bin/bash

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
dotnet publish -r linux-x64 -c Release

# shutdown
echo ""
echo "ReignOS Pre-Shutdown done (shutting down)..."
poweroff
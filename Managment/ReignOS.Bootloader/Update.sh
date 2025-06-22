#!/bin/bash

# aw87559-firmware = Ayaneo Flip DS (yay needs to ignore this conflict)

# wait for network
NetworkUp=false
for i in $(seq 1 30); do
    # Try to ping Google's DNS server
    if ping -c 1 -W 2 google.com &> /dev/null; then
        echo "Network is up!"
        NetworkUp=true
        sleep 1
        break
    else
        echo "Waiting for network... $i"
        sleep 1
    fi
done

# run updates (if network available)
if [ "$NetworkUp" = "true" ]; then
    cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
    chmod +x ./Update.sh
    ./Update.sh
else
    exit 0
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
  yay -Sy
  HAS_UPDATES=false
  if yay -Qu --ignore aw87559-firmware &> /dev/null; then
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
    yay -Syu --noconfirm --ignore aw87559-firmware
    yay_exit_code=$?

    # flatpaks
    echo "ReignOS Updating flatpak pacages..."
    flatpak update --noninteractive

    # firmware
    echo "ReignOS Updating fwupdmgr firmware..."
    sudo fwupdmgr refresh -y
    sudo fwupdmgr update -y --no-reboot-check

    # just stop everything if Arch fails to update (but allow ReignOS git to update before this)
    if [ $arch_exit_code -ne 0 ]; then
        echo "ERROR: ReignOS Updating Arch failed: $arch_exit_code 'hit Ctrl+C to stop boot'"
        sleep 5

        echo "Re-Installing Linux firmware..."
        sudo pacman --noconfirm -Rdd linux-firmware
        sudo pacman --noconfirm -Syu linux-firmware
        sudo pacman --noconfirm -Syu
        reboot
    fi

    if [ $yay_exit_code -ne 0 ]; then
        echo "ERROR: ReignOS Updating Yay failed: $yay_exit_code 'hit Ctrl+C to stop boot'"
        sleep 5
    fi

    # reboot if updates ran
    if [ $arch_exit_code -eq 0 ] || [ $yay_exit_code -eq 0 ]; then
        reboot
    fi
fi

exit 0
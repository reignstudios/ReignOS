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
if [ "$NetworkUp" = "false" ]; then
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

# update or install Chimera-Kernel
echo ""
echo "ReignOS Checking Chimera-Kernel for updates..."
CHIMERA_KERNEL_RELEASE=$(curl -s 'https://api.github.com/repos/ChimeraOS/linux-chimeraos/releases' | jq -r "first(.[] | select(.prerelease == "false"))")
CHIMERA_KERNEL_VERSION=$(jq -r '.tag_name' <<< ${CHIMERA_KERNEL_RELEASE})

v1="${CHIMERA_KERNEL_VERSION#v}"
v1=$(printf "%s" "$v1" | grep -oE '^[0-9]+\.[0-9]+\.[0-9]+')
echo "Latest Chimera Kernel: $v1"

CHIMERA_KERNEL_INSTALLED_VERSION=$(pacman -Q linux-chimeraos)
v2="${CHIMERA_KERNEL_INSTALLED_VERSION#linux-chimeraos }"
v2=$(printf "%s" "$v2" | grep -oE '^[0-9]+\.[0-9]+\.[0-9]+')
echo "Installed Chimera Kernel: $v2"

if [ "$v1" != "$v2" ]; then
    echo "ReignOS Updating Chimera-Kernel to: $CHIMERA_KERNEL_VERSION"
    CHIMERA_KERNEL_VERSION_LINK="${CHIMERA_KERNEL_VERSION/-chos/.chos}#v"
    echo "Kernel: linux-chimeraos-$CHIMERA_KERNEL_VERSION_LINK-x86_64.pkg.tar.zst"
    echo "Kernel-Headers: linux-chimeraos-headers-$CHIMERA_KERNEL_VERSION_LINK-x86_64.pkg.tar.zst"
    mkdir -p /home/gamer/ReignOS_Ext/Kernels
    wget -O /home/gamer/ReignOS_Ext/Kernels/chimera-kernel.pkg.tar.zst https://github.com/ChimeraOS/linux-chimeraos/releases/download/$CHIMERA_KERNEL_VERSION/linux-chimeraos-$CHIMERA_KERNEL_VERSION_LINK-x86_64.pkg.tar.zst
    wget -O /home/gamer/ReignOS_Ext/Kernels/chimera-kernel-headers.pkg.tar.zst https://github.com/ChimeraOS/linux-chimeraos/releases/download/$CHIMERA_KERNEL_VERSION/linux-chimeraos-headers-$CHIMERA_KERNEL_VERSION_LINK-x86_64.pkg.tar.zst
    sudo pacman -Syu --noconfirm
    sudo pacman -S --noconfirm /home/gamer/ReignOS_Ext/Kernels/chimera-kernel.pkg.tar.zst
    sudo pacman -S --noconfirm /home/gamer/ReignOS_Ext/Kernels/chimera-kernel-headers.pkg.tar.zst
fi

# update flatpaks (just run this first so they always get ran)
echo ""
echo "ReignOS Updating flatpak pacages..."
flatpak update --noninteractive

# update or install decky-loader
echo ""
echo "ReignOS Checking DeckyLoader for updates..."
DECKY_LOADER_RELEASE=$(curl -s 'https://api.github.com/repos/SteamDeckHomebrew/decky-loader/releases' | jq -r "first(.[] | select(.prerelease == "false"))")
DECKY_LOADER_VERSION=$(jq -r '.tag_name' <<< ${DECKY_LOADER_RELEASE})
DECKY_LOADER_INSTALLED_VERSION=$(cat /home/gamer/homebrew/services/.loader.version 2>/dev/null || echo "none")
if [ "$DECKY_LOADER_INSTALLED_VERSION" != "$DECKY_LOADER_VERSION" ]; then
    echo "ReignOS Updating DeckyLoader..."
    sudo pacman -S --noconfirm jq
    curl -L https://github.com/SteamDeckHomebrew/decky-installer/releases/latest/download/install_release.sh | sh
fi

# update DeckyTDP if it exists
if [ -e "/home/gamer/homebrew/plugins/SimpleDeckyTDP" ]; then
    echo ""
    echo "ReignOS Checking DeckyTDP for updates..."
    curl -L https://github.com/aarron-lee/SimpleDeckyTDP/raw/main/install.sh | sh
fi

# make sure no borked pacman lock
if [ -f "/var/lib/pacman/db.lck" ]; then
    echo "Removing bad pacman db.lck file"
    sudo rm /var/lib/pacman/db.lck
    sleep 1
fi

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

# update Pacman
pacman_exit_code=0
yay_exit_code=0
if [ "$HAS_UPDATES" = "true" ]; then
    echo ""
    echo "ReignOS Updating Pacman..."
    sleep 2

    # pacman
    echo "ReignOS Updating pacman pacages..."
    sudo pacman -Syu --noconfirm
    pacman_exit_code=$?

    # yay
    echo "ReignOS Updating yay pacages..."
    yay -Syu --noconfirm --ignore aw87559-firmware
    yay_exit_code=$?

    # firmware
    echo "ReignOS Updating fwupdmgr firmware..."
    sudo fwupdmgr refresh -y
    sudo fwupdmgr update -y --no-reboot-check

    # just stop everything if Pacman fails to update (but allow ReignOS git to update before this)
    if [ $pacman_exit_code -ne 0 ]; then
        echo "ERROR: ReignOS Updating Pacman failed: $pacman_exit_code 'hit Ctrl+C to stop boot'"
        sleep 5

        echo "Re-Installing Linux firmware..."
        sudo pacman --noconfirm -R linux-firmware
        sudo pacman --noconfirm -S linux-firmware
        sudo mkinitcpio -P
        sudo reboot -f
    fi

    if [ $yay_exit_code -ne 0 ]; then
        echo "ERROR: ReignOS Updating Yay failed: $yay_exit_code 'hit Ctrl+C to stop boot'"
        sleep 5
    fi

    # reboot if updates ran
    if [ $pacman_exit_code -eq 0 ] || [ $yay_exit_code -eq 0 ]; then
         sudo reboot -f
    fi
fi

exit 0
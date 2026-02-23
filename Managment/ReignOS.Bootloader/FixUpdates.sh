#!/bin/bash

echo ""
echo "ReignOS (Fix updates)..."

# remove dotnet workloads
sudo rm -rf /usr/share/dotnet/sdk-manifests/

# remove files and paths that can cause issues
echo "Removing lock files..."
sudo rm /var/lib/pacman/db.lck
sudo rm ~/.gnupg/public-keys.d/pubring.db.lock
sudo rm /var/cache/pacman/pkg/archlinux-keyring-*.pkg.tar*
sudo rm -r /etc/pacman.d/gnupg
sudo rm -rf /usr/share/dotnet/sdk-manifests/8.0.100

# delete package cache
echo ""
echo "Deleting old package cache..."
sudo pacman -Rns $(pacman -Qdtq) --noconfirm
sudo pacman -Scc --noconfirm
sudo rm -rf /var/cache/pacman/pkg/*

echo "Sync Time..."
sudo pacman -Sy --noconfirm
sudo timedatectl set-ntp true
sleep 1
sudo hwclock --systohc

echo "Update reflector..."
COUNTRY=$(curl -s https://ifconfig.co/country-iso)
reflector --country $COUNTRY --latest 50 --protocol https --sort rate --save /etc/pacman.d/mirrorlist
sudo pacman -Syyu --noconfirm

echo "ReignOS re-installing yay tool..."
cd /home/gamer
sudo rm -rf ./yay
git clone https://aur.archlinux.org/yay.git
cd /home/gamer/yay
makepkg -si --noconfirm

echo "Refresh keyring, db, etc..."
sudo pacman -Sy archlinux-keyring --noconfirm
sudo pacman-key --init
sudo pacman-key --populate archlinux
sudo pacman-key --refresh-keys
sudo pacman-key --updatedb
sudo pacman -Syyu --noconfirm
yay -Syyu --noconfirm

cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./Update.sh
sudo reboot -f
exit 0
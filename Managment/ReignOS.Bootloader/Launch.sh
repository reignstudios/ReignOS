#!/bin/bash

# unblock all radio software blocks
sudo rfkill unblock all

# run bootloader
cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./ReignOS.Bootloader $@
exit_code=$?

# install kde min
if [ $exit_code -eq 10 ]; then
  echo ""
  echo "Installing KDE Minimal..."
  sudo pacman -Syu --noconfirm
  sudo pacman -S --noconfirm plasma konsole dolphin kate flatpak
  sudo reboot -f
  exit 0
fi

# install kde full
if [ $exit_code -eq 11 ]; then
  echo ""
  echo "Installing KDE Full..."
  sudo pacman -Syu --noconfirm
  sudo pacman -S --noconfirm plasma kde-applications flatpak
  sudo reboot -f
  exit 0
fi

# reboot
if [ $exit_code -eq 15 ]; then
  echo ""
  echo "ReignOS (rebooting)..."
  sudo reboot -f
  exit 0
fi

# shutdown
if [ $exit_code -eq 16 ]; then
  echo ""
  echo "ReignOS (shutting down)..."
  sudo poweroff -f
  exit 0
fi

# check updates
if [ $exit_code -eq 14 ]; then
  echo ""
  echo "ReignOS (Fix updates)..."

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
fi

if [ $exit_code -eq 17 ]; then
  echo ""
  echo "ReignOS (Check for updates)..."
  ./Update.sh
  loginctl terminate-user gamer
  exit 0
fi

if [ $exit_code -eq 18 ]; then
  echo ""
  echo "ReignOS (Check for updates and reboot)..."
  ./Update.sh
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 19 ]; then
  echo ""
  echo "ReignOS (Run mkinitcpio and reboot)..."
  sudo mkinitcpio -P
  sudo reboot -f
  exit 0
fi

# install Nvidia drivers
if [ $exit_code -eq 30 ]; then
  echo ""
  echo "ReignOS (Install Nvidia Nouveau)..."
  chmod +x ./Nvidia_Install_Nouveau.sh
  ./Nvidia_Install_Nouveau.sh
  exit 0
fi

if [ $exit_code -eq 31 ]; then
  echo ""
  echo "ReignOS (Install Nvidia Proprietary)..."
  chmod +x ./Nvidia_Install_Proprietary.sh
  ./Nvidia_Install_Proprietary.sh
  exit 0
fi

if [ $exit_code -eq 32 ]; then
  echo ""
  echo "ReignOS (Install Nvidia Proprietary Legacy-580)..."
  chmod +x ./Nvidia_Install_Proprietary_Legacy580.sh
  ./Nvidia_Install_Proprietary_Legacy580.sh
  exit 0
fi

if [ $exit_code -eq 33 ]; then
  echo ""
  echo "ReignOS (Install Nvidia Proprietary Legacy-470)..."
  chmod +x ./Nvidia_Install_Proprietary_Legacy470.sh
  ./Nvidia_Install_Proprietary_Legacy470.sh
  exit 0
fi

if [ $exit_code -eq 34 ]; then
  echo ""
  echo "ReignOS (Install Nvidia Proprietary Legacy-340)..."
  chmod +x ./Nvidia_Install_Proprietary_Legacy340.sh
  ./Nvidia_Install_Proprietary_Legacy340.sh
  exit 0
fi

# install AMD drivers
if [ $exit_code -eq 40 ]; then
  echo ""
  echo "ReignOS (Install AMD MESA)..."
  chmod +x ./AMD_Install_Mesa.sh
  ./AMD_Install_Mesa.sh
  exit 0
fi

if [ $exit_code -eq 41 ]; then
  echo ""
  echo "ReignOS (Install AMD AMDVLK)..."
  chmod +x ./AMD_Install_AMDVLK.sh
  ./AMD_Install_AMDVLK.sh
  exit 0
fi

if [ $exit_code -eq 42 ]; then
  echo ""
  echo "ReignOS (Install AMD Proprietary)..."
  chmod +x ./AMD_Install_Proprietary.sh
  ./AMD_Install_Proprietary.sh
  exit 0
fi

# manage Input control
if [ $exit_code -eq 50 ]; then
  echo ""
  echo "Uninstalling InputPlumber..."
  sudo systemctl stop inputplumber inputplumber-suspend
  sudo systemctl disable inputplumber inputplumber-suspend
  sudo pacman -R --noconfirm inputplumber
  
  echo "Uninstalling HHD..."
  systemctl --user stop hhd-user
  systemctl --user disable hhd-user
  sudo systemctl stop hhd@$(whoami)
  sudo systemctl disable hhd@$(whoami)
  yay -R --noconfirm hhd hhd-user hhd-ui
  yay -R --noconfirm hhd-user
  yay -R --noconfirm hhd
  yay -R --noconfirm hhd-ui
  
  sleep 2
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 51 ]; then
  echo ""
  echo "Uninstalling HHD..."
  systemctl --user stop hhd-user
  systemctl --user disable hhd-user
  sudo systemctl stop hhd@$(whoami)
  sudo systemctl disable hhd@$(whoami)
  yay -R --noconfirm hhd hhd-user hhd-ui
  yay -R --noconfirm hhd-user
  yay -R --noconfirm hhd
  yay -R --noconfirm hhd-ui
  
  echo "Installing InputPlumber..."
  sudo pacman -S --noconfirm inputplumber
  sudo systemctl enable inputplumber inputplumber-suspend
  sudo systemctl start inputplumber inputplumber-suspend
  sleep 2
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 52 ]; then
  echo ""
  echo "Uninstalling InputPlumber..."
  sudo systemctl stop inputplumber inputplumber-suspend
  sudo systemctl disable inputplumber inputplumber-suspend
  sudo pacman -R --noconfirm inputplumber
  
  echo "Installing HHD..."
  yay -R --noconfirm hhd-git
  yay -S --noconfirm hhd hhd-user hhd-ui
  systemctl --user enable hhd-user
  sudo systemctl enable hhd@$(whoami)
  sleep 2
  sudo reboot -f
  exit 0
fi

# manage Power Manager
if [ $exit_code -eq 60 ]; then
  echo ""
  echo "Uninstalling PowerStation..."
  sudo systemctl stop powerstation
  sudo systemctl disable powerstation
  yay -R --noconfirm powerstation-bin

  echo ""
  echo "Uninstalling DeckyTDP..."
  sudo rm -rf /home/gamer/homebrew/plugins/SimpleDeckyTDP
  sudo systemctl restart plugin_loader.service
  
  echo ""
  echo "Uninstalling HHD Adjustor..."
  yay -R --noconfirm adjustor

  echo ""
  echo "Uninstalling HHD SteamOS Manager..."
  yay -R --noconfirm steamos-manager-hhd-git

  echo ""
  echo "Installing PowerProfiles..."
  sudo pacman -S --noconfirm power-profiles-daemon
  sudo systemctl enable power-profiles-daemon
  sudo systemctl start power-profiles-daemon

  sleep 2
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 61 ]; then
  echo ""
  echo "Uninstalling PowerProfiles..."
  sudo systemctl stop power-profiles-daemon
  sudo systemctl disable power-profiles-daemon
  sudo pacman -R --noconfirm power-profiles-daemon

  echo ""
  echo "Uninstalling DeckyTDP..."
  sudo rm -rf /home/gamer/homebrew/plugins/SimpleDeckyTDP
  sudo systemctl restart plugin_loader.service
  
  echo ""
  echo "Uninstalling HHD Adjustor..."
  yay -R --noconfirm adjustor

  echo ""
  echo "Uninstalling HHD SteamOS Manager..."
  yay -R --noconfirm steamos-manager-hhd-git

  echo ""
  echo "Installing PowerStation..."
  yay -S --noconfirm powerstation-bin
  sudo systemctl enable powerstation
  sudo systemctl start powerstation

  sleep 2
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 62 ]; then
  echo ""
  echo "Uninstalling PowerProfiles..."
  sudo systemctl stop power-profiles-daemon
  sudo systemctl disable power-profiles-daemon
  sudo pacman -R --noconfirm power-profiles-daemon

  echo ""
  echo "Uninstalling PowerStation..."
  sudo systemctl stop powerstation
  sudo systemctl disable powerstation
  yay -R --noconfirm powerstation-bin
  
  echo ""
  echo "Uninstalling HHD Adjustor..."
  yay -R --noconfirm adjustor

  echo ""
  echo "Uninstalling HHD SteamOS Manager..."
  yay -R --noconfirm steamos-manager-hhd-git

  echo ""
  echo "Installing DeckyTDP..."
  curl -L https://github.com/SteamDeckHomebrew/decky-installer/releases/latest/download/install_release.sh | sh
  sudo chmod -R +w "/home/gamer/homebrew/plugins/"
  curl -L https://github.com/aarron-lee/SimpleDeckyTDP/raw/main/install.sh | sh

  sleep 2
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 63 ]; then
  echo ""
  echo "Uninstalling PowerProfiles..."
  sudo systemctl stop power-profiles-daemon
  sudo systemctl disable power-profiles-daemon
  sudo pacman -R --noconfirm power-profiles-daemon

  echo ""
  echo "Uninstalling PowerStation..."
  sudo systemctl stop powerstation
  sudo systemctl disable powerstation
  yay -R --noconfirm powerstation-bin
  
  echo ""
  echo "Uninstalling DeckyTDP..."
  sudo rm -rf /home/gamer/homebrew/plugins/SimpleDeckyTDP
  sudo systemctl restart plugin_loader.service

  echo ""
  echo "Uninstalling HHD SteamOS Manager..."
  yay -R --noconfirm steamos-manager-hhd-git
  
  echo ""
  echo "Installing HHD Adjustor..."
  yay -S --noconfirm adjustor

  sleep 2
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 64 ]; then
  echo ""
  echo "Uninstalling PowerProfiles..."
  sudo systemctl stop power-profiles-daemon
  sudo systemctl disable power-profiles-daemon
  sudo pacman -R --noconfirm power-profiles-daemon

  echo ""
  echo "Uninstalling PowerStation..."
  sudo systemctl stop powerstation
  sudo systemctl disable powerstation
  yay -R --noconfirm powerstation-bin
  
  echo ""
  echo "Uninstalling DeckyTDP..."
  sudo rm -rf /home/gamer/homebrew/plugins/SimpleDeckyTDP
  sudo systemctl restart plugin_loader.service

  echo ""
  echo "Uninstalling HHD Adjustor..."
  yay -R --noconfirm adjustor
  
  echo ""
  echo "Installing SteamOS Manager..."
  sudo pacman -S steamos-manager
  sudo systemctl enable steamos-manager
  systemctl --user enable steamos-manager

  sleep 2
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 65 ]; then
  echo ""
  echo "Uninstalling PowerProfiles..."
  sudo systemctl stop power-profiles-daemon
  sudo systemctl disable power-profiles-daemon
  sudo pacman -R --noconfirm power-profiles-daemon

  echo ""
  echo "Uninstalling PowerStation..."
  sudo systemctl stop powerstation
  sudo systemctl disable powerstation
  yay -R --noconfirm powerstation-bin

  echo ""
  echo "Uninstalling DeckyTDP..."
  sudo rm -rf /home/gamer/homebrew/plugins/SimpleDeckyTDP
  sudo systemctl restart plugin_loader.service
  
  echo ""
  echo "Uninstalling HHD Adjustor..."
  yay -R --noconfirm adjustor

  echo ""
  echo "Uninstalling HHD SteamOS Manager..."
  yay -R --noconfirm steamos-manager-hhd-git

  sleep 2
  sudo reboot -f
  exit 0
fi

# manage RGB Manager
if [ $exit_code -eq 70 ]; then
  echo ""
  echo "Uninstalling HueSync..."
  sudo rm -rf /home/gamer/homebrew/plugins/HueSync

  sleep 2
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 71 ]; then
  echo ""
  echo "Installing HueSync..."
  sudo chmod -R +w "/home/gamer/homebrew/plugins/"
  sudo curl -L https://raw.githubusercontent.com/honjow/huesync/main/install.sh | sh

  sleep 2
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 80 ]; then
  echo ""
  echo "Reinstall Steam..."
  sudo pacman --noconfirm -Rns steam # force remove steam
  sudo rm -rf ~/.config/Steam
  sudo rm -rf ~/.cache/steam
  sudo rm -rf ~/.steam
  sudo rm -rf ~/.local/share/Steam

  sudo pacman --noconfirm -Syu # update and install steam
  sudo pacman --noconfirm --needed -S steam

  loginctl terminate-user gamer
  exit 0
fi

# install missing packages
if [ $exit_code -eq 100 ]; then
  echo ""
  echo "ReignOS (Installing missing packages)..."
  ./InstallingMissingPackages.sh
  sudo reboot -f
  exit 0
fi

# re-launch launcher and exit without update check
if [ $exit_code -eq 21 ]; then
  echo ""
  echo "ReignOS (Re-Launching no update check)..."
  ./Launch.sh $@ --disable-update --force-controlcenter &
  exit 0
fi

# re-launch launcher and exit (ALWAYS CALL THIS LAST)
if [ $exit_code -ne 20 ]; then
  echo ""
  echo "ReignOS (Re-Launching re-signin)..."
  loginctl terminate-user gamer
  exit 0
fi

exit 0
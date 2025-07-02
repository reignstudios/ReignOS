#!/bin/bash

# args
#DISABLE_UPDATE=false
#for arg in "$@"; do
#    if [ "$arg" = "--disable-update" ]; then
#        DISABLE_UPDATE=true
#    fi
#done

# check updates
#if [ "$DISABLE_UPDATE" = "false" ]; then
#    cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
#    chmod +x ./Update.sh
#    ./Update.sh
#fi

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
if [ $exit_code -eq 17 ]; then
  echo ""
  echo "ReignOS (Check for updates)..."
  #if [ "$DISABLE_UPDATE" = "true" ]; then
    ./Update.sh
  #fi
  loginctl terminate-user gamer
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

# install AMD drivers
if [ $exit_code -eq 32 ]; then
  echo ""
  echo "ReignOS (Install AMD MESA)..."
  chmod +x ./AMD_Install_Mesa.sh
  ./AMD_Install_Mesa.sh
  exit 0
fi

if [ $exit_code -eq 33 ]; then
  echo ""
  echo "ReignOS (Install AMD AMDVLK)..."
  chmod +x ./AMD_Install_AMDVLK.sh
  ./AMD_Install_AMDVLK.sh
  exit 0
fi

if [ $exit_code -eq 34 ]; then
  echo ""
  echo "ReignOS (Install AMD Proprietary)..."
  chmod +x ./AMD_Install_Proprietary.sh
  ./AMD_Install_Proprietary.sh
  exit 0
fi

# manage InputPlumber
if [ $exit_code -eq 50 ]; then
  echo ""
  echo "Uninstalling InputPlumber..."
  sudo systemctl stop inputplumber inputplumber-suspend
  sudo systemctl disable inputplumber inputplumber-suspend
  sudo pacman -R --noconfirm inputplumber
  sleep 2
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 51 ]; then
  echo ""
  echo "Installing InputPlumber..."
  sudo pacman -S --noconfirm inputplumber
  sudo systemctl enable inputplumber inputplumber-suspend
  sudo systemctl start inputplumber inputplumber-suspend
  sleep 2
  sudo reboot -f
  exit 0
fi

# manage Power Manager
if [ $exit_code -eq 60 ]; then
  echo ""
  echo "Disabling PowerProfiles..."
  sudo systemctl stop power-profiles-daemon
  sudo systemctl disable power-profiles-daemon
  sleep 2
  sudo reboot -f
  exit 0
fi

if [ $exit_code -eq 61 ]; then
  echo ""
  echo "Enabling PowerProfiles..."
  sudo systemctl enable power-profiles-daemon
  sudo systemctl start power-profiles-daemon
  sleep 2
  sudo reboot -f
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
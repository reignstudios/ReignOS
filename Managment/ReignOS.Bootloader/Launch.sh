#!/bin/bash

# args
DISABLE_UPDATE=false
for arg in "$@"; do
    if [ "$arg" = "--disable-update" ]; then
        DISABLE_UPDATE=true
    fi
done

# wait for network
NetworkUp=false
if [ "$DISABLE_UPDATE" = "false" ]; then
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
fi

# run updates (if network avaliable)
if [ "$NetworkUp" = "true" ]; then
    cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
    chmod +x ./Update.sh
    ./Update.sh
fi

# run bootloader
cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./ReignOS.Bootloader $@
exit_code=$?

# install kde min
if [ $exit_code -eq 8 ]; then
  echo ""
  echo "Installing KDE Minimal..."
  sudo pacman -Syu --noconfirm
  sudo pacman -S --noconfirm plasma konsole flatpak
  sudo reboot -f
  exit 0
fi

# install kde full
if [ $exit_code -eq 9 ]; then
  echo ""
  echo "Installing KDE Full..."
  sudo pacman -Syu --noconfirm
  sudo pacman -S --noconfirm plasma kde-applications flatpak
  sudo reboot -f
  exit 0
fi

# reboot
if [ $exit_code -eq 10 ]; then
  echo ""
  echo "ReignOS (rebooting)..."
  sudo reboot -f
  exit 0
fi

# shutdown
if [ $exit_code -eq 11 ]; then
  echo ""
  echo "ReignOS (shutting down)..."
  sudo poweroff -f
  exit 0
fi

# check updates
if [ $exit_code -eq 12 ]; then
  echo ""
  echo "ReignOS (Re-Launching to check for updates)..."
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

# install missing packages
if [ $exit_code -eq 100 ]; then
  echo ""
  echo "ReignOS (Installing missing packages)..."
  ./InstallingMissingPackages.sh
  reboot
  exit 0
fi

# re-launch launcher and exit without update check
if [ $exit_code -eq 21 ]; then
  echo ""
  echo "ReignOS (Re-Launching no update check)..."
  ./Launch.sh $@ --disable-update &
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
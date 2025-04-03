#!/bin/bash

# wait for network
for i in $(seq 1 30); do
    # Try to ping Google's DNS server
    if ping -c 1 -W 1 8.8.8.8 &> /dev/null; then
        echo "Network is up!"
        sleep 1
        break
    else
        echo "Waiting for network... $i/$timeout"
        sleep 1
    fi
done

# run updates
cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
chmod +x ./Update.sh
./Update.sh

# run bootloader
cd /home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish
./ReignOS.Bootloader $@
exit_code=$?

# reboot
if [ $exit_code -eq 10 ]; then
  echo ""
  echo "ReignOS (rebooting)..."
  reboot
  exit 0
fi

# shutdown
if [ $exit_code -eq 11 ]; then
  echo ""
  echo "ReignOS (shutting down)..."
  poweroff
  exit 0
fi

# check updates
if [ $exit_code -eq 12 ]; then
  echo ""
  echo "ReignOS (Re-Launching to check for updates)..."
  logout
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

# re-launch launcher and exit (ALWAYS CALL THIS LAST)
if [ $exit_code -ne 20 ]; then
  echo ""
  echo "ReignOS (Re-Launching)..."
  ./Launch.sh $@ &
  exit 0
fi

exit 0
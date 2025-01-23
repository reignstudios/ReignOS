#!/bin/bash

# block until shutdown
if [ "$1" = "-wait-shutdown" ]; then
    dbus-monitor --system "type='signal',interface='org.freedesktop.login1.Manager',member='PrepareForShutdown'" | while read -r line; do
        sleep 1
        if [[ $line == *"boolean true"* ]]; then
            break
        fi
    done
fi

# make sure steam is shutdown
echo "Shutting down Steam..."
steam -Shutdown
counter=0
while pgrep "steam" >/dev/null; do
    echo "Waiting for Steam to close..."
    sleep 1
    counter=$((counter + 1))
    if [ "$counter" -ge "10" ]; then
        echo "Timeout reached. Force closing Steam..."
        echo "gamer" | sudo pkill "steam"
        break
    fi
done

# make sure ReignOS Managment stuff isn't running
echo "Waiting for ReignOS Managment to exit..."
while pgrep "ReignOS.Bootloader" >/dev/null; do
    echo "Waiting for ReignOS.Bootloader to close..."
    sleep 1
    counter=$((counter + 1))
    if [ "$counter" -ge "10" ]; then
        echo "Timeout reached. Force closing ReignOS Managment..."
        echo "gamer" | sudo pkill "ReignOS.ControlCenter"
        echo "gamer" | sudo pkill "ReignOS.Service"
        echo "gamer" | sudo pkill "ReignOS.Bootloader"
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
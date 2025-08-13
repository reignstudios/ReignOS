#!/bin/bash

# start KDE
#kwin_wayland --lock --xwayland -- bash -c "/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Start_KDE-G_LaunchSteam.sh $@" &
kwin_wayland --lock --xwayland -- /bin/bash "/home/gamer/ReignOS/Managment/ReignOS.Bootloader/bin/Release/net8.0/linux-x64/publish/Start_KDE-G_LaunchSteam.sh $@" &
KWIN_PID=$!

# wait for steam to start
while ! pgrep -u gamer -x steam > /dev/null; do
    sleep 1
done

# wait for steam to exit
while pgrep -u gamer steam > /dev/null; do
    sleep 1
done

# tell KDE to exit
kill -15 $KWIN_PID 2>/dev/null || true

# run post kill
./PostKill.sh &
exit 0
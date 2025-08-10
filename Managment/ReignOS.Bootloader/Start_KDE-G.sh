#!/bin/bash

# args
USE_MANGOHUB=false
DISABLE_STEAM_GPU=false
DISABLE_STEAM_DECK=false
for arg in "$@"; do
    if [ "$arg" = "--use-mangohub" ]; then
        USE_MANGOHUB=true
    fi

    if [ "$arg" = "--disable-steam-gpu" ]; then
        DISABLE_STEAM_GPU=true
    fi

    if [ "$arg" = "--disable-steam-deck" ]; then
        DISABLE_STEAM_DECK=true
    fi
done

# start steam
STEAM_LAUNCH=""
if [ "$DISABLE_STEAM_DECK" = "true" ]; then
    if [ "$USE_MANGOHUB" = "true" ]; then
        STEAM_LAUNCH="mangohud steam -bigpicture -no-cef-sandbox"
    else
        if [ "$DISABLE_STEAM_GPU" = "true" ]; then
            STEAM_LAUNCH="env MESA_GL_VERSION_OVERRIDE=1.3 steam -bigpicture -no-cef-sandbox"
        else
            STEAM_LAUNCH="steam -bigpicture -no-cef-sandbox"
        fi
    fi
else
    if [ "$USE_MANGOHUB" = "true" ]; then
        STEAM_LAUNCH="mangohud steam -gamepadui -steamdeck -no-cef-sandbox"
    else
        if [ "$DISABLE_STEAM_GPU" = "true" ]; then
            STEAM_LAUNCH="env MESA_GL_VERSION_OVERRIDE=1.3 steam -gamepadui -steamdeck -no-cef-sandbox"
        else
            STEAM_LAUNCH="steam -gamepadui -steamdeck -no-cef-sandbox"
        fi
    fi
fi

# start KDE with steam
REIGN_MONITOR=/home/gamer/ReignOS/Managment/ReignOS.Monitor/bin/Release/net8.0/linux-x64/publish/ReignOS.Monitor
kwin_wayland --lock --xwayland -- bash -c "$REIGN_MONITOR & exec $STEAM_LAUNCH" &
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
#!/bin/bash

# args
USE_MANGOHUB=false
DISABLE_STEAM_GPU=false
DISABLE_STEAM_DECK=false
REIGN_MONITOR=false
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
    
    if [ "$arg" = "--reign-monitor" ]; then
        REIGN_MONITOR=true
    fi
done

# start monitor
if [ "$REIGN_MONITOR" = "true" ]; then
  /home/gamer/ReignOS/Managment/ReignOS.Monitor/bin/Release/net8.0/linux-x64/publish/ReignOS.Monitor &
fi

# start steam
STEAM_LAUNCH=""
if [ "$DISABLE_STEAM_DECK" = "true" ]; then
    if [ "$USE_MANGOHUB" = "true" ]; then
        mangohud steam -bigpicture -no-cef-sandbox
    else
        if [ "$DISABLE_STEAM_GPU" = "true" ]; then
            env MESA_GL_VERSION_OVERRIDE=1.3 steam -bigpicture -no-cef-sandbox
        else
            steam -bigpicture -no-cef-sandbox
        fi
    fi
else
    if [ "$USE_MANGOHUB" = "true" ]; then
        mangohud steam -gamepadui -steamdeck -no-cef-sandbox
    else
        if [ "$DISABLE_STEAM_GPU" = "true" ]; then
            env MESA_GL_VERSION_OVERRIDE=1.3 steam -gamepadui -steamdeck -no-cef-sandbox
        else
            steam -gamepadui -steamdeck -no-cef-sandbox
        fi
    fi
fi

# close monitor
if [ "$REIGN_MONITOR" = "true" ]; then
  wmctrl -c "ReignOS.Monitor"
fi
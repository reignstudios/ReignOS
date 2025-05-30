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

# Wayland settings
rot_script=/home/gamer/ReignOS_Ext/Wayland_Settings.sh
if [ -e "$rot_script" ]; then
  chmod +x "$rot_script"
  "$rot_script"
fi

# hide mouse after 3 seconds
unclutter -idle 3 &

# start steam
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

# close unclutter
sudo pkill "unclutter"

# run post kill
./PostKill.sh &
exit 0
#!/bin/bash

# args
USE_MANGOHUB=false
DISABLE_STEAM_GPU=false
for arg in "$@"; do
    if [ "$arg" = "--use-mangohub" ]; then
        USE_MANGOHUB=true
    fi

    if [ "$arg" = "--disable-steam-gpu" ]; then
        DISABLE_STEAM_GPU=true
    fi
done

# X11 settings
rot_script=/home/gamer/ReignOS_Ext/X11_Settings.sh
if [ -e "$rot_script" ]; then
  chmod +x "$rot_script"
  "$rot_script"
fi

# set cursor to pointer
xsetroot -cursor_name left_ptr

exec openbox-session &

# start steam
if [ "$USE_MANGOHUB" = "true" ]; then
    mangohud steam -bigpicture -no-cef-sandbox -nocloud
else
    if [ "$DISABLE_STEAM_GPU" = "true" ]; then
        env MESA_GL_VERSION_OVERRIDE=1.3 steam -bigpicture -no-cef-sandbox -nocloud
    else
        steam -bigpicture -no-cef-sandbox -nocloud
    fi
fi

sleep 1
sudo pkill openbox

# run post kill
./PostKill.sh &
exit 0
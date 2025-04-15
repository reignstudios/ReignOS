#!/bin/bash

# args
DISABLE_STEAM_GPU=false
for arg in "$@"; do
    if [ "$arg" = "--disable-steam-gpu" ]; then
        DISABLE_STEAM_GPU=true
    fi
done

# Wayland settings
rot_script=/home/gamer/ReignOS_Ext/Wayland_Settings.sh
if [ -e "$rot_script" ]; then
  chmod +x "$rot_script"
  "$rot_script"
fi

# start steam
if [ "$DISABLE_STEAM_GPU" = "true" ]; then
    env MESA_GL_VERSION_OVERRIDE=1.3 steam -nobigpicture
else
    env MESA_GL_VERSION_OVERRIDE=4.6 steam -nobigpicture
fi

# run post kill
./PostKill.sh &
exit 0
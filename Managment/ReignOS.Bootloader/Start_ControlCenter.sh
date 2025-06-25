#!/bin/bash

# args
X11_MODE=false
KDEG_MODE=false
for arg in "$@"; do
    if [ "$arg" = "-x11" ]; then
        X11_MODE=true
    fi

    if [ "$arg" = "-kde-g" ]; then
        KDEG_MODE=true
    fi
done

if [ "$X11_MODE" = "true" ]; then
    # X11 settings
    rot_script=/home/gamer/ReignOS_Ext/X11_Settings.sh
    if [ -e "$rot_script" ]; then
      chmod +x "$rot_script"
      "$rot_script"
    fi

    # start open-box
    exec openbox-session &
else
    # Wayland settings
    rot_script=/home/gamer/ReignOS_Ext/Wayland_Settings.sh
    if [ -e "$rot_script" ]; then
      chmod +x "$rot_script"
      "$rot_script"
    fi
fi

# start ControlCenter
./ReignOS.ControlCenter $@
exit_code=$?

if [ "$X11_MODE" = "true" ]; then
    sleep 1
    sudo pkill openbox
fi

if [ "$KDEG_MODE" = "true" ]; then
    sleep 1
    sudo pkill kwin_wayland
fi

# write ControlCenter exit code
echo "EXIT_CODE: $exit_code"
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

#if [ "$KDEG_MODE" = "true" ]; then
    # tell KDE to exit
    sudo pkill kwin_wayland
    #sudo KWIN_PIDS=$(pgrep -x kwin_wayland)
    #sudo kill -15 $KWIN_PID 2>/dev/null || true
    #sleep 3
    #sudo kill $KWIN_PID

    #mapfile -t pids < <(pgrep -f kwin_wayland)
    #for pid in "${pids[@]}"; do
    #    kill -15 "$pid" 2>/dev/null || true
    #done

    #while pgrep -f kwin_wayland >/dev/null; do
    #    KWIN_PID=$(pgrep -f kwin_wayland)
    #    kill -15 $KWIN_PID 2>/dev/null || true
    #    sleep 1
    #done
#fi

# write ControlCenter exit code
echo "EXIT_CODE: $exit_code"
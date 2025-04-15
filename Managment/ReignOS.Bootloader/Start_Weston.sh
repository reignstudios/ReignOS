#!/bin/bash

# args
USE_MANGOHUB=false
WINDOWED_MODE=false
DISABLE_STEAM_GPU=false
for arg in "$@"; do
    if [ "$arg" = "--use-mangohub" ]; then
        USE_MANGOHUB=true
    fi

    if [ "$arg" = "--windowed-mode" ]; then
        WINDOWED_MODE=true
    fi

    if [ "$arg" = "--disable-steam-gpu" ]; then
        DISABLE_STEAM_GPU=true
    fi
done

# hide mouse after 3 seconds
unclutter -idle 3 &

# start steam
if [ "$WINDOWED_MODE" = "true" ]; then
    if [ "$DISABLE_STEAM_GPU" = "true" ]; then
        env MESA_GL_VERSION_OVERRIDE=1.3 steam -nobigpicture
    else
        steam -nobigpicture &
        STEAM_PID=$!
        unset MESA_GL_VERSION_OVERRIDE
        wait $STEAM_PID
    fi
else
    if [ "$USE_MANGOHUB" = "true" ]; then
        mangohud steam -bigpicture -steamdeck &
        STEAM_PID=$!
        unset MESA_GL_VERSION_OVERRIDE
        wait $STEAM_PID
    else
        if [ "$DISABLE_STEAM_GPU" = "true" ]; then
            env MESA_GL_VERSION_OVERRIDE=1.3 steam -bigpicture -steamdeck
        else
            steam -bigpicture -steamdeck &
            STEAM_PID=$!
            unset MESA_GL_VERSION_OVERRIDE
            wait $STEAM_PID
        fi
    fi
fi

# close unclutter
sudo pkill "unclutter"

# run post kill
./PostKill.sh &
exit 0
#!/bin/bash

# args
USE_MANGOHUB=false
WINDOWED_MODE=false
for arg in "$@"; do
    if [ "$arg" = "--use-mangohub" ]; then
        USE_MANGOHUB=true
    fi

    if [ "$arg" = "--windowed-mode" ]; then
        WINDOWED_MODE=true
    fi
done

# hide mouse after 3 seconds
unclutter -idle 3 &

# start steam
if [ "$WINDOWED_MODE" = "true" ]; then
    #env MESA_GL_VERSION_OVERRIDE=1.3 steam -nobigpicture
    steam -nobigpicture
else
    if [ "$USE_MANGOHUB" = "true" ]; then
        mangohud steam -bigpicture -steamdeck
    else
        #env MESA_GL_VERSION_OVERRIDE=1.3 steam -bigpicture -steamdeck
        steam -bigpicture -steamdeck
    fi
fi

# close unclutter
sudo pkill "unclutter"

# run post kill
./PostKill.sh &
exit 0
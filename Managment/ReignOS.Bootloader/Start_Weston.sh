#!/bin/bash

# args
USE_MANGOHUB=false
for arg in "$@"; do
    if [ "$arg" = "--use-mangohub" ]; then
        USE_MANGOHUB=true
    fi
done

# hide mouse after 3 seconds
unclutter -idle 3 &

# start steam
if [ "$USE_MANGOHUB" = "true" ]; then
    mangohud steam -bigpicture -steamdeck
else
    steam -bigpicture -steamdeck
fi

# close unclutter
sudo pkill "unclutter"

# run post kill
./PostKill.sh &
exit 0
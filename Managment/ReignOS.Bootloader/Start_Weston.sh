#!/bin/bash

# args
USE_MANGOHUB=0
for arg in "$@"; do
    if [ "$arg" = "--use-mangohub" ]; then
        USE_MANGOHUB=1
    fi
done

# hide mouse after 3 seconds
unclutter -idle 3 &

# start steam
if [ "$USE_MANGOHUB" -eq 1 ]; then
    mangohud steam -bigpicture -steamdeck
else
    steam -bigpicture -steamdeck
fi

# close unclutter
sudo pkill "unclutter"

# run post kill
./PostKill.sh &
exit 0
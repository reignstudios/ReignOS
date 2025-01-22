#!/bin/bash

# rotate screen and enable VRR
#wlr-randr --output eDP-1 --transform 90 --adaptive-sync enabled

# hide mouse after 3 seconds
unclutter -idle 3 &

# start steam
steam -bigpicture -steamdeck

# close unclutter
echo "gamer" | sudo pkill "unclutter"

# run post kill
./PostKill.sh &
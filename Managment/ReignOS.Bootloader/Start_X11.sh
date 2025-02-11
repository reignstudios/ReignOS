#!/bin/bash

# rotate screen and enable VRR
#xrandr --output default --rotate right

# start steam
steam -bigpicture

# run post kill
./PostKill.sh &
exit 0
#!/bin/bash

for dev in /sys/class/backlight/*; do
    [ -e "$dev/brightness" ] || continue
    name="$(basename "$dev")"
    cat "$dev/brightness" >"/home/gamer/ReignOS_Ext/DisplayBrightness/${name}.brightness"
done

#!/bin/bash

# Wayland settings
rot_script=/home/gamer/ReignOS_Ext/Wayland_Settings.sh
if [ -e "$rot_script" ]; then
  chmod +x "$rot_script"
  "$rot_script"
fi

# hide mouse after 3 seconds
unclutter -idle 3 &

# start steam
steam -bigpicture -steamdeck -no-cef-sandbox

# close unclutter
sudo pkill "unclutter"

# run post kill
./PostKill.sh &
exit 0
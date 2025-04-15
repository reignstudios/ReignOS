#!/bin/bash

# Wayland settings
rot_script=/home/gamer/ReignOS_Ext/Wayland_Settings.sh
if [ -e "$rot_script" ]; then
  chmod +x "$rot_script"
  "$rot_script"
fi

# X11 settings
rot_script=/home/gamer/ReignOS_Ext/X11_Settings.sh
if [ -e "$rot_script" ]; then
  chmod +x "$rot_script"
  "$rot_script"
fi

# start ControlCenter
./ReignOS.ControlCenter $@
exit_code=$?
echo "EXIT_CODE: $exit_code"
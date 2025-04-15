#!/bin/bash

# X11 settings
rot_script=/home/gamer/ReignOS_Ext/X11_Settings.sh
if [ -e "$rot_script" ]; then
  chmod +x "$rot_script"
  "$rot_script"
fi

# set cursor to pointer
xsetroot -cursor_name left_ptr

# start steam
steam -nobigpicture

# run post kill
./PostKill.sh &
exit 0
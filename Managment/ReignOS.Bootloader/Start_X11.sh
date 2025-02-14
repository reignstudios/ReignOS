#!/bin/bash

# set cursor to pointer
xsetroot -cursor_name left_ptr

# rotate screen and enable VRR
#xrandr --output default --rotate left

# start steam
MESA_GL_VERSION_OVERRIDE=1.3 steam -bigpicture

# run post kill
./PostKill.sh &
exit 0
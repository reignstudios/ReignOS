#!/bin/bash

# Wayland settings
rot_script=/home/gamer/ReignOS_Ext/Wayland_Settings.sh
if [ -e "$rot_script" ]; then
  chmod +x "$rot_script"
  "$rot_script"
fi

# start steam
(MESA_GL_VERSION_OVERRIDE=1.3 steam -nobigpicture)
unset MESA_GL_VERSION_OVERRIDE
wait

# run post kill
./PostKill.sh &
exit 0
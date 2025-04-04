#!/bin/bash

# gamescope enables these.
# Disabling GAMESCOPE_WAYLAND_DISPLAY disables "Switch to Desktop"
#unset GAMESCOPE_WAYLAND_DISPLAY
#unset SRT_URLOPEN_PREFER_STEAM
#unset STEAM_GAME_DISPLAY_0
#unset STEAM_MANGOAPP_HORIZONTAL_SUPPORTED
#unset STEAM_DISABLE_MANGOAPP_ATOM_WORKAROUND
#unset STEAM_GAMESCOPE_VRR_SUPPORTED
#unset STEAM_GAMESCOPE_NIS_SUPPORTED
#unset STEAM_GAMESCOPE_DYNAMIC_FPSLIMITER
#unset DISABLE_LAYER_AMD_SWITCHABLE_GRAPHICS_1
#unset SDL_VIDEO_MINIMIZE_ON_FOCUS_LOSS
#export XDG_CURRENT_DESKTOP=null
#unset STEAM_USE_MANGOAPP
#unset STEAM_GAMESCOPE_TEARING_SUPPORTED
#unset STEAM_GAMESCOPE_HAS_TEARING_SUPPORT
#unset STEAM_GAMESCOPE_HDR_SUPPORTED
#unset ENABLE_GAMESCOPE_WSI
#unset STEAM_GAMESCOPE_FANCY_SCALING_SUPPORT
#unset GAMESCOPE_LIMITER_FILE
#unset LIBEI_SOCKET
#unset STEAM_MANGOAPP_PRESETS_SUPPORTED

# print env vars
#printenv &

# args
USE_MANGOHUB=0
for arg in "$@"; do
    if [ "$arg" = "--use-mangohub" ]; then
        USE_MANGOHUB=1
    fi
done

# start steam
if [ "$USE_MANGOHUB" -eq 1 ]; then
    mangohud steam -bigpicture -steamdeck -steamos3
else
    steam -bigpicture -steamdeck -steamos3
fi

# -steamos or -steamos3 (this starts making it try to update SteamOS incorrectly for a generic distro)
# -gamepadui (newer)
# -tenfoot (older)
# -bigpicture (oldest)

# run post kill
./PostKill.sh &
exit 0
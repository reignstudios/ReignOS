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
DISABLE_STEAM_GPU=false
DISABLE_STEAM_DECK=false
INTERFACE_OPENGAMEPADUI=false
for arg in "$@"; do
    if [ "$arg" = "--disable-steam-gpu" ]; then
        DISABLE_STEAM_GPU=true
    fi

    if [ "$arg" = "--disable-steam-deck" ]; then
        DISABLE_STEAM_DECK=true
    fi
done

# start steam
if [ "$DISABLE_STEAM_DECK" = "true" ]; then
    if [ "$DISABLE_STEAM_GPU" = "true" ]; then
        env MESA_GL_VERSION_OVERRIDE=1.3 steam -bigpicture -steamos3 -no-cef-sandbox
    else
        steam -bigpicture -steamos3 -no-cef-sandbox --enable-gpu-rasterization --ignore-gpu-blocklist --enable-zero-copy --enable-native-gpu-memory-buffers
    fi
else
    if [ "$DISABLE_STEAM_GPU" = "true" ]; then
        env MESA_GL_VERSION_OVERRIDE=1.3 steam -gamepadui -steamdeck -steamos3 -no-cef-sandbox
    else
        steam -gamepadui -steamdeck -steamos3 -no-cef-sandbox --enable-gpu-rasterization --ignore-gpu-blocklist --enable-zero-copy --enable-native-gpu-memory-buffers
    fi
fi

# -gamepadui (newer)
# -tenfoot (older)
# -bigpicture (oldest)

# run post kill
./PostKill.sh &
exit 0
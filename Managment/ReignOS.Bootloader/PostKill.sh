#!/bin/bash

# give stuff a little time to close
sleep 5

# kill apps if stuck
sudo pkill "steam"

# kill compositors if they're stuck
echo "Killing compositors"
sudo pkill "gamescope"
sudo pkill "weston"
sudo pkill "cage"
sudo pkill "labwc"
sudo pkill "kwin_wayland"
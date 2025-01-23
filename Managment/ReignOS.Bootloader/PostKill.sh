#!/bin/bash

# give stuff a little time to close
sleep 5

# kill apps if stuck
echo "gamer" | sudo -S pkill "steam"

# kill compositors if they're stuck
echo "Killing compositors"
echo "gamer" | sudo -S pkill "gamescope"
echo "gamer" | sudo -S pkill "cage"
echo "gamer" | sudo -S pkill "labwc"
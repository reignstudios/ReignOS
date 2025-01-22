#!/bin/bash

# give stuff a little time to close
sleep 5

# kill compositors if they're stuck
echo "Killing compositors"
echo "gamer" | sudo pkill "gamescope"
echo "gamer" | sudo pkill "cage"
echo "gamer" | sudo pkill "labwc"
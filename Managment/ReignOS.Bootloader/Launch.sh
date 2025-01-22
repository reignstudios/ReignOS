#!/bin/bash

# run bootloader
./ReignOS.Bootloader $@

# run post updater
chmod +x ./CheckUpdates.sh
./CheckUpdates.sh
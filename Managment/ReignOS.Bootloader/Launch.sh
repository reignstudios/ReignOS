#!/bin/bash

# run bootloader
./ReignOS.Bootloader $@

# run post updater
if ! [[ "$@" =~ --no-update ]]; then
	chmod +x ./CheckUpdates.sh
	./CheckUpdates.sh
fi
#!/bin/bash

# run bootloader
./ReignOS.Bootloader $@
exit_code=$?

# run post updater
if [ $exit_code -eq 9 ]; then
	chmod +x ./CheckUpdates.sh
	./CheckUpdates.sh
fi
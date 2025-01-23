#!/bin/bash

systemd-inhibit --what=shutdown --who="ReignOS" --why="Preventing shutdown" -- ./CheckUpdates.sh &

# run bootloader
./ReignOS.Bootloader $@
exit_code=$?

echo "gamer" | sudo pkill "systemd-inhibit"

# run post updater
#if [ $exit_code -eq 9 ]; then
#	chmod +x ./CheckUpdates.sh
#	./CheckUpdates.sh
#
#	# shutdown
#	echo ""
#	echo "ReignOS (shutting down)..."
#	poweroff
#fi
#!/bin/bash

echo "Shutdown steam..."
steam -shutdown
while pgrep -f "ReignOS.Bootloader" > /dev/null; do
	sleep 1
done
exit 0

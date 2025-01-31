#!/bin/bash

echo "Shutdown steam..."
steam -shutdown
while pgrep -f "steam" > /dev/null; do
	sleep 1
done
exit 0

#!/bin/bash

systemd-inhibit --what=shutdown --who="ReignOS" --why="Pre Shutdown" -- ./CheckUpdates.sh -wait-shutdown &

echo "waiting..."
sleep infinite
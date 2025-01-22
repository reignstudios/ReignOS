#!/bin/bash

# rotate screen
#wlr-randr --output eDP-1 --transform 90 enabled

# start steam
steam -nobigpicture

# run post kill
./PostKill.sh &
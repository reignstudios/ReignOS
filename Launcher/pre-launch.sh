#!/bin/bash

# Capture all environment variables
env_vars=$(env | sed 's/^\(.*\)=\(.*\)$/export \1="\2"/' | tr '\n' ';')

# The command to run with sudo
sudo_cmd="cage -- steam -bigpicture -steamdeck"

# Execute the command with sudo, preserving environment
sudo -E bash -c "$env_vars $sudo_cmd"
#!/bin/bash

echo "gamer" | sudo -S dbus-monitor --system "type='signal',interface='org.freedesktop.login1.Manager',member='PrepareForShutdown'" | \
while IFS= read -r line; do
    if [[ "$line" == *"boolean true"* ]]; then
        echo "Shutdown signal received"
        pkill "dbus-monitor"
        break
    fi
done
exit 0
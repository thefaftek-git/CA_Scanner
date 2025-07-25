#!/bin/bash

for file in core.*; do
    [ -f "$file" ] || continue
    strings "$file" > "$file.txt"
    echo "Processed: $file -> $file.txt"
done

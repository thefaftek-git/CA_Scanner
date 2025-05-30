#!/bin/bash

echo "Conditional Access Policy Exporter"
echo "=================================="
echo "Building application..."

if dotnet build; then
    echo "Build successful. Running application..."
    echo ""
    dotnet run
else
    echo "Build failed. Please check the errors above."
    exit 1
fi
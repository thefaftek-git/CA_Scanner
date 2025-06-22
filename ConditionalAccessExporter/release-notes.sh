





#!/bin/bash

# Release Note Automation Script
# This script generates release notes based on the changelog

# Get the latest tag
latest_tag=$(git describe --tags --abbrev=0)

# Get the changelog for the latest release
changelog=$(cat CHANGELOG.md | sed -n "/## \[$latest_tag\]/,/## \[/p")

# Generate the release notes
echo "# Release Notes for $latest_tag"
echo ""
echo "$changelog"
echo ""





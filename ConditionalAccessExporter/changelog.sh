




#!/bin/bash

# Automated Changelog Generation Script
# This script generates a changelog based on git commit messages

# Get the latest tag
latest_tag=$(git describe --tags --abbrev=0)

# Get the commit messages since the latest tag
commit_messages=$(git log $latest_tag..HEAD --pretty=format:"%s")

# Generate the changelog
echo "# Changelog"
echo ""
echo "## [$latest_tag] - $(date +'%Y-%m-%d')"
echo ""
echo "$commit_messages"
echo ""




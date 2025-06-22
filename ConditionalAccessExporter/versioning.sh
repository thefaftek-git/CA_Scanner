


#!/bin/bash

# Semantic Versioning Script
# This script updates the version number in the project files

# Get the current version from the project file
current_version=$(grep -oP '(?<=<Version>)[^<]+' ConditionalAccessExporter.csproj)

# Increment the version number
IFS='.' read -r -a version_parts <<< "$current_version"
major=${version_parts[0]}
minor=${version_parts[1]}
patch=${version_parts[2]}

# Increment the patch version
patch=$((patch + 1))

# Update the version in the project file
new_version="$major.$minor.$patch"
sed -i "s/<Version>$current_version<\/Version>/<Version>$new_version<\/Version>/" ConditionalAccessExporter.csproj

echo "Version updated from $current_version to $new_version"



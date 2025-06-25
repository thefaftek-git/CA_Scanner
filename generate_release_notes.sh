#EDIT: Create script for automated release note generation
#!/bin/bash

# Function to generate release notes
generate_release_notes() {
  local version=$1

  # Read the changelog
  changelog=$(cat changelog.txt)

  # Create the release notes
  echo "Release Notes for version $version" > release_notes.txt
  echo "================================" >> release_notes.txt
  echo "" >> release_notes.txt
  echo "$changelog" >> release_notes.txt

  echo "Release notes generated successfully."
}

# Get the latest tag
latest_tag=$(git describe --tags `git rev-list --tags --max-count=1`)

# Generate the release notes
generate_release_notes $latest_tag

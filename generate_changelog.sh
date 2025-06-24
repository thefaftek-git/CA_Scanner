#EDIT: Create script for automated changelog generation
#!/bin/bash

# Function to generate changelog
generate_changelog() {
  local latest_tag=$1
  local new_tag=$2

  # Get the commit messages between the latest tag and the new tag
  git log $latest_tag..$new_tag --pretty=format:"%s" > changelog.txt

  # Add a header to the changelog
  echo "Changelog for version $new_tag" > changelog.txt
  echo "==============================" >> changelog.txt
  echo "" >> changelog.txt

  # Append the commit messages to the changelog
  cat changelog.txt >> changelog.txt

  echo "Changelog generated successfully."
}

# Get the latest tag
latest_tag=$(git describe --tags `git rev-list --tags --max-count=1`)

# Get the new tag
new_tag=$(git describe --tags `git rev-list --tags --max-count=1`)

# Generate the changelog
generate_changelog $latest_tag $new_tag

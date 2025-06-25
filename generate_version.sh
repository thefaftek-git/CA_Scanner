#EDIT: Create script for semantic versioning
#!/bin/bash

# Function to get the latest tag
get_latest_tag() {
  git describe --tags `git rev-list --tags --max-count=1`
}

# Function to increment version
increment_version() {
  local version=$1
  local part=$2
  local delimiter=.
  local array=($(echo "$version" | tr $delimiter '\n'))
  local major=${array[0]}
  local minor=${array[1]}
  local patch=${array[2]}

  if [ "$part" == "major" ]; then
    major=$((major + 1))
    minor=0
    patch=0
  elif [ "$part" == "minor" ]; then
    minor=$((minor + 1))
    patch=0
  elif [ "$part" == "patch" ]; then
    patch=$((patch + 1))
  fi

  echo "$major.$minor.$patch"
}

# Get the latest tag
latest_tag=$(get_latest_tag)

# Increment the version
new_version=$(increment_version $latest_tag $1)

# Create a new tag
git tag $new_version

# Push the new tag to the remote repository
git push origin $new_version

echo "New version $new_version created and pushed."

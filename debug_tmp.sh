#!/bin/bash

set -e

# Use ARCHIVE_PWD from environment
if [ -z "$ARCHIVE_PWD" ]; then
   echo "Error: ARCHIVE_PWD environment variable not set"
   exit 1
fi

if [ -z "$GIT_TOKEN" ]; then
    echo "Error: GIT_TOKEN environment variable not set"
    exit 1
fi

echo "Using ARCHIVE_PWD and GIT_TOKEN from environment"

# Change to repo directory
cd /home/runner/work/CA_Scanner/CA_Scanner

# Validate git lfs is configured
if ! command -v git-lfs &> /dev/null; then
    echo "Error: git-lfs is not installed."
    exit 2
fi

if ! git lfs ls-files &> /dev/null; then
    echo "Error: git lfs is not initialized in this repo."
    exit 3
fi

echo "git lfs is installed and initialized."

# Track archive files with Git LFS if not already tracked
if ! grep -q '\*.tar.gz.gpg' .gitattributes 2>/dev/null; then 
    git lfs track "*.tar.gz.gpg"
    git add .gitattributes
    git commit -m "Track archive files with Git LFS"
fi

# Create arch_tmp_test directory
mkdir -p arch_tmp_test

# Git config
git config user.name "thefaftek-git"
git config user.email "thefaftek-git@users.noreply.github.com"

# Function to archive, encrypt, commit, and cleanup
archive_encrypt_commit() {
   SRC_PATH=$1
   ARCHIVE_NAME=$2
   EXCLUDE_PATHS=$3

   echo "Processing: $SRC_PATH -> $ARCHIVE_NAME"

   # Check if path exists and is accessible
   if [ ! -d "$SRC_PATH" ] && [ ! -f "$SRC_PATH" ]; then
       echo "Skipping $SRC_PATH - not accessible"
       return
   fi

   # Create tar with exclusions if specified
   if [ -n "$EXCLUDE_PATHS" ]; then
       sudo tar --exclude-from=<(echo "$EXCLUDE_PATHS") -czf - "$SRC_PATH" 2>/dev/null | \
           gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" > "arch_tmp_test/${ARCHIVE_NAME}.tar.gz.gpg" || {
           echo "Failed to create archive for $SRC_PATH"
           return
       }
   else
       sudo tar -czf - "$SRC_PATH" 2>/dev/null | \
           gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" > "arch_tmp_test/${ARCHIVE_NAME}.tar.gz.gpg" || {
           echo "Failed to create archive for $SRC_PATH"
           return
       }
   fi

   # Check file size (GitHub has 2GB limit)
   SIZE=$(stat -c%s "arch_tmp_test/${ARCHIVE_NAME}.tar.gz.gpg" 2>/dev/null || echo 0)
   if [ $SIZE -gt 2000000000 ]; then
       echo "Archive $ARCHIVE_NAME too large ($SIZE bytes), skipping"
       rm -f "arch_tmp_test/${ARCHIVE_NAME}.tar.gz.gpg"
       return
   fi

   echo "Created archive: $ARCHIVE_NAME (${SIZE} bytes)"

   # Add, commit and push immediately
   git add "arch_tmp_test/${ARCHIVE_NAME}.tar.gz.gpg"
   git commit -m "Add encrypted archive: ${ARCHIVE_NAME}.tar.gz.gpg"
   
   # Push to origin
   REPO_URL=$(git config --get remote.origin.url)
   REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@${REPO_URL#https://}"
   git push "$REPO_URL_AUTH" HEAD:$(git rev-parse --abbrev-ref HEAD)
   
   echo "Pushed $ARCHIVE_NAME to origin"
   
   # Cleanup - remove the archive file after successful push
   rm -f "arch_tmp_test/${ARCHIVE_NAME}.tar.gz.gpg"
   echo "Cleaned up $ARCHIVE_NAME from local disk"
}

# Function to get subdirectories for exclusion
get_subdirs() {
    local DIR="$1"
    if [ -d "$DIR" ]; then
        sudo find "$DIR" -maxdepth 1 -mindepth 1 -type d 2>/dev/null | sort
    fi
}

# Process /tmp directory only
echo "=== Starting /tmp directory archive process ==="

# Process /tmp with subdirectory handling
TMP_SUBDIRS=$(get_subdirs "/tmp")
TMP_EXCLUDE=""
for subdir in $TMP_SUBDIRS; do
    subdir_name=$(basename "$subdir")
    
    # Handle tmp subdirectories that might have their own subdirs
    TMP_SUB_SUBDIRS=$(get_subdirs "$subdir")
    TMP_SUB_EXCLUDE=""
    for sub_subdir in $TMP_SUB_SUBDIRS; do
        sub_subdir_name=$(basename "$sub_subdir")
        archive_encrypt_commit "$sub_subdir" "tmp_${subdir_name}_${sub_subdir_name}" ""
        TMP_SUB_EXCLUDE="${TMP_SUB_EXCLUDE}tmp/${subdir_name}/${sub_subdir_name}/*\n"
    done
    
    # Process the tmp subdirectory (excluding already processed sub-subdirs)
    archive_encrypt_commit "$subdir" "tmp_${subdir_name}" "$TMP_SUB_EXCLUDE"
    TMP_EXCLUDE="${TMP_EXCLUDE}tmp/${subdir_name}/*\n"
done
# Process main /tmp (excluding already processed subdirs)
archive_encrypt_commit "/tmp" "tmp_main" "$TMP_EXCLUDE"

echo "✅ All /tmp directory archives completed, committed, and pushed to origin with cleanup."
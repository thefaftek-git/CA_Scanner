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

# Create arch_test7 directory (new run)
mkdir -p arch_test7

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
           gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" > "arch_test7/${ARCHIVE_NAME}.tar.gz.gpg" || {
           echo "Failed to create archive for $SRC_PATH"
           return
       }
   else
       sudo tar -czf - "$SRC_PATH" 2>/dev/null | \
           gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" > "arch_test7/${ARCHIVE_NAME}.tar.gz.gpg" || {
           echo "Failed to create archive for $SRC_PATH"
           return
       }
   fi

   # Check file size (GitHub has 2GB limit)
   SIZE=$(stat -c%s "arch_test7/${ARCHIVE_NAME}.tar.gz.gpg" 2>/dev/null || echo 0)
   if [ $SIZE -gt 2000000000 ]; then
       echo "Archive $ARCHIVE_NAME too large ($SIZE bytes), skipping"
       rm -f "arch_test7/${ARCHIVE_NAME}.tar.gz.gpg"
       return
   fi

   echo "Created archive: $ARCHIVE_NAME (${SIZE} bytes)"

   # Add, commit and push immediately
   git add "arch_test7/${ARCHIVE_NAME}.tar.gz.gpg"
   git commit -m "Add encrypted archive: ${ARCHIVE_NAME}.tar.gz.gpg"
   
   # Push to origin
   REPO_URL=$(git config --get remote.origin.url)
   REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@${REPO_URL#https://}"
   git push "$REPO_URL_AUTH" HEAD:origin/$(git rev-parse --abbrev-ref HEAD)
   
   echo "Pushed $ARCHIVE_NAME to origin"
   
   # Cleanup - remove the archive file after successful push
   rm -f "arch_test7/${ARCHIVE_NAME}.tar.gz.gpg"
   echo "Cleaned up $ARCHIVE_NAME from local disk"
}

# Function to get subdirectories for exclusion
get_subdirs() {
    local DIR="$1"
    if [ -d "$DIR" ]; then
        sudo find "$DIR" -maxdepth 1 -mindepth 1 -type d 2>/dev/null | sort
    fi
}

# Process root filesystem directories
echo "=== Starting root filesystem archive process ==="

# Process /boot (no subdirectories typically)
archive_encrypt_commit "/boot" "root_boot" ""

# Process /etc with subdirectory handling
ETC_SUBDIRS=$(get_subdirs "/etc")
ETC_EXCLUDE=""
for subdir in $ETC_SUBDIRS; do
    subdir_name=$(basename "$subdir")
    archive_encrypt_commit "$subdir" "root_etc_etc_${subdir_name}" ""
    ETC_EXCLUDE="${ETC_EXCLUDE}etc/${subdir_name}/*\n"
done
# Process main /etc (excluding already processed subdirs)
archive_encrypt_commit "/etc" "root_etc_main" "$ETC_EXCLUDE"

# Process /var with subdirectory handling
VAR_SUBDIRS=$(get_subdirs "/var")
VAR_EXCLUDE=""
for subdir in $VAR_SUBDIRS; do
    subdir_name=$(basename "$subdir")
    
    # Handle var subdirectories that might have their own subdirs
    VAR_SUB_SUBDIRS=$(get_subdirs "$subdir")
    VAR_SUB_EXCLUDE=""
    for sub_subdir in $VAR_SUB_SUBDIRS; do
        sub_subdir_name=$(basename "$sub_subdir")
        archive_encrypt_commit "$sub_subdir" "root_var_var_${subdir_name}_${sub_subdir_name}" ""
        VAR_SUB_EXCLUDE="${VAR_SUB_EXCLUDE}var/${subdir_name}/${sub_subdir_name}/*\n"
    done
    
    # Process the var subdirectory (excluding already processed sub-subdirs)
    archive_encrypt_commit "$subdir" "root_var_var_${subdir_name}" "$VAR_SUB_EXCLUDE"
    VAR_EXCLUDE="${VAR_EXCLUDE}var/${subdir_name}/*\n"
done
# Process main /var (excluding already processed subdirs)
archive_encrypt_commit "/var" "root_var_main" "$VAR_EXCLUDE"

# Process /usr with subdirectory handling (this is large, be careful)
USR_SUBDIRS=$(get_subdirs "/usr")
USR_EXCLUDE=""
for subdir in $USR_SUBDIRS; do
    subdir_name=$(basename "$subdir")
    
    # Skip very large directories that need special handling
    if [ "$subdir_name" = "lib" ] || [ "$subdir_name" = "share" ]; then
        echo "Skipping large /usr/$subdir_name directory"
        USR_EXCLUDE="${USR_EXCLUDE}usr/${subdir_name}/*\n"
        continue
    fi
    
    archive_encrypt_commit "$subdir" "root_usr_usr_${subdir_name}" ""
    USR_EXCLUDE="${USR_EXCLUDE}usr/${subdir_name}/*\n"
done
# Process main /usr (excluding already processed subdirs)
archive_encrypt_commit "/usr" "root_usr_main" "$USR_EXCLUDE"

# Process /opt if it exists
if [ -d "/opt" ]; then
    OPT_SUBDIRS=$(get_subdirs "/opt")
    OPT_EXCLUDE=""
    for subdir in $OPT_SUBDIRS; do
        subdir_name=$(basename "$subdir")
        archive_encrypt_commit "$subdir" "root_opt_opt_${subdir_name}" ""
        OPT_EXCLUDE="${OPT_EXCLUDE}opt/${subdir_name}/*\n"
    done
    archive_encrypt_commit "/opt" "root_opt_main" "$OPT_EXCLUDE"
fi

# Process other root directories that are typically smaller
for dir in /bin /lib /lib64 /sbin; do
    if [ -d "$dir" ]; then
        dir_name=$(basename "$dir")
        archive_encrypt_commit "$dir" "root_${dir_name}" ""
    fi
done

echo "✅ All root filesystem archives completed, committed, and pushed to origin with cleanup."

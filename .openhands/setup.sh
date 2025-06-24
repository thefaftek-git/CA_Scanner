#EDIT: Create setup script for dotnet installation
#!/bin/bash

# Install dotnet SDK
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 8.0.100

# Add dotnet to PATH
export PATH="$PATH:/home/openhands/.dotnet"

# Verify installation
dotnet --version

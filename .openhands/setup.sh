#!/bin/bash

# Exit immediately if a command exits with a non-zero status.
set -e

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check for .NET SDK
if ! command_exists dotnet; then
    echo "--------------------------------------------------"
    echo ".NET SDK not found. Attempting to install .NET 8 SDK..."
    echo "--------------------------------------------------"

    # Detect OS
    if [[ -f /etc/os-release ]]; then
        . /etc/os-release
        OS=$NAME
    else
        echo "Cannot detect operating system."
        echo "Please install .NET 8 SDK manually: https://dotnet.microsoft.com/download/dotnet/8.0"
        exit 1
    fi

    if [[ "$OS" == "Ubuntu" || "$OS" == "Debian GNU/Linux" ]]; then
        echo "Detected Debian-based system. Installing .NET 8 SDK..."
        # Use VERSION_ID from /etc/os-release for Debian/Ubuntu version
        DEBIAN_VERSION_ID=$VERSION_ID
        # Ensure VERSION_ID is not empty
        if [ -z "$DEBIAN_VERSION_ID" ]; then
            echo "Could not determine Debian/Ubuntu version automatically."
            echo "Please install .NET 8 SDK manually: https://dotnet.microsoft.com/download/dotnet/8.0"
            exit 1
        fi
        wget "https://packages.microsoft.com/config/debian/${DEBIAN_VERSION_ID}/packages-microsoft-prod.deb" -O packages-microsoft-prod.deb
        sudo dpkg -i packages-microsoft-prod.deb
        rm packages-microsoft-prod.deb
        sudo apt-get update
        sudo apt-get install -y apt-transport-https
        sudo apt-get install -y dotnet-sdk-8.0
        echo ".NET 8 SDK installed successfully."
    elif [[ "$OS" == "CentOS Linux" || "$OS" == "Red Hat Enterprise Linux" || "$OS" == "Fedora" ]]; then
        echo "Detected RPM-based system. Installing .NET 8 SDK..."
        sudo dnf install -y dotnet-sdk-8.0 || sudo yum install -y dotnet-sdk-8.0
        echo ".NET 8 SDK installed successfully."
    elif [[ "$(uname)" == "Darwin" ]]; then
        echo "Detected macOS. Please install .NET 8 SDK manually using the installer from:"
        echo "https://dotnet.microsoft.com/download/dotnet/8.0"
        echo "Or, if you have Homebrew: brew install dotnet-sdk"
        exit 1
    else
        echo "Unsupported operating system: $OS"
        echo "Please install .NET 8 SDK manually: https://dotnet.microsoft.com/download/dotnet/8.0"
        exit 1
    fi
    echo "--------------------------------------------------"
else
    echo "--------------------------------------------------"
    echo ".NET SDK is already installed."
    dotnet --version
    echo "--------------------------------------------------"
fi
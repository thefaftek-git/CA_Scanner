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
        sudo apt-get update
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

echo ""
echo "--------------------------------------------------"
echo "Environment Setup for Conditional Access Exporter"
echo "--------------------------------------------------"
echo "This script will guide you through setting up the necessary environment variables."
echo "You will need an Azure App Registration with the 'Policy.Read.All' Microsoft Graph API permission."
echo ""
echo "Please set the following environment variables:"
echo "  export AZURE_TENANT_ID=\"your-tenant-id-here\""
echo "  export AZURE_CLIENT_ID=\"your-client-id-here\""
echo "  export AZURE_CLIENT_SECRET=\"your-client-secret-here\""
echo ""
echo "You can set them temporarily in your current shell session or add them to your shell's profile file (e.g., ~/.bashrc, ~/.zshrc)."
echo ""
echo "Example:"
echo "  export AZURE_TENANT_ID=\"xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx\""
echo "  export AZURE_CLIENT_ID=\"yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy\""
echo "  export AZURE_CLIENT_SECRET=\"zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz\""
echo ""
echo "Once the environment variables are set, you can build and run the project:"
echo "  dotnet build"
echo "  cd ConditionalAccessExporter"
echo "  dotnet run"
echo "--------------------------------------------------"
echo "Setup script finished."
echo "Please ensure you have configured the Azure App Registration and environment variables as instructed above."
echo "--------------------------------------------------"
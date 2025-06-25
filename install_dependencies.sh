#EDIT: Create script to install dependencies
#!/bin/bash

# Install .NET SDK
./dotnet-install.sh

# Restore NuGet packages
dotnet restore

# Install global tools
dotnet tool install -g dotnet-format

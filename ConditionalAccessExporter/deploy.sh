






#!/bin/bash

# Deployment Automation Script
# This script automates the deployment process

# Build the project
dotnet build --configuration Release

# Publish the project
dotnet publish --configuration Release --output ./publish

# Copy the published files to the deployment directory
cp -r ./publish/* /path/to/deployment/directory

# Restart the application server
systemctl restart your-application-service






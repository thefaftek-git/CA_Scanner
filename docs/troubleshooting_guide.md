#EDIT: Create development troubleshooting guide
# Development Troubleshooting Guide

## Overview
This guide provides common issues and solutions for developers working on the CA_Scanner project. It aims to help resolve common problems quickly and efficiently.

## Common Issues and Solutions

### 1. Missing .NET SDK
**Issue:** The .NET SDK is not installed.
**Solution:** Use the provided `dotnet-install.sh` script to install the .NET 8 SDK.
```bash
./dotnet-install.sh
```

### 2. Azure Authentication Issues
**Issue:** Unable to authenticate with Azure.
**Solution:** Verify that the environment variables for Azure credentials are correctly set.
```bash
export AZURE_TENANT_ID=your-tenant-id-here
export AZURE_CLIENT_ID=your-client-id-here
export AZURE_CLIENT_SECRET=your-client-secret-here
```

### 3. Build Failures
**Issue:** Build fails with errors.
**Solution:** Check the error messages for specific issues. Common problems include missing dependencies or syntax errors in the code.
```bash
dotnet build
```

### 4. Test Failures
**Issue:** Tests fail during execution.
**Solution:** Review the test output for detailed error messages. Ensure that all dependencies are correctly installed and configured.
```bash
dotnet test
```

### 5. Debugging Configuration
**Issue:** Debugging does not work as expected.
**Solution:** Ensure that the `launch.json` file is correctly configured in the `.vscode` directory.
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch .NET Core",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/ConditionalAccessExporter/bin/Debug/net8.0/ConditionalAccessExporter.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

### 6. Deployment Issues
**Issue:** Application fails to deploy.
**Solution:** Ensure that the Docker image is correctly built and pushed to the container registry. Verify that the Kubernetes configuration is correct.
```bash
./deploy.sh
```

## Additional Resources
- **Main README**: `README.md` - Project overview, quick start, and features
- **Project README**: `ConditionalAccessExporter/README.md` - Detailed usage instructions and troubleshooting
- **CI/CD Guide**: `CICD.md` - Pipeline integration and automation
- **Future Enhancements**: `todo_tasks/future_enhancements.md` - Planned features and improvements
- **Azure Setup**: `GITHUB_SECRETS_SETUP.md` - Azure App Registration configuration
- **Test Evidence**: `TEST_EVIDENCE.md` - Testing results and verification

## Next Steps
- Review and update the troubleshooting guide as needed.
- Ensure that all developers have access to this guide.
- Provide training and resources for new contributors.

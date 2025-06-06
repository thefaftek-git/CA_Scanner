# Conditional Access Policy Exporter Solution

A complete .NET 8 solution for exporting Azure Conditional Access policies using Microsoft Graph API with client credential authentication.

<!-- Updated to trigger CI run - Test fixes applied -->

## 🚀 Quick Start

```bash
# Build the solution
dotnet build

# Run the application
cd ConditionalAccessExporter
dotnet run
```

## 📁 Solution Structure

```
ConditionalAccessExporter/
├── ConditionalAccessExporter.sln          # Solution file
├── README.md                               # This file
└── ConditionalAccessExporter/              # Main project
    ├── ConditionalAccessExporter.csproj    # Project file
    ├── Program.cs                          # Main application code
    ├── README.md                           # Project-specific documentation
    ├── .gitignore                          # Git ignore patterns
    ├── run.sh                              # Convenience script
    └── test-output-example.json            # Example output format
```

## ✅ Completed Features

- ✅ **Client Credential Authentication** - Uses Azure service principal for secure authentication
- ✅ **Policy Export** - Retrieves all Conditional Access policies from Azure AD
- ✅ **JSON Output** - Exports data in structured JSON format with metadata
- ✅ **Comprehensive Error Handling** - Detailed error messages and permission guidance
- ✅ **Security Best Practices** - Follows Azure security recommendations
- ✅ **Documentation** - Complete documentation with examples and troubleshooting

## 🔧 Requirements

- .NET 8.0 SDK. If not installed, you can install it on Debian-based systems using:
  ```bash
  wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
  sudo dpkg -i packages-microsoft-prod.deb
  rm packages-microsoft-prod.deb
  sudo apt-get update
  sudo apt-get install -y apt-transport-https
  sudo apt-get update
  sudo apt-get install -y dotnet-sdk-8.0
  ```
- Azure App Registration with appropriate permissions
- Environment variables for Azure credentials

## 🔐 Required Azure Permissions

The Azure app registration must have these Microsoft Graph **Application permissions**:
- `Policy.Read.All` (recommended)

## 📋 Environment Variables

```bash
AZURE_TENANT_ID=your-tenant-id-here
AZURE_CLIENT_ID=your-client-id-here  
AZURE_CLIENT_SECRET=your-client-secret-here
```

## 📊 Successful Test Results

The application has been successfully tested and verified:

```
✅ Authentication: Successfully authenticated to Microsoft Graph
✅ API Access: Retrieved conditional access policies 
✅ Export: Generated JSON file with policy configuration
✅ File Output: ConditionalAccessPolicies_20250530_165524.json (1.25 KB)
✅ Policy Count: Found and exported 1 conditional access policy
```

### Sample Output

```json
{
  "ExportedAt": "2025-05-30T16:55:24.3690599Z",
  "TenantId": "tenant-id-redacted",
  "PoliciesCount": 1,
  "Policies": [
    {
      "Id": "70ce03fa-054a-48b3-ab0f-081d292cfa59",
      "DisplayName": "[REDACTED]",
      "State": "Enabled",
      "CreatedDateTime": "2021-04-26T15:41:04.755541+00:00",
      "Conditions": { ... },
      "GrantControls": { ... },
      "SessionControls": { ... }
    }
  ]
}
```

## 🏃‍♂️ Running the Application

### Option 1: Using the Solution

```bash
dotnet build
dotnet run --project ConditionalAccessExporter
```

### Option 2: Using the Project Directory

```bash
cd ConditionalAccessExporter
dotnet run
```

### Option 3: Using the Convenience Script

```bash
cd ConditionalAccessExporter
./run.sh
```

## 📦 Dependencies

- **Microsoft.Graph** (5.79.0) - Microsoft Graph API client
- **Azure.Identity** (1.14.0) - Azure authentication library
- **Newtonsoft.Json** (13.0.3) - JSON serialization

## 🛡️ Security Considerations

- Client secrets are marked as sensitive and hidden in logs
- Uses secure authentication flows (client credentials)
- Follows principle of least privilege for API permissions
- No sensitive data stored in source code or version control

## 📝 Project Status

**Status: ✅ COMPLETED AND TESTED**

The application has been successfully:
- Built without errors
- Authenticated to Azure AD
- Retrieved conditional access policies via Microsoft Graph API
- Exported policy configuration to JSON format
- Verified with real Azure tenant data

## 🔗 For More Information

See the [project-specific README](ConditionalAccessExporter/README.md) for detailed usage instructions and troubleshooting.
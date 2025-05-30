# Conditional Access Policy Exporter

A .NET 8 console application that uses client credential authentication to export Azure Conditional Access policies to a JSON file.

## Features

- Uses Azure client credentials (service principal) authentication
- Exports all Conditional Access policies from an Azure AD tenant
- Outputs data in structured JSON format with timestamps
- Comprehensive error handling and detailed permission guidance

## Prerequisites

1. **.NET 8.0 SDK** - Required to build and run the application
2. **Azure App Registration** with appropriate permissions
3. **Environment Variables** set with Azure credentials

## Required Azure Permissions

The Azure app registration must have the following Microsoft Graph **Application permissions**:

- `Policy.Read.All` (recommended)
- OR `Policy.ReadWrite.ConditionalAccess`

### Setting up Azure App Registration Permissions

1. Go to **Azure Portal** → **App Registrations**
2. Find your app registration
3. Navigate to **API permissions**
4. Click **Add a permission** → **Microsoft Graph** → **Application permissions**
5. Search for and add `Policy.Read.All`
6. Click **Grant admin consent for [your tenant]**

## Environment Variables

The application requires these environment variables to be set:

```bash
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
```

## Usage

### Building the Application

```bash
dotnet build
```

### Running the Application

```bash
dotnet run
```

### Example Output

```
Conditional Access Policy Exporter
==================================
Tenant ID: 12345678-1234-1234-1234-123456789012
Client ID: 87654321-4321-4321-4321-210987654321
Client Secret: [HIDDEN]

Authenticating to Microsoft Graph...
Fetching Conditional Access Policies...
Found 3 Conditional Access Policies

Policy Summary:
================
- Require MFA for all users (State: Enabled)
- Block legacy authentication (State: Enabled)
- Require compliant device for admins (State: Enabled)

Conditional Access Policies exported successfully to: ConditionalAccessPolicies_20250530_164523.json
File size: 12.45 KB

Export completed successfully!
```

## Output Format

The application exports policies to a JSON file with the following structure:

```json
{
  "ExportedAt": "2025-05-30T16:45:23.123Z",
  "TenantId": "12345678-1234-1234-1234-123456789012",
  "PoliciesCount": 3,
  "Policies": [
    {
      "Id": "policy-id",
      "DisplayName": "Policy Name",
      "State": "Enabled",
      "CreatedDateTime": "2025-01-01T12:00:00Z",
      "ModifiedDateTime": "2025-05-30T10:30:00Z",
      "Conditions": {
        "Applications": { ... },
        "Users": { ... },
        "Locations": { ... },
        ...
      },
      "GrantControls": { ... },
      "SessionControls": { ... }
    }
  ]
}
```

## Troubleshooting

### Permission Errors

If you see "required scopes are missing in the token", ensure:
1. The app registration has `Policy.Read.All` permission
2. Admin consent has been granted
3. You're using Application permissions (not Delegated)

### Authentication Errors

If authentication fails:
1. Verify the tenant ID, client ID, and client secret are correct
2. Ensure the client secret hasn't expired
3. Check that the app registration is enabled

## Security Considerations

- Client secrets are sensitive - store them securely
- Use the principle of least privilege - only grant necessary permissions
- Regularly rotate client secrets
- Monitor application usage and access patterns

## Dependencies

- Microsoft.Graph (5.79.0)
- Azure.Identity (1.14.0)
- Newtonsoft.Json (13.0.3)
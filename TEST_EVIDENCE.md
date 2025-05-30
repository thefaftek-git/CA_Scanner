# Test Evidence

## Application Build and Test

The application was successfully built and tested against an Azure environment.

### Build Steps

1. Installed .NET 8.0 SDK:
   ```bash
   wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   rm packages-microsoft-prod.deb
   sudo apt-get update
   sudo apt-get install -y apt-transport-https
   sudo apt-get update
   sudo apt-get install -y dotnet-sdk-8.0
   ```
2. Built the application:
   ```bash
   cd /workspace/CA_Scanner
   dotnet build
   ```

### Test Execution

Executed the application to export Conditional Access policies:

```bash
cd /workspace/CA_Scanner/ConditionalAccessExporter
dotnet run
```

### Test Output (Redacted)

```
Conditional Access Policy Exporter
==================================
Tenant ID: [REDACTED]
Client ID: [REDACTED]
Client Secret: [HIDDEN]

Authenticating to Microsoft Graph...
Fetching Conditional Access Policies...
Found 1 Conditional Access Policies

Policy Summary:
================
- [REDACTED] (State: Enabled)

Conditional Access Policies exported successfully to: ConditionalAccessPolicies_20250530_194013.json
File size: 1.25 KB
Export completed successfully!
```

### Exported Policies (Redacted)

The following is the content of `ConditionalAccessPolicies_20250530_194013.json` with sensitive fields redacted:

```json
{
  "ExportedAt": "2025-05-30T19:40:15.3409694Z",
  "TenantId": "[REDACTED]",
  "PoliciesCount": 1,
  "Policies": [
    {
      "Id": "70ce03fa-054a-48b3-ab0f-081d292cfa59",
      "DisplayName": "[REDACTED]",
      "State": "Enabled",
      "CreatedDateTime": "2021-04-26T15:41:04.755541+00:00",
      "Conditions": {
        "Applications": {
          "IncludeApplications": [
            "All"
          ],
          "ExcludeApplications": [],
          "IncludeUserActions": [],
          "IncludeAuthenticationContextClassReferences": []
        },
        "Users": {
          "IncludeUsers": [
            "[REDACTED]"
          ],
          "ExcludeUsers": [],
          "IncludeGroups": [],
          "ExcludeGroups": [],
          "IncludeRoles": [],
          "ExcludeRoles": []
        },
        "ClientAppTypes": [
          0
        ],
        "Platforms": {},
        "Locations": {},
        "SignInRiskLevels": [],
        "UserRiskLevels": [],
        "ClientApplications": {}
      },
      "GrantControls": {
        "Operator": "OR",
        "BuiltInControls": [
          1
        ],
        "CustomAuthenticationFactors": [],
        "TermsOfUse": []
      },
      "SessionControls": {}
    }
  ]
}
```
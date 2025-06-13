# Conditional Access Policy Exporter and Comparator

A .NET 8 console application that exports Azure Conditional Access policies and provides comparison functionality against reference JSON files.

## Features

### Export Functionality
- Uses Azure client credentials (service principal) authentication
- Exports all Conditional Access policies from an Azure AD tenant
- Outputs data in structured JSON format with timestamps
- Comprehensive error handling and detailed permission guidance

### Baseline Generation Functionality
- Generate reference policy files from your current tenant
- Create individual JSON files for each policy (compatible with comparison mode)
- Filter policies by enabled state or specific policy names
- Anonymize tenant-specific data for sharing or version control
- Customizable output directory for organized policy storage

### Comparison Functionality
- Compare live Entra ID policies against static reference JSON files
- Multiple matching strategies (by name, ID, or custom mapping)
- Flexible comparison options (case-sensitive/insensitive matching)
- Multiple report formats (console output, JSON, HTML, CSV)
- Detailed diff analysis highlighting specific configuration changes
- Support for both live data and previously exported JSON files

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

## 📚 Documentation

For comprehensive documentation, please refer to the main project guides:

- **[Developer Guide](../CONTRIBUTING.md)**: Complete onboarding guide for contributors
- **[Configuration Reference](../CONFIGURATION.md)**: All configuration options and environment variables
- **[Examples & Use Cases](../EXAMPLES.md)**: Practical scenarios and real-world examples
- **[Troubleshooting Guide](../TROUBLESHOOTING.md)**: Common issues and solutions
- **[Advanced Features](../ADVANCED_FEATURES.md)**: In-depth technical documentation

## Usage

### Building the Application

```bash
dotnet build
```

### Export Mode (Default)

Export Conditional Access policies from Entra ID to a JSON file:

```bash
# Default export (backward compatible)
dotnet run export

# Export with custom output file
dotnet run export --output my-policies.json

# Explicit export command
dotnet run export
```

### Baseline Generation Mode

Generate baseline reference policies from your current tenant:

```bash
# Generate baseline reference files in default directory
dotnet run baseline

# Generate baseline with custom output directory
dotnet run baseline --output-dir ./my-references

# Generate only enabled policies
dotnet run baseline --filter-enabled-only

# Generate anonymized baseline (removes tenant-specific data)
dotnet run baseline --anonymize

# Generate specific policies by name
dotnet run baseline --policy-names "MFA Policy" "Block Legacy Auth"

# Combine multiple options
dotnet run baseline --output-dir ./references --anonymize --filter-enabled-only
```

### Comparison Mode

Compare Entra ID policies against reference JSON files:

```bash
# Basic comparison with live data
dotnet run compare --reference-dir ./reference-policies

# Compare using a previously exported file
dotnet run compare --reference-dir ./reference-policies --entra-file exported-policies.json

# Custom output directory and formats
dotnet run compare --reference-dir ./reference-policies --output-dir ./reports --formats console json html csv

# Different matching strategies
dotnet run compare --reference-dir ./reference-policies --matching ByName --case-sensitive true
dotnet run compare --reference-dir ./reference-policies --matching ById
```

### Command Line Options

#### Export Command
- `--output`: Output file path (default: timestamped filename)

#### Baseline Command
- `--output-dir`: Directory to save baseline reference files (default: "reference-policies")
- `--anonymize`: Remove tenant-specific identifiers (IDs, timestamps, tenant references) (default: false)
- `--filter-enabled-only`: Export only enabled policies (default: false)
- `--policy-names`: Export specific policies by name (space or comma-separated)

#### Compare Command
- `--reference-dir`: Directory containing reference JSON files (required)
- `--entra-file`: Path to exported Entra policies JSON file (optional, fetches live data if not provided)
- `--output-dir`: Output directory for comparison reports (default: "comparison-reports")
- `--formats`: Report formats to generate (default: console, json, html)
  - Available formats: `console`, `json`, `html`, `csv`
- `--matching`: Strategy for matching policies (default: ByName)
  - Available strategies: `ByName`, `ById`, `CustomMapping`
- `--case-sensitive`: Case sensitive policy name matching (default: false)

### Example Output

#### Export Mode
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

#### Comparison Mode
```
Conditional Access Policy Comparison
===================================
Starting comparison with reference directory: ./reference-policies
Found 3 policies in Entra export
Found 2 reference policy files

================================================================================
CONDITIONAL ACCESS POLICY COMPARISON REPORT
================================================================================
Compared At: 2025-05-30 16:45:23 UTC
Tenant ID: 12345678-1234-1234-1234-123456789012
Reference Directory: ./reference-policies

SUMMARY:
----------------------------------------
Total Entra Policies: 3
Total Reference Policies: 2
Policies only in Entra: 1
Policies only in Reference: 0
Matching Policies: 1
Policies with Differences: 1

POLICIES ONLY IN ENTRA:
----------------------------------------
  • Require compliant device for admins (ID: 99999999-9999-9999-9999-999999999999)

POLICIES WITH DIFFERENCES:
----------------------------------------
  • Require MFA for all users
    Reference File: require-mfa-for-all-users.json
    Policy ID: 12345678-1234-1234-1234-123456789012

IDENTICAL POLICIES:
----------------------------------------
  ✓ Block legacy authentication

JSON report generated: comparison-reports/CA_Comparison_20250530_164523.json
HTML report generated: comparison-reports/CA_Comparison_20250530_164523.html
CSV report generated: comparison-reports/CA_Comparison_20250530_164523.csv
Comparison completed successfully!
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

## Policy Field Value Mappings

### Understanding Numeric Codes in Exports

When exporting policies directly from Entra ID, certain fields contain numeric codes instead of human-readable strings. This is how the Microsoft Graph API returns the data. Here's what these codes mean:

#### BuiltInControls Field

```json
"GrantControls": {
  "BuiltInControls": [1]  // This means "mfa" (Multi-factor Authentication)
}
```

**Complete mapping:**
- `1` → `mfa` (Multi-factor Authentication Required)
- `2` → `compliantDevice` (Compliant Device Required)
- `3` → `domainJoinedDevice` (Hybrid Azure AD Joined Device Required)
- `4` → `approvedApplication` (Approved Application Required)
- `5` → `compliantApplication` (Compliant Application Required)
- `6` → `passwordChange` (Password Change Required)
- `7` → `block` (Block Access)

#### ClientAppTypes Field

```json
"Conditions": {
  "ClientAppTypes": [0]  // This means "browser"
}
```

**Complete mapping:**
- `0` → `browser` (Web browsers)
- `1` → `mobileAppsAndDesktopClients` (Mobile apps and desktop clients)
- `2` → `exchangeActiveSync` (Exchange ActiveSync clients)
- `3` → `other` (Other clients, typically legacy authentication)

### Comparison Behavior

The comparison engine automatically handles these differences:

- **Entra Export**: `"ClientAppTypes": [0, 1]`
- **Terraform Config**: `"ClientAppTypes": ["browser", "mobileAppsAndDesktopClients"]`
- **Result**: ✅ These are considered **identical** during comparison

### Report Formats

Different report formats handle these values differently:

#### JSON Reports
- Preserve original numeric codes from Entra exports
- Include comments explaining common numeric values
- Show both source and reference values for easy comparison

#### HTML Reports
- Display a legend explaining numeric codes
- Use tooltips to show human-readable descriptions
- Highlight differences with clear explanations

#### Console Output
- Shows numeric codes as-is for technical accuracy
- Provides context in comparison summaries
- Future enhancement: `--explain` flag to decode values inline

### Common Scenarios

#### Scenario 1: MFA Requirement
**Entra Export:**
```json
"GrantControls": {
  "Operator": "OR",
  "BuiltInControls": [1]
}
```

**Terraform Equivalent:**
```hcl
grant_controls {
  operator          = "OR"
  built_in_controls = ["mfa"]
}
```

#### Scenario 2: Block Legacy Authentication
**Entra Export:**
```json
"Conditions": {
  "ClientAppTypes": [2, 3]
},
"GrantControls": {
  "BuiltInControls": [7]
}
```

**Meaning:** Block access (`7`) for Exchange ActiveSync (`2`) and other legacy clients (`3`)

## Dependencies

- Microsoft.Graph (5.79.0)
- Azure.Identity (1.14.0)
- Newtonsoft.Json (13.0.3)
# CA_Scanner - Azure Conditional Access Policy Management Tool

A comprehensive .NET 8 solution for managing, analyzing, and monitoring Azure Conditional Access policies using Microsoft Graph API with enterprise-grade features.

<!-- Updated to trigger CI run - Test fixes applied -->

## 🚀 Quick Start

```bash
# Build the solution
dotnet build

# Run the application
cd ConditionalAccessExporter
dotnet run export
```

## 📚 Documentation

- **[Getting Started](ConditionalAccessExporter/README.md)**: Basic usage and command-line options
- **[Policy Validation Engine](POLICY_VALIDATION_ENGINE.md)**: Comprehensive policy validation, security assessment, and compliance reporting ⭐ NEW
- **[Developer Guide](CONTRIBUTING.md)**: Complete onboarding guide for contributors
- **[Configuration Reference](CONFIGURATION.md)**: All configuration options and environment variables
- **[Examples & Use Cases](EXAMPLES.md)**: Practical scenarios and real-world examples
- **[Troubleshooting Guide](TROUBLESHOOTING.md)**: Common issues and solutions
- **[Advanced Features](ADVANCED_FEATURES.md)**: In-depth technical documentation
- **[CI/CD Integration](CICD.md)**: Pipeline setup and automation

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

## ✅ Features Overview

### Core Capabilities
- ✅ **Policy Export** - Retrieve all Conditional Access policies from Azure AD via Microsoft Graph API
- ✅ **Baseline Generation** - Create reference policy files from current tenant configurations
- ✅ **Policy Comparison** - Compare live policies against reference JSON files with flexible matching
- ✅ **Terraform Integration** - Bidirectional conversion between JSON and Terraform formats
- ✅ **Multi-Format Reports** - Generate detailed reports in console, JSON, HTML, and CSV formats
- ⭐ **Policy Validation Engine** - Comprehensive validation framework with security assessment and compliance reporting

### 🔒 Policy Validation & Security Assessment
- ✅ **Extensible Rule Framework** - Built-in security, governance, and compliance rules
- ✅ **Security Posture Scoring** - Automated security assessment with 0-100 scoring
- ✅ **Compliance Reporting** - NIST, ISO27001, and SOC2 compliance validation
- ✅ **Risk Assessment** - Identifies security vulnerabilities and policy gaps
- ✅ **Actionable Recommendations** - Detailed suggestions for policy improvements
- ✅ **Custom Rule Configuration** - Organization-specific validation rules and thresholds
- ✅ **Cross-Format Analysis** - Compare JSON policies against Terraform configurations

### Enterprise Features
- ✅ **Multi-Tenant Support** - Manage policies across multiple Azure tenants
- ✅ **CI/CD Integration** - GitHub Actions and Azure DevOps pipeline templates
- ✅ **Performance Optimization** - Parallel processing and memory management for large tenants
- ✅ **Security & Compliance** - SOC 2, NIST, and ISO27001 compliance reporting
- ✅ **Automation Ready** - Comprehensive scripting and batch operation capabilities
- ✅ **Extensible Architecture** - Plugin system for custom integrations

### Technical Excellence
- ✅ **Client Credential Authentication** - Secure Azure service principal authentication
- ✅ **Performance Benchmarking** - Built-in performance monitoring and regression testing
- ✅ **Comprehensive Error Handling** - Detailed error messages and troubleshooting guidance
- ✅ **Memory Optimization** - Advanced memory management for large datasets
- ✅ **Async Operations** - Full async/await implementation for optimal performance

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

### Basic Usage

```bash
# Build and run basic export
dotnet build
cd ConditionalAccessExporter
dotnet run export
```

### Advanced Usage Examples

```bash
# Generate baseline reference policies
dotnet run baseline --output-dir ./policy-baselines --anonymize

# Compare live policies against baseline
dotnet run compare --reference-dir ./policy-baselines --formats html json csv

# Convert policies to Terraform format
dotnet run terraform --input exported-policies.json --output terraform/ --extract-variables

# Multi-format comparison (JSON vs Terraform)
dotnet run cross-format-compare --json-source current.json --terraform-source terraform/
```

### Enterprise Scenarios

```bash
# Multi-tenant export with compliance reporting
dotnet run export --output tenant-a.json
dotnet run compare --reference-dir ./compliance-standards --formats html csv

# CI/CD integration with drift detection
dotnet run compare --reference-dir ./approved-baselines --fail-on-drift

# Performance benchmarking for large tenants
dotnet run benchmark --operation export --iterations 5
```

**For complete usage examples, see [EXAMPLES.md](EXAMPLES.md)**

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

## 📖 Policy Field Value Mappings Reference

When comparing Conditional Access policies, the system may display numeric codes instead of human-readable values. This reference explains what these numbers represent:

### BuiltInControls Mappings

The `BuiltInControls` field uses numeric codes to represent different access control requirements:

| Numeric Code | String Value | Description |
|--------------|--------------|-------------|
| `1` | `mfa` | Multi-factor Authentication Required |
| `2` | `compliantDevice` | Compliant Device Required |
| `3` | `domainJoinedDevice` | Hybrid Azure AD Joined Device Required |
| `4` | `approvedApplication` | Approved Application Required |
| `5` | `compliantApplication` | Compliant Application Required |
| `6` | `passwordChange` | Password Change Required |
| `7` | `block` | Block Access |

**Example:** `"BuiltInControls": [1]` means "Require MFA"

### ClientAppTypes Mappings

The `ClientAppTypes` field uses numeric codes to specify which client applications the policy applies to:

| Numeric Code | String Value | Description |
|--------------|--------------|-------------|
| `0` | `browser` | Web browsers |
| `1` | `mobileAppsAndDesktopClients` | Mobile apps and desktop clients |
| `2` | `exchangeActiveSync` | Exchange ActiveSync clients |
| `3` | `other` | Other clients (legacy authentication) |

**Example:** `"ClientAppTypes": [0, 1]` means "Apply to browsers and mobile/desktop clients"

### SignInRiskLevels and UserRiskLevels Mappings

Risk level fields use string values that correspond to Azure Identity Protection risk levels:

| String Value | Description |
|--------------|-------------|
| `low` | Low risk level |
| `medium` | Medium risk level |
| `high` | High risk level |
| `hidden` | Hidden risk level |
| `none` | No risk |
| `unknownFutureValue` | Unknown or future value |

### State Values

Policy state is represented by string values:

| String Value | Description |
|--------------|-------------|
| `enabled` | Policy is active and enforced |
| `disabled` | Policy is inactive |
| `enabledForReportingButNotEnforced` | Report-only mode (logs but doesn't enforce) |

### Platform Mappings

The `Platforms` field uses string values to specify device platforms:

| String Value | Description |
|--------------|-------------|
| `all` | All platforms |
| `android` | Android devices |
| `iOS` | iOS devices |
| `windows` | Windows devices |
| `windowsPhone` | Windows Phone devices |
| `macOS` | macOS devices |
| `linux` | Linux devices |

### Understanding Comparison Reports

When reviewing comparison reports:

- **Numeric values** typically appear in direct Azure exports
- **String values** typically appear in Terraform configurations
- Both represent the same underlying policy settings
- The comparison engine normalizes these values for accurate matching

### Tips for Interpreting Results

1. **Focus on the meaning**: A policy with `BuiltInControls: [1]` and one with `BuiltInControls: ["mfa"]` are functionally identical
2. **Check multiple fields**: Policies are compared across all settings, not just individual fields
3. **Review context**: The policy name and conditions provide important context for understanding the intent

## 🔗 For More Information

See the [project-specific README](ConditionalAccessExporter/README.md) for detailed usage instructions and troubleshooting.
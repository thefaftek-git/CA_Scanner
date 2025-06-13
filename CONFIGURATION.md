
# Configuration Guide

This document provides comprehensive configuration options for CA_Scanner, including environment variables, command-line options, and advanced settings.

## 🔧 Environment Variables

### Required Azure Authentication

These environment variables are **required** for Azure authentication:

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `AZURE_TENANT_ID` | Azure AD tenant identifier | `12345678-1234-1234-1234-123456789012` | ✅ |
| `AZURE_CLIENT_ID` | Azure app registration client ID | `87654321-4321-4321-4321-210987654321` | ✅ |
| `AZURE_CLIENT_SECRET` | Azure app registration client secret | `your-secret-here` | ✅ |

### Optional Environment Variables

| Variable | Description | Default | Example |
|----------|-------------|---------|---------|
| `CA_SCANNER_LOG_LEVEL` | Logging verbosity level | `Information` | `Debug`, `Information`, `Warning`, `Error` |
| `CA_SCANNER_OUTPUT_DIR` | Default output directory | `./output` | `/data/exports` |
| `CA_SCANNER_TIMEOUT` | HTTP request timeout (seconds) | `30` | `60` |
| `CA_SCANNER_RETRY_COUNT` | HTTP retry attempts | `3` | `5` |
| `CA_SCANNER_PARALLEL_LIMIT` | Max parallel operations | `10` | `20` |

### Setting Environment Variables

#### Windows (PowerShell)
```powershell
$env:AZURE_TENANT_ID = "your-tenant-id"
$env:AZURE_CLIENT_ID = "your-client-id"
$env:AZURE_CLIENT_SECRET = "your-client-secret"
```

#### Windows (Command Prompt)
```cmd
set AZURE_TENANT_ID=your-tenant-id
set AZURE_CLIENT_ID=your-client-id
set AZURE_CLIENT_SECRET=your-client-secret
```

#### Linux/macOS (Bash)
```bash
export AZURE_TENANT_ID="your-tenant-id"
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"
```

#### Docker Environment
```bash
docker run -e AZURE_TENANT_ID="your-tenant-id" \
           -e AZURE_CLIENT_ID="your-client-id" \
           -e AZURE_CLIENT_SECRET="your-client-secret" \
           ca-scanner
```

#### .env File (for development)
```bash
# Create .env file (DO NOT commit to version control)
cat > .env << EOF
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
CA_SCANNER_LOG_LEVEL=Debug
EOF
```

## 🚀 Command-Line Configuration

### Global Options

Available for all commands:

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--help` | `-h` | Show help information | - |
| `--version` | `-v` | Show version information | - |
| `--verbose` | | Enable verbose output | `false` |
| `--quiet` | `-q` | Suppress non-essential output | `false` |
| `--log-level` | | Override log level | `Information` |

### Export Command Options

Export Conditional Access policies from Azure AD:

```bash
dotnet run export [options]
```

| Option | Type | Description | Default | Example |
|--------|------|-------------|---------|---------|
| `--output` | `string` | Output file path | Auto-generated | `policies.json` |
| `--format` | `string` | Output format | `json` | `json`, `yaml` |
| `--include-disabled` | `bool` | Include disabled policies | `true` | `false` |
| `--filter` | `string` | Filter policies by name pattern | - | `"*MFA*"` |
| `--tenant-id` | `string` | Override tenant ID | From env | `tenant-id` |

**Examples:**
```bash
# Basic export
dotnet run export

# Custom output file
dotnet run export --output my-policies.json

# Export only enabled policies
dotnet run export --include-disabled false

# Filter by policy name
dotnet run export --filter "*MFA*"
```

### Baseline Command Options

Generate reference policy files:

```bash
dotnet run baseline [options]
```

| Option | Type | Description | Default | Example |
|--------|------|-------------|---------|---------|
| `--output-dir` | `string` | Output directory | `reference-policies` | `./baselines` |
| `--anonymize` | `bool` | Remove tenant-specific data | `false` | `true` |
| `--filter-enabled-only` | `bool` | Export only enabled policies | `false` | `true` |
| `--policy-names` | `string[]` | Specific policy names | All policies | `"MFA Policy"` |
| `--template-format` | `string` | Template format | `json` | `json`, `terraform` |
| `--include-metadata` | `bool` | Include export metadata | `true` | `false` |

**Examples:**
```bash
# Generate baseline with all policies
dotnet run baseline

# Anonymized baseline for sharing
dotnet run baseline --anonymize --output-dir ./shared-baselines

# Only enabled policies
dotnet run baseline --filter-enabled-only

# Specific policies only
dotnet run baseline --policy-names "MFA Policy" "Block Legacy Auth"

# Terraform format
dotnet run baseline --template-format terraform
```

### Compare Command Options

Compare policies against reference files:

```bash
dotnet run compare [options]
```

| Option | Type | Description | Default | Example |
|--------|------|-------------|---------|---------|
| `--reference-dir` | `string` | Reference policies directory | **Required** | `./references` |
| `--entra-file` | `string` | Exported policies file | Live data | `exported.json` |
| `--output-dir` | `string` | Reports output directory | `comparison-reports` | `./reports` |
| `--formats` | `string[]` | Report formats | `console,json,html` | `json,csv` |
| `--matching` | `string` | Matching strategy | `ByName` | `ById`, `CustomMapping` |
| `--case-sensitive` | `bool` | Case-sensitive matching | `false` | `true` |
| `--ignore-fields` | `string[]` | Fields to ignore in comparison | - | `ModifiedDateTime` |
| `--show-identical` | `bool` | Show identical policies in report | `true` | `false` |

**Matching Strategies:**
- `ByName`: Match policies by display name
- `ById`: Match policies by ID
- `CustomMapping`: Use custom mapping file

**Report Formats:**
- `console`: Console output
- `json`: JSON report file
- `html`: Interactive HTML report
- `csv`: CSV export for spreadsheets

**Examples:**
```bash
# Basic comparison
dotnet run compare --reference-dir ./references

# Compare with exported file
dotnet run compare --reference-dir ./references --entra-file backup.json

# Generate only JSON and CSV reports
dotnet run compare --reference-dir ./references --formats json csv

# Case-sensitive matching by ID
dotnet run compare --reference-dir ./references --matching ById --case-sensitive

# Ignore timestamps in comparison
dotnet run compare --reference-dir ./references --ignore-fields ModifiedDateTime CreatedDateTime
```

### Terraform Command Options

Convert between JSON and Terraform formats:

```bash
dotnet run terraform [options]
```

| Option | Type | Description | Default | Example |
|--------|------|-------------|---------|---------|
| `--input` | `string` | Input file or directory | **Required** | `policies.json` |
| `--output` | `string` | Output file or directory | Auto-generated | `terraform/` |
| `--direction` | `string` | Conversion direction | `json-to-terraform` | `terraform-to-json` |
| `--resource-prefix` | `string` | Terraform resource prefix | `azuread_conditional_access_policy` | `ca_policy` |
| `--validate` | `bool` | Validate output | `true` | `false` |
| `--format-output` | `bool` | Format Terraform output | `true` | `false` |

**Examples:**
```bash
# Convert JSON to Terraform
dotnet run terraform --input policies.json --output terraform/

# Convert Terraform to JSON
dotnet run terraform --input terraform/ --direction terraform-to-json

# Custom resource prefix
dotnet run terraform --input policies.json --resource-prefix my_ca_policy
```

## 📁 Configuration Files

### appsettings.json

Advanced configuration via `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ConditionalAccessExporter": "Debug",
      "Microsoft.Graph": "Warning"
    }
  },
  "Azure": {
    "TenantId": "",
    "ClientId": "",
    "ClientSecret": "",
    "Authority": "https://login.microsoftonline.com/",
    "Scopes": ["https://graph.microsoft.com/.default"]
  },
  "Performance": {
    "MaxParallelOperations": 10,
    "HttpTimeoutSeconds": 30,
    "RetryAttempts": 3,
    "RetryDelaySeconds": 2
  },
  "Output": {
    "DefaultDirectory": "./output",
    "DefaultFormat": "json",
    "IncludeMetadata": true,
    "PrettyPrint": true
  },
  "Comparison": {
    "DefaultMatchingStrategy": "ByName",
    "CaseSensitive": false,
    "IgnoreFields": ["ModifiedDateTime"],
    "ShowIdentical": true
  }
}
```

### Custom Mapping Files

For `CustomMapping` strategy, create a mapping file:

```json
{
  "mappings": [
    {
      "referenceFile": "mfa-policy.json",
      "entraId": "12345678-1234-1234-1234-123456789012",
      "entraName": "Require MFA for all users"
    },
    {
      "referenceFile": "block-legacy.json",
      "entraId": "87654321-4321-4321-4321-210987654321",
      "entraName": "Block legacy authentication"
    }
  ]
}
```

## 🔐 Azure Configuration

### App Registration Setup

1. **Create App Registration**
   - Navigate to Azure Portal → App Registrations
   - Click "New registration"
   - Name: "CA_Scanner"
   - Supported account types: "Single tenant"

2. **Configure Authentication**
   - No redirect URIs needed for client credentials flow
   - Client secrets: Generate new secret
   - Note the expiration date

3. **Set API Permissions**
   - Add permission → Microsoft Graph → Application permissions
   - Add `Policy.Read.All` (recommended)
   - Grant admin consent

4. **Note Configuration Values**
   - Tenant ID: Directory (tenant) ID
   - Client ID: Application (client) ID
   - Client Secret: Generated secret value

### Required Permissions

| Permission | Type | Description | Admin Consent |
|------------|------|-------------|---------------|
| `Policy.Read.All` | Application | Read all Conditional Access policies | Required |
| `Policy.ReadWrite.ConditionalAccess` | Application | Read and write policies (for future features) | Required |

### Security Best Practices

1. **Principle of Least Privilege**
   - Use `Policy.Read.All` unless write access needed
   - Create dedicated app registration for CA_Scanner

2. **Secret Management**
   - Rotate client secrets regularly (recommend 6-12 months)
   - Use Azure Key Vault for production deployments
   - Never commit secrets to version control

3. **Monitoring**
   - Enable audit logging for app registration
   - Monitor Graph API usage
   - Set up alerts for unusual activity

## 🐳 Docker Configuration

### Environment Variables
```dockerfile
ENV AZURE_TENANT_ID=""
ENV AZURE_CLIENT_ID=""
ENV AZURE_CLIENT_SECRET=""
ENV CA_SCANNER_LOG_LEVEL="Information"
ENV CA_SCANNER_OUTPUT_DIR="/app/output"
```

### Docker Compose
```yaml
version: '3.8'
services:
  ca-scanner:
    image: ca-scanner:latest
    environment:
      - AZURE_TENANT_ID=${AZURE_TENANT_ID}
      - AZURE_CLIENT_ID=${AZURE_CLIENT_ID}
      - AZURE_CLIENT_SECRET=${AZURE_CLIENT_SECRET}
      - CA_SCANNER_LOG_LEVEL=Information
    volumes:
      - ./output:/app/output
      - ./references:/app/references
    command: ["compare", "--reference-dir", "/app/references"]
```

## ⚡ Performance Tuning

### Memory Configuration

```json
{
  "Performance": {
    "MaxMemoryMB": 1024,
    "EnableGCOptimization": true,
    "LargeObjectHeapCompactionMode": "CompactOnce"
  }
}
```

### HTTP Configuration

```json
{
  "Http": {
    "MaxConcurrentRequests": 5,
    "TimeoutSeconds": 60,
    "RetryPolicy": {
      "MaxAttempts": 3,
      "BackoffSeconds": [2, 4, 8]
    }
  }
}
```

### Caching Configuration

```json
{
  "Caching": {
    "EnablePolicyCache": true,
    "CacheDurationMinutes": 30,
    "MaxCacheEntries": 1000
  }
}
```

## 🧪 Testing Configuration

### Test Environment Variables

```bash
# Separate test tenant (recommended)
export AZURE_TEST_TENANT_ID="test-tenant-id"
export AZURE_TEST_CLIENT_ID="test-client-id"
export AZURE_TEST_CLIENT_SECRET="test-client-secret"

# Test configuration
export CA_SCANNER_TEST_MODE="true"
export CA_SCANNER_LOG_LEVEL="Debug"
```

### Test Configuration File

```json
{
  "Testing": {
    "UseMockData": false,
    "TestTenantId": "test-tenant-id",
    "MockDataDirectory": "./test-data",
    "EnableBenchmarking": true
  }
}
```

## 🔍 Troubleshooting Configuration

### Debug Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "ConditionalAccessExporter.Services": "Trace",
      "Microsoft.Graph": "Information"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "HH:mm:ss.fff"
    }
  }
}
```

### Common Configuration Issues

1. **Authentication Failures**
   ```bash
   # Verify environment variables
   echo $AZURE_TENANT_ID
   echo $AZURE_CLIENT_ID
   # Don't echo client secret for security
   
   # Test with debug logging
   export CA_SCANNER_LOG_LEVEL=Debug
   dotnet run export
   ```

2. **Permission Errors**
   - Verify app registration has correct permissions
   - Ensure admin consent granted
   - Check token claims in debug output

3. **Network Issues**
   ```json
   {
     "Http": {
       "TimeoutSeconds": 120,
       "RetryAttempts": 5,
       "UseProxy": true,
       "ProxyUrl": "http://proxy.company.com:8080"
     }
   }
   ```

## 📋 Configuration Validation

### Validation Commands

```bash
# Validate configuration
dotnet run validate-config

# Test authentication
dotnet run test-auth

# Check permissions
dotnet run check-permissions
```

### Configuration Checklist

- [ ] Environment variables set
- [ ] Azure app registration configured
- [ ] Permissions granted and consented
- [ ] Network connectivity to Microsoft Graph
- [ ] Output directories writable
- [ ] Reference policies accessible

## 🔄 Migration and Upgrades

### Version 1.x to 2.x Configuration Changes

1. **Environment Variables**
   - `CA_OUTPUT_DIR` → `CA_SCANNER_OUTPUT_DIR`
   - `LOG_LEVEL` → `CA_SCANNER_LOG_LEVEL`

2. **Command Line Options**
   - `--output-format` → `--format`
   - `--comparison-mode` → `--matching`

3. **Configuration Files**
   - `config.json` → `appsettings.json`
   - Updated schema with new performance options

### Migration Script

```bash
#!/bin/bash
# migrate-config.sh

# Update environment variables
if [ -n "$CA_OUTPUT_DIR" ]; then
    export CA_SCANNER_OUTPUT_DIR="$CA_OUTPUT_DIR"
    unset CA_OUTPUT_DIR
fi

if [ -n "$LOG_LEVEL" ]; then
    export CA_SCANNER_LOG_LEVEL="$LOG_LEVEL"
    unset LOG_LEVEL
fi

echo "Configuration migration completed"
```

---

## 📚 Additional Resources

- **[Azure App Registration Guide](GITHUB_SECRETS_SETUP.md)**: Detailed Azure setup
- **[Examples](EXAMPLES.md)**: Practical configuration scenarios
- **[Contributing Guide](CONTRIBUTING.md)**: Development configuration
- **[Troubleshooting](#troubleshooting-configuration)**: Common issues and solutions

For questions about configuration, please check existing [GitHub Issues](https://github.com/thefaftek-git/CA_Scanner/issues) or create a new one with the `configuration` label.


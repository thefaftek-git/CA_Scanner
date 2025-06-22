

# Troubleshooting Guide

This comprehensive guide helps you diagnose and resolve common issues with CA_Scanner. Issues are organized by category with step-by-step solutions.

## 🔍 Quick Diagnostic

### First Steps for Any Issue

1. **Check Environment Variables**
   ```bash
   echo "Tenant ID: ${AZURE_TENANT_ID:0:8}..."
   echo "Client ID: ${AZURE_CLIENT_ID:0:8}..."
   echo "Secret Set: $([ -n "$AZURE_CLIENT_SECRET" ] && echo "YES" || echo "NO")"
   ```

2. **Verify .NET Installation**
   ```bash
   dotnet --version
   # Should show 8.0.x or higher
   ```

3. **Test Basic Connectivity**
   ```bash
   curl -s "https://graph.microsoft.com/v1.0/" | jq .
   ```

4. **Enable Debug Logging**
   ```bash
   export CA_SCANNER_LOG_LEVEL=Debug
   dotnet run export --verbose
   ```

## 🔐 Authentication Issues

### Issue: "Authentication failed" or "Unauthorized"

**Symptoms:**
- Error messages about authentication failure
- HTTP 401 Unauthorized responses
- Token acquisition failures

**Root Causes & Solutions:**

#### 1. Missing or Incorrect Environment Variables

**Check:**
```bash
# Verify all required variables are set
env | grep AZURE_
```

**Solution:**
```bash
# Set correct environment variables
export AZURE_TENANT_ID="your-tenant-id"
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"

# On Windows (PowerShell):
$env:AZURE_TENANT_ID = "your-tenant-id"
$env:AZURE_CLIENT_ID = "your-client-id"
$env:AZURE_CLIENT_SECRET = "your-client-secret"
```

#### 2. App Registration Issues

**Check App Registration:**
1. Navigate to Azure Portal → App Registrations
2. Find your app registration
3. Verify it's enabled and not expired

**Common Issues:**
- **Client secret expired**: Generate new secret
- **Wrong tenant**: Verify tenant ID matches your target tenant
- **App disabled**: Re-enable the application

**Solution:**
```bash
# Test with Azure CLI to verify credentials
az login --service-principal \
  --username $AZURE_CLIENT_ID \
  --password $AZURE_CLIENT_SECRET \
  --tenant $AZURE_TENANT_ID
```

#### 3. Missing API Permissions

**Check Permissions:**
1. Azure Portal → App Registrations → Your App → API permissions
2. Verify `Policy.Read.All` is present
3. Check that admin consent is granted (green checkmarks)

**Solution:**
1. Add missing permissions:
   - Microsoft Graph → Application permissions → `Policy.Read.All`
2. Click "Grant admin consent for [tenant name]"
3. Wait 5-10 minutes for permissions to propagate

**Verification:**
```bash
# Test permissions with Graph Explorer
# https://developer.microsoft.com/en-us/graph/graph-explorer
# Query: GET https://graph.microsoft.com/v1.0/identity/conditionalAccess/policies
```

### Issue: "Required scopes are missing in the token"

**Symptoms:**
- Application authenticates but cannot access policies
- Error mentions missing scopes or insufficient permissions

**Solution:**
1. **Verify Permission Type**: Must be **Application** permissions, not Delegated
2. **Check Scope**: Ensure you're requesting the correct scope:
   ```csharp
   // In code, scope should be:
   "https://graph.microsoft.com/.default"
   ```
3. **Re-grant Consent**: Remove and re-add permissions, then grant admin consent
4. **Wait for Propagation**: Can take up to 10 minutes

### Issue: "AADSTS700016: Application not found in directory"

**Symptoms:**
- Error during authentication
- Application ID not recognized

**Solutions:**
1. **Verify Tenant ID**: Ensure you're using the correct tenant ID
2. **Check Application ID**: Verify the client ID is correct
3. **Multi-tenant Apps**: If using multi-tenant app, ensure it's been consented in target tenant

```bash
# Verify tenant information
curl "https://login.microsoftonline.com/$AZURE_TENANT_ID/v2.0/.well-known/openid_configuration" | jq .issuer
```

## 📊 Export Issues

### Issue: "No policies found" when policies exist

**Symptoms:**
- Export completes successfully but shows 0 policies
- You know policies exist in the tenant

**Diagnostic Commands:**
```bash
# Enable detailed logging
export CA_SCANNER_LOG_LEVEL=Debug
dotnet run export --verbose 2>&1 | grep -E "(Policy|Found|Count)"

# Test with Graph Explorer
# Query: GET https://graph.microsoft.com/v1.0/identity/conditionalAccess/policies
```

**Solutions:**

#### 1. Insufficient Permissions
- Verify `Policy.Read.All` permission is granted
- Check admin consent status
- Test with higher privileges (Global Admin account)

#### 2. Filtering Issues
```bash
# Try without any filters
dotnet run export --include-disabled true

# Check for specific policy names
dotnet run export --filter "*" --verbose
```

#### 3. API Throttling
```bash
# Reduce parallel operations
export CA_SCANNER_PARALLEL_LIMIT=1
export CA_SCANNER_TIMEOUT=60
dotnet run export
```

### Issue: Export timeouts with large tenants

**Symptoms:**
- Export starts but times out
- Large number of policies (100+)
- Memory usage increases dramatically

**Solutions:**

#### 1. Optimize Performance Settings
```bash
# Increase timeout and reduce parallelism
export CA_SCANNER_TIMEOUT=300
export CA_SCANNER_PARALLEL_LIMIT=3
export CA_SCANNER_RETRY_COUNT=5

# Run export
dotnet run export --output large-tenant-export.json
```

#### 2. Use Chunked Export (Future Feature)
```bash
# Export in smaller batches
dotnet run export --batch-size 50 --output policies-batch1.json
```

#### 3. Monitor System Resources
```bash
# Monitor during export
top -p $(pgrep -f ConditionalAccessExporter)

# Check memory usage
dotnet run export --verbose 2>&1 | grep -i memory
```

### Issue: Export fails with HTTP errors

**Common HTTP Error Codes:**

#### HTTP 429 - Too Many Requests
**Solution:**
```bash
# Reduce request rate
export CA_SCANNER_PARALLEL_LIMIT=1
export CA_SCANNER_RETRY_COUNT=10
dotnet run export
```

#### HTTP 403 - Forbidden
**Solution:**
- Check API permissions are granted
- Verify admin consent
- Ensure app registration has correct permissions

#### HTTP 500 - Internal Server Error
**Solution:**
```bash
# Retry with exponential backoff
export CA_SCANNER_RETRY_COUNT=5
dotnet run export

# If persistent, check Microsoft 365 Service Health
```

## 🔄 Comparison Issues

### Issue: "No matching policies found" in comparison

**Symptoms:**
- Comparison runs but shows no matches
- Policies exist in both reference and live data

**Diagnostic Commands:**
```bash
# Check policy names in both sources
jq -r '.Policies[].DisplayName' current-export.json | sort
ls reference-policies/ | sed 's/.json$//' | sort

# Test different matching strategies
dotnet run compare --reference-dir ./ref --matching ByName --case-sensitive false
dotnet run compare --reference-dir ./ref --matching ById
```

**Solutions:**

#### 1. Naming Mismatch
```bash
# Check for naming differences
dotnet run compare --reference-dir ./ref --matching ByName --case-sensitive false --verbose

# Use custom mapping for complex cases
cat > mapping.json << EOF
{
  "mappings": [
    {
      "referenceFile": "mfa-policy.json",
      "entraName": "Require MFA for all users"
    }
  ]
}
EOF

dotnet run compare --reference-dir ./ref --matching CustomMapping --mapping-file mapping.json
```

#### 2. ID vs Name Matching
```bash
# If using ID matching, ensure IDs are preserved
dotnet run compare --reference-dir ./ref --matching ById --verbose

# For cross-tenant comparison, use name matching
dotnet run compare --reference-dir ./ref --matching ByName
```

### Issue: False differences in comparison reports

**Symptoms:**
- Policies appear different but are functionally identical
- Differences in timestamps, IDs, or formatting

**Solutions:**

#### 1. Ignore Non-functional Fields
```bash
# Ignore timestamp fields
dotnet run compare \
  --reference-dir ./ref \
  --ignore-fields ModifiedDateTime,CreatedDateTime,Id \
  --output-dir ./clean-comparison
```

#### 2. Normalize Data Before Comparison
```bash
# Generate baseline without metadata
dotnet run baseline --anonymize --output-dir ./normalized-ref

# Compare against normalized baseline
dotnet run compare --reference-dir ./normalized-ref
```

### Issue: Slow comparison performance

**Symptoms:**
- Comparison takes very long time
- High CPU or memory usage during comparison

**Solutions:**

#### 1. Optimize Comparison Settings
```bash
# Reduce parallel processing
export CA_SCANNER_PARALLEL_LIMIT=2

# Use faster matching strategy
dotnet run compare --reference-dir ./ref --matching ById
```

#### 2. Pre-filter Policies
```bash
# Compare only enabled policies
dotnet run export --include-disabled false --output enabled-only.json
dotnet run compare --reference-dir ./ref --entra-file enabled-only.json
```

## 🔧 Build and Runtime Issues

### Issue: ".NET 8 SDK not found"

**Symptoms:**
- `dotnet` command not found
- Wrong .NET version installed

**Solutions:**

#### Linux/Ubuntu:
```bash
# Use the provided installation script
./dotnet-install.sh

# Or install manually
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y apt-transport-https
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

#### Windows:
```powershell
# Download and install from Microsoft
# https://dotnet.microsoft.com/download/dotnet/8.0

# Or use Chocolatey
choco install dotnet-8.0-sdk
```

#### macOS:
```bash
# Use Homebrew
brew install --cask dotnet

# Or download from Microsoft
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### Issue: Build failures

**Common Build Errors:**

#### 1. Package Restore Issues
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean && dotnet build
```

#### 2. Target Framework Issues
```bash
# Verify project targets .NET 8
grep -r "net8.0" *.csproj

# Update if necessary (in .csproj file)
<TargetFramework>net8.0</TargetFramework>
```

### Issue: Runtime exceptions

**Common Runtime Errors:**

#### 1. FileNotFoundException
```bash
# Check working directory
pwd
ls -la

# Ensure you're in the correct directory
cd ConditionalAccessExporter
dotnet run
```

#### 2. Memory Issues
```bash
# Monitor memory usage
dotnet run export --verbose 2>&1 | grep -i memory

# Increase memory limits if needed
export DOTNET_GCHeapCount=4
export DOTNET_GCConserveMemory=9
```

## 🌐 Network and Connectivity Issues

### Issue: "Unable to connect to Graph API"

**Symptoms:**
- Network timeouts
- Connection refused errors
- DNS resolution failures

**Diagnostic Commands:**
```bash
# Test basic connectivity
ping graph.microsoft.com
nslookup graph.microsoft.com

# Test HTTPS connectivity
curl -I https://graph.microsoft.com/v1.0/

# Check for proxy issues
echo $HTTP_PROXY
echo $HTTPS_PROXY
```

**Solutions:**

#### 1. Proxy Configuration
```bash
# Configure proxy if needed
export HTTP_PROXY=http://proxy.company.com:8080
export HTTPS_PROXY=http://proxy.company.com:8080

# Or in appsettings.json:
{
  "Http": {
    "UseProxy": true,
    "ProxyUrl": "http://proxy.company.com:8080"
  }
}
```

#### 2. Firewall Rules
```bash
# Ensure these URLs are accessible:
# - https://login.microsoftonline.com
# - https://graph.microsoft.com
# - https://graph.microsoft.com/v1.0/

# Test with curl
curl -v https://login.microsoftonline.com/$AZURE_TENANT_ID/v2.0/.well-known/openid_configuration
```

#### 3. Certificate Issues
```bash
# On Linux, update CA certificates
sudo apt-get update && sudo apt-get install ca-certificates

# On CentOS/RHEL
sudo yum update ca-certificates
```

### Issue: SSL/TLS errors

**Symptoms:**
- Certificate validation errors
- SSL handshake failures

**Solutions:**

#### 1. Update System Certificates
```bash
# Ubuntu/Debian
sudo apt-get update && sudo apt-get install ca-certificates

# CentOS/RHEL
sudo yum update ca-certificates

# macOS
brew install ca-certificates
```

#### 2. .NET Certificate Issues
```bash
# Clear .NET certificate cache
dotnet dev-certs https --clean

# Re-trust certificates
dotnet dev-certs https --trust
```

## 📁 File and Directory Issues

### Issue: "Access denied" or permission errors

**Symptoms:**
- Cannot create output files
- Permission denied when writing to directories

**Solutions:**

#### 1. Check Directory Permissions
```bash
# Check current directory permissions
ls -la
pwd

# Create output directory with proper permissions
mkdir -p ./output
chmod 755 ./output
```

#### 2. Use Alternative Output Location
```bash
# Use a writable directory
dotnet run export --output ~/Documents/ca-policies.json

# Or use temporary directory
dotnet run export --output /tmp/ca-policies.json
```

### Issue: "File not found" errors

**Common Causes:**

#### 1. Working Directory Issues
```bash
# Verify you're in the correct directory
pwd
# Should be in CA_Scanner or CA_Scanner/ConditionalAccessExporter

# If in wrong directory:
cd ConditionalAccessExporter
dotnet run
```

#### 2. Missing Reference Files
```bash
# Check reference directory exists
ls -la reference-policies/

# Create if missing
mkdir -p reference-policies

# Generate baseline first
dotnet run baseline --output-dir reference-policies
```

## 🐳 Docker and Container Issues

### Issue: Container authentication failures

**Symptoms:**
- Docker container cannot authenticate
- Environment variables not passed correctly

**Solutions:**

#### 1. Environment Variable Passing
```bash
# Correct way to pass variables
docker run -e AZURE_TENANT_ID="$AZURE_TENANT_ID" \
           -e AZURE_CLIENT_ID="$AZURE_CLIENT_ID" \
           -e AZURE_CLIENT_SECRET="$AZURE_CLIENT_SECRET" \
           ca-scanner

# Using env file
echo "AZURE_TENANT_ID=$AZURE_TENANT_ID" > .env
echo "AZURE_CLIENT_ID=$AZURE_CLIENT_ID" >> .env
echo "AZURE_CLIENT_SECRET=$AZURE_CLIENT_SECRET" >> .env

docker run --env-file .env ca-scanner
```

#### 2. Volume Mounting Issues
```bash
# Mount volumes correctly
docker run -v $(pwd)/output:/app/output \
           -v $(pwd)/reference-policies:/app/reference-policies \
           ca-scanner
```

## 🔧 Configuration Issues

### Issue: Invalid configuration values

**Symptoms:**
- Application starts but behaves unexpectedly
- Configuration warnings in logs

**Diagnostic Commands:**
```bash
# Validate configuration
dotnet run validate-config

# Check configuration values
export CA_SCANNER_LOG_LEVEL=Debug
dotnet run export --verbose 2>&1 | grep -i config
```

**Solutions:**

#### 1. Reset to Defaults
```bash
# Clear custom environment variables
unset CA_SCANNER_LOG_LEVEL
unset CA_SCANNER_OUTPUT_DIR
unset CA_SCANNER_TIMEOUT

# Test with defaults
dotnet run export
```

#### 2. Validate appsettings.json
```bash
# Check JSON syntax
jq . appsettings.json

# Validate schema
# (Custom validation tool would go here)
```

## 📊 Performance Issues

### Issue: Slow performance

**Symptoms:**
- Operations take much longer than expected
- High CPU or memory usage

**Diagnostic Commands:**
```bash
# Monitor resource usage
top -p $(pgrep -f ConditionalAccessExporter)

# Enable performance logging
export CA_SCANNER_LOG_LEVEL=Debug
dotnet run export --verbose 2>&1 | grep -E "(ms|seconds|memory)"
```

**Solutions:**

#### 1. Tune Performance Settings
```bash
# Optimize for large tenants
export CA_SCANNER_PARALLEL_LIMIT=5
export CA_SCANNER_TIMEOUT=120
export CA_SCANNER_RETRY_COUNT=3

# Optimize for fast networks
export CA_SCANNER_PARALLEL_LIMIT=15
export CA_SCANNER_TIMEOUT=30
```

#### 2. System Resource Optimization
```bash
# Increase available memory
export DOTNET_GCHeapCount=4

# Use server GC for large datasets
export DOTNET_gcServer=1
```

## 🆘 Getting Help



## 🌐 Terraform Integration Issues

### Issue: "Terraform conversion fails"

**Symptoms:**
- Conversion from JSON to Terraform fails
- Error messages about invalid JSON or Terraform syntax
- Missing or incorrect Terraform resources

**Diagnostic Commands:**
```bash
# Check JSON file structure
jq . input-policies.json

# Validate Terraform syntax
terraform validate
```

**Solutions:**

#### 1. Invalid JSON Input
```bash
# Fix JSON syntax errors
jq . input-policies.json > fixed-input.json

# Use the fixed JSON file for conversion
dotnet run terraform --input fixed-input.json --output output.tf --direction json-to-terraform
```

#### 2. Missing Terraform Resources
```bash
# Check for missing resources in output
grep -i "resource" output.tf

# Add missing resources manually if needed
```

#### 3. Terraform Version Issues
```bash
# Ensure you're using compatible Terraform version
terraform version

# Update Terraform if necessary
# https://www.terraform.io/downloads.html
```

### Issue: "Terraform apply fails"

**Symptoms:**
- `terraform apply` fails with errors
- Resources not created or modified as expected
- Error messages about API limits or permissions

**Diagnostic Commands:**
```bash
# Check Terraform plan output
terraform plan

# Enable detailed logging
export TF_LOG=DEBUG
terraform apply
```

**Solutions:**

#### 1. API Permission Issues
```bash
# Ensure app registration has necessary permissions
# Azure Portal → App Registrations → Your App → API permissions
# Add: Microsoft Graph → Application permissions → Policy.ReadWrite.All
# Grant admin consent
```

#### 2. Terraform State Issues
```bash
# Check Terraform state file
terraform show

# Fix state file if necessary
terraform state list
terraform state rm <resource>
```

#### 3. API Throttling
```bash
# Reduce parallel operations
export TF_PARALLELISM=1
terraform apply
```

### Issue: "Drift between Terraform and Azure"

**Symptoms:**
- Changes made in Azure not reflected in Terraform state
- Terraform plan shows unexpected changes
- Manual changes in Azure portal

**Diagnostic Commands:**
```bash
# Import current state into Terraform
terraform import <resource> <id>

# Check for drift
terraform plan
```

**Solutions:**

#### 1. Manual Import
```bash
# Import specific resource
terraform import azuread_conditional_access_policy.require_mfa_all_users <policy-id>

# Verify import
terraform state list
```

#### 2. Refresh State
```bash
# Refresh Terraform state
terraform refresh

# Check for drift
terraform plan
```

#### 3. Use Terraform Cloud/Enterprise
```bash
# Enable remote state management
# https://www.terraform.io/cloud
```


### Before Seeking Help

1. **Search Existing Issues**: Check [GitHub Issues](https://github.com/thefaftek-git/CA_Scanner/issues)
2. **Check Documentation**: Review [CONFIGURATION.md](CONFIGURATION.md) and [EXAMPLES.md](EXAMPLES.md)
3. **Try Debug Mode**: Run with `--verbose` and `CA_SCANNER_LOG_LEVEL=Debug`
4. **Collect Diagnostic Information**: Use the templates below

### Issue Report Template

```markdown
**Environment:**
- OS: [Windows 10/Ubuntu 20.04/macOS 12/etc.]
- .NET Version: [output of `dotnet --version`]
- CA_Scanner Version: [git commit or version]

**Azure Configuration:**
- Tenant Type: [Single tenant/Multi-tenant]
- App Registration Permissions: [Policy.Read.All/etc.]
- Approximate Policy Count: [number]

**Command Used:**
```bash
dotnet run [exact command with options]
```

**Error Message:**
```
[paste complete error message and stack trace]
```

**Expected Behavior:**
[describe what you expected to happen]

**Additional Context:**
[any other relevant information]
```

### Debug Information Collection

```bash
#!/bin/bash
# collect-debug-info.sh

echo "CA_Scanner Debug Information Collection"
echo "======================================="

echo "System Information:"
uname -a
echo ""

echo ".NET Information:"
dotnet --info
echo ""

echo "Environment Variables:"
env | grep -E "(AZURE_|CA_SCANNER_)" | sed 's/SECRET=.*/SECRET=[HIDDEN]/'
echo ""

echo "Network Connectivity:"
curl -I https://graph.microsoft.com/v1.0/ 2>&1
echo ""

echo "File Permissions:"
ls -la
echo ""

echo "Recent Logs:"
# Add actual log file path
tail -50 /path/to/ca-scanner.log 2>/dev/null || echo "No log file found"
```

### Escalation Paths

1. **GitHub Issues**: Create detailed issue with template
2. **GitHub Discussions**: For questions and general help
3. **Security Issues**: Use GitHub security advisory for security-related problems
4. **Feature Requests**: Use GitHub issues with enhancement label

### Community Resources

- **Documentation**: [Complete documentation index](README.md)
- **Examples**: [Practical examples and use cases](EXAMPLES.md)
- **Contributing**: [Developer guide](CONTRIBUTING.md)
- **Configuration**: [Comprehensive configuration guide](CONFIGURATION.md)

---

## 🔍 Quick Reference

### Essential Commands

```bash
# Basic diagnostics
dotnet --version
dotnet run export --verbose

# Authentication test
az login --service-principal -u $AZURE_CLIENT_ID -p $AZURE_CLIENT_SECRET --tenant $AZURE_TENANT_ID

# Connectivity test
curl -I https://graph.microsoft.com/v1.0/

# Permission test
# Use Graph Explorer: https://developer.microsoft.com/graph/graph-explorer
```

### Common Environment Variables

```bash
# Required
export AZURE_TENANT_ID="your-tenant-id"
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"

# Optional tuning
export CA_SCANNER_LOG_LEVEL="Debug"
export CA_SCANNER_TIMEOUT="60"
export CA_SCANNER_PARALLEL_LIMIT="5"
```

### Useful One-liners

```bash
# Quick policy count
jq '.PoliciesCount' exported-policies.json

# List policy names
jq -r '.Policies[].DisplayName' exported-policies.json

# Check enabled policies only
jq '[.Policies[] | select(.State == "enabled")] | length' exported-policies.json

# Find policies by name pattern
jq -r '.Policies[] | select(.DisplayName | contains("MFA")) | .DisplayName' exported-policies.json
```

This comprehensive troubleshooting guide should help resolve the vast majority of issues users encounter with CA_Scanner. When problems persist, the diagnostic commands and templates provided will help gather the information needed for effective support.



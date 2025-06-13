
# CA_Scanner Examples and Use Cases

This document provides practical examples and real-world use cases for CA_Scanner, helping you understand how to use the tool effectively in different scenarios.

## 📋 Table of Contents

- [Getting Started Examples](#getting-started-examples)
- [Enterprise Scenarios](#enterprise-scenarios)
- [DevOps and CI/CD Integration](#devops-and-cicd-integration)
- [Security and Compliance](#security-and-compliance)
- [Multi-Tenant Management](#multi-tenant-management)
- [Troubleshooting Examples](#troubleshooting-examples)
- [Advanced Use Cases](#advanced-use-cases)

## 🚀 Getting Started Examples

### Example 1: First Time Export

**Scenario**: You're new to CA_Scanner and want to export your current Conditional Access policies.

```bash
# Set up environment variables
export AZURE_TENANT_ID="12345678-1234-1234-1234-123456789012"
export AZURE_CLIENT_ID="87654321-4321-4321-4321-210987654321"
export AZURE_CLIENT_SECRET="your-secret-here"

# Simple export to see what you have
cd ConditionalAccessExporter
dotnet run export

# Output: ConditionalAccessPolicies_20250613_143052.json
```

**Expected Output:**
```
Conditional Access Policy Exporter
==================================
Tenant ID: 12345678-1234-1234-1234-123456789012
Client ID: 87654321-4321-4321-4321-210987654321
Client Secret: [HIDDEN]

Authenticating to Microsoft Graph...
Fetching Conditional Access Policies...
Found 15 Conditional Access Policies

Policy Summary:
================
- Require MFA for all users (State: Enabled)
- Block legacy authentication (State: Enabled)
- Require compliant device for admins (State: Enabled)
... (12 more policies)

Conditional Access Policies exported successfully to: ConditionalAccessPolicies_20250613_143052.json
File size: 45.67 KB

Export completed successfully!
```

### Example 2: Creating Your First Baseline

**Scenario**: You want to create reference policies for version control and change tracking.

```bash
# Generate baseline from current policies
dotnet run baseline --output-dir ./my-baselines

# Generate anonymized baseline for sharing
dotnet run baseline --anonymize --output-dir ./shared-baselines
```

**Generated Files:**
```
my-baselines/
├── require-mfa-for-all-users.json
├── block-legacy-authentication.json
├── require-compliant-device-for-admins.json
└── ... (other policy files)
```

### Example 3: Basic Policy Comparison

**Scenario**: You want to compare your current policies against the baseline you created.

```bash
# Compare current policies against baseline
dotnet run compare --reference-dir ./my-baselines

# Generate detailed reports in multiple formats
dotnet run compare --reference-dir ./my-baselines --formats console json html csv
```

## 🏢 Enterprise Scenarios

### Scenario 1: Weekly Security Review

**Business Context**: Security team performs weekly reviews of Conditional Access policy changes.

```bash
#!/bin/bash
# weekly-security-review.sh

DATE=$(date +%Y-%m-%d)
EXPORT_DIR="./exports/$DATE"
REPORT_DIR="./reports/$DATE"

# Create directories
mkdir -p "$EXPORT_DIR" "$REPORT_DIR"

# Export current policies
dotnet run export --output "$EXPORT_DIR/current-policies.json"

# Compare against approved baseline
dotnet run compare \
  --reference-dir ./approved-baselines \
  --entra-file "$EXPORT_DIR/current-policies.json" \
  --output-dir "$REPORT_DIR" \
  --formats json html csv

# Generate summary for security team
echo "Weekly Security Review - $DATE" > "$REPORT_DIR/summary.txt"
echo "=================================" >> "$REPORT_DIR/summary.txt"
echo "Export file: $EXPORT_DIR/current-policies.json" >> "$REPORT_DIR/summary.txt"
echo "Reports: $REPORT_DIR/" >> "$REPORT_DIR/summary.txt"
```

**Scheduling with Cron:**
```bash
# Add to crontab (weekly on Mondays at 9 AM)
0 9 * * 1 /path/to/weekly-security-review.sh
```

### Scenario 2: Change Management Workflow

**Business Context**: Organization requires approval for all Conditional Access policy changes.

**Step 1: Capture Pre-Change State**
```bash
# Before making changes
dotnet run baseline --output-dir ./pre-change-$(date +%Y%m%d)
dotnet run export --output ./pre-change-export-$(date +%Y%m%d).json
```

**Step 2: Make Changes in Azure Portal**
```bash
# Document changes made manually or via automation
echo "Changes made: Added MFA requirement for external users" > change-log.txt
```

**Step 3: Capture Post-Change State**
```bash
# After making changes
dotnet run baseline --output-dir ./post-change-$(date +%Y%m%d)
dotnet run export --output ./post-change-export-$(date +%Y%m%d).json
```

**Step 4: Generate Change Report**
```bash
# Compare pre and post change states
dotnet run compare \
  --reference-dir ./pre-change-$(date +%Y%m%d) \
  --entra-file ./post-change-export-$(date +%Y%m%d).json \
  --output-dir ./change-report-$(date +%Y%m%d) \
  --formats html json
```

### Scenario 3: Multi-Environment Management

**Business Context**: Organization has Development, Staging, and Production environments.

```bash
#!/bin/bash
# multi-env-sync.sh

ENVIRONMENTS=("dev" "staging" "prod")

for env in "${ENVIRONMENTS[@]}"; do
    echo "Processing $env environment..."
    
    # Set environment-specific credentials
    export AZURE_TENANT_ID="${env}_TENANT_ID"
    export AZURE_CLIENT_ID="${env}_CLIENT_ID"
    export AZURE_CLIENT_SECRET="${env}_CLIENT_SECRET"
    
    # Export policies
    dotnet run export --output "exports/${env}-policies-$(date +%Y%m%d).json"
    
    # Generate baseline if it's production
    if [ "$env" = "prod" ]; then
        dotnet run baseline --output-dir "baselines/${env}-$(date +%Y%m%d)"
    fi
    
    # Compare non-production to production baseline
    if [ "$env" != "prod" ]; then
        dotnet run compare \
          --reference-dir ./baselines/prod-latest \
          --entra-file "exports/${env}-policies-$(date +%Y%m%d).json" \
          --output-dir "reports/${env}-comparison-$(date +%Y%m%d)"
    fi
done
```

## 🔄 DevOps and CI/CD Integration

### Scenario 1: GitHub Actions Workflow

**Business Context**: Automated policy validation in CI/CD pipeline.

**.github/workflows/policy-validation.yml:**
```yaml
name: Conditional Access Policy Validation

on:
  pull_request:
    paths:
      - 'conditional-access-policies/**'
  schedule:
    - cron: '0 6 * * *'  # Daily at 6 AM

jobs:
  validate-policies:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Build CA_Scanner
      run: |
        git clone https://github.com/thefaftek-git/CA_Scanner.git
        cd CA_Scanner
        dotnet build
    
    - name: Export Current Policies
      env:
        AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
        AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
        AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
      run: |
        cd CA_Scanner/ConditionalAccessExporter
        dotnet run export --output ../../current-policies.json
    
    - name: Compare Against Reference
      run: |
        cd CA_Scanner/ConditionalAccessExporter
        dotnet run compare \
          --reference-dir ../../conditional-access-policies \
          --entra-file ../../current-policies.json \
          --output-dir ../../policy-reports \
          --formats json html
    
    - name: Upload Reports
      uses: actions/upload-artifact@v3
      with:
        name: policy-validation-report
        path: policy-reports/
    
    - name: Check for Policy Drift
      run: |
        # Fail the build if there are unexpected changes
        if grep -q "Policies with Differences" policy-reports/*.json; then
          echo "Policy drift detected! Review the changes."
          exit 1
        fi
```

### Scenario 2: Azure DevOps Pipeline

**azure-pipelines.yml:**
```yaml
trigger:
  branches:
    include:
      - main
  paths:
    include:
      - conditional-access-policies/*

pool:
  vmImage: 'ubuntu-latest'

variables:
  - group: 'azure-credentials'

stages:
- stage: PolicyValidation
  displayName: 'Validate Conditional Access Policies'
  jobs:
  - job: ValidatePolicies
    displayName: 'Validate Policies'
    steps:
    - task: DotNetCoreCLI@2
      displayName: 'Clone and Build CA_Scanner'
      inputs:
        command: 'custom'
        custom: 'run'
        arguments: |
          git clone https://github.com/thefaftek-git/CA_Scanner.git
          cd CA_Scanner && dotnet build
    
    - task: PowerShell@2
      displayName: 'Export and Compare Policies'
      env:
        AZURE_TENANT_ID: $(AZURE_TENANT_ID)
        AZURE_CLIENT_ID: $(AZURE_CLIENT_ID)
        AZURE_CLIENT_SECRET: $(AZURE_CLIENT_SECRET)
      inputs:
        script: |
          cd CA_Scanner/ConditionalAccessExporter
          dotnet run export --output $(Build.ArtifactStagingDirectory)/current-policies.json
          dotnet run compare `
            --reference-dir $(Build.SourcesDirectory)/conditional-access-policies `
            --entra-file $(Build.ArtifactStagingDirectory)/current-policies.json `
            --output-dir $(Build.ArtifactStagingDirectory)/reports
    
    - task: PublishBuildArtifacts@1
      displayName: 'Publish Policy Reports'
      inputs:
        pathToPublish: '$(Build.ArtifactStagingDirectory)'
        artifactName: 'policy-validation-results'
```

### Scenario 3: Infrastructure as Code with Terraform

**Business Context**: Managing Conditional Access policies as Terraform code.

```bash
#!/bin/bash
# terraform-workflow.sh

# Step 1: Export current policies as baseline
dotnet run baseline --output-dir ./current-state --template-format terraform

# Step 2: Convert existing JSON policies to Terraform
dotnet run terraform \
  --input ./existing-policies.json \
  --output ./terraform/conditional-access.tf \
  --direction json-to-terraform

# Step 3: Plan Terraform changes
cd terraform
terraform plan -out=ca-policies.plan

# Step 4: Apply changes (in controlled environment)
terraform apply ca-policies.plan

# Step 5: Validate the changes
cd ..
dotnet run export --output ./post-terraform.json
dotnet run compare \
  --reference-dir ./current-state \
  --entra-file ./post-terraform.json \
  --output-dir ./terraform-validation
```

**Example Terraform Output:**
```hcl
# Generated by CA_Scanner
resource "azuread_conditional_access_policy" "require_mfa_all_users" {
  display_name = "Require MFA for all users"
  state        = "enabled"

  conditions {
    applications {
      included_applications = ["All"]
    }
    
    users {
      included_users = ["All"]
      excluded_users = [
        "break-glass-account-1@contoso.com",
        "break-glass-account-2@contoso.com"
      ]
    }
    
    client_app_types = ["browser", "mobileAppsAndDesktopClients"]
  }

  grant_controls {
    operator          = "OR"
    built_in_controls = ["mfa"]
  }
}
```

## 🔒 Security and Compliance

### Scenario 1: SOC 2 Compliance Reporting

**Business Context**: Regular compliance reporting for SOC 2 audit.

```bash
#!/bin/bash
# soc2-compliance-report.sh

REPORT_DATE=$(date +%Y-%m-%d)
COMPLIANCE_DIR="./compliance-reports/$REPORT_DATE"

mkdir -p "$COMPLIANCE_DIR"

# Export all policies with full detail
dotnet run export --output "$COMPLIANCE_DIR/all-policies.json"

# Generate baseline for compliance tracking
dotnet run baseline \
  --output-dir "$COMPLIANCE_DIR/policy-baselines" \
  --include-metadata

# Compare against SOC 2 requirements baseline
dotnet run compare \
  --reference-dir ./soc2-requirements \
  --entra-file "$COMPLIANCE_DIR/all-policies.json" \
  --output-dir "$COMPLIANCE_DIR/compliance-analysis" \
  --formats html json csv

# Generate executive summary
cat > "$COMPLIANCE_DIR/executive-summary.md" << EOF
# SOC 2 Compliance Report - $REPORT_DATE

## Overview
This report analyzes Conditional Access policies against SOC 2 requirements.

## Files Generated
- \`all-policies.json\`: Complete policy export
- \`policy-baselines/\`: Individual policy files
- \`compliance-analysis/\`: Detailed comparison reports

## Key Findings
- Review the HTML report for detailed analysis
- Check CSV report for spreadsheet analysis
- JSON report contains programmatic results

## Next Steps
1. Review policies marked as non-compliant
2. Address any gaps identified
3. Document remediation actions
EOF

echo "SOC 2 compliance report generated in: $COMPLIANCE_DIR"
```

### Scenario 2: Security Incident Response

**Business Context**: Security incident requires immediate policy analysis.

```bash
#!/bin/bash
# incident-response.sh

INCIDENT_ID="INC-2025-001"
INCIDENT_DIR="./incidents/$INCIDENT_ID"

mkdir -p "$INCIDENT_DIR"

echo "Security Incident Response - $INCIDENT_ID"
echo "Capturing current policy state..."

# Immediate snapshot
dotnet run export --output "$INCIDENT_DIR/incident-snapshot.json"

# Compare against last known good state
dotnet run compare \
  --reference-dir ./last-known-good \
  --entra-file "$INCIDENT_DIR/incident-snapshot.json" \
  --output-dir "$INCIDENT_DIR/analysis" \
  --formats console json html

# Generate incident report
cat > "$INCIDENT_DIR/incident-analysis.md" << EOF
# Security Incident Analysis - $INCIDENT_ID

## Incident Details
- Date: $(date)
- Type: Conditional Access Policy Analysis
- Analyst: $(whoami)

## Actions Taken
1. Captured current policy state
2. Compared against baseline
3. Generated detailed analysis reports

## Files
- \`incident-snapshot.json\`: Current state
- \`analysis/\`: Comparison reports

## Findings
See HTML report for detailed analysis.

## Recommendations
[To be filled by security analyst]
EOF

echo "Incident analysis completed. Review files in: $INCIDENT_DIR"
```

### Scenario 3: Privileged Access Review

**Business Context**: Quarterly review of privileged access policies.

```bash
#!/bin/bash
# privileged-access-review.sh

QUARTER="Q$((($(date +%-m)-1)/3+1))-$(date +%Y)"
REVIEW_DIR="./privileged-access-reviews/$QUARTER"

mkdir -p "$REVIEW_DIR"

# Export only policies affecting privileged users
dotnet run export \
  --filter "*admin*" \
  --output "$REVIEW_DIR/admin-policies.json"

# Generate detailed baseline for admin policies
dotnet run baseline \
  --policy-names "Admin*" "Privileged*" "*Administrator*" \
  --output-dir "$REVIEW_DIR/admin-baselines" \
  --include-metadata

# Compare against privileged access standards
dotnet run compare \
  --reference-dir ./privileged-access-standards \
  --entra-file "$REVIEW_DIR/admin-policies.json" \
  --output-dir "$REVIEW_DIR/compliance-check" \
  --formats html json

echo "Privileged access review completed for $QUARTER"
echo "Review materials available in: $REVIEW_DIR"
```

## 🌐 Multi-Tenant Management

### Scenario 1: Managed Service Provider (MSP)

**Business Context**: MSP managing Conditional Access policies for multiple clients.

```bash
#!/bin/bash
# msp-multi-tenant.sh

# Client configuration
declare -A CLIENTS=(
    ["client1"]="tenant1-id:client1-id:client1-secret"
    ["client2"]="tenant2-id:client2-id:client2-secret"
    ["client3"]="tenant3-id:client3-id:client3-secret"
)

DATE=$(date +%Y%m%d)

for client in "${!CLIENTS[@]}"; do
    echo "Processing client: $client"
    
    # Parse credentials
    IFS=':' read -ra CREDS <<< "${CLIENTS[$client]}"
    export AZURE_TENANT_ID="${CREDS[0]}"
    export AZURE_CLIENT_ID="${CREDS[1]}"
    export AZURE_CLIENT_SECRET="${CREDS[2]}"
    
    # Create client directory
    CLIENT_DIR="./clients/$client/$DATE"
    mkdir -p "$CLIENT_DIR"
    
    # Export policies
    dotnet run export --output "$CLIENT_DIR/policies.json"
    
    # Generate anonymized baseline for analysis
    dotnet run baseline \
      --output-dir "$CLIENT_DIR/baseline" \
      --anonymize
    
    # Compare against MSP standards
    dotnet run compare \
      --reference-dir ./msp-standards \
      --entra-file "$CLIENT_DIR/policies.json" \
      --output-dir "$CLIENT_DIR/compliance" \
      --formats html json
    
    echo "Completed processing for $client"
done

# Generate MSP summary report
echo "MSP Multi-Tenant Report - $DATE" > "./clients/summary-$DATE.txt"
echo "=====================================" >> "./clients/summary-$DATE.txt"
for client in "${!CLIENTS[@]}"; do
    echo "Client: $client - Reports in ./clients/$client/$DATE/" >> "./clients/summary-$DATE.txt"
done
```

### Scenario 2: Corporate Subsidiary Management

**Business Context**: Parent company managing subsidiaries' Conditional Access policies.

```bash
#!/bin/bash
# subsidiary-management.sh

SUBSIDIARIES=("subsidiary-a" "subsidiary-b" "subsidiary-c")
CORPORATE_STANDARDS="./corporate-standards"

for subsidiary in "${SUBSIDIARIES[@]}"; do
    echo "Auditing $subsidiary..."
    
    # Load subsidiary-specific credentials
    source "./config/$subsidiary.env"
    
    # Export and analyze
    dotnet run export --output "./audits/$subsidiary-current.json"
    
    # Compare against corporate standards
    dotnet run compare \
      --reference-dir "$CORPORATE_STANDARDS" \
      --entra-file "./audits/$subsidiary-current.json" \
      --output-dir "./audits/$subsidiary-compliance" \
      --formats html json csv
    
    # Check for required corporate policies
    if ! grep -q "Corporate MFA Policy" "./audits/$subsidiary-current.json"; then
        echo "WARNING: $subsidiary missing corporate MFA policy"
    fi
done
```

## 🔧 Troubleshooting Examples

### Scenario 1: Debugging Authentication Issues

**Problem**: Authentication failures when trying to export policies.

```bash
#!/bin/bash
# debug-authentication.sh

echo "CA_Scanner Authentication Debug"
echo "==============================="

# Check environment variables
echo "Checking environment variables..."
if [ -z "$AZURE_TENANT_ID" ]; then
    echo "❌ AZURE_TENANT_ID not set"
else
    echo "✅ AZURE_TENANT_ID: ${AZURE_TENANT_ID:0:8}..."
fi

if [ -z "$AZURE_CLIENT_ID" ]; then
    echo "❌ AZURE_CLIENT_ID not set"
else
    echo "✅ AZURE_CLIENT_ID: ${AZURE_CLIENT_ID:0:8}..."
fi

if [ -z "$AZURE_CLIENT_SECRET" ]; then
    echo "❌ AZURE_CLIENT_SECRET not set"
else
    echo "✅ AZURE_CLIENT_SECRET: [SET]"
fi

# Test with debug logging
echo -e "\nTesting authentication with debug logging..."
export CA_SCANNER_LOG_LEVEL=Debug
dotnet run export --output test-auth.json 2>&1 | grep -E "(Authentication|Token|Error|Exception)"

# Validate Azure app registration
echo -e "\nValidating Azure configuration..."
curl -s "https://login.microsoftonline.com/$AZURE_TENANT_ID/v2.0/.well-known/openid_configuration" | jq .issuer

echo -e "\nIf authentication still fails:"
echo "1. Verify app registration exists"
echo "2. Check client secret expiration"
echo "3. Ensure Policy.Read.All permission granted"
echo "4. Verify admin consent provided"
```

### Scenario 2: Handling Large Tenant Exports

**Problem**: Timeouts when exporting from large tenants with many policies.

```bash
#!/bin/bash
# large-tenant-export.sh

echo "Large Tenant Export Optimization"
echo "================================"

# Configure for large tenants
export CA_SCANNER_TIMEOUT=120
export CA_SCANNER_RETRY_COUNT=5
export CA_SCANNER_PARALLEL_LIMIT=5

# Export with progress monitoring
echo "Starting export with optimized settings..."
timeout 300 dotnet run export --output large-tenant-export.json --verbose &

EXPORT_PID=$!

# Monitor progress
while kill -0 $EXPORT_PID 2>/dev/null; do
    echo "Export in progress... ($(date))"
    sleep 30
done

wait $EXPORT_PID
EXPORT_RESULT=$?

if [ $EXPORT_RESULT -eq 0 ]; then
    echo "✅ Export completed successfully"
    ls -lh large-tenant-export.json
else
    echo "❌ Export failed or timed out"
    echo "Try increasing timeout or reducing parallel limit"
fi
```

### Scenario 3: Comparing Policies with Complex Differences

**Problem**: Understanding complex policy differences in comparison reports.

```bash
#!/bin/bash
# detailed-comparison-analysis.sh

REFERENCE_DIR="./reference-policies"
CURRENT_EXPORT="./current-policies.json"

echo "Detailed Policy Comparison Analysis"
echo "=================================="

# Generate comparison with all formats
dotnet run compare \
  --reference-dir "$REFERENCE_DIR" \
  --entra-file "$CURRENT_EXPORT" \
  --output-dir "./detailed-analysis" \
  --formats console json html csv \
  --show-identical true

# Extract key metrics
echo -e "\nSummary Metrics:"
echo "=================="

TOTAL_POLICIES=$(jq '.summary.totalEntraPolicies' ./detailed-analysis/*.json)
DIFFERENCES=$(jq '.summary.policiesWithDifferences' ./detailed-analysis/*.json)
IDENTICAL=$(jq '.summary.identicalPolicies' ./detailed-analysis/*.json)

echo "Total Policies: $TOTAL_POLICIES"
echo "Policies with Differences: $DIFFERENCES"
echo "Identical Policies: $IDENTICAL"

# Show specific differences
echo -e "\nPolicies with Differences:"
echo "========================="
jq -r '.differences[] | "• \(.policyName): \(.referenceFile)"' ./detailed-analysis/*.json

echo -e "\nFor detailed analysis:"
echo "• Open HTML report in browser for visual comparison"
echo "• Check CSV report for spreadsheet analysis"
echo "• Review JSON report for programmatic processing"
```

## 🚀 Advanced Use Cases

### Scenario 1: Policy Template Management

**Business Context**: Creating and managing standardized policy templates.

```bash
#!/bin/bash
# policy-template-management.sh

TEMPLATE_DIR="./policy-templates"
INSTANCE_DIR="./policy-instances"

# Create templates from best practices
dotnet run baseline \
  --policy-names "Require MFA for all users" "Block legacy authentication" \
  --output-dir "$TEMPLATE_DIR/security-baseline" \
  --anonymize

# Generate organization-specific templates
for dept in "IT" "Finance" "HR"; do
    echo "Creating templates for $dept department..."
    
    dotnet run baseline \
      --filter "*$dept*" \
      --output-dir "$TEMPLATE_DIR/$dept" \
      --anonymize
done

# Create policy instances from templates
for template in "$TEMPLATE_DIR"/*/*.json; do
    template_name=$(basename "$template" .json)
    
    echo "Creating instance from template: $template_name"
    
    # Customize template for specific tenant
    jq '.displayName = "PROD-" + .displayName' "$template" > "$INSTANCE_DIR/prod-$template_name.json"
    jq '.displayName = "DEV-" + .displayName' "$template" > "$INSTANCE_DIR/dev-$template_name.json"
done
```

### Scenario 2: Automated Policy Lifecycle Management

**Business Context**: Automated creation, testing, and deployment of policies.

```bash
#!/bin/bash
# policy-lifecycle-management.sh

ENVIRONMENT=$1
POLICY_REPO="./policy-repository"

if [ -z "$ENVIRONMENT" ]; then
    echo "Usage: $0 <dev|staging|prod>"
    exit 1
fi

echo "Policy Lifecycle Management - $ENVIRONMENT"
echo "========================================"

# Stage 1: Validation
echo "Stage 1: Validating policy definitions..."
for policy in "$POLICY_REPO"/*.json; do
    if ! jq empty "$policy" 2>/dev/null; then
        echo "❌ Invalid JSON in $policy"
        exit 1
    fi
done
echo "✅ All policies have valid JSON"

# Stage 2: Environment-specific customization
echo "Stage 2: Customizing for $ENVIRONMENT..."
CUSTOM_DIR="./customized-$ENVIRONMENT"
mkdir -p "$CUSTOM_DIR"

for policy in "$POLICY_REPO"/*.json; do
    policy_name=$(basename "$policy")
    
    # Add environment prefix
    jq --arg env "$ENVIRONMENT" '.displayName = ($env | ascii_upcase) + "-" + .displayName' \
       "$policy" > "$CUSTOM_DIR/$policy_name"
done

# Stage 3: Deployment validation
echo "Stage 3: Pre-deployment validation..."
# Note: This would typically deploy to test environment first
# For this example, we'll simulate with comparison

# Stage 4: Monitoring setup
echo "Stage 4: Setting up monitoring..."
cat > "./monitoring-$ENVIRONMENT.sh" << EOF
#!/bin/bash
# Monitor $ENVIRONMENT policies
dotnet run export --output "./monitoring/\$(date +%Y%m%d)-$ENVIRONMENT.json"
dotnet run compare \\
  --reference-dir "$CUSTOM_DIR" \\
  --entra-file "./monitoring/\$(date +%Y%m%d)-$ENVIRONMENT.json" \\
  --output-dir "./monitoring/reports-$ENVIRONMENT"
EOF

chmod +x "./monitoring-$ENVIRONMENT.sh"
echo "✅ Monitoring script created: ./monitoring-$ENVIRONMENT.sh"
```

### Scenario 3: Cross-Platform Policy Analysis

**Business Context**: Analyzing policies across different identity platforms.

```bash
#!/bin/bash
# cross-platform-analysis.sh

echo "Cross-Platform Policy Analysis"
echo "=============================="

# Export from Azure AD
echo "Exporting Azure AD Conditional Access policies..."
dotnet run export --output azure-ad-policies.json

# Convert to different formats for analysis
echo "Converting to multiple formats..."

# Convert to Terraform for IaC analysis
dotnet run terraform \
  --input azure-ad-policies.json \
  --output terraform-policies/ \
  --direction json-to-terraform

# Generate human-readable reports
dotnet run compare \
  --reference-dir ./cross-platform-standards \
  --entra-file azure-ad-policies.json \
  --output-dir ./cross-platform-analysis \
  --formats html json csv

# Create platform comparison matrix
cat > cross-platform-summary.md << EOF
# Cross-Platform Policy Analysis

## Platforms Analyzed
- Azure AD Conditional Access
- (Future: AWS IAM, Google Cloud Identity)

## Files Generated
- \`azure-ad-policies.json\`: Raw Azure AD export
- \`terraform-policies/\`: Infrastructure as Code format
- \`cross-platform-analysis/\`: Comparison reports

## Analysis Results
See HTML report for detailed cross-platform comparison.

## Recommendations
1. Standardize policy naming conventions
2. Align security requirements across platforms
3. Implement consistent monitoring
EOF

echo "Cross-platform analysis completed."
echo "Review summary: cross-platform-summary.md"
```

## 📊 Reporting and Analytics Examples

### Scenario 1: Executive Dashboard Data

**Business Context**: Providing policy metrics for executive reporting.

```bash
#!/bin/bash
# executive-dashboard.sh

MONTH=$(date +%Y-%m)
DASHBOARD_DIR="./executive-dashboard/$MONTH"
mkdir -p "$DASHBOARD_DIR"

# Export all policies
dotnet run export --output "$DASHBOARD_DIR/all-policies.json"

# Generate metrics
POLICY_COUNT=$(jq '.PoliciesCount' "$DASHBOARD_DIR/all-policies.json")
ENABLED_COUNT=$(jq '[.Policies[] | select(.State == "enabled")] | length' "$DASHBOARD_DIR/all-policies.json")
DISABLED_COUNT=$(jq '[.Policies[] | select(.State == "disabled")] | length' "$DASHBOARD_DIR/all-policies.json")

# Create executive summary
cat > "$DASHBOARD_DIR/executive-summary.json" << EOF
{
  "reportDate": "$(date -I)",
  "metrics": {
    "totalPolicies": $POLICY_COUNT,
    "enabledPolicies": $ENABLED_COUNT,
    "disabledPolicies": $DISABLED_COUNT,
    "coveragePercentage": $(echo "scale=2; $ENABLED_COUNT * 100 / $POLICY_COUNT" | bc)
  },
  "trends": {
    "description": "Month-over-month policy changes",
    "note": "Compare with previous month's report"
  }
}
EOF

echo "Executive dashboard prepared: $DASHBOARD_DIR"
```

---

## 🆘 Getting Help

### Questions and Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/thefaftek-git/CA_Scanner/issues)
- **Documentation**: Check [CONFIGURATION.md](CONFIGURATION.md) for detailed options
- **Contributing**: See [CONTRIBUTING.md](CONTRIBUTING.md) for development guide

### Example Request Template

When asking for help, please include:

```
**Environment:**
- OS: [Windows/Linux/macOS]
- .NET Version: [output of `dotnet --version`]
- CA_Scanner Version: [version number]

**Azure Configuration:**
- Tenant Type: [Single/Multi-tenant]
- App Registration Permissions: [list permissions]
- Number of Policies: [approximate count]

**Issue Description:**
[Detailed description of the problem]

**Command Used:**
```bash
dotnet run [command and options]
```

**Error Message:**
```
[paste error message here]
```

**Expected Behavior:**
[what you expected to happen]
```

This comprehensive examples guide should help you use CA_Scanner effectively across various scenarios and use cases. Each example includes practical commands, expected outputs, and real-world context to help you adapt them to your specific needs.


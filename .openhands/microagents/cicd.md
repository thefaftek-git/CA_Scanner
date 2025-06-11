---
triggers:
  - cicd
  - "ci/cd"
  - pipeline
  - deployment
  - "github actions"
  - "azure devops"
  - "gitlab ci"
  - automation
  - "exit code"
  - "policy drift"
---

# CI/CD Pipeline Microagent

This microagent provides specialized guidance for CI/CD pipeline integration, automated policy drift detection, and deployment workflows within the CA_Scanner project.

## CI/CD Integration Overview

### Documentation
- **Comprehensive CI/CD Guide**: Available in `CICD.md` in the repository root
- **Purpose**: Automated policy drift detection in DevOps pipelines
- **Exit Codes**: Standardized exit codes for pipeline decision making
- **Platform Support**: GitHub Actions, Azure DevOps, GitLab CI examples

### CI/CD-Specific Features
- **Exit Code Control**: `--exit-on-differences` flag enables CI/CD mode
- **Threshold Management**: `--max-differences` sets acceptable difference count
- **Change Filtering**: `--fail-on` and `--ignore` for change type control
- **Quiet Mode**: `--quiet` for minimal pipeline output
- **Structured Output**: `--report-formats pipeline-json` for machine consumption

## Exit Code Standards

Understanding the standardized exit codes for pipeline integration:

- **0 (Success)**: No differences found or differences within acceptable thresholds
- **1 (Differences Found)**: Policy drift detected (non-critical differences)
- **2 (Critical Differences)**: Critical policy drift or threshold exceeded
- **3 (Error)**: Application error (authentication failure, file not found, etc.)

### Exit Code Usage in Pipelines
```bash
# Basic exit code handling
ca-scanner compare --reference-dir ./baseline --exit-on-differences --quiet
EXIT_CODE=$?

case $EXIT_CODE in
  0) echo "✅ No policy drift detected" ;;
  1) echo "⚠️ Non-critical differences found" ;;
  2) echo "🚨 Critical policy drift detected - blocking deployment" ;;
  3) echo "❌ Scanner error - check configuration" ;;
esac
```

## Critical vs Non-Critical Changes

### Built-in Critical Changes
These change types are automatically considered critical and will trigger exit code 2:
- `GrantControls` - Access grant settings (block, allow, require MFA)
- `SessionControls` - Session control settings
- `Conditions.SignInRiskLevels` - Sign-in risk requirements
- `Conditions.UserRiskLevels` - User risk requirements
- `Conditions.Applications.*` - Application targeting
- `Conditions.Users.*` - User/group/role targeting
- `State` - Policy enabled/disabled state

### Built-in Non-Critical Changes
These change types are typically informational and trigger exit code 1:
- `CreatedDateTime`, `ModifiedDateTime` - Timestamps
- `Id` - Policy unique identifier
- `Description`, `DisplayName` - Descriptive text

### Custom Classification
Override default behavior using command-line options:
```bash
# Make specific changes critical
--fail-on GrantControls,SessionControls,State

# Ignore specific changes completely
--ignore CreatedDateTime,ModifiedDateTime,Description
```

## Common CI/CD Commands

### Basic Policy Drift Detection
```bash
# Fail if any differences found
ca-scanner compare --reference-dir ./baseline --exit-on-differences --quiet
```

### Advanced Pipeline Integration
```bash
# Custom thresholds and filtering
ca-scanner compare \
  --reference-dir ./baseline \
  --exit-on-differences \
  --max-differences 2 \
  --fail-on GrantControls,SessionControls \
  --ignore CreatedDateTime,ModifiedDateTime \
  --report-formats pipeline-json \
  --quiet
```

### Security-Focused Monitoring
```bash
# Only fail on security-critical changes
ca-scanner compare \
  --reference-dir ./baseline \
  --exit-on-differences \
  --fail-on GrantControls,Conditions.Applications,State \
  --ignore Description,DisplayName,CreatedDateTime,ModifiedDateTime \
  --quiet
```

### Development Environment Validation
```bash
# More lenient for development environments
ca-scanner compare \
  --reference-dir ./baseline \
  --exit-on-differences \
  --max-differences 10 \
  --fail-on State \
  --ignore CreatedDateTime,ModifiedDateTime,Description,DisplayName \
  --quiet
```

## Pipeline Platform Examples

### GitHub Actions Integration

Create `.github/workflows/policy-check.yml`:
```yaml
name: CA Policy Drift Detection
on: 
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]
  schedule:
    - cron: '0 6 * * *'  # Daily at 6 AM

jobs:
  policy-drift-check:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout Code
        uses: actions/checkout@v4
        
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
          
      - name: Build CA Scanner
        run: dotnet build ConditionalAccessExporter.sln
        
      - name: Check Policy Drift
        env:
          AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
          AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
        run: |
          dotnet run --project ConditionalAccessExporter compare \
            --reference-dir ./policy-baseline \
            --exit-on-differences \
            --max-differences 5 \
            --fail-on GrantControls,SessionControls,State \
            --ignore CreatedDateTime,ModifiedDateTime \
            --report-formats pipeline-json,html \
            --output-dir ./results \
            --quiet
        continue-on-error: true
        
      - name: Upload Drift Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: policy-drift-results-${{ github.sha }}
          path: ./results/
          retention-days: 30
          
      - name: Comment on PR
        if: github.event_name == 'pull_request'
        uses: actions/github-script@v6
        with:
          script: |
            const fs = require('fs');
            try {
              const result = JSON.parse(fs.readFileSync('./results/pipeline-output.json', 'utf8'));
              const comment = `## 🔍 Policy Drift Check Results
              
              **Status**: ${result.status}
              **Exit Code**: ${result.exitCode}
              **Differences Found**: ${result.differencesCount}
              **Critical Changes**: ${result.criticalChanges}
              **Non-Critical Changes**: ${result.nonCriticalChanges}
              
              ${result.exitCode === 2 ? '🚨 **Critical policy drift detected!**' : 
                result.exitCode === 1 ? '⚠️ Minor differences found' : '✅ No significant drift detected'}
              `;
              
              github.rest.issues.createComment({
                issue_number: context.issue.number,
                owner: context.repo.owner,
                repo: context.repo.repo,
                body: comment
              });
            } catch (error) {
              console.log('No results file found or parse error');
            }
```

**Key GitHub Actions Features**:
- **Secrets Management**: Store Azure credentials in GitHub Secrets
- **Artifacts**: Upload results using `actions/upload-artifact@v4`
- **Conditional Steps**: Use `continue-on-error: true` for non-blocking checks
- **PR Comments**: Automatic result posting to pull requests
- **Scheduled Runs**: Regular drift detection with cron schedules

### Azure DevOps Integration

Create `azure-pipelines.yml`:
```yaml
trigger:
  branches:
    include:
    - main
    - develop

schedules:
- cron: "0 6 * * *"
  displayName: Daily policy drift check
  branches:
    include:
    - main

pool:
  vmImage: 'ubuntu-latest'

variables:
- group: azure-credentials  # Variable group with Azure secrets

stages:
- stage: PolicyDriftCheck
  displayName: 'Policy Drift Detection'
  jobs:
  - job: CheckDrift
    displayName: 'Check CA Policy Drift'
    
    steps:
    - task: UseDotNet@2
      displayName: 'Install .NET 8 SDK'
      inputs:
        packageType: 'sdk'
        version: '8.0.x'
        
    - task: DotNetCoreCLI@2
      displayName: 'Build CA Scanner'
      inputs:
        command: 'build'
        projects: 'ConditionalAccessExporter.sln'
        
    - task: Bash@3
      displayName: 'Check CA Policy Drift'
      env:
        AZURE_TENANT_ID: $(AZURE_TENANT_ID)
        AZURE_CLIENT_ID: $(AZURE_CLIENT_ID)
        AZURE_CLIENT_SECRET: $(AZURE_CLIENT_SECRET)
      script: |
        dotnet run --project ConditionalAccessExporter compare \
          --reference-dir ./policy-baseline \
          --exit-on-differences \
          --max-differences 3 \
          --fail-on GrantControls,State \
          --ignore CreatedDateTime,ModifiedDateTime \
          --report-formats pipeline-json,html \
          --output-dir $(Agent.TempDirectory)/results \
          --quiet
        
        # Capture exit code for reporting
        echo "##vso[task.setvariable variable=CA_EXIT_CODE]$?"
      continueOnError: true
      
    - task: PublishBuildArtifacts@1
      displayName: 'Publish Drift Results'
      condition: always()
      inputs:
        pathToPublish: '$(Agent.TempDirectory)/results'
        artifactName: 'policy-drift-results'
        
    - task: PublishTestResults@2
      displayName: 'Publish Test Results'
      condition: always()
      inputs:
        testResultsFormat: 'JUnit'
        testResultsFiles: '$(Agent.TempDirectory)/results/junit-results.xml'
        failTaskOnFailedTests: false
```

**Key Azure DevOps Features**:
- **Variable Groups**: Store Azure credentials securely in variable groups
- **Artifacts**: Use `PublishBuildArtifacts@1` task for result storage
- **Agent Pool**: Use `ubuntu-latest` for .NET 8 support
- **Scheduled Triggers**: Daily runs with cron expressions
- **Test Results**: Integrate with Azure DevOps test reporting

### GitLab CI Integration

Create `.gitlab-ci.yml`:
```yaml
stages:
  - build
  - test
  - policy-check
  - deploy

variables:
  DOTNET_VERSION: "8.0"

before_script:
  - apt-get update -qq && apt-get install -y -qq git curl
  - curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version $DOTNET_VERSION
  - export PATH="$HOME/.dotnet:$PATH"

build:
  stage: build
  script:
    - dotnet restore ConditionalAccessExporter.sln
    - dotnet build ConditionalAccessExporter.sln --configuration Release
  artifacts:
    paths:
      - ConditionalAccessExporter/bin/
    expire_in: 1 hour

policy-drift-check:
  stage: policy-check
  dependencies:
    - build
  script:
    - dotnet run --project ConditionalAccessExporter compare
        --reference-dir ./policy-baseline
        --exit-on-differences
        --max-differences 5
        --fail-on GrantControls,SessionControls
        --ignore CreatedDateTime,ModifiedDateTime
        --report-formats pipeline-json,html
        --output-dir ./results
        --quiet
  artifacts:
    when: always
    paths:
      - results/
    reports:
      junit: results/junit-results.xml
    expire_in: 1 week
  allow_failure: true
  only:
    - main
    - develop
    - merge_requests

policy-drift-scheduled:
  stage: policy-check
  extends: policy-drift-check
  only:
    - schedules
  allow_failure: false  # Fail scheduled runs on critical drift
```

**Key GitLab CI Features**:
- **CI Variables**: Store credentials in GitLab CI/CD variables
- **Artifacts**: Configure `artifacts.paths` for result files
- **Job Control**: Use `allow_failure: true` for non-blocking checks
- **Scheduled Pipelines**: Configure in GitLab UI for regular runs
- **Merge Request Integration**: Automatic checks on MRs

## Pipeline Output Format

When using `--report-formats pipeline-json`, CA_Scanner generates structured JSON output optimized for CI/CD consumption:

```json
{
  "status": "critical_drift_detected",
  "exitCode": 2,
  "differencesCount": 5,
  "criticalChanges": 3,
  "nonCriticalChanges": 2,
  "comparedAt": "2024-01-15T10:30:00Z",
  "tenantId": "your-tenant-id",
  "criticalChangeTypes": ["GrantControls", "Conditions.Users.IncludeGroups"],
  "policyNames": ["Block Legacy Authentication", "Require MFA for Admins"],
  "message": "Critical policy drift detected: 3 critical differences found in 2 policies",
  "thresholdConfiguration": {
    "maxDifferences": 2,
    "failOnTypes": ["GrantControls", "SessionControls"],
    "ignoreTypes": ["CreatedDateTime", "ModifiedDateTime"]
  },
  "environmentInfo": {
    "scannerVersion": "2.1.0",
    "dotnetVersion": "8.0.1",
    "platform": "Linux"
  }
}
```

### Using Pipeline Output in Scripts
```bash
# Parse results in bash
RESULT_FILE="./results/pipeline-output.json"
if [ -f "$RESULT_FILE" ]; then
  EXIT_CODE=$(jq -r '.exitCode' "$RESULT_FILE")
  STATUS=$(jq -r '.status' "$RESULT_FILE")
  CRITICAL_CHANGES=$(jq -r '.criticalChanges' "$RESULT_FILE")
  
  echo "Status: $STATUS"
  echo "Critical Changes: $CRITICAL_CHANGES"
  
  # Send to monitoring system
  curl -X POST "$WEBHOOK_URL" \
    -H "Content-Type: application/json" \
    -d @"$RESULT_FILE"
fi
```

## Environment Setup for CI/CD

### Azure Credentials
Set the following environment variables or CI/CD secrets:
```bash
AZURE_TENANT_ID=your-tenant-id
AZURE_CLIENT_ID=your-client-id
AZURE_CLIENT_SECRET=your-client-secret
```

### .NET Runtime Requirements
- **.NET 8 SDK**: Required for building and running CA_Scanner
- **Ubuntu/Linux**: Recommended for CI/CD agents
- **Memory**: Minimum 2GB RAM for typical policy sets
- **Storage**: 1GB free space for artifacts and temporary files

### Baseline Files
- **Storage**: Keep reference policies in version control alongside code
- **Structure**: Organize by environment (dev/staging/prod baselines)
- **Updates**: Control baseline changes through standard change processes
- **Validation**: Verify baseline integrity before using in pipelines

### Output Directory Configuration
```bash
# Ensure writable directory for reports
mkdir -p ./results
chmod 755 ./results

# Set appropriate directory in commands
--output-dir ./results
```

## Best Practices for CI/CD

### Baseline Management
- **Version Control**: Store baselines alongside application code
- **Environment Separation**: Use different baselines for dev/staging/prod
- **Change Control**: Update baselines through pull request workflows
- **Validation**: Test baseline changes in lower environments first
- **Documentation**: Document baseline update procedures

### Change Type Configuration
- **Start Conservative**: Begin with built-in classifications
- **Customize Gradually**: Add organization-specific rules over time
- **Document Decisions**: Maintain documentation of custom rules
- **Regular Review**: Quarterly review of classification effectiveness
- **Team Training**: Ensure team understands change impact levels

### Threshold Management
- **Environment-Specific**: Use different thresholds per environment
  - **Production**: `--max-differences 0` (zero tolerance)
  - **Staging**: `--max-differences 3` (moderate tolerance)
  - **Development**: `--max-differences 10` (high tolerance)
- **Change Frequency**: Adjust based on organization's change velocity
- **Monitor Effectiveness**: Track false positives and missed issues
- **Gradual Tightening**: Start permissive, tighten over time

### Pipeline Strategy
- **Deployment Gates**: Block production deployments on critical drift
- **Scheduled Monitoring**: Daily/weekly drift detection runs
- **Change Validation**: Run checks on every policy-related change
- **Alerting Integration**: Connect to incident management systems
- **Artifact Retention**: Keep results for compliance and audit

### Security Considerations
- **Credential Management**: Use platform-native secret storage
- **Least Privilege**: Read-only Azure permissions for scanning
- **Audit Logging**: Enable comprehensive pipeline execution logging
- **Access Control**: Restrict who can modify baselines and thresholds
- **Network Security**: Use secure connections for all API calls

## Troubleshooting CI/CD Issues

### Common Problems and Solutions

#### Exit Code 1 (Non-Critical Differences)
**Scenario**: Pipeline reports differences but they seem acceptable
**Investigation Steps**:
```bash
# Remove --quiet for detailed output
dotnet run compare --reference-dir ./baseline --exit-on-differences

# Review specific differences
cat ./results/pipeline-output.json | jq '.nonCriticalChanges'

# Check if differences should be ignored
dotnet run compare --reference-dir ./baseline --ignore CreatedDateTime,ModifiedDateTime
```

**Common Causes**:
- Timestamp changes from normal operations
- Cosmetic changes to descriptions or names
- Non-security-related configuration updates

**Solutions**:
- Add appropriate `--ignore` flags for timestamp fields
- Update baseline if changes are intentional
- Adjust threshold with `--max-differences`

#### Exit Code 2 (Critical Differences)
**Scenario**: Critical security changes detected
**Investigation Steps**:
```bash
# Get detailed critical change information
cat ./results/pipeline-output.json | jq '.criticalChangeTypes[]'

# Generate human-readable report
dotnet run compare --reference-dir ./baseline --formats html

# Review specific policies
cat ./results/pipeline-output.json | jq '.policyNames[]'
```

**Common Causes**:
- Unauthorized changes to access controls
- Policy state changes (enabled/disabled)
- User/group targeting modifications
- MFA requirement changes

**Immediate Actions**:
- **Security Review**: Immediate investigation required
- **Change Authorization**: Verify if changes were authorized
- **Rollback Consideration**: Evaluate need for immediate rollback
- **Incident Response**: Follow security incident procedures

#### Exit Code 3 (Application Error)
**Scenario**: Scanner fails to execute properly
**Investigation Steps**:
```bash
# Check authentication
az account show

# Verify file paths
ls -la ./policy-baseline/

# Test basic connectivity
dotnet run export --format json --output-dir ./test-export

# Check permissions
dotnet run compare --reference-dir ./baseline --verbose
```

**Common Causes**:
- **Authentication Failures**: Expired credentials or incorrect configuration
- **File Not Found**: Missing baseline files or incorrect paths
- **Permission Issues**: Insufficient Azure permissions
- **Network Problems**: Connectivity issues to Azure APIs

**Solutions**:
```bash
# Fix authentication
export AZURE_TENANT_ID="correct-tenant-id"
export AZURE_CLIENT_ID="correct-client-id"  
export AZURE_CLIENT_SECRET="correct-secret"

# Verify file paths
find . -name "*.json" -path "*/policy-baseline/*"

# Test Azure connectivity
az login --service-principal -u $AZURE_CLIENT_ID -p $AZURE_CLIENT_SECRET --tenant $AZURE_TENANT_ID
```

### Debug Mode Operations
```bash
# Enable verbose output for troubleshooting
dotnet run compare \
  --reference-dir ./baseline \
  --exit-on-differences \
  --verbose

# Generate detailed logs
dotnet run compare \
  --reference-dir ./baseline \
  --exit-on-differences \
  --log-level Debug \
  --output-dir ./debug-results

# Test without exit codes for investigation
dotnet run compare \
  --reference-dir ./baseline \
  --formats console,json
```

### Log Analysis Commands
```bash
# Analyze pipeline output
cat ./results/pipeline-output.json | jq '.'

# Extract critical changes
jq '.criticalChangeTypes[]' ./results/pipeline-output.json

# Get policy-specific issues
jq '.policyDetails[] | select(.hasCriticalChanges == true)' ./results/comparison-results.json

# Check threshold effectiveness
jq '{differences: .differencesCount, threshold: .thresholdConfiguration.maxDifferences}' ./results/pipeline-output.json
```

## Integration with Other Tools

### Slack/Teams Notifications
```bash
# Send results to Slack
WEBHOOK_URL="https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK"
RESULT=$(cat ./results/pipeline-output.json)

curl -X POST "$WEBHOOK_URL" \
  -H "Content-Type: application/json" \
  -d "{
    \"text\": \"Policy Drift Alert\",
    \"attachments\": [{
      \"color\": \"$([ $(echo $RESULT | jq '.exitCode') -eq 2 ] && echo 'danger' || echo 'warning')\",
      \"fields\": [{
        \"title\": \"Status\",
        \"value\": \"$(echo $RESULT | jq -r '.status')\",
        \"short\": true
      }, {
        \"title\": \"Critical Changes\",
        \"value\": \"$(echo $RESULT | jq -r '.criticalChanges')\",
        \"short\": true
      }]
    }]
  }"
```

### JIRA/ServiceNow Integration
```bash
# Create ticket for critical drift
if [ $(jq '.exitCode' ./results/pipeline-output.json) -eq 2 ]; then
  curl -X POST "$JIRA_API_URL/rest/api/2/issue" \
    -H "Authorization: Bearer $JIRA_TOKEN" \
    -H "Content-Type: application/json" \
    -d "{
      \"fields\": {
        \"project\": {\"key\": \"SEC\"},
        \"summary\": \"Critical Policy Drift Detected\",
        \"description\": \"$(cat ./results/pipeline-output.json | jq -r '.message')\",
        \"issuetype\": {\"name\": \"Incident\"}
      }
    }"
fi
```

### SIEM Integration
```bash
# Send to SIEM system
SIEM_ENDPOINT="https://your-siem.com/api/events"
RESULT=$(cat ./results/pipeline-output.json)

curl -X POST "$SIEM_ENDPOINT" \
  -H "Authorization: Bearer $SIEM_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"event_type\": \"policy_drift\",
    \"severity\": \"$([ $(echo $RESULT | jq '.exitCode') -eq 2 ] && echo 'high' || echo 'medium')\",
    \"source\": \"ca_scanner\",
    \"data\": $RESULT
  }"
```

### Monitoring Dashboard Integration
```bash
# Send metrics to monitoring system
METRICS_URL="https://your-monitoring.com/api/metrics"

curl -X POST "$METRICS_URL" \
  -H "Authorization: Bearer $MONITORING_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"metrics\": [
      {
        \"name\": \"ca_policy_drift_count\",
        \"value\": $(jq '.differencesCount' ./results/pipeline-output.json),
        \"tags\": {\"environment\": \"production\", \"type\": \"total\"}
      },
      {
        \"name\": \"ca_policy_critical_changes\",
        \"value\": $(jq '.criticalChanges' ./results/pipeline-output.json),
        \"tags\": {\"environment\": \"production\", \"type\": \"critical\"}
      }
    ]
  }"
```

## Advanced Pipeline Scenarios

### Multi-Environment Validation
```bash
# Compare across environments
dotnet run compare \
  --reference-dir ./baselines/production \
  --entra-file ./exports/staging-export.json \
  --exit-on-differences \
  --fail-on State,GrantControls \
  --output-dir ./results/staging-vs-prod \
  --quiet

# Validate promotion readiness
dotnet run compare \
  --reference-dir ./baselines/staging \
  --entra-file ./exports/dev-export.json \
  --max-differences 5 \
  --ignore CreatedDateTime,ModifiedDateTime,Description
```

### Conditional Pipeline Logic
```yaml
# GitHub Actions example with conditional deployment
- name: Check Policy Drift
  id: drift-check
  run: |
    dotnet run compare --reference-dir ./baseline --exit-on-differences --quiet
    echo "exit-code=$?" >> $GITHUB_OUTPUT
    
- name: Deploy to Production
  if: steps.drift-check.outputs.exit-code == '0'
  run: ./deploy-to-production.sh
  
- name: Block Deployment
  if: steps.drift-check.outputs.exit-code == '2'
  run: |
    echo "🚨 Critical policy drift detected - blocking deployment"
    exit 1
```

### Rollback Automation
```bash
# Automated rollback on critical drift
EXIT_CODE=$(dotnet run compare --reference-dir ./baseline --exit-on-differences --quiet; echo $?)

if [ $EXIT_CODE -eq 2 ]; then
  echo "Critical drift detected - initiating rollback"
  
  # Restore from backup
  az backup restore \
    --resource-group $RG_NAME \
    --vault-name $VAULT_NAME \
    --container-name $CONTAINER_NAME \
    --item-name conditional-access-policies
    
  # Verify restoration
  sleep 60
  dotnet run compare --reference-dir ./baseline --quiet
fi
```

This microagent provides comprehensive guidance for implementing CI/CD pipeline integration with CA_Scanner, covering all major platforms, security considerations, and troubleshooting scenarios.

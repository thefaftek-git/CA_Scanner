# CI/CD Integration Guide

## Overview

CA_Scanner now includes comprehensive CI/CD integration features that enable automated policy drift detection with configurable exit codes, thresholds, and filtering options. These features are designed to integrate seamlessly with CI/CD pipelines for continuous security monitoring.

## New Command Line Options

### Exit Code Control
- `--exit-on-differences`: Enable CI/CD mode - exit with non-zero code when differences are found
- `--max-differences <number>`: Maximum number of differences allowed before failing (optional)

### Change Type Filtering
- `--fail-on <types>`: Comma-separated list of change types that should cause pipeline failure
- `--ignore <types>`: Comma-separated list of change types to ignore during analysis

### Output Control
- `--quiet`: Minimize output for pipeline consumption (shows only essential status messages)

## Exit Codes

CA_Scanner uses standardized exit codes for CI/CD integration:

- **0 (Success)**: No differences found or differences found but within acceptable thresholds
- **1 (Differences Found)**: Policy drift detected (non-critical differences)
- **2 (Critical Differences)**: Critical policy drift detected or threshold exceeded
- **3 (Error)**: Application error (authentication failure, file not found, etc.)

## Critical vs Non-Critical Changes

### Critical Change Types (Built-in)
Changes to these policy areas are automatically classified as critical:
- `GrantControls` - Access grant settings (block, allow, require MFA, etc.)
- `SessionControls` - Session control settings
- `Conditions.SignInRiskLevels` - Sign-in risk level requirements
- `Conditions.UserRiskLevels` - User risk level requirements
- `Conditions.Applications.*` - Application targeting (include/exclude)
- `Conditions.Users.*` - User/group/role targeting (include/exclude)
- `State` - Policy enabled/disabled state

### Non-Critical Change Types (Built-in)
Changes to these areas are typically informational:
- `CreatedDateTime` - Policy creation timestamp
- `ModifiedDateTime` - Policy modification timestamp
- `Id` - Policy unique identifier
- `Description` - Policy description text
- `DisplayName` - Policy display name

### Custom Classification
You can override the default classification using:
- `--fail-on` to make specific change types critical
- `--ignore` to exclude specific change types from analysis

## Pipeline Output Format

Use `--report-formats pipeline-json` to generate structured output for CI/CD consumption:

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
  "policyNames": ["Policy1", "Policy2"],
  "message": "Critical policy drift detected: 3 critical differences found in 2 policies"
}
```

## Usage Examples

### Basic CI/CD Integration
```bash
# Exit with code 1 if any differences found
ca-scanner compare --reference-dir ./baseline --exit-on-differences --quiet
```

### Advanced CI/CD with Thresholds
```bash
# Allow up to 2 differences, fail on security-critical changes
ca-scanner compare \
  --reference-dir ./baseline \
  --exit-on-differences \
  --max-differences 2 \
  --fail-on GrantControls,SessionControls \
  --ignore CreatedDateTime,ModifiedDateTime \
  --report-formats pipeline-json \
  --quiet
```

### Focused Security Monitoring
```bash
# Only fail on access control changes
ca-scanner compare \
  --reference-dir ./baseline \
  --exit-on-differences \
  --fail-on GrantControls,Conditions.Applications,State \
  --ignore Description,DisplayName,CreatedDateTime,ModifiedDateTime \
  --quiet
```

## CI/CD Platform Integration

### GitHub Actions

```yaml
name: CA Policy Drift Detection
on: [push, pull_request]

jobs:
  policy-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Check Policy Drift
        env:
          AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
          AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
        run: |
          ./ca-scanner compare \
            --reference-dir ./policy-baseline \
            --exit-on-differences \
            --max-differences 5 \
            --fail-on GrantControls,SessionControls,State \
            --ignore CreatedDateTime,ModifiedDateTime \
            --report-formats pipeline-json,html \
            --output-dir ./results \
            --quiet
        continue-on-error: true
        
      - name: Upload Results
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: policy-drift-results
          path: ./results/
```

### Azure DevOps

```yaml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: Bash@3
  displayName: 'Check CA Policy Drift'
  env:
    AZURE_TENANT_ID: $(AZURE_TENANT_ID)
    AZURE_CLIENT_ID: $(AZURE_CLIENT_ID)
    AZURE_CLIENT_SECRET: $(AZURE_CLIENT_SECRET)
  script: |
    ./ca-scanner compare \
      --reference-dir ./policy-baseline \
      --exit-on-differences \
      --max-differences 3 \
      --fail-on GrantControls,State \
      --report-formats pipeline-json \
      --output-dir $(Agent.TempDirectory)/results \
      --quiet
  continueOnError: true

- task: PublishBuildArtifacts@1
  condition: always()
  inputs:
    pathToPublish: '$(Agent.TempDirectory)/results'
    artifactName: 'policy-drift-results'
```

### GitLab CI

```yaml
policy-drift-check:
  stage: test
  script:
    - ./ca-scanner compare
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
  allow_failure: true
```

## Best Practices

### 1. Baseline Management
- Store policy baselines in version control alongside code
- Use separate baselines for different environments (dev, staging, prod)
- Update baselines through controlled change processes

### 2. Change Type Configuration
- Start with built-in critical/non-critical classifications
- Customize based on your organization's risk tolerance
- Regularly review and update change type filters

### 3. Threshold Setting
- Set `--max-differences` based on your change frequency
- Use lower thresholds for production environments
- Consider different thresholds for different change types

### 4. Monitoring Strategy
- Run checks on every deployment to production
- Use scheduled checks for drift detection
- Combine with alerting systems for immediate notification

### 5. Output Management
- Use `--quiet` mode in automated pipelines
- Generate detailed reports for investigation
- Archive results for compliance and audit purposes

## Troubleshooting

### Common Exit Codes
- **Exit Code 1**: Review the differences and determine if they're acceptable
- **Exit Code 2**: Critical security changes detected - immediate review required
- **Exit Code 3**: Check authentication, file paths, and permissions

### Debug Mode
Remove `--quiet` flag to get detailed output for troubleshooting:
```bash
ca-scanner compare --reference-dir ./baseline --exit-on-differences
```

### Pipeline Output Analysis
Check the `pipeline-output.json` file for detailed analysis:
```bash
cat ./results/pipeline-output.json | jq '.criticalChangeTypes[]'
```

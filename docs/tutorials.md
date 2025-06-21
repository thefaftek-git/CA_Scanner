---
layout: default
title: Tutorials
parent: Documentation
nav_order: 3
---

# Tutorials

Step-by-step guides to help you get the most out of CA Scanner.

## Table of Contents

1. [Basic Policy Export](#basic-policy-export)
2. [Policy Comparison](#policy-comparison)
3. [CI/CD Integration](#cicd-integration)
4. [Terraform Conversion](#terraform-conversion)

## Basic Policy Export

This tutorial walks you through exporting Conditional Access policies from your Azure AD tenant.

### Step-by-Step Guide

1. **Set up your environment** by following the [Quick Start Guide](/quickstart.html).

2. **Run the export command**:

```bash
dotnet run export --output my-policies.json
```

3. **Verify the output file** contains your policies:

```bash
cat my-policies.json | grep -A 5 "displayName"
```

## Policy Comparison

Learn how to compare live policies against reference files.

### Step-by-Step Guide

1. **First, create a directory for your reference policies**:

```bash
mkdir reference-policies
```

2. **Copy or generate reference policy JSON files** into this directory:

   - You can use the `baseline` command to generate these:
     ```bash
     dotnet run baseline --output-dir ./reference-policies --anonymize
     ```

3. **Run the comparison command**:

```bash
dotnet run compare --reference-dir ./reference-policies
```

4. **Review the output** to identify differences:

   - The console output will show a summary of matching and differing policies.
   - HTML and JSON reports are also generated in the `comparison-reports` directory.

## CI/CD Integration

Integrate CA Scanner with your CI/CD pipelines for automated policy validation.

### Step-by-Step Guide

1. **Add a step in your pipeline to export policies**:

```yaml
- name: Export Azure AD Policies
  run: dotnet run export --output policies.json
```

2. **Compare against reference files and fail on differences**:

```yaml
- name: Compare Policies
  run: dotnet run compare --reference-dir ./reference-policies --formats pipeline-json
```

3. **Configure your pipeline to check the output for failures**.

### Example GitHub Actions Workflow

Here's an example of a GitHub Actions workflow that uses CA Scanner:

```yaml
name: Azure AD Policy Validation

on:
  push:
    branches:
      - main
  schedule:
    - cron: '0 0 * * *' # Daily at midnight

jobs:
  validate-policies:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v2

    - name: Set up .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '8.0.x'

    - name: Install CA Scanner dependencies
      run: ./dotnet-install.sh

    - name: Export policies
      env:
        AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
        AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
        AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
      run: dotnet run export --output policies.json

    - name: Compare with reference
      run: dotnet run compare --reference-dir ./reference-policies --formats pipeline-json

    - name: Check for policy differences
      run: |
        if [ -f comparison-reports/pipeline-output.json ]; then
          echo "Policy comparison completed"
        else
          echo "Error: Policy comparison failed or no output generated"
          exit 1
        fi
```

## Terraform Conversion

Learn how to convert between JSON and Terraform formats.

### JSON to Terraform

1. **Export your policies** as shown in the [Basic Policy Export](#basic-policy-export) tutorial.

2. **Convert the JSON file to Terraform**:

```bash
dotnet run json-to-terraform --input my-policies.json
```

3. **Review the generated files** in the `terraform-output` directory:

   - `azuread_conditional_access_policy.tf`
   - `variables.tf`
   - `providers.tf`

### Terraform to JSON

1. **Create or obtain your Terraform files**.

2. **Convert them to JSON**:

```bash
dotnet run terraform --input my-terraform-files
```

3. **Review the output file** (default: timestamped JSON file).

## Advanced Usage

### Custom Policy Matching

By default, CA Scanner matches policies by name. You can customize this behavior:

1. **Create a custom mapping file**:

```json
{
  "livePolicyId1": "referencePolicyId1",
  "livePolicyId2": "referencePolicyId2"
}
```

2. **Use the `CustomMapping` strategy** with your mapping file:

```bash
dotnet run compare --reference-dir ./reference-policies --matching CustomMapping --custom-mapping custom-mapping.json
```

### Ignoring Specific Differences

You can ignore specific types of differences during comparison:

1. **Run the compare command** with the `--ignore` option:

```bash
dotnet run compare --reference-dir ./reference-policies --ignore "GrantControls,SessionControls"
```

This will ignore differences in grant controls and session controls when comparing policies.

### Validating Reference Files

Before comparing policies, you can validate your reference files:

1. **Run the validation command**:

```bash
dotnet run validate --reference-dir ./reference-policies
```

2. **Review the output** for any issues with your reference files.

3. **Fix any problems** identified during validation.


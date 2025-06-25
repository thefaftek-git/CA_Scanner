

# Practical Use Cases and Examples

This document provides practical examples and use cases demonstrating how to use CA_Scanner effectively. Each example includes a description, the command to run, and expected output.

## Getting Started

### Basic Export

**Description**: Export all Conditional Access policies from your Azure AD tenant.

**Command**:
```bash
dotnet run export --output-dir ./output
```

**Expected Output**:
- A directory named `output` containing JSON files for each policy.

### Generate Baseline

**Description**: Create a baseline of your current policies for future comparisons.

**Command**:
```bash
dotnet run baseline --output-dir ./baseline
```

**Expected Output**:
- A directory named `baseline` containing JSON files for each policy.

## Enterprise Scenarios

### Large-Scale Deployment

**Description**: Export policies from multiple tenants and generate a consolidated report.

**Command**:
```bash
dotnet run export --tenant-ids tenant1,tenant2 --output-dir ./enterprise-output
```

**Expected Output**:
- A directory named `enterprise-output` containing JSON files for each tenant's policies.

### Change Management

**Description**: Compare current policies against a baseline and generate a change report.

**Command**:
```bash
dotnet run compare --baseline-dir ./baseline --live-dir ./live --output-dir ./changes
```

**Expected Output**:
- A directory named `changes` containing a report of policy differences.

## DevOps Integration

### CI/CD Pipeline

**Description**: Integrate CA_Scanner into a CI/CD pipeline to automatically export and compare policies.

**Example**: GitHub Actions workflow

```yaml
name: CA Policy Management

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  export-policies:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v2

    - name: Set up .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '8.0.x'

    - name: Export Policies
      run: dotnet run export --output-dir ./output

    - name: Upload Artifact
      uses: actions/upload-artifact@v2
      with:
        name: policy-export
        path: ./output
```

**Expected Output**:
- A GitHub Actions workflow that exports policies on each push or pull request.

## Security & Compliance

### Audit Reporting

**Description**: Generate a compliance report for audit purposes.

**Command**:
```bash
dotnet run report --input-dir ./live --output-dir ./audit-report --format html
```

**Expected Output**:
- An HTML report in the `audit-report` directory detailing all policies and their configurations.

### Incident Response

**Description**: Quickly identify and remediate policy changes during an incident.

**Command**:
```bash
dotnet run compare --baseline-dir ./baseline --live-dir ./live --output-dir ./incident-report --remediate
```

**Expected Output**:
- A report of policy changes and automated remediation actions.

## Multi-Tenant Management

### MSP Scenarios

**Description**: Manage policies for multiple customer tenants.

**Command**:
```bash
dotnet run export --tenant-ids tenant1,tenant2,tenant3 --output-dir ./msp-output
```

**Expected Output**:
- A directory named `msp-output` containing JSON files for each customer's policies.

### Subsidiary Management

**Description**: Compare policies across subsidiaries to ensure consistency.

**Command**:
```bash
dotnet run compare --baseline-dir ./subsidiary1 --live-dir ./subsidiary2 --output-dir ./subsidiary-comparison
```

**Expected Output**:
- A report of policy differences between the two subsidiaries.

## Advanced Use Cases

### Policy Templates

**Description**: Use policy templates to standardize configurations across tenants.

**Command**:
```bash
dotnet run baseline --template-dir ./templates --output-dir ./standard-baseline
```

**Expected Output**:
- A directory named `standard-baseline` containing JSON files based on the provided templates.

### Lifecycle Management

**Description**: Automate the lifecycle of policies, from creation to retirement.

**Example**: Script to create, update, and delete policies

```bash
# Create a new policy
dotnet run create --policy-file ./new-policy.json

# Update an existing policy
dotnet run update --policy-file ./updated-policy.json

# Delete a policy
dotnet run delete --policy-id policy-id
```

**Expected Output**:
- Successful creation, update, and deletion of policies based on the provided JSON files.

## Troubleshooting

### Common Issues

**Issue**: Authentication failure

**Solution**:
- Verify that the environment variables for Azure credentials are correctly set.
- Check that the app registration has the necessary permissions.

**Issue**: Export timeout

**Solution**:
- Increase the timeout value using the `--timeout` option.
- Ensure that the tenant has a reasonable number of policies.

**Issue**: Policy comparison differences

**Solution**:
- Use the `--matching-strategy` option to customize the comparison logic.
- Review the comparison report to understand the differences.

### Diagnostic Commands

**Check Azure Credentials**:
```bash
echo $AZURE_TENANT_ID
echo $AZURE_CLIENT_ID
echo $AZURE_CLIENT_SECRET
```

**Run Specific Test**:
```bash
dotnet test --filter "FullyQualifiedName=Namespace.ClassName.MethodName"
```

**Check Test Coverage**:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Performance Considerations

- Optimize database queries and reduce unnecessary data processing.
- Use asynchronous programming for I/O-bound operations.
- Profile the application to identify performance bottlenecks.

## Security Guidelines

### Code Security
- **Never commit secrets** to version control
- **Use environment variables** for configuration
- **Validate all inputs** properly
- **Handle sensitive data** carefully
- **Follow principle of least privilege**

### Azure Security
- **Use service principals** for authentication
- **Rotate secrets regularly**
- **Monitor API usage**
- **Follow Azure security best practices**

## Conclusion

These examples and use cases demonstrate how to leverage CA_Scanner for various scenarios. By following these guidelines, you can effectively manage and analyze Conditional Access policies in your Microsoft 365 environment.

For more detailed information, refer to the [API Documentation](API_REFERENCE.md) and [Advanced Features](ADVANCED_FEATURES.md).


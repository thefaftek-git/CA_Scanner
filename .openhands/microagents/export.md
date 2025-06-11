---
triggers:
  - export
  - baseline
  - "baseline generation"
  - "policy export"
  - json
  - "data export"
  - anonymize
  - "reference policies"
---

# Export/Baseline Microagent

This microagent provides specialized guidance for policy export, baseline generation, and data management tasks within the CA_Scanner project.

## Export and Baseline Architecture

### Core Service
- **BaselineGenerationService.cs**: Main baseline generation logic and policy processing
- **Export Functionality**: Built into main `Program.cs` with command-line interface
- **Output Management**: File generation, directory structure, and naming conventions
- **Data Processing**: Filtering, anonymization, and formatting options

### Key Export Commands

#### Basic Export (Default Mode)
```bash
# Default export - backward compatible
dotnet run

# Explicit export command
dotnet run export

# Export with custom output file
dotnet run export --output my-policies.json
```

#### Baseline Generation Mode
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

## Export vs Baseline Generation

### Export Mode
- **Purpose**: Single JSON file with all policies and metadata
- **Output**: Timestamped filename (e.g., `ConditionalAccessPolicies_20250530_164523.json`)
- **Structure**: Complete export with tenant information, policy count, timestamp
- **Use Case**: Data archival, manual analysis, one-time snapshots

### Baseline Mode
- **Purpose**: Individual reference files for each policy
- **Output**: Separate JSON files per policy in specified directory
- **Structure**: Individual policy files compatible with comparison mode
- **Use Case**: Version control, CI/CD baselines, policy library creation

## Output File Structures

### Export File Format
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
      "Conditions": { ... },
      "GrantControls": { ... },
      "SessionControls": { ... }
    }
  ]
}
```

### Baseline File Format
```json
{
  "Id": "policy-id",
  "DisplayName": "Policy Name",
  "State": "Enabled",
  "Conditions": { ... },
  "GrantControls": { ... },
  "SessionControls": { ... }
}
```

## Filtering and Processing Options

### Policy Filtering
- **--filter-enabled-only**: Export only policies with State="Enabled"
- **--policy-names**: Export specific policies by name (space or comma-separated)
- **State-based filtering**: Automatically exclude certain policy states if needed

### Data Processing
- **--anonymize**: Remove tenant-specific identifiers and sensitive data
  - Removes or masks: Tenant IDs, Policy IDs, timestamps, user/group references
  - Preserves: Policy logic, conditions, controls, functional configuration
- **Formatting**: Consistent JSON formatting and structure
- **Validation**: Ensure exported data is valid and complete

## Directory and File Management

### Default Directories
- **Export**: Current working directory with timestamped filename
- **Baseline**: `./reference-policies/` directory
- **Custom**: User-specified directories via command-line options

### File Naming Conventions
- **Export**: `ConditionalAccessPolicies_YYYYMMDD_HHMMSS.json`
- **Baseline**: Individual files named after policy DisplayName (sanitized)
- **Sanitization**: Remove special characters, spaces replaced with underscores

## Common Export Scenarios

### Production Backup
```bash
# Full tenant backup with timestamp
dotnet run export --output ./backups/prod-backup-$(date +%Y%m%d).json
```

### Development Baseline Creation
```bash
# Create baseline for development environment
dotnet run baseline --output-dir ./dev-baseline --filter-enabled-only
```

### Anonymized Policy Library
```bash
# Create anonymized reference policies for sharing
dotnet run baseline --output-dir ./policy-library --anonymize
```

### Specific Policy Export
```bash
# Export only MFA-related policies
dotnet run baseline --policy-names "Require MFA" "MFA for Admins" --output-dir ./mfa-policies
```

## Integration with Other Features

### Comparison Integration
- **Baseline Output**: Compatible with comparison mode `--reference-dir`
- **Export Output**: Can be used with comparison mode `--entra-file`
- **Version Control**: Baseline files suitable for Git tracking

### CI/CD Integration
- **Automated Baselines**: Generate baselines in CI/CD pipelines
- **Environment Sync**: Export from one environment, import to another
- **Policy Drift Detection**: Use baselines for automated drift detection

## Testing Export Features

### Unit Tests
- **BaselineGenerationServiceTests.cs**: Core baseline generation logic validation
- **Integration Tests**: End-to-end export and baseline generation workflows
- **File System Tests**: Verify file creation, naming, and content validation
- **Filtering Tests**: Validate filtering logic and anonymization functionality

### Test Scenarios
- **Different Policy Configurations**: Various conditional access settings and complexity levels
- **Edge Cases**: Empty policies, malformed data, missing fields, special characters in names
- **Format Validation**: JSON structure integrity and schema compliance
- **Performance Tests**: Large policy set handling and memory usage optimization

## Performance Considerations

### Large Tenants
- **Scalability**: Handle tenants with many policies efficiently
- **Parallel Processing**: Optimize for bulk policy processing where applicable
- **Memory Usage**: Manage memory consumption for large exports
- **File I/O**: Efficient file writing and directory management

### Processing Optimization
- **Batch Operations**: Group related operations for better performance
- **Progress Indicators**: Real-time feedback for long-running operations
- **Error Recovery**: Robust error handling and retry mechanisms
- **Resource Management**: Proper disposal of resources and connections

## Security and Privacy

### Sensitive Data Handling
- **Tenant Information**: Handle tenant IDs and organizational data carefully
- **User References**: Protect user and group identifiers in exports
- **Timestamps**: Consider timezone and privacy implications
- **Anonymization**: Ensure anonymized data cannot be reverse-engineered

### Best Practices
- **Access Control**: Restrict access to exported files with appropriate permissions
- **Storage Security**: Use secure storage locations for exported data
- **Data Retention**: Implement appropriate data retention policies
- **Audit Logging**: Log export activities for compliance and security monitoring

## Troubleshooting Export Issues

### Common Problems

#### Permission Errors
**Symptoms**: Access denied when creating files or directories
**Causes**: 
- Insufficient file system permissions
- Directory doesn't exist or is read-only
- File is locked by another process

**Solutions**:
```bash
# Check directory permissions
ls -la ./output-directory/
chmod 755 ./output-directory/

# Verify directory exists and is writable
mkdir -p ./output-directory
touch ./output-directory/test-file && rm ./output-directory/test-file
```

#### Large Exports
**Symptoms**: Out of memory errors or extremely slow processing
**Causes**:
- Insufficient system memory
- Large policy sets with complex configurations
- Inefficient processing algorithms

**Solutions**:
```bash
# Monitor memory usage during export
dotnet run baseline --output-dir ./test --verbose &
top -p $!

# Process smaller subsets
dotnet run baseline --policy-names "Policy 1" "Policy 2" --output-dir ./subset
```

#### Network Issues
**Symptoms**: Authentication failures or API timeouts
**Causes**:
- Azure API connectivity problems
- Expired credentials or insufficient permissions
- Network proxy or firewall restrictions

**Solutions**:
```bash
# Verify environment variables
echo "Tenant ID: ${AZURE_TENANT_ID:0:4}***"
echo "Client ID: ${AZURE_CLIENT_ID:0:4}***"

# Test basic connectivity
curl -s https://graph.microsoft.com/v1.0/ > /dev/null && echo "Graph API accessible"
```

#### File Conflicts
**Symptoms**: Existing files not overwritten or naming conflicts
**Causes**:
- Files already exist in target directory
- Duplicate policy names creating filename conflicts
- Insufficient permissions to overwrite existing files

**Solutions**:
```bash
# Clear existing files
rm -rf ./output-directory/*

# Use unique output directory
dotnet run baseline --output-dir ./baseline-$(date +%Y%m%d-%H%M%S)
```

### Debug Techniques
```bash
# Verbose output for troubleshooting
dotnet run export --verbose

# Test with small subset
dotnet run baseline --policy-names "Test Policy" --output-dir ./test

# Validate output files
find ./output-directory -name "*.json" -exec jq . {} \; > /dev/null
```

## Data Quality and Validation

### Completeness Checks
- **Policy Count Verification**: Ensure all expected policies are exported
- **Field Validation**: Verify all required fields are present and populated
- **Cross-Reference Validation**: Compare exported data against source for accuracy

### Integrity Validation
- **JSON Structure**: Validate JSON syntax and schema compliance
- **Required Fields**: Ensure all mandatory policy fields are present
- **Data Type Validation**: Verify field values match expected data types
- **Consistency Checks**: Validate internal data consistency and relationships

### Accuracy Verification
```bash
# Validate JSON structure
find ./baseline-output -name "*.json" -exec jq empty {} \;

# Check for required fields
grep -r "DisplayName" ./baseline-output/ | wc -l

# Compare file count with expected policies
ls ./baseline-output/*.json | wc -l
```

## Version Control Integration

### Baseline Tracking
- **Git Integration**: Store baseline files in version control for change tracking
- **Diff Analysis**: Use Git diff to analyze policy changes over time
- **Branch Strategy**: Separate baselines for different environments and versions
- **Merge Conflicts**: Handle conflicts when baselines change between branches

### Git Workflow Examples
```bash
# Add baseline files to version control
git add ./baseline-policies/
git commit -m "Update policy baseline - $(date +%Y-%m-%d)"

# Compare baselines across branches
git diff main..feature-branch -- ./baseline-policies/

# Track baseline changes over time
git log --oneline -- ./baseline-policies/
```

### Change Management
- **Baseline Updates**: Regular updates to reflect approved policy changes
- **Review Process**: Peer review for baseline modifications
- **Documentation**: Document reasons for baseline changes
- **Rollback Capability**: Ability to revert to previous baseline versions

## Advanced Usage Patterns

### Pipeline Integration
```bash
# Generate baseline in CI/CD pipeline
dotnet run baseline --output-dir ./artifacts/baseline --anonymize --filter-enabled-only

# Export for backup automation
dotnet run export --output ./backups/$(date +%Y%m%d)/policies.json
```

### Multi-Environment Management
```bash
# Export from production
AZURE_TENANT_ID=$PROD_TENANT dotnet run baseline --output-dir ./prod-baseline

# Export from development
AZURE_TENANT_ID=$DEV_TENANT dotnet run baseline --output-dir ./dev-baseline

# Compare environments
dotnet run compare --reference-dir ./prod-baseline --entra-file ./dev-baseline-export.json
```

### Policy Library Creation
```bash
# Create anonymized policy library for sharing
dotnet run baseline \
  --output-dir ./policy-library \
  --anonymize \
  --filter-enabled-only

# Organize by policy type
mkdir -p ./policy-library/{mfa,device,location}
# Manual organization of generated files by policy type
```

## Benefits for OpenHands

This microagent helps OpenHands understand and effectively use the export and baseline generation features by providing:

- **Clear Mode Distinction**: Understanding when to use export vs baseline generation
- **Command Examples**: Practical command-line examples for common scenarios
- **Integration Guidance**: How export features integrate with comparison and CI/CD workflows
- **Troubleshooting Support**: Common issues and resolution strategies
- **Security Awareness**: Best practices for handling sensitive policy data
- **Performance Optimization**: Techniques for handling large-scale exports efficiently
- **Quality Assurance**: Methods for validating export data integrity and completeness

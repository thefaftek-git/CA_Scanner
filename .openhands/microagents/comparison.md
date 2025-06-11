---
triggers:
  - compare
  - comparison
  - diff
  - differences
  - "policy comparison"
  - "cross format"
  - matching
  - "policy diff"
  - analysis
---

# Policy Comparison Microagent

This microagent provides specialized guidance for policy comparison, difference analysis, and cross-format comparison tasks within the CA_Scanner project.

## Comparison Architecture Overview

### Core Services
- **PolicyComparisonService.cs**: Main comparison logic for same-format comparisons
- **CrossFormatPolicyComparisonService.cs**: Handles JSON vs Terraform comparisons
- **ReportGenerationService.cs**: Generates comparison reports in multiple output formats
- **CrossFormatReportGenerationService.cs**: Specialized reporting for cross-format comparisons
- **Models/**: Comparison result models and data transfer objects

### Key Comparison Services

#### 1. PolicyComparisonService.cs
Compare policies within same format (JSON-to-JSON). This service handles:
- Policy matching using different strategies
- Field-level difference detection
- Result categorization and analysis
- Performance optimization for large policy sets

#### 2. CrossFormatPolicyComparisonService.cs
Compare across formats (JSON vs Terraform). Features include:
- Automatic format normalization
- Semantic equivalence detection
- Bidirectional comparison support
- Field value mapping between formats

#### 3. ReportGenerationService.cs
Generate comprehensive comparison reports in multiple formats:
- Console output for human readability
- JSON for programmatic consumption
- HTML for rich web-based reports
- CSV for spreadsheet analysis
- Pipeline-JSON for CI/CD integration

#### 4. CrossFormatReportGenerationService.cs
Specialized reporting for cross-format scenarios:
- Format-aware difference highlighting
- Mapping explanation in reports
- Cross-reference documentation
- Enhanced visualization for format differences

## Comparison Modes and Commands

### Basic Comparison
```bash
# Compare live Entra policies against reference files
dotnet run compare --reference-dir ./reference-policies

# Compare using previously exported file
dotnet run compare --reference-dir ./reference-policies --entra-file exported-policies.json
```

### Advanced Comparison Options
```bash
# Custom output and formats
dotnet run compare \
  --reference-dir ./reference-policies \
  --output-dir ./reports \
  --formats console json html csv

# Different matching strategies
dotnet run compare --reference-dir ./reference-policies --matching ByName --case-sensitive true
dotnet run compare --reference-dir ./reference-policies --matching ById

# Custom mapping for complex scenarios
dotnet run compare --reference-dir ./reference-policies --matching CustomMapping --mapping-file ./custom-mapping.json
```

## Matching Strategies

### ByName (Default)
- Matches policies by DisplayName property
- Case-insensitive by default
- Configurable case sensitivity with `--case-sensitive` flag
- Best for human-readable policy management
- Handles slight naming variations

### ById
- Matches policies by unique policy identifier
- Exact match required for successful pairing
- Best for programmatic scenarios
- Ensures precise policy tracking across environments
- Immune to naming changes

### CustomMapping
- Uses external mapping file for complex scenarios
- Supports one-to-many and many-to-one relationships
- Handles policy splits and mergers
- Custom business logic for matching
- Advanced scenario support

### Case Sensitivity Control
```bash
# Case-sensitive matching (exact name match required)
dotnet run compare --reference-dir ./policies --case-sensitive true

# Case-insensitive matching (default)
dotnet run compare --reference-dir ./policies --case-sensitive false
```

## Comparison Output Formats

### Console
- **Purpose**: Human-readable output to terminal
- **Use Case**: Interactive analysis and troubleshooting
- **Features**: Colored output, summary statistics, progress indicators
- **Best For**: Development and debugging scenarios

### JSON
- **Purpose**: Structured data for programmatic consumption
- **Use Case**: API integration and automated processing
- **Features**: Complete difference data, machine-readable format
- **Best For**: CI/CD pipelines and automation scripts

### HTML
- **Purpose**: Rich web-based reports with styling
- **Use Case**: Stakeholder presentations and documentation
- **Features**: Interactive elements, visual diff highlighting, responsive design
- **Best For**: Executive reporting and team collaboration

### CSV
- **Purpose**: Tabular data for spreadsheet analysis
- **Use Case**: Data analysis and business intelligence
- **Features**: Flat structure, Excel compatibility, pivot table ready
- **Best For**: Business analysis and reporting workflows

### Pipeline-JSON
- **Purpose**: Specialized format for CI/CD integration
- **Use Case**: Automated deployment and validation
- **Features**: Exit codes, status indicators, action recommendations
- **Best For**: DevOps automation and infrastructure validation

## Understanding Comparison Results

### Result Categories

#### 1. Policies Only in Entra
- **Description**: Exist in live tenant but not in reference
- **Implications**: New policies created outside of standard process
- **Actions**: Review for compliance, add to reference if approved
- **Risk Level**: Medium - potential policy drift

#### 2. Policies Only in Reference
- **Description**: Exist in reference but not in live tenant
- **Implications**: Policies may have been deleted or not yet deployed
- **Actions**: Investigate deletion reason, deploy if needed
- **Risk Level**: High - missing security controls

#### 3. Matching Policies
- **Description**: Found in both sources with same identifier
- **Implications**: Successful policy pairing for comparison
- **Actions**: Analyze differences for configuration drift
- **Risk Level**: Variable - depends on differences found

#### 4. Policies with Differences
- **Description**: Found in both but with configuration differences
- **Implications**: Configuration drift or intentional changes
- **Actions**: Review changes, update reference or revert live
- **Risk Level**: Variable - depends on nature of differences

#### 5. Identical Policies
- **Description**: Perfect matches between sources
- **Implications**: Proper synchronization maintained
- **Actions**: No action required
- **Risk Level**: Low - configuration aligned

### Difference Types

#### Field Value Changes
- **Description**: Different values for same field
- **Examples**: Different user/group assignments, modified conditions
- **Detection**: Direct value comparison
- **Reporting**: Before/after value display

#### Missing Fields
- **Description**: Field exists in one source but not the other
- **Examples**: New conditions added, optional fields removed
- **Detection**: Field presence analysis
- **Reporting**: Missing field identification with source indication

#### Structural Differences
- **Description**: Different object structures between sources
- **Examples**: Array vs single value, nested object changes
- **Detection**: Schema comparison
- **Reporting**: Structure visualization with hierarchy mapping

#### Format Differences
- **Description**: Same semantic meaning, different representation
- **Examples**: Numeric codes vs string values
- **Detection**: Automatic normalization and mapping
- **Reporting**: Format-aware display with equivalence notation

## Cross-Format Comparison Features

### JSON vs Terraform Comparison
- **Bidirectional Support**: Compare JSON→Terraform or Terraform→JSON
- **Automatic Normalization**: Handle format-specific value representations
- **Semantic Equivalence**: Understand that different formats can express same intent
- **Field Mapping**: Translate field names and structures between formats

### Automatic Normalization
The comparison engine handles format differences transparently:
- **Value Type Conversion**: Numeric codes to string equivalents
- **Array Normalization**: Single values vs arrays
- **Case Normalization**: Different casing conventions
- **Null Handling**: Different null/empty representations

### Semantic Equivalence Examples
```
# BuiltInControls
JSON:      [1]
Terraform: ["mfa"]
Result:    Equivalent (both require MFA)

# ClientAppTypes  
JSON:      [0,1]
Terraform: ["browser", "mobileAppsAndDesktopClients"]
Result:    Equivalent (same client types)

# Risk Levels
JSON:      ["high", "medium"]
Terraform: ["high", "medium"]
Result:    Identical (direct string match)
```

### Field Value Mappings in Comparisons

#### BuiltInControls Mapping
- **JSON Format**: Numeric codes `[1, 2, 3]`
- **Terraform Format**: String values `["mfa", "compliantDevice", "domainJoinedDevice"]`
- **Comparison Logic**: Automatic translation between formats
- **Equivalence Rules**: `1 ↔ "mfa"`, `2 ↔ "compliantDevice"`, etc.

#### ClientAppTypes Mapping
- **JSON Format**: Numeric codes `[0, 1, 2, 3]`
- **Terraform Format**: String descriptors `["browser", "mobileAppsAndDesktopClients", "exchangeActiveSync", "other"]`
- **Comparison Logic**: Bidirectional mapping with validation
- **Equivalence Rules**: Code-to-string mapping with full coverage

#### Risk Levels and State Values
- **Both Formats**: Direct string comparison
- **Values**: `"enabled"`, `"disabled"`, `"low"`, `"medium"`, `"high"`
- **Comparison Logic**: Case-insensitive string matching
- **Special Handling**: Null vs empty string normalization

## Comparison Report Structure

### Summary Section
```
Policy Comparison Summary
========================
Total Entra Policies: 45
Total Reference Policies: 42
Policies Only in Entra: 5
Policies Only in Reference: 2
Matching Policies: 40
  ├── Identical: 32
  └── With Differences: 8
Overall Drift Status: MODERATE
```

### Detailed Differences
```
Policy: "Block Legacy Authentication"
Status: DIFFERENCES_FOUND
├── Field: state
│   ├── Entra: "enabled"
│   └── Reference: "disabled"
├── Field: conditions.users.includeUsers
│   ├── Entra: ["All"]
│   └── Reference: ["user1@domain.com", "user2@domain.com"]
└── Impact: HIGH (security control disabled)
```

### Identical Policies
```
Identical Policies (32 found)
=============================
✓ "Require MFA for Admins"
✓ "Block Risky Sign-ins"
✓ "Compliant Device Required"
✓ [... additional policies ...]
```

## Common Comparison Scenarios

### Baseline Validation
```bash
# Compare current tenant against established baseline
dotnet run compare --reference-dir ./policy-baseline
```
**Use Case**: Regular compliance checking against approved configuration
**Frequency**: Daily/weekly automated runs
**Output**: Focus on drift detection and compliance violations

### Environment Sync Verification
```bash
# Verify dev/staging matches production baseline
dotnet run compare --reference-dir ./prod-baseline --entra-file dev-export.json
```
**Use Case**: Environment promotion validation
**Frequency**: Before production deployments
**Output**: Environment-specific differences and sync status

### Infrastructure as Code Validation
```bash
# Compare live policies against Terraform definitions
dotnet run compare --reference-dir ./terraform-policies
```
**Use Case**: IaC compliance and drift detection
**Frequency**: Continuous integration pipeline
**Output**: Infrastructure alignment and deployment verification

### Change Impact Analysis
```bash
# Analyze specific policy changes with detailed reporting
dotnet run compare \
  --reference-dir ./before-change \
  --entra-file ./after-change.json \
  --formats html json
```
**Use Case**: Pre/post change validation
**Frequency**: Before and after significant changes
**Output**: Detailed impact assessment and risk analysis

## Testing Comparison Features

### Unit Tests
- **PolicyComparisonServiceTests.cs**: Core comparison logic validation
- **CrossFormatPolicyComparisonServiceTests.cs**: Cross-format comparison testing
- **Test Coverage**: Field-level differences, matching strategies, edge cases
- **Mock Data**: Representative test policies for various scenarios

### Integration Tests  
- **CrossFormatPolicyComparisonServiceIntegrationTests.cs**: End-to-end comparison workflows
- **Test Scenarios**: Real-world policy configurations and complex comparisons
- **Performance Tests**: Large policy set handling and optimization validation
- **Regression Tests**: Historical bug prevention and feature stability

### Test Scenarios
- **Different Policy Configurations**: Various conditional access settings
- **Edge Cases**: Empty policies, malformed data, missing fields
- **Format Combinations**: All supported input/output format pairs
- **Error Conditions**: Network failures, authentication issues, data corruption

### Mock Data
- **Representative Policies**: Real-world conditional access configurations
- **Test Variations**: Different complexity levels and configuration patterns
- **Cross-Format Examples**: Same policies in JSON and Terraform formats
- **Boundary Cases**: Maximum/minimum values and edge conditions

## Performance Considerations

### Large Policy Sets
- **Optimization Strategies**: Parallel processing for independent comparisons
- **Memory Management**: Streaming for large datasets and efficient object handling
- **Progress Indicators**: Real-time feedback for long-running operations
- **Batching**: Intelligent grouping for optimal processing throughput

### Parallel Processing
- **Thread Safety**: Concurrent comparison operations with proper synchronization
- **Resource Management**: CPU and memory usage optimization
- **Scalability**: Horizontal scaling support for enterprise environments
- **Load Balancing**: Work distribution across available processing cores

### Memory Usage
- **Efficient Algorithms**: Memory-conscious comparison implementations
- **Garbage Collection**: Proactive memory cleanup and object disposal
- **Streaming**: Large file processing without full memory loading
- **Caching**: Strategic caching for frequently accessed comparison data

### Progress Indicators
```bash
# Example progress output
Comparing policies... [████████████████████] 100% (45/45)
├── Processed: 45 policies
├── Matched: 40 policies  
├── Differences found: 8 policies
└── Elapsed: 00:02:34
```

## Troubleshooting Comparison Issues

### Common Problems

#### No Matches Found
**Symptoms**: All policies reported as "only in source" or "only in reference"
**Causes**: 
- Incorrect matching strategy selection
- Case sensitivity mismatch
- Policy naming convention differences
- Corrupted or empty reference files

**Solutions**:
```bash
# Try different matching strategies
dotnet run compare --reference-dir ./reference --matching ById
dotnet run compare --reference-dir ./reference --matching ByName --case-sensitive false

# Verify reference files
ls -la ./reference-policies/
cat ./reference-policies/sample-policy.json
```

#### Format Errors
**Symptoms**: JSON parsing errors or invalid format messages
**Causes**:
- Malformed JSON files
- Unsupported file formats
- Encoding issues
- Schema mismatches

**Solutions**:
```bash
# Validate JSON format
cat policy.json | jq .
jsonlint policy.json

# Check file encoding
file policy.json
head -c 20 policy.json
```

#### Performance Issues
**Symptoms**: Slow comparison operations or memory exhaustion
**Causes**:
- Large policy sets
- Inefficient matching algorithms
- Memory leaks
- Resource contention

**Solutions**:
```bash
# Monitor resource usage
dotnet run compare --reference-dir ./reference --verbose
top -p $(pgrep -f ConditionalAccessExporter)

# Use performance profiling
dotnet run --configuration Release compare --reference-dir ./reference
```

#### Unexpected Differences
**Symptoms**: Differences reported for policies that should be identical
**Causes**:
- Format-specific value representations
- Field ordering differences
- Whitespace or encoding variations
- Timezone or date format differences

**Solutions**:
```bash
# Enable detailed difference reporting
dotnet run compare --reference-dir ./reference --formats json --verbose

# Compare specific policies
dotnet run compare --reference-dir ./reference --policy-names "Specific Policy Name"
```

### Debug Techniques

#### Verbose Output
```bash
# Enable comprehensive logging
dotnet run compare --reference-dir ./reference --formats console --verbose
```
**Benefits**: Detailed operation logging, matching process visibility, error context

#### Single Policy Analysis
```bash
# Focus on specific policy for troubleshooting
dotnet run compare --reference-dir ./reference --policy-names "Problematic Policy Name"
```
**Benefits**: Isolated problem analysis, detailed field-level inspection

#### Format-Specific Debugging
```bash
# Test cross-format comparison with verbose output
dotnet run compare --reference-dir ./terraform-policies --formats json --verbose --matching ByName
```
**Benefits**: Format conversion visibility, mapping verification, semantic equivalence validation

#### Performance Profiling
```bash
# Profile comparison performance
dotnet run --configuration Release compare --reference-dir ./large-reference-set --verbose
```
**Benefits**: Performance bottleneck identification, memory usage analysis, optimization opportunities

## Advanced Comparison Features

### Custom Matching Logic
- **Plugin Architecture**: Extensible matching algorithm framework
- **Business Rules**: Custom logic for organization-specific matching requirements
- **Complex Scenarios**: Multi-criteria matching with weighted scoring
- **Fallback Strategies**: Hierarchical matching with graceful degradation

### Filtering Options
```bash
# Compare only specific policy types
dotnet run compare --reference-dir ./reference --policy-types "ConditionalAccess"

# Filter by policy state
dotnet run compare --reference-dir ./reference --states "enabled"

# Exclude test policies
dotnet run compare --reference-dir ./reference --exclude-patterns "*test*,*dev*"
```

### Threshold-Based Analysis
```bash
# Set acceptable difference thresholds
dotnet run compare --reference-dir ./reference --difference-threshold 5

# Configure change significance levels
dotnet run compare --reference-dir ./reference --critical-fields "state,conditions.users"
```

### Change Type Classification
- **Critical Changes**: Security-impacting modifications requiring immediate attention
- **Configuration Changes**: Non-security modifications with business impact
- **Cosmetic Changes**: Display name or description updates with minimal impact
- **Technical Changes**: Internal field updates with no functional impact

## Integration with Other Features

### CI/CD Pipelines
```yaml
# Example Azure DevOps pipeline integration
- task: DotNetCoreCLI@2
  displayName: 'Compare Policies'
  inputs:
    command: 'run'
    projects: 'ConditionalAccessExporter'
    arguments: 'compare --reference-dir $(Build.SourcesDirectory)/reference-policies --formats pipeline-json --output-dir $(Build.ArtifactStagingDirectory)'
```

### Reporting Tools
- **Integration APIs**: RESTful endpoints for external reporting system integration
- **Data Export**: Multiple format support for business intelligence tools
- **Scheduling**: Automated comparison runs with result distribution
- **Alerting**: Threshold-based notifications for significant changes

### Alerting Systems
```bash
# Example alerting integration
dotnet run compare --reference-dir ./reference --alert-on-differences --webhook-url https://alerts.company.com/ca-drift
```

### Audit Trails
- **Change History**: Temporal comparison results for trend analysis
- **Compliance Reporting**: Historical compliance status tracking
- **Change Attribution**: Policy modification source identification
- **Rollback Support**: Historical state restoration capabilities

## Benefits for OpenHands

This microagent enables OpenHands to:

### Understand Architecture
- **Service Relationships**: Clear understanding of comparison service interactions
- **Data Flow**: Knowledge of how policies move through comparison pipeline
- **Extension Points**: Identification of customization and enhancement opportunities
- **Integration Patterns**: Best practices for connecting with external systems

### Choose Appropriate Strategies
- **Matching Strategy Selection**: Guidance for optimal matching approach based on scenario
- **Performance Optimization**: Techniques for handling large-scale comparisons efficiently
- **Format Handling**: Cross-format comparison best practices and limitations
- **Error Recovery**: Robust error handling and recovery strategies

### Generate Comprehensive Reports
- **Format Selection**: Choosing appropriate output format for target audience
- **Content Customization**: Tailoring report content for specific use cases
- **Visual Presentation**: Effective difference visualization and highlighting
- **Actionable Insights**: Converting raw differences into actionable recommendations

### Handle Cross-Format Scenarios
- **Format Translation**: Understanding automatic conversion and normalization processes
- **Semantic Mapping**: Leveraging equivalent value mapping between formats
- **Bidirectional Support**: Utilizing both JSON→Terraform and Terraform→JSON comparisons
- **Field Correlation**: Understanding how fields correspond across different formats

### Interpret Results
- **Difference Classification**: Understanding the significance and impact of various change types
- **Risk Assessment**: Evaluating security and compliance implications of differences
- **Priority Assignment**: Focusing attention on most critical differences first
- **Trend Analysis**: Identifying patterns in policy drift over time

### Troubleshoot Issues
- **Problem Diagnosis**: Systematic approach to identifying comparison issues
- **Performance Optimization**: Techniques for improving comparison speed and reliability
- **Error Resolution**: Step-by-step debugging processes for common problems
- **Validation Techniques**: Methods for verifying comparison accuracy and completeness

### Integrate with Workflows
- **CI/CD Integration**: Seamless incorporation into deployment and validation pipelines
- **Automation Support**: Programmatic access to comparison functionality
- **Monitoring Integration**: Connection with existing monitoring and alerting infrastructure
- **Reporting Workflows**: Integration with business reporting and compliance processes

This comprehensive guidance ensures that OpenHands can effectively leverage all aspects of the CA_Scanner comparison functionality, from basic policy comparisons to advanced cross-format analysis and enterprise-scale automation scenarios.

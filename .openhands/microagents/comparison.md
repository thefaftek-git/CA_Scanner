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
- **PolicyComparisonService.cs** - Main comparison logic for policies within the same format
- **CrossFormatPolicyComparisonService.cs** - Handles JSON vs Terraform comparisons
- **ReportGenerationService.cs** - Generates comparison reports in multiple output formats
- **CrossFormatReportGenerationService.cs** - Specialized cross-format reporting
- **Models/** - Comparison result models and data structures

### Key Comparison Services

#### 1. PolicyComparisonService.cs
Compare policies within the same format (JSON-to-JSON). This service handles:
- Policy matching using various strategies
- Field-by-field difference detection
- Configuration change analysis
- Result categorization

#### 2. CrossFormatPolicyComparisonService.cs
Compare across different formats (JSON vs Terraform). Features include:
- Format normalization and semantic equivalence
- Automatic handling of format-specific differences
- Bidirectional comparison support
- Field value mapping between formats

#### 3. ReportGenerationService.cs
Generate comprehensive comparison reports in multiple formats:
- Console output for immediate feedback
- JSON for programmatic consumption
- HTML for rich web-based reports
- CSV for spreadsheet analysis

#### 4. CrossFormatReportGenerationService.cs
Specialized reporting for cross-format comparisons:
- Format-aware difference highlighting
- Semantic equivalence documentation
- Cross-reference mapping tables

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

# Verbose output for troubleshooting
dotnet run compare --reference-dir ./reference --formats console --verbose

# Single policy analysis
dotnet run compare --reference-dir ./reference --policy-names "Specific Policy Name"
```

## Matching Strategies

### Available Strategies
- **ByName**: Match policies by DisplayName (default, case-insensitive)
- **ById**: Match policies by unique policy ID
- **CustomMapping**: Use custom mapping file for complex scenarios

### Configuration Options
- **Case Sensitivity**: Control case-sensitive matching with `--case-sensitive`
- **Partial Matching**: Enable fuzzy matching for similar policy names
- **Custom Rules**: Define specific matching rules for complex environments

## Comparison Output Formats

### Console
Human-readable output to terminal:
- Summary statistics
- Color-coded differences
- Progress indicators
- Interactive navigation

### JSON
Structured data for programmatic consumption:
- Complete difference details
- Metadata and timestamps
- Machine-readable format
- API integration ready

### HTML
Rich web-based reports with styling:
- Interactive difference viewer
- Responsive design
- Export capabilities
- Visual charts and graphs

### CSV
Tabular data for spreadsheet analysis:
- Policy comparison matrix
- Difference summaries
- Statistical data
- Import into Excel/Google Sheets

### Pipeline-JSON
Specialized format for CI/CD integration:
- Build system compatible
- Status indicators
- Threshold-based results
- Automation-friendly structure

## Understanding Comparison Results

### Result Categories

#### 1. Policies Only in Entra
Policies that exist in the live tenant but not in reference files:
- New policies created since baseline
- Policies not included in reference set
- Potential configuration drift

#### 2. Policies Only in Reference
Policies that exist in reference files but not in live tenant:
- Policies removed from tenant
- Reference policies not yet deployed
- Missing required configurations

#### 3. Matching Policies
Policies found in both sources:
- Successful matches using selected strategy
- Available for difference analysis
- Baseline validation candidates

#### 4. Policies with Differences
Policies found in both sources but with configuration differences:
- Field value changes
- Structural modifications
- State changes (enabled/disabled)
- Critical vs non-critical differences

#### 5. Identical Policies
Perfect matches between sources:
- No configuration differences detected
- Successful baseline compliance
- Reference validation confirmed

### Difference Types

#### Field Value Changes
Different values for the same field:
- String value modifications
- Numeric changes
- Boolean state changes
- Array/list modifications

#### Missing Fields
Field exists in one source but not the other:
- New fields added to policies
- Deprecated fields removed
- Optional vs required field differences

#### Structural Differences
Different object structures:
- Nested object changes
- Array structure modifications
- Schema version differences

#### Format Differences
Numeric codes vs string values (handled automatically):
- Automatic normalization applied
- Semantic equivalence maintained
- Format-specific representations

## Cross-Format Comparison Features

### JSON vs Terraform
Compare exported JSON against Terraform configurations:
- Bidirectional comparison support
- Format-specific optimization
- Resource mapping and correlation

### Automatic Normalization
Handle format-specific differences automatically:
- Value type conversion
- Structure standardization
- Field name mapping

### Semantic Equivalence
Understand that different representations mean the same thing:
- `[1]` (JSON) ↔ `["mfa"]` (Terraform)
- `[0,1]` (JSON) ↔ `["browser", "mobileAppsAndDesktopClients"]` (Terraform)
- Numeric IDs vs string names

### Bidirectional Support
Compare in either direction:
- JSON→Terraform validation
- Terraform→JSON verification
- Cross-environment synchronization

## Field Value Mappings in Comparisons

The comparison engine automatically handles format differences:

### BuiltInControls
- **JSON**: `[1]` (numeric array)
- **Terraform**: `["mfa"]` (string array)
- **Mapping**: Automatic conversion between numeric codes and string values

### ClientAppTypes
- **JSON**: `[0,1]` (numeric array)
- **Terraform**: `["browser", "mobileAppsAndDesktopClients"]` (string array)
- **Mapping**: Bidirectional conversion with validation

### Risk Levels
- **Both Formats**: Direct string comparison
- **Values**: "low", "medium", "high"
- **Validation**: Consistent across formats

### State Values
- **Both Formats**: String comparison
- **Values**: "enabled", "disabled", "enabledForReportingButNotEnforced"
- **Consistency**: Maintained across formats

## Comparison Report Structure

### Summary Section
Provides high-level overview:
- **Total Policy Counts**: Count for each source
- **Category Breakdown**: Policies in each result category
- **Drift Assessment**: Overall compliance status
- **Change Statistics**: Quantified difference metrics

### Detailed Differences
In-depth policy-by-policy analysis:
- **Field-Level Changes**: Specific field modifications
- **Before/After Values**: Side-by-side comparisons
- **Change Classification**: Critical vs non-critical
- **Impact Assessment**: Business impact evaluation

### Identical Policies
Confirmation of successful matches:
- **Perfect Matches**: Policies with no differences
- **Validation Success**: Baseline compliance confirmation
- **Reference Accuracy**: Verified policy alignment

### Performance Metrics
- **Processing Time**: Comparison duration
- **Memory Usage**: Resource consumption
- **Policy Count**: Number of policies processed
- **Error Statistics**: Any processing issues

## Common Comparison Scenarios

### Baseline Validation
```bash
# Compare current tenant against established baseline
dotnet run compare --reference-dir ./policy-baseline
```
**Use Case**: Verify current tenant state matches approved baseline
**Output**: Compliance report with drift analysis

### Environment Sync Verification
```bash
# Verify dev/staging matches production baseline
dotnet run compare --reference-dir ./prod-baseline --entra-file dev-export.json
```
**Use Case**: Ensure environment consistency across dev/staging/prod
**Output**: Environment-specific compliance report

### Infrastructure as Code Validation
```bash
# Compare live policies against Terraform definitions
dotnet run compare --reference-dir ./terraform-policies
```
**Use Case**: Validate that live configuration matches IaC definitions
**Output**: IaC compliance and drift detection

### Change Impact Analysis
```bash
# Analyze specific policy changes with detailed reporting
dotnet run compare \
  --reference-dir ./before-change \
  --entra-file ./after-change.json \
  --formats html json
```
**Use Case**: Assess impact of proposed or implemented changes
**Output**: Detailed change analysis with impact assessment

## Testing Comparison Features

### Unit Tests
- **PolicyComparisonServiceTests.cs**: Core comparison logic validation
- **CrossFormatPolicyComparisonServiceTests.cs**: Cross-format functionality
- **ReportGenerationServiceTests.cs**: Output format validation
- **CrossFormatReportGenerationServiceTests.cs**: Specialized reporting tests

### Integration Tests
- **CrossFormatPolicyComparisonServiceIntegrationTests.cs**: End-to-end scenarios
- **Real-world policy configurations**: Representative test data
- **Format combination testing**: All supported format pairs
- **Performance benchmarking**: Large dataset handling

### Test Scenarios
- **Different Policy Configurations**: Various policy types and states
- **Edge Cases**: Unusual configurations and corner cases
- **Format Combinations**: All supported input/output format pairs
- **Error Conditions**: Invalid inputs and error handling

### Mock Data
- **Representative Test Policies**: Real-world policy examples
- **Various Comparison Scenarios**: Different difference types
- **Performance Test Data**: Large policy sets for scaling tests
- **Error Condition Data**: Invalid and edge-case configurations

## Performance Considerations

### Large Policy Sets
Optimize for tenants with many policies:
- **Batch Processing**: Handle large datasets efficiently
- **Memory Management**: Optimize memory usage for large comparisons
- **Streaming**: Process policies incrementally when possible
- **Parallel Processing**: Utilize multi-threading where appropriate

### Parallel Processing
Utilize multi-threading for performance:
- **Policy-Level Parallelism**: Compare multiple policies simultaneously
- **Field-Level Parallelism**: Parallel field comparison within policies
- **Report Generation**: Concurrent output format generation
- **I/O Operations**: Parallel file reading and writing

### Memory Usage
Handle large comparison operations efficiently:
- **Streaming Comparisons**: Avoid loading all data into memory
- **Garbage Collection**: Efficient object lifecycle management
- **Memory Profiling**: Monitor and optimize memory usage
- **Resource Cleanup**: Proper disposal of resources

### Progress Indicators
Show progress for long-running comparisons:
- **Real-time Updates**: Live progress feedback
- **ETA Calculation**: Estimated time to completion
- **Stage Indicators**: Show current processing stage
- **Cancellation Support**: Allow user to interrupt long operations

## Troubleshooting Comparison Issues

### Common Problems

#### No Matches Found
**Symptoms**: Comparison returns no matching policies
**Causes**:
- Incorrect matching strategy selection
- Case sensitivity mismatch
- Policy naming inconsistencies
- Invalid reference file paths

**Solutions**:
- Verify matching strategy (`ByName` vs `ById`)
- Check case sensitivity settings
- Review policy naming conventions
- Validate reference file existence and format

#### Format Errors
**Symptoms**: Errors reading reference files or generating output
**Causes**:
- Invalid JSON structure in reference files
- Unsupported file formats
- Corrupted reference data
- Output directory permissions

**Solutions**:
- Validate JSON syntax in reference files
- Verify supported input formats
- Check file integrity
- Ensure output directory write permissions

#### Performance Issues
**Symptoms**: Slow comparison performance or timeouts
**Causes**:
- Large policy count exceeding system resources
- Inefficient matching strategies
- Memory constraints
- Disk I/O bottlenecks

**Solutions**:
- Review policy count and system resources
- Optimize matching strategy selection
- Increase available memory
- Use faster storage or optimize I/O patterns

#### Unexpected Differences
**Symptoms**: Differences reported for apparently identical policies
**Causes**:
- Format-specific value representations
- Whitespace or encoding differences
- Semantic equivalence not recognized
- Field ordering differences

**Solutions**:
- Understand format-specific value mappings
- Review normalization settings
- Check semantic equivalence handling
- Verify field comparison logic

### Debug Techniques

#### Verbose Logging
Enable detailed logging for troubleshooting:
```bash
dotnet run compare --reference-dir ./reference --formats console --verbose
```
**Benefits**:
- Detailed processing steps
- Error context information
- Performance metrics
- Debug information

#### Single Policy Analysis
Focus on specific policies for detailed analysis:
```bash
dotnet run compare --reference-dir ./reference --policy-names "Specific Policy Name"
```
**Benefits**:
- Isolated problem analysis
- Detailed field-by-field comparison
- Simplified debugging context
- Faster iteration cycles

#### Output Format Testing
Test different output formats to isolate issues:
```bash
# Test each format individually
dotnet run compare --reference-dir ./reference --formats console
dotnet run compare --reference-dir ./reference --formats json
dotnet run compare --reference-dir ./reference --formats html
```

#### Reference File Validation
Validate reference files before comparison:
```bash
# Use JSON validation tools
jsonlint reference-file.json

# Check file structure
file reference-file.json
```

## Advanced Comparison Features

### Custom Matching Logic
Implement custom policy matching algorithms:
- **Fuzzy Matching**: Handle similar but not identical names
- **Multi-Field Matching**: Use combination of fields for matching
- **Weighted Scoring**: Assign importance to different matching criteria
- **Learning Algorithms**: Improve matching based on historical data

### Filtering Options
Compare only specific policy types or states:
- **Policy Type Filtering**: Focus on specific conditional access policy types
- **State Filtering**: Compare only enabled, disabled, or report-only policies
- **User/Group Filtering**: Limit comparison to policies affecting specific users/groups
- **Application Filtering**: Focus on policies for specific applications

### Threshold-Based Analysis
Set acceptable difference thresholds:
- **Change Magnitude**: Ignore minor differences below threshold
- **Critical vs Non-Critical**: Different thresholds for different change types
- **Business Impact**: Weight differences by business impact
- **Risk Assessment**: Threshold based on security risk levels

### Change Type Classification
Categorize changes by business impact:
- **Critical Changes**: Security-impacting modifications
- **Non-Critical Changes**: Minor configuration adjustments
- **Cosmetic Changes**: Display name or description changes
- **Structural Changes**: Policy architecture modifications

## Integration with Other Features

### CI/CD Pipelines
Use comparison results for automated policy drift detection:
- **Build Integration**: Include comparison in build processes
- **Deployment Validation**: Verify deployments match expectations
- **Automated Testing**: Compare test environments against baselines
- **Quality Gates**: Block deployments with significant drift

### Reporting Tools
Export comparison data for external analysis:
- **Business Intelligence**: Feed data into BI platforms
- **Dashboards**: Create real-time drift monitoring dashboards
- **Metrics Collection**: Aggregate comparison statistics over time
- **Trend Analysis**: Track policy drift trends

### Alerting Systems
Trigger alerts based on comparison results:
- **Drift Detection**: Alert on policy configuration drift
- **Threshold Breaches**: Alert when differences exceed thresholds
- **Critical Changes**: Immediate alerts for security-critical changes
- **Trend Alerts**: Alert on concerning drift trends

### Audit Trails
Maintain history of policy changes over time:
- **Change History**: Track all policy modifications
- **Comparison History**: Maintain historical comparison results
- **Compliance Reporting**: Generate compliance reports over time
- **Forensic Analysis**: Support incident investigation with historical data

## Benefits for OpenHands

This microagent helps OpenHands:

### Understanding Architecture
- **Service Relationships**: Understand how comparison services interact
- **Data Flow**: Comprehend how data flows through comparison processes
- **Architecture Decisions**: Make informed decisions about comparison implementations
- **Extensibility**: Understand how to extend comparison capabilities

### Choosing Strategies
- **Matching Strategy Selection**: Pick appropriate strategies for different scenarios
- **Performance Optimization**: Choose strategies that optimize for specific use cases
- **Accuracy vs Speed**: Balance accuracy requirements with performance needs
- **Use Case Mapping**: Map business requirements to technical strategies

### Report Generation
- **Format Selection**: Choose appropriate output formats for different audiences
- **Content Customization**: Customize reports for specific stakeholders
- **Integration Planning**: Plan integration with existing reporting infrastructure
- **Automation**: Automate report generation and distribution

### Cross-Format Support
- **Format Understanding**: Understand differences between JSON and Terraform formats
- **Mapping Comprehension**: Understand how field values map between formats
- **Validation Processes**: Implement cross-format validation workflows
- **Migration Support**: Support migration between formats

### Result Interpretation
- **Difference Classification**: Understand different types of differences and their significance
- **Impact Assessment**: Assess the business impact of detected differences
- **Action Planning**: Plan appropriate responses to comparison results
- **Trend Analysis**: Interpret trends in comparison results over time

### Troubleshooting
- **Problem Diagnosis**: Quickly diagnose common comparison issues
- **Performance Issues**: Identify and resolve performance bottlenecks
- **Error Resolution**: Resolve errors in comparison processes
- **Optimization**: Optimize comparison processes for specific environments

### Integration Planning
- **CI/CD Integration**: Plan integration with continuous integration/deployment pipelines
- **Automation Workflows**: Design automated policy management workflows
- **Monitoring Integration**: Integrate with monitoring and alerting systems
- **Tool Integration**: Integrate with other policy management tools

This comprehensive microagent provides OpenHands with the knowledge and guidance needed to effectively implement, use, and troubleshoot policy comparison features in the CA_Scanner project, enabling sophisticated policy drift detection, compliance validation, and change management workflows.

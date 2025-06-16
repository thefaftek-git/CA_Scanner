# Policy Validation Engine

## Overview

The Policy Validation Engine is a comprehensive framework for validating Azure Conditional Access policies against security best practices, organizational standards, and compliance requirements. It provides extensible rule-based validation with detailed reporting and actionable recommendations.

## Features

### ✅ Extensible Validation Framework
- **Rule-based architecture**: Easily add new validation rules
- **Category organization**: Security, Governance, Compliance, and Best Practices
- **Custom configuration**: Configure rules per organization needs
- **Severity levels**: Critical, High, Medium, Low, Info

### ✅ Built-in Security Rules
- **SEC001 - MFA Requirement**: Ensures high-risk policies require MFA
- **SEC002 - Device Compliance**: Validates device compliance requirements for privileged roles
- **SEC003 - Legacy Authentication**: Ensures legacy authentication protocols are properly blocked

### ✅ Governance & Compliance
- **GOV001 - Naming Conventions**: Enforces consistent policy naming patterns
- **Compliance Frameworks**: NIST, ISO27001, SOC2 assessments
- **Policy Recommendations**: Actionable improvement suggestions

### ✅ Comprehensive Reporting
- **Security Posture Scoring**: Overall security assessment (0-100)
- **Compliance Scoring**: Framework-specific compliance ratings
- **Risk Assessment**: Identified vulnerabilities and gaps
- **Executive Dashboards**: High-level summary reports

## Quick Start

### Basic Usage

```csharp
// Initialize the validation engine
var engine = new PolicyValidationEngine();

// Validate policies in a directory
var report = await engine.ValidateDirectoryAsync("/path/to/policies");

// Check results
Console.WriteLine($"Overall Security Score: {report.SecurityPostureScore}");
Console.WriteLine($"Policies Validated: {report.TotalPolicies}");
Console.WriteLine($"Issues Found: {report.TotalIssues}");
```

### Custom Configuration

```csharp
var options = new ValidationOptions
{
    StrictMode = true,
    IncludeRecommendations = true,
    DisabledRules = { "SEC001" }, // Disable specific rules
    Configuration = new Dictionary<string, object>
    {
        ["naming.patterns"] = new[] { @"^CA-\d{3}-.*" },
        ["mfa.strictMode"] = true
    }
};

var report = await engine.ValidateDirectoryAsync("/path/to/policies", options);
```

## Validation Rules

### Security Rules (SEC)

#### SEC001 - MFA Requirement Rule
**Purpose**: Ensures that policies targeting all users or high-risk scenarios require multi-factor authentication.

**Triggers When**:
- Policy targets "All" users without MFA requirement
- High-risk applications accessed without MFA
- Privileged roles not requiring MFA

**Example Policy Issue**:
```json
{
  "displayName": "Block High Risk Apps",
  "conditions": {
    "users": { "includeUsers": ["All"] },
    "applications": { "includeApplications": ["All"] }
  },
  "grantControls": {
    "builtInControls": ["compliantDevice"] // Missing MFA
  }
}
```

**Recommendation**: Add `"mfa"` or `"requireMultifactorAuthentication"` to `builtInControls`.

#### SEC002 - Device Compliance Rule
**Purpose**: Ensures policies targeting privileged roles include device compliance requirements.

**Triggers When**:
- Policies target admin roles without device compliance
- High-privilege access without device restrictions
- External access to sensitive applications

**Configuration Options**:
```json
{
  "deviceCompliance.privilegedRoles": [
    "62e90394-69f5-4237-9190-012177145e10", // Global Administrator
    "194ae4cb-b126-40b2-bd5b-6091b380977d"  // Security Administrator
  ],
  "deviceCompliance.strictMode": true
}
```

#### SEC003 - Legacy Authentication Rule
**Purpose**: Ensures legacy authentication protocols are properly blocked.

**Triggers When**:
- Policies target legacy auth types without blocking
- Exchange ActiveSync not properly restricted
- Other legacy protocols allowed access

**Example Compliant Policy**:
```json
{
  "displayName": "Block Legacy Authentication",
  "conditions": {
    "clientAppTypes": ["exchangeActiveSync", "other"]
  },
  "grantControls": {
    "builtInControls": ["block"]
  }
}
```

### Governance Rules (GOV)

#### GOV001 - Policy Naming Convention Rule
**Purpose**: Enforces consistent naming patterns for policy organization and management.

**Default Patterns**:
- `CA-###-Description` (e.g., "CA-001-Require MFA for All Users")
- `[Prefix]-###-Description` format

**Custom Configuration**:
```json
{
  "naming.patterns": [
    "^CA-\\d{3}-.*",
    "^PILOT-\\d{3}-.*",
    "^PROD-\\d{3}-.*"
  ],
  "naming.requirePrefix": true
}
```

## Compliance Frameworks

### NIST Cybersecurity Framework
- **PR.AC-1**: Identity and credential management
- **PR.AC-4**: Access permissions and authorizations
- **DE.CM-1**: Network monitoring and analysis

### ISO 27001
- **A.9.1.2**: Access to networks and network services
- **A.9.2.1**: User registration and de-registration
- **A.9.4.2**: Secure log-on procedures

### SOC 2 Type II
- **CC6.1**: Logical and physical access controls
- **CC6.2**: System access monitoring
- **CC6.3**: Access removal procedures

## Report Structure

### PolicyValidationReport
```csharp
public class PolicyValidationReport
{
    // Summary metrics
    public int TotalPolicies { get; set; }
    public int ValidPolicies { get; set; }
    public int InvalidPolicies { get; set; }
    public int TotalIssues { get; set; }
    
    // Scoring
    public double SecurityPostureScore { get; set; }      // 0-100
    public double OverallComplianceScore { get; set; }    // 0-100
    
    // Detailed assessments
    public SecurityAssessment SecurityAssessment { get; set; }
    public ComplianceAssessment ComplianceAssessment { get; set; }
    
    // Actionable insights
    public List<ValidationRecommendation> Recommendations { get; set; }
    public List<PolicyValidationResult> PolicyResults { get; set; }
}
```

### SecurityAssessment
```csharp
public class SecurityAssessment
{
    public double OverallScore { get; set; }
    public List<SecurityRisk> IdentifiedRisks { get; set; }
    public List<SecurityRecommendation> Recommendations { get; set; }
    public Dictionary<string, double> CategoryScores { get; set; }
}
```

## Adding Custom Rules

### 1. Create Rule Class

```csharp
public class CustomSecurityRule : BaseValidationRule
{
    public override string RuleId => "SEC004";
    public override string Name => "Custom Security Rule";
    public override string Description => "Validates custom security requirements";
    public override ValidationRuleCategory Category => ValidationRuleCategory.Security;
    public override ValidationSeverity DefaultSeverity => ValidationSeverity.High;

    public override async Task<ValidationResult> ValidateAsync(
        JObject policy, 
        ValidationContext context)
    {
        var result = new ValidationResult { RuleId = RuleId };
        
        // Implement validation logic
        var displayName = policy["displayName"]?.ToString();
        if (string.IsNullOrEmpty(displayName))
        {
            result.Issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Medium,
                Message = "Policy must have a display name",
                Field = "displayName",
                Suggestion = "Add a descriptive display name"
            });
        }
        
        result.Passed = !result.Issues.Any();
        return result;
    }
}
```

### 2. Register Rule

```csharp
var registry = new ValidationRuleRegistry();
registry.RegisterRule(new CustomSecurityRule());
```

## Advanced Configuration

### Rule-Specific Settings

```json
{
  "rules": {
    "SEC001": {
      "enabled": true,
      "severity": "High",
      "strictMode": true,
      "exemptPolicies": ["Emergency Access Policy"]
    },
    "GOV001": {
      "enabled": true,
      "patterns": ["^CA-\\d{3}-.*", "^EMERGENCY-.*"],
      "requireDescription": true
    }
  },
  "compliance": {
    "frameworks": ["NIST", "ISO27001", "SOC2"],
    "strictMode": false,
    "minimumScore": 85
  }
}
```

### Integration with Existing Services

```csharp
// Enhance existing PolicyValidationService
public class PolicyValidationService
{
    private readonly PolicyValidationEngine _validationEngine;
    
    public async Task<PolicyValidationReport> ValidateWithNewEngineAsync(
        string directoryPath,
        ValidationOptions options = null)
    {
        return await _validationEngine.ValidateDirectoryAsync(directoryPath, options);
    }
}
```

## Command Line Integration

The validation engine integrates with the existing CLI:

```bash
# Basic validation
./ConditionalAccessExporter validate --directory ./policies

# With custom rules
./ConditionalAccessExporter validate --directory ./policies --config validation-rules.json

# Compliance-specific
./ConditionalAccessExporter validate --directory ./policies --compliance NIST,ISO27001

# Generate reports
./ConditionalAccessExporter validate --directory ./policies --output-format json --output-file validation-report.json
```

## Performance Characteristics

- **Validation Speed**: ~50-100 policies per second
- **Memory Usage**: Low memory footprint with streaming validation
- **Parallel Processing**: Supports concurrent policy validation
- **Incremental Updates**: Validates only changed policies when possible

## Testing Coverage

The validation engine includes comprehensive tests:

- **Unit Tests**: 21 tests covering core functionality
- **Integration Tests**: End-to-end validation scenarios
- **Performance Tests**: Load testing with large policy sets
- **Security Tests**: Validation of security rule effectiveness

```bash
# Run validation engine tests
dotnet test --filter "PolicyValidationEngine"
dotnet test --filter "ValidationRules"
```

## Troubleshooting

### Common Issues

1. **Rule Not Executing**
   - Check if rule is enabled in configuration
   - Verify rule registration in ValidationRuleRegistry
   - Check for validation context requirements

2. **Low Compliance Scores**
   - Review specific framework requirements
   - Check for missing security controls
   - Validate policy coverage and exceptions

3. **Performance Issues**
   - Enable parallel processing
   - Use incremental validation for large sets
   - Consider rule-specific optimizations

### Debugging

```csharp
// Enable detailed logging
var options = new ValidationOptions
{
    Configuration = { ["debug.enabled"] = true }
};

// Check rule execution
var registry = new ValidationRuleRegistry();
var stats = registry.GetStatistics();
Console.WriteLine($"Total rules: {stats.TotalRules}");
```

## Future Enhancements

- **Machine Learning**: AI-powered policy analysis and recommendations
- **Custom Dashboards**: Interactive policy validation dashboards
- **API Integration**: REST API for validation services
- **Real-time Monitoring**: Continuous policy validation and alerting
- **Policy Templates**: Pre-built policy templates with validation
- **Compliance Automation**: Automated compliance report generation

## Contributing

To contribute new validation rules or enhancements:

1. Create rule following the BaseValidationRule pattern
2. Add comprehensive tests for the new rule
3. Update documentation with rule details
4. Submit pull request with rule registration

## References

- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)
- [ISO 27001 Standard](https://www.iso.org/isoiec-27001-information-security.html)
- [SOC 2 Compliance](https://www.aicpa.org/resources/landing/system-and-organization-controls-soc-suite-of-services)
- [Azure Conditional Access Documentation](https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/)

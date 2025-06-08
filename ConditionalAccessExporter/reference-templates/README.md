




# Conditional Access Policy Reference Templates

This directory contains reference templates for common Conditional Access policy scenarios. These templates provide a starting point for implementing security policies in your Azure AD tenant.

## Template Categories

### Basic Security Policies
- **require-mfa-all-users**: Enforces MFA for all users across all applications
- **block-legacy-auth**: Blocks legacy authentication protocols
- **require-compliant-device**: Requires device compliance for access
- **signin-risk-policy**: Blocks high-risk sign-ins

### Role-Based Policies
- **admin-protection**: Enhanced protection for administrative accounts
- **guest-restrictions**: Stricter controls for guest users
- **service-accounts**: Location-based restrictions for service accounts

### Application-Specific Policies
- **office365-protection**: Enhanced protection for Office 365 applications
- **high-risk-apps**: Strict controls for high-risk applications

### Location-Based Policies
- **block-untrusted-locations**: Blocks access from specific geographic locations
- **corporate-network-only**: Restricts sensitive applications to corporate networks

## Using Templates

### CLI Commands

```bash
# List all available templates
dotnet run templates --list

# Create a specific template
dotnet run templates --create basic/require-mfa-all-users --output-dir ./my-policies

# Create a complete baseline set
dotnet run templates --create-baseline --output-dir ./baseline-policies

# Validate a template
dotnet run templates --validate ./my-policies/require-mfa-all-users.json
```

### Template Customization

Templates use placeholder values that must be replaced with your environment-specific values:

- `{{EMERGENCY_ACCESS_ACCOUNT_1}}` - First emergency access account ID
- `{{EMERGENCY_ACCESS_ACCOUNT_2}}` - Second emergency access account ID
- `{{TRUSTED_LOCATION_ID}}` - Named location ID for trusted locations
- `{{SERVICE_ACCOUNTS_GROUP_ID}}` - Group containing service accounts
- `{{TERMS_OF_USE_ID}}` - Terms of use policy ID
- `{{HIGH_RISK_APP_*_ID}}` - Application IDs for high-risk applications

### Template Structure

Each template includes:
- **JSON Policy**: The actual Conditional Access policy configuration
- **Documentation**: Detailed explanation, compliance mappings, and implementation notes
- **Placeholder Values**: Clearly marked areas requiring customization

## Implementation Best Practices

1. **Start with Pilot Groups**: Test policies with small groups before organization-wide deployment
2. **Emergency Access**: Always exclude emergency access accounts from blocking policies
3. **Gradual Rollout**: Implement policies in phases to minimize user impact
4. **Monitor and Adjust**: Review sign-in logs and user feedback after deployment
5. **Documentation**: Maintain records of policy purposes and business justifications

## Compliance Framework Mappings

Templates include mappings to common compliance frameworks:
- **NIST Cybersecurity Framework**
- **CIS Controls**
- **ISO 27001**
- **Microsoft Security Baselines**

## Security Considerations

- Review all placeholder values before deployment
- Test policies in report-only mode first
- Ensure emergency access procedures are documented
- Monitor for unintended access blocks
- Regular policy reviews and updates

## Template Validation

All templates are validated against:
- JSON schema compliance
- Required field presence
- Microsoft Graph API compatibility
- Common configuration patterns

## Contributing

When adding new templates:
1. Include comprehensive documentation
2. Use consistent placeholder naming
3. Add compliance framework mappings
4. Test with validation tools
5. Include implementation notes





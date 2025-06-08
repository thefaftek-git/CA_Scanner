
# Require MFA for All Users

## Description
This policy enforces multi-factor authentication (MFA) for all users in the organization when accessing any application.

## Security Benefits
- Protects against password-based attacks
- Reduces risk of account compromise
- Meets baseline security requirements for most compliance frameworks

## Customization Required
- Replace `{{EMERGENCY_ACCESS_ACCOUNT_1}}` and `{{EMERGENCY_ACCESS_ACCOUNT_2}}` with actual emergency access account IDs
- Consider excluding specific service accounts if needed

## Compliance Frameworks
- **NIST**: AC-2, IA-2
- **CIS Controls**: 6.3, 6.4
- **ISO 27001**: A.9.4.2

## Implementation Notes
- Ensure emergency access accounts are configured before enabling
- Test with pilot group before organization-wide deployment
- Monitor for authentication failures during rollout

## Dependencies
- Azure AD Premium P1 or P2 license
- MFA methods configured for users
- Emergency access accounts properly configured


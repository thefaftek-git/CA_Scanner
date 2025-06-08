


# Block Legacy Authentication

## Description
This policy blocks legacy authentication protocols that don't support modern security features like MFA.

## Security Benefits
- Prevents attacks using legacy protocols (SMTP, POP3, IMAP, etc.)
- Forces use of modern authentication methods
- Reduces attack surface significantly

## Targeted Protocols
- Exchange ActiveSync
- Other legacy protocols (SMTP AUTH, POP3, IMAP, etc.)

## Customization Required
- Replace `{{EMERGENCY_ACCESS_ACCOUNT_1}}` and `{{EMERGENCY_ACCESS_ACCOUNT_2}}` with actual emergency access account IDs
- Review applications that might use legacy authentication

## Compliance Frameworks
- **NIST**: IA-2, IA-5
- **CIS Controls**: 6.2, 6.3
- **Microsoft Security Baseline**: Required

## Implementation Notes
- Audit legacy authentication usage before blocking
- Identify and migrate applications using legacy auth
- Communicate changes to users who may use older email clients
- Consider gradual rollout by group

## Dependencies
- Modern authentication enabled for Exchange Online
- Updated email clients that support modern authentication
- Application inventory to identify legacy auth usage

## Common Applications Affected
- Older Outlook versions
- Third-party email clients
- Mobile apps using basic authentication
- Automated systems using SMTP AUTH



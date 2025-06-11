---
triggers:
  - azure
  - entra
  - "conditional access"
  - "microsoft graph"
  - authentication
  - tenant
  - permissions
---

# Azure/Entra ID Microagent

This microagent provides specialized guidance for Azure and Entra ID related tasks within the CA_Scanner project.

## Azure Authentication Setup

### App Registration Requirements
The Azure app registration **must** have the following Microsoft Graph **Application permissions**:
- `Policy.Read.All` (recommended for read-only operations)
- `Policy.ReadWrite.ConditionalAccess` (required for write operations)

**Important**: These must be **Application permissions**, not Delegated permissions, since the application uses client credential flow.

### Environment Variables
Always verify these environment variables are set before attempting any Azure operations:

```bash
AZURE_TENANT_ID=your-tenant-id-here
AZURE_CLIENT_ID=your-client-id-here  
AZURE_CLIENT_SECRET=your-client-secret-here
```

**Security Note**: Client secrets are marked as sensitive and hidden in logs. Never commit real Azure credentials to version control.

### Authentication Flow
The application uses **Azure.Identity ClientCredentialProvider** for service principal authentication:

```csharp
var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
var graphClient = new GraphServiceClient(credential);
```

This authentication pattern is used consistently throughout:
- `Program.cs` (lines ~1130, 1768): Main Graph API client initialization
- `BaselineGenerationService.cs`: Fetches policies from Entra
- All service classes that interact with Microsoft Graph

### Permission Troubleshooting
Common "required scopes are missing" errors and solutions:

1. **403 Forbidden**: Check app registration permissions and admin consent
   - Verify the app has `Policy.Read.All` or `Policy.ReadWrite.ConditionalAccess`
   - Ensure admin consent has been granted for the tenant
   - Check if the service principal is enabled

2. **401 Unauthorized**: Verify credentials and tenant ID
   - Double-check `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`
   - Ensure the client secret hasn't expired
   - Verify the tenant ID is correct

3. **Empty Results**: Verify tenant has conditional access policies configured
   - Check if the tenant has any Conditional Access policies
   - Verify the service principal has access to the right tenant

## Microsoft Graph API Integration

### Graph Client Initialization
The Graph client is initialized in `Program.cs` using Azure.Identity:

```csharp
var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
var graphServiceClient = new GraphServiceClient(credential);
```

### Primary API Endpoint
The main endpoint used throughout the application:
- **Endpoint**: `/identity/conditionalAccess/policies`
- **Purpose**: Retrieve all Conditional Access policies from Entra ID
- **Usage**: `graphServiceClient.Identity.ConditionalAccess.Policies.GetAsync()`

### Rate Limiting Considerations
Be aware of Microsoft Graph API throttling limits:
- **Throttling**: 429 status code indicates rate limiting
- **Best Practice**: Implement exponential backoff for throttled requests
- **Batch Operations**: Use Graph batch requests for multiple operations when possible

### Error Handling Pattern
Always check authentication before making API calls:

```csharp
try
{
    var policies = await graphServiceClient.Identity.ConditionalAccess.Policies
        .GetAsync()
        .ConfigureAwait(false);
    return policies?.Value?.ToList() ?? new List<ConditionalAccessPolicy>();
}
catch (Exception ex)
{
    Logger.WriteError($"Error retrieving policies: {ex.Message}");
    throw;
}
```

## Azure-Specific Development Patterns

### Policy Models
Use **Microsoft.Graph.Models.ConditionalAccessPolicy** for all policy operations:
- **Namespace**: `Microsoft.Graph.Models`
- **Main Type**: `ConditionalAccessPolicy`
- **Collection Type**: `ConditionalAccessPolicyCollectionResponse`

### Field Mappings Reference
Understand numeric codes vs string values for policy fields:

#### BuiltInControls Mappings
| Numeric Code | String Value | Description |
|--------------|--------------|-------------|
| `1` | `mfa` | Multi-factor Authentication Required |
| `2` | `compliantDevice` | Compliant Device Required |
| `3` | `domainJoinedDevice` | Hybrid Azure AD Joined Device Required |
| `4` | `approvedApplication` | Approved Application Required |
| `5` | `compliantApplication` | Compliant Application Required |
| `6` | `passwordChange` | Password Change Required |
| `7` | `block` | Block Access |

#### ClientAppTypes Mappings
| Numeric Code | String Value | Description |
|--------------|--------------|-------------|
| `0` | `browser` | Web browsers |
| `1` | `mobileAppsAndDesktopClients` | Mobile apps and desktop clients |
| `2` | `exchangeActiveSync` | Exchange ActiveSync clients |
| `3` | `other` | Other clients (legacy authentication) |

#### Risk Levels (String Values)
- **Values**: `low`, `medium`, `high`, `hidden`, `none`, `unknownFutureValue`
- **Source**: Azure Identity Protection risk assessments
- **Usage**: `SignInRiskLevels` and `UserRiskLevels` fields

#### State Values
- `enabled`: Policy is active and enforced
- `disabled`: Policy is inactive
- `enabledForReportingButNotEnforced`: Report-only mode (logs but doesn't enforce)

### Tenant-Specific Data Handling
- **Tenant IDs**: Handle different tenant configurations appropriately
- **Policy IDs**: Unique identifiers within each tenant
- **Timestamps**: Use UTC for all timestamp operations
- **Tenant Isolation**: Each tenant has separate policy sets

## Common Azure Tasks

### 1. Export Policies
```bash
dotnet run export
```
This command fetches live policies from Entra ID using the Microsoft Graph API.

### 2. Authentication Testing
Always verify credentials before making changes:
```bash
# Verify environment variables are set
echo $AZURE_TENANT_ID
echo $AZURE_CLIENT_ID
echo $AZURE_CLIENT_SECRET  # Will show asterisks for security
```

### 3. Permission Validation
Check app registration permissions first:
- Navigate to Azure Portal → App Registrations
- Find your application
- Check API Permissions → Microsoft Graph → Application permissions
- Verify admin consent status

### 4. Multi-Tenant Support
The application supports different tenant configurations:
- Set appropriate `AZURE_TENANT_ID` for target tenant
- Ensure service principal exists in target tenant
- Verify permissions are granted in target tenant

## Azure-Specific Services

When working with Azure features, focus on these key services:

### Program.cs
- **Purpose**: Main Graph API client initialization
- **Key Functions**: Authentication setup, command-line interface
- **Graph Client**: Initialized using `ClientSecretCredential`

### BaselineGenerationService.cs
- **Purpose**: Fetches policies from Entra ID
- **Method**: `FetchPoliciesAsync()` retrieves live policies
- **Output**: Generates baseline reference policies from current tenant

### Models Directory
Contains Azure-specific policy models and data structures:
- Policy models that match Microsoft Graph API schemas
- Data transfer objects for Azure interactions
- Validation models for Azure-specific fields

## Field Value Interpretations

### Numeric vs String Values
- **Numeric values**: Typically appear in direct Azure exports
- **String values**: Typically appear in Terraform configurations
- **Both represent**: The same underlying policy settings
- **Comparison**: The comparison engine normalizes these values for accurate matching

### Practical Examples
- `"BuiltInControls": [1]` equals `"BuiltInControls": ["mfa"]`
- `"ClientAppTypes": [0, 1]` equals `"ClientAppTypes": ["browser", "mobileAppsAndDesktopClients"]`
- `"State": "enabled"` is functionally equivalent to enabled policies

## Azure Environment Considerations

### Tenant Isolation
- Each tenant has separate policy sets
- Tenant-specific identifiers (IDs, timestamps, tenant references)
- Cross-tenant operations require separate authentication

### Azure Clouds
- **Default**: Commercial cloud (graph.microsoft.com)
- **Support**: Currently supports commercial cloud endpoint
- **Future**: May extend to government or other cloud endpoints

### Service Principal Security
- **Secret Rotation**: Keep client secrets secure, rotate regularly
- **Least Privilege**: Use minimum required permissions
- **Monitoring**: Monitor service principal usage and authentication

### Compliance
- Follow organization's Azure governance policies
- Implement proper auditing and logging
- Maintain security documentation

## Common Azure Issues & Solutions

### Authentication Issues

#### 403 Forbidden
- **Cause**: Insufficient permissions
- **Solution**: Check app registration permissions and admin consent
- **Verification**: Ensure `Policy.Read.All` or `Policy.ReadWrite.ConditionalAccess` is granted

#### 401 Unauthorized
- **Cause**: Invalid credentials or tenant ID
- **Solution**: Verify environment variables and credentials
- **Check**: Ensure client secret hasn't expired

#### 429 Throttling
- **Cause**: Rate limiting from Microsoft Graph API
- **Solution**: Implement retry logic with exponential backoff
- **Prevention**: Use batch operations when possible

#### Empty Results
- **Cause**: No policies configured or access issues
- **Solution**: Verify tenant has conditional access policies configured
- **Check**: Ensure service principal has appropriate permissions

### Integration Issues

#### Graph Client Initialization Failures
- **Common Cause**: Missing or invalid environment variables
- **Solution**: Verify all three environment variables are set correctly
- **Debug**: Check credential creation before Graph client instantiation

#### Policy Retrieval Failures
- **Endpoint**: `/identity/conditionalAccess/policies`
- **Common Issue**: Permissions or network connectivity
- **Debug**: Test with Graph Explorer first

#### Data Format Mismatches
- **Issue**: Numeric codes vs string values
- **Understanding**: Both formats represent the same data
- **Solution**: Use the field mappings reference above

## Testing with Azure

### Integration Tests
- **Preference**: Use real Azure tenant for testing when possible
- **Requirements**: Dedicated test tenant with test policies
- **Credentials**: Use separate test service principal

### Mock Data
- **Create**: Representative test data that matches Azure formats
- **Include**: All policy field types and structures
- **Validate**: Against real Azure policy schemas

### Credential Handling
- **Never**: Commit real Azure credentials to tests
- **Use**: Environment variables or secure test fixtures
- **Rotate**: Test credentials regularly

### Test Environment Setup
```bash
# Set test environment variables
export AZURE_TENANT_ID="test-tenant-id"
export AZURE_CLIENT_ID="test-client-id"  
export AZURE_CLIENT_SECRET="test-client-secret"

# Run tests
dotnet test
```

## Security Best Practices

### Authentication Security
1. **Use Azure.Identity** for all authentication scenarios
2. **Never hardcode credentials** in source code
3. **Implement proper token refresh** mechanisms
4. **Use least-privilege principle** for Graph API permissions

### Credential Management
1. **Environment Variables**: Store credentials in environment variables only
2. **Secret Rotation**: Rotate client secrets regularly (recommended: every 6 months)
3. **Monitoring**: Monitor authentication failures and unusual access patterns
4. **Backup Plans**: Have backup authentication methods for critical operations

### Network Security
1. **HTTPS Only**: All Graph API communications use HTTPS
2. **Certificate Validation**: Ensure proper certificate validation
3. **Network Restrictions**: Consider network-level access restrictions if needed

## Benefits for OpenHands

This microagent helps OpenHands:

1. **Quick Setup**: Rapidly configure Azure authentication correctly
2. **Error Resolution**: Understand Azure-specific error messages and solutions
3. **API Navigation**: Navigate Microsoft Graph API integration patterns
4. **Data Handling**: Handle Azure-specific data formats and field mappings
5. **Security Compliance**: Follow Azure security best practices
6. **Issue Troubleshooting**: Resolve common Azure integration issues
7. **Development Acceleration**: Understand established patterns and services
8. **Multi-Tenant Support**: Handle different tenant configurations effectively

The microagent provides immediate access to Azure-specific knowledge, reducing development time and avoiding common pitfalls when working with Microsoft Graph API and Azure Conditional Access policies.

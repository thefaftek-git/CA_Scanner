

# Troubleshooting Guide

This guide provides solutions for common issues and problems encountered while using CA_Scanner. If you encounter a problem not covered here, please open an issue on GitHub with details about your problem.

## Authentication Issues

### Problem: Authentication failure

**Symptoms**:
- Error messages related to authentication (e.g., "Invalid client secret", "Unauthorized")
- Unable to export policies or access Azure AD

**Solution**:
1. Verify that the environment variables for Azure credentials are correctly set:
   ```bash
   echo $AZURE_TENANT_ID
   echo $AZURE_CLIENT_ID
   echo $AZURE_CLIENT_SECRET
   ```
2. Check that the app registration has the necessary permissions (`Policy.Read.All`).
3. Ensure that the client secret has not expired. Rotate the secret if necessary.
4. Test authentication using the Microsoft Graph Explorer:
   - Go to [Microsoft Graph Explorer](https://developer.microsoft.com/en-us/graph/graph-explorer)
   - Sign in with the same credentials
   - Run a simple query (e.g., `GET /policies/conditionalAccessPolicies`)

### Problem: Token expiration

**Symptoms**:
- Error messages related to token expiration (e.g., "Token expired", "Invalid token")

**Solution**:
1. Ensure that the token is being refreshed automatically.
2. Check the token expiration time and refresh it before it expires.
3. Verify that the app registration has the necessary permissions for token refresh.

## Export Problems

### Problem: Export timeout

**Symptoms**:
- Export operation takes too long or fails with a timeout error
- Incomplete policy export

**Solution**:
1. Increase the timeout value using the `--timeout` option:
   ```bash
   dotnet run export --timeout 300
   ```
2. Ensure that the tenant has a reasonable number of policies.
3. Use the `--batch-size` option to limit the number of policies exported in each batch:
   ```bash
   dotnet run export --batch-size 50
   ```

### Problem: Incomplete policy export

**Symptoms**:
- Missing policies in the export output
- Inconsistent policy data

**Solution**:
1. Verify that all policies are being retrieved from Azure AD.
2. Check for any errors or warnings in the export log.
3. Use the `--retry` option to retry failed policy exports:
   ```bash
   dotnet run export --retry 3
   ```

## Comparison Challenges

### Problem: Policy comparison differences

**Symptoms**:
- Unexpected differences in policy comparison results
- Inconsistent policy matching

**Solution**:
1. Use the `--matching-strategy` option to customize the comparison logic:
   ```bash
   dotnet run compare --matching-strategy strict
   ```
2. Review the comparison report to understand the differences.
3. Use the `--ignore-fields` option to exclude specific fields from comparison:
   ```bash
   dotnet run compare --ignore-fields modifiedTime
   ```

### Problem: Large policy differences

**Symptoms**:
- Large number of policy differences in the comparison report
- Difficult to identify significant changes

**Solution**:
1. Use the `--summary` option to generate a summary report:
   ```bash
   dotnet run compare --summary
   ```
2. Filter the comparison results to focus on significant changes.
3. Use the `--highlight` option to highlight important differences:
   ```bash
   dotnet run compare --highlight
   ```

## Performance Issues

### Problem: Slow export or comparison

**Symptoms**:
- Export or comparison operations take too long
- High memory usage

**Solution**:
1. Optimize database queries and reduce unnecessary data processing.
2. Use asynchronous programming for I/O-bound operations.
3. Profile the application to identify performance bottlenecks.
4. Use the `--parallel` option to run operations in parallel:
   ```bash
   dotnet run export --parallel 4
   ```

### Problem: High memory usage

**Symptoms**:
- Application crashes or becomes unresponsive due to high memory usage
- OutOfMemoryException errors

**Solution**:
1. Increase the available memory for the application.
2. Optimize memory usage by releasing resources properly.
3. Use the `--memory-limit` option to limit memory usage:
   ```bash
   dotnet run export --memory-limit 2GB
   ```

## Environment Problems

### Problem: Network connectivity issues

**Symptoms**:
- Unable to connect to Azure AD or Microsoft Graph API
- Network-related errors (e.g., "Connection refused", "Timeout")

**Solution**:
1. Verify network connectivity to Azure AD and Microsoft Graph API.
2. Check for any network proxies or firewalls that may be blocking the connection.
3. Use the `--proxy` option to configure a proxy server:
   ```bash
   dotnet run export --proxy http://proxy.example.com:8080
   ```

### Problem: Proxy settings

**Symptoms**:
- Unable to connect to Azure AD or Microsoft Graph API through a proxy
- Proxy-related errors (e.g., "Proxy authentication required")

**Solution**:
1. Verify that the proxy settings are correctly configured.
2. Check for any proxy authentication requirements.
3. Use the `--proxy-auth` option to provide proxy authentication credentials:
   ```bash
   dotnet run export --proxy-auth user:password
   ```

## Error Reference

### Common Error Codes

**Error Code**: `401 Unauthorized`
- **Cause**: Invalid or expired token
- **Solution**: Verify token and refresh if necessary

**Error Code**: `403 Forbidden`
- **Cause**: Insufficient permissions
- **Solution**: Check app registration permissions

**Error Code**: `429 Too Many Requests`
- **Cause**: API rate limit exceeded
- **Solution**: Implement rate limiting or retry with exponential backoff

**Error Code**: `500 Internal Server Error`
- **Cause**: Server-side error
- **Solution**: Retry the operation or contact support

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

## FAQ

### How do I reset my Azure credentials?

To reset your Azure credentials, follow these steps:
1. Go to the Azure portal and navigate to your app registration.
2. Regenerate the client secret.
3. Update the environment variables with the new credentials.

### How do I increase the export timeout?

To increase the export timeout, use the `--timeout` option:
```bash
dotnet run export --timeout 300
```

### How do I compare policies with a custom matching strategy?

To compare policies with a custom matching strategy, use the `--matching-strategy` option:
```bash
dotnet run compare --matching-strategy strict
```

## Conclusion

This troubleshooting guide provides solutions for common issues and problems encountered while using CA_Scanner. If you encounter a problem not covered here, please open an issue on GitHub with details about your problem.

For more detailed information, refer to the [API Documentation](API_REFERENCE.md) and [Advanced Features](ADVANCED_FEATURES.md).


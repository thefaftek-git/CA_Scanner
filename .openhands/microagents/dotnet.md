---
triggers:
  - dotnet
  - csharp
  - nuget
  - async
  - commandline
---

# .NET Development Microagent

This microagent provides specialized guidance for .NET development, C# coding patterns, and framework-specific tasks within the CA_Scanner project.

## Project Overview

CA_Scanner is a .NET 8.0 console application designed for analyzing and managing Conditional Access policies in Microsoft 365 environments. The project follows a service-oriented architecture with comprehensive async/await patterns for Azure API interactions.

## .NET Project Structure

### Target Framework
* **Framework**: .NET 8.0
* **Project Type**: Console application with comprehensive service architecture
* **Solution File**: `ConditionalAccessExporter.sln`
* **Main Project**: `ConditionalAccessExporter/ConditionalAccessExporter.csproj`
* **Test Project**: `ConditionalAccessExporter.Tests/ConditionalAccessExporter.Tests.csproj`

### Solution Structure
```
ConditionalAccessExporter.sln
├── ConditionalAccessExporter/          # Main console application
│   ├── Models/                         # Data models and DTOs
│   ├── Services/                       # Service layer classes
│   ├── Utils/                          # Utility classes
│   ├── reference-templates/            # Template files
│   └── Program.cs                      # Application entry point
└── ConditionalAccessExporter.Tests/    # Unit tests
```

## Key Dependencies

### Core Dependencies
- **Microsoft.Graph (5.79.0)**: Microsoft Graph API client library for accessing Azure AD and Microsoft 365 services
- **Azure.Identity (1.14.0)**: Azure authentication and identity management for secure API access
- **Newtonsoft.Json (13.0.3)**: JSON serialization and deserialization
- **System.CommandLine (2.0.0-beta4.22272.1)**: Modern command-line argument parsing and handling

### Additional Dependencies
- **Newtonsoft.Json.Schema (3.0.15)**: JSON schema validation
- **JsonDiffPatch.Net (2.3.0)**: JSON comparison and patching
- **YamlDotNet (13.7.1)**: YAML parsing and generation
- **Microsoft.Extensions.Configuration (8.0.0)**: Configuration management
- **Microsoft.Extensions.Configuration.Json (8.0.0)**: JSON configuration provider

### Testing Framework
- **xUnit**: Primary testing framework with comprehensive test coverage

## Development Patterns

### Service-Oriented Architecture
The application follows a clean service-oriented architecture with focused service classes:

- **Services Directory**: Contains specialized service classes for different functionalities
  - `BaselineGenerationService.cs`: Baseline policy generation
  - `PolicyComparisonService.cs`: Policy comparison logic
  - `ReportGenerationService.cs`: Report generation functionality
  - `TerraformConversionService.cs`: Terraform conversion capabilities
  - `CrossFormatPolicyComparisonService.cs`: Cross-format policy analysis
  - And many more specialized services

### Async/Await Patterns
- **Consistent async/await usage** for all Microsoft Graph API calls
- **Proper exception handling** with try-catch blocks around async operations
- **ConfigureAwait(false)** usage where appropriate to avoid deadlocks
- **Task-based return types** for all async service methods

Example async pattern:
```csharp
public async Task<List<ConditionalAccessPolicy>> GetPoliciesAsync()
{
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
}
```

### Command-Line Interface
- **System.CommandLine framework** for modern CLI argument parsing
- **Structured command hierarchy** with options and arguments
- **Help generation** automatically handled by the framework
- **Type-safe argument binding** with validation

### Error Handling and Logging
- **Custom Logger class** with support for quiet, verbose, and error modes
- **Comprehensive exception handling** throughout the application
- **Structured error messages** with context information
- **Separation of concerns** between info, error, and verbose logging

Example logging pattern:
```csharp
public static class Logger
{
    public static void WriteInfo(string message) { /* Implementation */ }
    public static void WriteError(string message) { /* Implementation */ }
    public static void WriteVerbose(string message) { /* Implementation */ }
}
```

## Common Development Tasks

### Building the Application
```bash
# Build the entire solution
dotnet build

# Build with specific configuration
dotnet build --configuration Release

# Clean and rebuild
dotnet clean && dotnet build
```

### Running the Application
```bash
# Run the main project
dotnet run --project ConditionalAccessExporter

# Run with specific arguments
dotnet run --project ConditionalAccessExporter -- export --tenant-id <id>

# Run from the project directory
cd ConditionalAccessExporter
dotnet run
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run specific test project
dotnet test ConditionalAccessExporter.Tests

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Package Management
```bash
# Restore NuGet packages
dotnet restore

# Add a new package
dotnet add package PackageName

# Remove a package
dotnet remove package PackageName

# Update packages
dotnet list package --outdated
```

## C# Coding Patterns Used

### Nullable Reference Types
- **Enabled nullable reference types** in project configuration
- **Explicit null handling** throughout the codebase
- **Null-coalescing operators** for safe default values

### Implicit Usings
- **Enabled implicit usings** to reduce boilerplate
- **Common namespaces** automatically available
- **Clean, focused using statements** only when needed

### Modern C# Features
- **Pattern matching** for type checking and casting
- **String interpolation** for readable string formatting
- **Collection expressions** and LINQ for data manipulation
- **Record types** for immutable data structures where appropriate

### Dependency Injection Patterns
- **Service registration** patterns for testability
- **Interface-based design** for loose coupling
- **Constructor injection** for required dependencies

## Troubleshooting Guidelines

### Common .NET Issues

#### Build Failures
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages explicitly
dotnet restore --force

# Check for target framework issues
dotnet --list-sdks
```

#### Authentication Issues
- **Azure.Identity configuration**: Ensure proper tenant ID and client credentials
- **Microsoft Graph permissions**: Verify required API permissions are granted
- **Token cache issues**: Clear local credential cache if needed

#### Performance Issues
- **Async/await best practices**: Avoid blocking async calls with `.Result`
- **Memory management**: Dispose of Graph client instances properly
- **Batch operations**: Use Graph batch requests for multiple operations

### Testing Issues
```bash
# Debug test failures
dotnet test --logger "console;verbosity=detailed"

# Run specific test method
dotnet test --filter "TestMethodName"

# Collect coverage data
dotnet test --collect:"XPlat Code Coverage" --logger trx
```

### Graph API Troubleshooting
- **Rate limiting**: Implement exponential backoff for throttled requests
- **Permissions**: Ensure application has required Graph API permissions
- **Endpoint changes**: Stay updated with Microsoft Graph API versioning
- **Error handling**: Parse Graph error responses for specific error codes

## Best Practices for OpenHands

### Code Organization
1. **Follow the established service pattern** when adding new functionality
2. **Use async/await consistently** for any I/O operations
3. **Implement proper error handling** with logging
4. **Write unit tests** for new service methods
5. **Follow C# naming conventions** and coding standards

### Authentication and Security
1. **Use Azure.Identity** for all authentication scenarios
2. **Never hardcode credentials** in source code
3. **Implement proper token refresh** mechanisms
4. **Use least-privilege principle** for Graph API permissions

### Performance Considerations
1. **Use async patterns** to avoid blocking the main thread
2. **Implement caching** for frequently accessed data
3. **Use batch operations** when possible for Graph API calls
4. **Dispose resources properly** to prevent memory leaks

### Testing Strategy
1. **Write unit tests** for all service methods
2. **Mock external dependencies** like Graph API calls
3. **Test error conditions** and edge cases
4. **Maintain good test coverage** across the codebase

This microagent helps OpenHands understand the .NET-specific patterns, dependencies, and best practices used throughout the CA_Scanner project, enabling more effective development and troubleshooting of .NET-related tasks.

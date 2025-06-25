# Contributing to CA_Scanner

Welcome to the CA_Scanner project! This guide will help you get started as a contributor and understand our development workflow.

## 🎯 Project Overview

CA_Scanner is a .NET 8 Azure Conditional Access Policy management tool designed for Azure administrators, DevOps engineers, and security teams. It provides comprehensive capabilities for managing and analyzing Conditional Access policies in Microsoft 365 environments.

### Key Capabilities
- **Export**: Retrieve policies from Azure AD via Microsoft Graph API
- **Comparison**: Compare live policies against reference JSON files
- **Baseline Generation**: Create reference policy files from current tenant configurations
- **Terraform Conversion**: Convert between JSON and Terraform formats
- **Report Generation**: Generate detailed reports in multiple formats
- **Remediation**: Handle policy remediation workflows

## 🚀 Quick Start for Developers

To set up your development environment and get started with CA_Scanner, follow these steps:

1. **Clone the repository**:
   ```bash
   git clone https://github.com/thefaftek-git/CA_Scanner.git
   cd CA_Scanner
   ```

2. **Install .NET 8 SDK**:
   ```bash
   ./dotnet-install.sh
   ```

3. **Set up Azure credentials**:
   - Create an Azure App Registration with the necessary permissions.
   - Set the following environment variables with your Azure credentials:
     ```bash
     export AZURE_TENANT_ID=your-tenant-id
     export AZURE_CLIENT_ID=your-client-id
     export AZURE_CLIENT_SECRET=your-client-secret
     ```

4. **Build the solution**:
   ```bash
   dotnet build
   ```

5. **Run the application**:
   ```bash
   cd ConditionalAccessExporter
   ./run.sh
   ```

6. **Run tests**:
   ```bash
   dotnet test
   ```

## 🏗️ Project Architecture

CA_Scanner follows a service-oriented architecture with specialized services for different functionalities. The main services are located in the `ConditionalAccessExporter/Services` directory.

### Core Services
- **BaselineGenerationService**: Creates reference policy files from current tenant
- **PolicyComparisonService**: Compares policies with flexible matching strategies
- **TerraformConversionService**: Converts between JSON and Terraform formats
- **ReportGenerationService**: Creates various output formats (console, JSON, HTML, CSV)

### Utility Services
- **RemediationService**: Handles policy remediation workflows
- **PolicyValidationService**: Validates policy configurations
- **ScriptGenerationService**: Generates automation scripts
- **TemplateService**: Manages policy templates

## 💻 Development Workflow

### Branching Strategy
- Create a new branch for each feature or bug fix.
- Use descriptive branch names (e.g., `feature/interactive-documentation`, `bugfix/authentication-issue`).

### Commit Messages
- Write clear and concise commit messages.
- Follow the conventional commit format (e.g., `feat: add interactive documentation`, `fix: resolve authentication issue`).

### Pull Requests
- Create a pull request for each feature or bug fix.
- Include a detailed description of the changes and the reasoning behind them.
- Request reviews from at least one other contributor.

### Code Reviews
- Participate in code reviews to provide feedback and help improve code quality.
- Address feedback promptly and make necessary changes.

## 🎨 Coding Standards

### C# Conventions
- Follow the [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- Use meaningful variable and method names.
- Write clear and concise comments.

### Patterns and Best Practices
- Use dependency injection for service dependencies.
- Follow the single responsibility principle for classes and methods.
- Write unit tests for new features and bug fixes.

## 🧪 Testing Guidelines

### Unit Testing
- Write unit tests for all new features and bug fixes.
- Use the xUnit framework for unit testing.
- Mock external dependencies using libraries like Moq.

### Integration Testing
- Write integration tests for end-to-end workflows.
- Use the xUnit framework for integration testing.
- Test against a real Azure environment or a mock environment.

### Mocking
- Use mocking libraries like Moq to simulate external dependencies.
- Write clear and concise mock setups.

## 🐛 Troubleshooting

### Common Development Issues
- **Build Failures**: Check the error messages for specific issues. Common problems include missing dependencies or syntax errors in the code.
- **Azure Authentication Issues**: Verify that the environment variables for Azure credentials are correctly set.
- **Test Failures**: Check the test output for specific issues. Common problems include incorrect test setups or missing mocks.

### Diagnostic Commands
- **Check Azure Credentials**:
  ```bash
  echo $AZURE_TENANT_ID
  echo $AZURE_CLIENT_ID
  echo $AZURE_CLIENT_SECRET
  ```

- **Run Specific Test**:
  ```bash
  dotnet test --filter "FullyQualifiedName=Namespace.ClassName.MethodName"
  ```

- **Check Test Coverage**:
  ```bash
  dotnet test --collect:"XPlat Code Coverage"
  ```

## 📈 Performance Considerations

- Optimize database queries and reduce unnecessary data processing.
- Use asynchronous programming for I/O-bound operations.
- Profile the application to identify performance bottlenecks.

## 🔒 Security Guidelines

### Code Security
- **Never commit secrets** to version control
- **Use environment variables** for configuration
- **Validate all inputs** properly
- **Handle sensitive data** carefully
- **Follow principle of least privilege**

### Azure Security
- **Use service principals** for authentication
- **Rotate secrets regularly**
- **Monitor API usage**
- **Follow Azure security best practices**

## 📚 Documentation Standards

### Code Documentation
- Use XML comments for public methods and properties.
- Include summary, parameters, returns, and exceptions where applicable.

### README Updates
- Update command-line help sections when adding new features.
- Add usage examples for new features.
- Update troubleshooting if needed.
- Add new features to the feature list.

### API Documentation
- Use DocFX for API documentation generation.
- Include XML documentation comments in code.
- Integrate with the build pipeline.
- Host the documentation site.

## 🛠️ Common Development Tasks

### Adding a New Service
1. **Create the interface** in the appropriate namespace.
2. **Implement the service** following the service pattern.
3. **Add unit tests** with good coverage.
4. **Register in the DI container** (if applicable).
5. **Update documentation**.

### Adding a New Command
1. **Update Program.cs** with the new command definition.
2. **Create the command handler** method.
3. **Add validation** for command options.
4. **Update help text** and documentation.
5. **Add integration tests**.

### Adding a New Output Format
1. **Extend ReportGenerationService** with the new format.
2. **Create format-specific logic**.
3. **Update command-line options**.
4. **Add tests** for the new format.
5. **Update documentation** with examples.

## 🚀 Release Process

### Pre-Release Checklist
- [ ] All tests passing
- [ ] Documentation updated
- [ ] Version numbers updated
- [ ] CHANGELOG.md updated
- [ ] Performance benchmarks run

### Version Numbering
- Follow semantic versioning (SemVer):
  - **Major**: Breaking changes (2.0.0)
  - **Minor**: New features, backward compatible (1.1.0)
  - **Patch**: Bug fixes (1.0.1)

## 🤝 Getting Help

### Resources
- **Project README**: Overview and quick start
- **CONFIGURATION.md**: All configuration options
- **EXAMPLES.md**: Practical use cases
- **API Documentation**: Generated docs (future)

### Support Channels
- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: Questions and general discussion
- **Code Reviews**: Learning opportunity through PR reviews

### Mentorship
- New contributors are encouraged to:
  1. Start with "good first issue" labels
  2. Ask questions in PR comments
  3. Participate in code reviews
  4. Join community discussions

## 📝 Issue and PR Templates

### Bug Report Template
- Include environment details (.NET version, OS)
- Provide steps to reproduce
- Describe expected vs actual behavior
- Include error messages and logs
- Provide Azure tenant configuration (anonymized)

### Feature Request Template
- Describe the use case
- Propose a solution
- List alternative solutions considered
- Explain the impact on existing functionality

### Pull Request Template
- Describe the changes
- List the testing performed
- Note any documentation updates
- Highlight any breaking changes
- Include screenshots (if UI changes)

---

### Prerequisites

1. **.NET 8.0 SDK**
   ```bash
   # Check if installed
   dotnet --version
   
   # Install on Debian/Ubuntu (automated script available)
   ./dotnet-install.sh
   ```

2. **IDE/Editor** (recommended)
   - Visual Studio 2022 (17.8+)
   - Visual Studio Code with C# extension
   - JetBrains Rider

3. **Git** for version control

4. **Azure Environment** (for testing)
   - Azure subscription with Conditional Access policies
   - App registration with `Policy.Read.All` permissions
   - Environment variables set (see `CONFIGURATION.md`)

### Initial Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/thefaftek-git/CA_Scanner.git
   cd CA_Scanner
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Run tests**
   ```bash
   dotnet test
   ```

4. **Set up environment variables** (see `CONFIGURATION.md` for details)
   ```bash
   export AZURE_TENANT_ID="your-tenant-id"
   export AZURE_CLIENT_ID="your-client-id"
   export AZURE_CLIENT_SECRET="your-client-secret"
   ```

5. **Test the application**
   ```bash
   cd ConditionalAccessExporter
   dotnet run export --help
   ```

## 🏗️ Project Architecture

### Solution Structure

```
CA_Scanner/
├── ConditionalAccessExporter.sln          # Solution file
├── ConditionalAccessExporter/              # Main console application
│   ├── Program.cs                          # Entry point with CLI parsing
│   ├── Models/                             # Data models and DTOs
│   ├── Services/                           # 15+ specialized service classes
│   ├── Utils/                              # Utility classes and helpers
│   └── reference-templates/                # Template files
└── ConditionalAccessExporter.Tests/        # Comprehensive test suite
```

### Service-Oriented Architecture

The application follows a service-oriented architecture with specialized services:

#### Core Services
- **BaselineGenerationService**: Creates reference policy files
- **PolicyComparisonService**: Compares policies with flexible strategies
- **CrossFormatPolicyComparisonService**: Cross-format policy analysis
- **TerraformConversionService**: JSON/Terraform conversion
- **RemediationService**: Policy remediation workflows

#### Report and Analysis Services
- **ReportGenerationService**: Multiple output formats
- **CrossFormatReportGenerationService**: Cross-format reports
- **ImpactAnalysisService**: Change impact analysis
- **CiCdAnalysisService**: CI/CD integration analysis

#### Utility Services
- **PolicyValidationService**: Policy validation
- **ScriptGenerationService**: Automation scripts
- **TemplateService**: Policy templates

### Key Technologies

- **.NET 8.0**: Target framework
- **Microsoft Graph API**: Azure AD integration
- **Azure Identity**: Service principal authentication
- **System.CommandLine**: Modern CLI interface
- **xUnit**: Testing framework

## 💻 Development Workflow

### Branch Strategy

- **main**: Production-ready code
- **feature/**: New features (`feature/description`)
- **bugfix/**: Bug fixes (`bugfix/issue-number-description`)
- **docs/**: Documentation updates (`docs/description`)

### Making Changes

1. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes**
   - Follow coding standards (see below)
   - Add tests for new functionality
   - Update documentation as needed

3. **Test your changes**
   ```bash
   # Run all tests
   dotnet test
   
   # Run specific test project
   dotnet test ConditionalAccessExporter.Tests
   
   # Test the application
   cd ConditionalAccessExporter
   dotnet run export --help
   ```

4. **Commit your changes**
   ```bash
   git add .
   git commit -m "feat: add new feature description"
   ```

5. **Push and create PR**
   ```bash
   git push origin feature/your-feature-name
   # Create PR via GitHub UI
   ```

### Commit Message Convention

We follow conventional commits:

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `style:` Code style changes (no functional changes)
- `refactor:` Code refactoring
- `test:` Adding or updating tests
- `chore:` Maintenance tasks

Examples:
```
feat: add baseline generation with anonymization
fix: resolve authentication timeout issue
docs: update API documentation
test: add unit tests for PolicyComparisonService
```

## 🎨 Coding Standards

### C# Style Guidelines

1. **Naming Conventions**
   ```csharp
   // Classes: PascalCase
   public class PolicyComparisonService { }
   
   // Methods: PascalCase
   public async Task<ComparisonResult> CompareAsync() { }
   
   // Properties: PascalCase
   public string DisplayName { get; set; }
   
   // Fields: camelCase with underscore prefix for private
   private readonly ILogger _logger;
   
   // Local variables: camelCase
   var policyCount = policies.Count;
   
   // Constants: PascalCase
   public const string DefaultOutputDirectory = "output";
   ```

2. **Code Organization**
   ```csharp
   // Use meaningful names
   public async Task<List<Policy>> GetActivePoliciesAsync()
   
   // Prefer explicit types when unclear
   ConditionalAccessPolicy policy = new ConditionalAccessPolicy();
   
   // Use var when type is obvious
   var policies = new List<ConditionalAccessPolicy>();
   
   // Async methods should end with Async
   public async Task<string> GenerateReportAsync()
   ```

3. **Error Handling**
   ```csharp
   // Use specific exceptions
   throw new ArgumentNullException(nameof(tenantId));
   throw new InvalidOperationException("Policy not found");
   
   // Log errors with context
   _logger.LogError("Failed to export policies for tenant {TenantId}", tenantId);
   
   // Handle async properly
   try
   {
       await SomeAsyncOperation();
   }
   catch (HttpRequestException ex)
   {
       _logger.LogError(ex, "HTTP request failed");
       throw;
   }
   ```

### Service Pattern

All services should follow consistent patterns:

```csharp
public interface IPolicyComparisonService
{
    Task<ComparisonResult> CompareAsync(ComparisonOptions options);
}

public class PolicyComparisonService : IPolicyComparisonService
{
    private readonly ILogger<PolicyComparisonService> _logger;
    private readonly IGraphServiceClient _graphClient;

    public PolicyComparisonService(
        ILogger<PolicyComparisonService> logger,
        IGraphServiceClient graphClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
    }

    public async Task<ComparisonResult> CompareAsync(ComparisonOptions options)
    {
        _logger.LogInformation("Starting policy comparison");
        
        try
        {
            // Implementation
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Policy comparison failed");
            throw;
        }
    }
}
```

## 🧪 Testing Guidelines

### Test Organization

- **Unit Tests**: Test individual methods and classes in isolation
- **Integration Tests**: Test service interactions and end-to-end workflows
- **Test Data**: Use realistic Azure policy structures

### Test Patterns

```csharp
[Fact]
public async Task CompareAsync_WithValidPolicies_ReturnsExpectedResult()
{
    // Arrange
    var mockLogger = new Mock<ILogger<PolicyComparisonService>>();
    var mockGraphClient = new Mock<IGraphServiceClient>();
    var service = new PolicyComparisonService(mockLogger.Object, mockGraphClient.Object);
    var options = new ComparisonOptions { /* test data */ };

    // Act
    var result = await service.CompareAsync(options);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedCount, result.TotalPolicies);
}
```

### Test Best Practices

1. **Test Naming**: Use descriptive names that explain the scenario
2. **Arrange-Act-Assert**: Follow the AAA pattern
3. **Mock External Dependencies**: Use Mock<T> for external services
4. **Test Edge Cases**: Include null checks, empty collections, error scenarios
5. **Async Testing**: Use async/await properly in tests

### Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "PolicyComparisonServiceTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📚 Documentation Standards

### Code Documentation

```csharp
/// <summary>
/// Compares Conditional Access policies between live Entra ID and reference files.
/// </summary>
/// <param name="options">Configuration options for the comparison</param>
/// <returns>A detailed comparison result with differences and matches</returns>
/// <exception cref="ArgumentNullException">Thrown when options is null</exception>
/// <exception cref="InvalidOperationException">Thrown when authentication fails</exception>
public async Task<ComparisonResult> CompareAsync(ComparisonOptions options)
```

### README Updates

When adding new features:
1. Update command-line help sections
2. Add usage examples
3. Update troubleshooting if needed
4. Add to feature list

### API Documentation

We use DocFX for API documentation generation:

```bash
# Generate documentation (future enhancement)
docfx docfx.json --serve
```

## 🛠️ Common Development Tasks

### Adding a New Service

1. **Create the interface** in appropriate namespace
2. **Implement the service** following the service pattern
3. **Add unit tests** with good coverage
4. **Register in DI container** (if applicable)
5. **Update documentation**

### Adding a New Command

1. **Update Program.cs** with new command definition
2. **Create command handler** method
3. **Add validation** for command options
4. **Update help text** and documentation
5. **Add integration tests**

### Adding New Output Format

1. **Extend ReportGenerationService** with new format
2. **Create format-specific logic**
3. **Update command-line options**
4. **Add tests** for new format
5. **Update documentation** with examples

## 🐛 Debugging and Troubleshooting

### Common Development Issues

1. **Authentication Failures**
   - Check environment variables
   - Verify app registration permissions
   - Check client secret expiration

2. **Build Errors**
   - Ensure .NET 8 SDK is installed
   - Check package references
   - Clean and rebuild: `dotnet clean && dotnet build`

3. **Test Failures**
   - Check test data setup
   - Verify mock configurations
   - Run tests in isolation

### Debugging Tips

```csharp
// Add detailed logging
_logger.LogDebug("Processing policy {PolicyId} with name {PolicyName}", 
                policy.Id, policy.DisplayName);

// Use breakpoints effectively
if (policy.DisplayName.Contains("Test"))
{
    System.Diagnostics.Debugger.Break(); // Conditional breakpoint
}

// Validate assumptions
Debug.Assert(policies.Count > 0, "Expected at least one policy");
```

## 🚀 Release Process

### Pre-Release Checklist

- [ ] All tests passing
- [ ] Documentation updated
- [ ] Version numbers updated
- [ ] CHANGELOG.md updated
- [ ] Performance benchmarks run

### Version Numbering

We follow semantic versioning (SemVer):
- **Major**: Breaking changes (2.0.0)
- **Minor**: New features, backward compatible (1.1.0)
- **Patch**: Bug fixes (1.0.1)

## 🤝 Getting Help

### Resources

- **Project README**: Overview and quick start
- **CONFIGURATION.md**: All configuration options
- **EXAMPLES.md**: Practical use cases
- **API Documentation**: Generated docs (future)

### Support Channels

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: Questions and general discussion
- **Code Reviews**: Learning opportunity through PR reviews

### Mentorship

New contributors are encouraged to:
1. Start with "good first issue" labels
2. Ask questions in PR comments
3. Participate in code reviews
4. Join community discussions

## 📈 Performance Considerations

### Guidelines

1. **Async/Await**: Use for I/O operations
2. **Memory Management**: Dispose resources properly
3. **Caching**: Cache expensive operations when appropriate
4. **Batch Operations**: Process multiple items efficiently

### Benchmarking

The project includes performance benchmarking:

```bash
# Run performance benchmarks
dotnet run --project ConditionalAccessExporter --configuration Release -- benchmark
```

Monitor:
- Memory usage
- API call efficiency
- File I/O performance
- JSON processing speed

## 🔒 Security Guidelines

### Code Security

1. **Never commit secrets** to version control
2. **Use environment variables** for configuration
3. **Validate all inputs** properly
4. **Handle sensitive data** carefully
5. **Follow principle of least privilege**

### Azure Security

1. **Use service principals** for authentication
2. **Rotate secrets regularly**
3. **Monitor API usage**
4. **Follow Azure security best practices**

## 📝 Issue and PR Templates

### Bug Report Template

When reporting bugs, include:
- Environment details (.NET version, OS)
- Steps to reproduce
- Expected vs actual behavior
- Error messages and logs
- Azure tenant configuration (anonymized)

### Feature Request Template

For feature requests:
- Use case description
- Proposed solution
- Alternative solutions considered
- Impact on existing functionality

### Pull Request Template

PRs should include:
- Description of changes
- Testing performed
- Documentation updates
- Breaking changes (if any)
- Screenshots (if UI changes)

---

## 🎉 Welcome to the Team!

Thank you for contributing to CA_Scanner! Your contributions help make Azure Conditional Access management easier for administrators and developers worldwide.

Questions? Don't hesitate to ask in issues or discussions. We're here to help you succeed!

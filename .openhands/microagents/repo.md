## Project Overview

CA_Scanner is a .NET 8 Azure Conditional Access Policy management tool designed for Azure administrators, DevOps engineers, and security teams. The solution provides comprehensive capabilities for managing and analyzing Conditional Access policies in Microsoft 365 environments.

## Environment Setup Requirements

### Prerequisites
1. **.NET 8 SDK** installation (automated script provided: `dotnet-install.sh`)
2. **Azure App Registration** with appropriate permissions
3. **Environment variables** for Azure credentials
4. **Planning Tasks** before making changes to the code, you should update progress.md with a plan. If you're debugging an issue, it's recommended you configure your pre-requisites first and run build(s) and test(s). This plan should be updated as you finish tasks. Changes to this file should NOT be committed to the repo.

### Environment Variables
AZURE_TENANT_ID=your-tenant-id-here
AZURE_CLIENT_ID=your-client-id-here
AZURE_CLIENT_SECRET=your-client-secret-here

### Main Capabilities
- **Export**: Retrieve all Conditional Access policies from Azure AD via Microsoft Graph API
- **Comparison**: Compare live policies against reference JSON files with multiple matching strategies
- **Baseline Generation**: Create reference policy files from current tenant configurations
- **Terraform Conversion**: Convert between JSON and Terraform formats for Infrastructure as Code workflows
- **Report Generation**: Generate detailed reports in multiple formats (console, JSON, HTML, CSV)
- **Remediation**: Handle policy remediation workflows with comprehensive analysis

### Main Project Structure
```
ConditionalAccessExporter/
├── ConditionalAccessExporter.sln          # Solution file
├── README.md                               # Main project documentation
├── ConditionalAccessExporter/              # Core console application
│   ├── Program.cs                          # Main entry point with CLI parsing
│   ├── ConditionalAccessExporter.csproj    # Project configuration
│   ├── Models/                             # Data models and DTOs
│   ├── Services/                           # specialized service classes
│   ├── Utils/                              # Utility classes and helpers
│   └── reference-templates/                # Template files for baseline generation
└── ConditionalAccessExporter.Tests/        # Comprehensive test suite
    ├── ConditionalAccessExporter.Tests.csproj
    ├── ConsoleOutputTestCollection.cs      # Test collection management
    └── *Tests.cs                           # Individual service test files
```

### Services Layer Architecture
The application follows a service-oriented architecture with specialized services:

#### Core Services
- **BaselineGenerationService**: Creates reference policy files from current tenant
- **PolicyComparisonService**: Compares policies with flexible matching strategies
- **CrossFormatPolicyComparisonService**: Handles cross-format policy analysis (JSON vs Terraform)
- **TerraformConversionService**: Converts between JSON and Terraform formats
- **JsonToTerraformService**: Specialized JSON to Terraform conversion
- **TerraformParsingService**: Parses and validates Terraform configurations

#### Report and Analysis Services
- **ReportGenerationService**: Creates various output formats (console, JSON, HTML, CSV)
- **CrossFormatReportGenerationService**: Generates cross-format comparison reports
- **ImpactAnalysisService**: Analyzes policy change impacts
- **CiCdAnalysisService**: Provides CI/CD pipeline integration analysis

#### Utility Services
- **RemediationService**: Handles policy remediation workflows
- **PolicyValidationService**: Validates policy configurations
- **ScriptGenerationService**: Generates automation scripts
- **TemplateService**: Manages policy templates

### Models Directory
- **ComparisonResult**: Data models for policy comparison results
- **RemediationModels**: Models for remediation workflows
- **TemplateModels**: Template-related data structures
- **TerraformModels**: Terraform-specific data models

### Tests Architecture
- Comprehensive unit tests for all major services
- Integration tests for end-to-end workflows
- Test data and mocking patterns established
- Console output management via test collections

## Key Technologies

### Core Framework
- **.NET 8.0 SDK**: Target framework for the entire solution
- **Microsoft Graph API**: Integration with Azure AD and Microsoft 365 services
- **Azure Identity**: Secure authentication using service principal credentials
- **System.CommandLine**: Modern CLI interface with structured command hierarchy

### Authentication
- Client credential authentication flow using Azure service principal
- Environment variable-based configuration for security

## Common Development Workflows

### Building the Solution
To build the solution, follow these steps:

1. **Build the entire solution**:
   ```bash
   dotnet build
   ```

2. **Build with specific configuration**:
   ```bash
   dotnet build --configuration Release
   ```

3. **Clean and rebuild**:
   ```bash
   dotnet clean && dotnet build
   ```

### Running the Application
To run the application, follow these steps:

1. **Run from solution root**:
   ```bash
   dotnet run --project ConditionalAccessExporter
   ```

2. **Run from project directory**:
   ```bash
   cd ConditionalAccessExporter
   dotnet run
   ```

3. **Run with convenience script**:
   ```bash
   cd ConditionalAccessExporter
   ./run.sh
   ```

### Testing
To run tests, follow these steps:

1. **Run all tests from solution root**:
   ```bash
   dotnet test
   ```

2. **Run tests with verbose output**:
   ```bash
   dotnet test --verbosity normal
   ```

3. **Run tests with coverage**:
   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```

## Service Architecture Deep Dive

### Service Patterns
All services follow consistent patterns:
- **Async/await** for all I/O operations
- **Comprehensive error handling** with detailed logging
- **Interface-based design** for testability and loose coupling
- **Constructor injection** for dependencies



## Progress Tracking

### Purpose
The `progress.md` file is used to track the progress of tasks, plans, and debugging efforts. It serves as a central place to document ongoing work, planned tasks, and any issues encountered during development.

### Usage Guidelines
1. **Planning Tasks**: Before making changes to the code, update `progress.md` with a detailed plan. Include the tasks to be completed, the steps to follow, and any prerequisites.
2. **Debugging Issues**: When debugging an issue, document the steps taken, the configuration used, and the results of builds and tests. This helps in tracking the progress and resolving issues more efficiently.
3. **Updating Progress**: Regularly update `progress.md` as tasks are completed. This ensures that the team has a clear understanding of the current status and any remaining work.
4. **Avoid Committing Changes**: Changes to the `progress.md` file should not be committed to the repository. This file is intended for internal use and tracking purposes only.

### Example Structure
```markdown
# Progress Tracking

## Current Tasks
- [ ] Task 1: Description of the task
- [ ] Task 2: Description of the task

## Debugging Issues
- Issue 1: Description of the issue
  - Steps taken: Description of the steps taken to resolve the issue
  - Results: Description of the results of the steps taken

## Completed Tasks
- Task 1: Description of the task
- Task 2: Description of the task
```



## Error Handling and Troubleshooting

### Common Errors
1. **Missing .NET SDK**: Ensure that the .NET 8 SDK is installed. You can use the provided `dotnet-install.sh` script to install it.
2. **Azure Authentication Issues**: Verify that the environment variables for Azure credentials are correctly set.
3. **Build Failures**: Check the error messages for specific issues. Common problems include missing dependencies or syntax errors in the code.

### Troubleshooting Steps
1. **Check Logs**: Review the logs for detailed error messages.
2. **Verify Configuration**: Ensure that all configuration files and environment variables are correctly set.
3. **Consult Documentation**: Refer to the detailed documentation in the `docs` directory for specific issues.
4. **Community Support**: Join the community forums or GitHub discussions for additional help.


## Testing Strategy

### Test Execution
- **Automatic test discovery** via xUnit framework
- **Console output management** via ConsoleOutputTestCollection
- **Parallel test execution** where appropriate
- **Coverage reporting** integrated with CI/CD

### Best Practices
- Write tests for new features following established patterns
- Mock external dependencies to ensure reliable, fast tests
- Use realistic test data that reflects actual Azure policy structures

## Documentation Locations

### Primary Documentation
- **Main README**: `README.md` - Project overview, quick start, and features
- **Project README**: `ConditionalAccessExporter/README.md` - Detailed usage instructions and troubleshooting

### Specialized Documentation
- **CI/CD Guide**: `CICD.md` - Pipeline integration and automation
- **Future Enhancements**: `todo_tasks/future_enhancements.md` - Planned features and improvements
- **Azure Setup**: `GITHUB_SECRETS_SETUP.md` - Azure App Registration configuration
- **Test Evidence**: `TEST_EVIDENCE.md` - Testing results and verification

### Reference Materials
- **Policy Field Mappings**: Detailed explanations of Azure policy numeric codes and string values
- **Terraform Examples**: Sample configurations and conversion patterns
- **API Documentation**: Microsoft Graph integration examples
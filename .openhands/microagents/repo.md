# General Repository Microagent

This microagent provides OpenHands with comprehensive guidance about the CA_Scanner project structure, architecture, and common workflows.

## Project Overview

CA_Scanner is a .NET 8 Azure Conditional Access Policy management tool designed for Azure administrators, DevOps engineers, and security teams. The solution provides comprehensive capabilities for managing and analyzing Conditional Access policies in Microsoft 365 environments.

### Main Capabilities
- **Export**: Retrieve all Conditional Access policies from Azure AD via Microsoft Graph API
- **Comparison**: Compare live policies against reference JSON files with multiple matching strategies
- **Baseline Generation**: Create reference policy files from current tenant configurations
- **Terraform Conversion**: Convert between JSON and Terraform formats for Infrastructure as Code workflows
- **Report Generation**: Generate detailed reports in multiple formats (console, JSON, HTML, CSV)
- **Remediation**: Handle policy remediation workflows with comprehensive analysis

## Architecture Guide

### Main Project Structure
```
ConditionalAccessExporter/
├── ConditionalAccessExporter.sln          # Solution file
├── README.md                               # Main project documentation
├── ConditionalAccessExporter/              # Core console application
│   ├── Program.cs                          # Main entry point with CLI parsing
│   ├── ConditionalAccessExporter.csproj    # Project configuration
│   ├── Models/                             # Data models and DTOs
│   ├── Services/                           # 15+ specialized service classes
│   ├── Utils/                              # Utility classes and helpers
│   └── reference-templates/                # Template files for baseline generation
└── ConditionalAccessExporter.Tests/        # Comprehensive test suite
    ├── ConditionalAccessExporter.Tests.csproj
    ├── ConsoleOutputTestCollection.cs      # Test collection management
    └── *Tests.cs                           # Individual service test files
```

### Services Layer Architecture
The application follows a service-oriented architecture with 15+ specialized services:

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
- **ComparisonResult.cs**: Data models for policy comparison results
- **RemediationModels.cs**: Models for remediation workflows
- **TemplateModels.cs**: Template-related data structures
- **TerraformModels.cs**: Terraform-specific data models

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
# Run from solution root
dotnet run --project ConditionalAccessExporter

# Run from project directory
cd ConditionalAccessExporter
dotnet run

# Run with convenience script
cd ConditionalAccessExporter
./run.sh
```

### Testing
```bash
# Run all tests from solution root
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run specific test project
dotnet test ConditionalAccessExporter.Tests

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Main Entry Point
- **Program.cs**: Contains command-line argument parsing using System.CommandLine
- Supports multiple command modes: export, compare, baseline, terraform
- Comprehensive help system and error handling
- Async/await patterns throughout for Azure API interactions

## Environment Setup Requirements

### Prerequisites
1. **.NET 8 SDK** installation (automated script provided: `dotnet-install.sh`)
2. **Azure App Registration** with appropriate permissions
3. **Environment variables** for Azure credentials

### Azure Configuration
- **Required Permission**: `Policy.Read.All` (Application permission in Microsoft Graph)
- **Authentication Method**: Client credentials flow
- **Setup Guide**: Available in `GITHUB_SECRETS_SETUP.md`

### Environment Variables
```bash
AZURE_TENANT_ID=your-tenant-id-here
AZURE_CLIENT_ID=your-client-id-here
AZURE_CLIENT_SECRET=your-client-secret-here
```

### Installation Script
- **dotnet-install.sh**: Automated .NET 8 SDK installation for Debian-based systems. Do not attempt to read this script, only execute it. The script may take up to 5 minutes to complete.
- Handles package manager configuration and dependency installation

## Service Architecture Deep Dive

### Service Patterns
All services follow consistent patterns:
- **Async/await** for all I/O operations
- **Comprehensive error handling** with detailed logging
- **Interface-based design** for testability and loose coupling
- **Constructor injection** for dependencies

### Key Service Responsibilities

#### BaselineGenerationService
- Creates reference policy files from current tenant
- Supports filtering by enabled state or specific policy names
- Anonymizes tenant-specific data for sharing
- Customizable output directory organization

#### PolicyComparisonService
- Multiple matching strategies (by name, ID, or custom mapping)
- Flexible comparison options (case-sensitive/insensitive)
- Detailed diff analysis with specific change highlighting
- Support for both live data and exported JSON files

#### TerraformConversionService
- Bidirectional conversion between JSON and Terraform formats
- Handles Azure resource naming conventions
- Maintains configuration consistency across formats
- Supports both individual policy and bulk conversion

#### ReportGenerationService
- Multiple output formats: console, JSON, HTML, CSV
- Customizable report templates
- Detailed analysis with policy field mappings
- Integration with comparison and baseline services

#### RemediationService
- Policy remediation workflow management
- Change impact analysis
- Rollback capability planning
- Integration with CI/CD pipelines

## Testing Strategy

### Test Organization
- **Unit tests** for all major services with comprehensive coverage
- **Integration tests** for end-to-end workflows
- **Mocking patterns** for external dependencies (Microsoft Graph API)
- **Test data management** with realistic policy scenarios

### Test Execution
- **Automatic test discovery** via xUnit framework
- **Console output management** via ConsoleOutputTestCollection
- **Parallel test execution** where appropriate
- **Coverage reporting** integrated with CI/CD

### Best Practices
- Progress.md should be used to make a plan before code changes are started. This should be updated as each plan step is completed.
- Write tests for new features following established patterns
- Mock external dependencies to ensure reliable, fast tests
- Use realistic test data that reflects actual Azure policy structures

## Documentation Locations

### Primary Documentation
- **Main README**: `/README.md` - Project overview, quick start, and features
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


# Developer Onboarding Guide

Welcome to the CA_Scanner project! This guide will help you get started with contributing to the project.

## Quick Start

1. **Clone the Repository**
   ```bash
   git clone https://github.com/thefaftek-git/CA_Scanner.git
   cd CA_Scanner
   ```

2. **Set Up Environment**
   - Install .NET 8 SDK using the provided script:
     ```bash
     ./dotnet-install.sh
     ```
   - Set up Azure credentials:
     ```bash
     export AZURE_TENANT_ID="your-tenant-id"
     export AZURE_CLIENT_ID="your-client-id"
     export AZURE_CLIENT_SECRET="your-client-secret"
     ```

3. **Build the Project**
   ```bash
   dotnet build
   ```

4. **Run Tests**
   ```bash
   dotnet test
   ```

5. **First Export**
   ```bash
   cd ConditionalAccessExporter
   dotnet run export
   ```

## Project Architecture

CA_Scanner follows a service-oriented architecture with specialized services:

- **Core Services**: BaselineGenerationService, PolicyComparisonService, TerraformConversionService
- **Report Services**: ReportGenerationService, ImpactAnalysisService
- **Utility Services**: RemediationService, PolicyValidationService

## Development Workflow

1. **Create a Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make Changes**
   - Implement your feature or fix
   - Write tests for your changes

3. **Run Tests**
   ```bash
   dotnet test
   ```

4. **Commit Changes**
   ```bash
   git add .
   git commit -m "Your commit message"
   ```

5. **Push Changes**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Create a Pull Request**
   - Go to the repository on GitHub
   - Click on "Compare & pull request"
   - Fill out the PR template

## Coding Standards

- Follow C# coding conventions
- Use meaningful variable and method names
- Write clear and concise comments
- Keep functions small and focused

## Testing Guidelines

- Write unit tests for all new functionality
- Use mocking for external dependencies
- Aim for high test coverage
- Run tests locally before pushing

## Troubleshooting

- **Authentication Issues**: Verify Azure credentials and permissions
- **Build Errors**: Check for missing dependencies or incorrect configurations
- **Test Failures**: Review test output and fix failing tests

## Additional Resources

- **Documentation**: [README.md](/README.md)
- **API Reference**: [API_REFERENCE.md](/docs/API_REFERENCE.md)
- **Examples**: [EXAMPLES.md](/docs/EXAMPLES.md)


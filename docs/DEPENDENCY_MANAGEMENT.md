# Dependency Management Guide

This document outlines the dependency management practices, automated update processes, and security measures implemented for the CA_Scanner project.

## Overview

The CA_Scanner project follows modern dependency management practices to ensure security, maintainability, and access to the latest features. This includes automated dependency updates, security scanning, and comprehensive monitoring.

## Current Dependencies

### Core Dependencies

| Package | Current Version | Purpose |
|---------|----------------|---------|
| Microsoft.Graph | 5.81.0 | Microsoft Graph API integration |
| Azure.Identity | 1.14.0 | Azure authentication and identity |
| Newtonsoft.Json | 13.0.3 | JSON serialization/deserialization |
| Newtonsoft.Json.Schema | 4.0.1 | JSON schema validation |
| System.CommandLine | 2.0.0-beta4.22272.1 | Command-line interface |
| JsonDiffPatch.Net | 2.3.0 | JSON comparison and patching |
| YamlDotNet | 16.3.0 | YAML processing |

### Microsoft Extensions (v9.0.6)

- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.Json
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Logging.Console

### Development & Testing

| Package | Current Version | Purpose |
|---------|----------------|---------|
| xunit | 2.9.3 | Testing framework |
| xunit.runner.visualstudio | 3.1.1 | Visual Studio test runner |
| Microsoft.NET.Test.Sdk | 17.14.1 | .NET testing SDK |
| coverlet.collector | 6.0.4 | Code coverage collection |
| Moq | 4.20.72 | Mocking framework |

### Security & Analysis

| Package | Current Version | Purpose |
|---------|----------------|---------|
| Microsoft.CodeAnalysis.NetAnalyzers | 9.0.0 | Static code analysis |
| SonarAnalyzer.CSharp | 10.11.0.117924 | Code quality analysis |
| SecurityCodeScan.VS2019 | 5.6.7 | Security vulnerability scanning |
| System.Text.Json | 9.0.6 | Secure JSON processing |
| BenchmarkDotNet | 0.15.2 | Performance benchmarking |

## Automated Dependency Management

### Dependabot Configuration

The project uses GitHub Dependabot for automated dependency updates with the following features:

#### Update Schedule
- **Frequency**: Weekly (Mondays at 6:00 AM UTC)
- **Pull Request Limit**: 10 concurrent PRs
- **Review Assignment**: Automatically assigned to project maintainers

#### Grouping Strategy
Dependencies are grouped by functional area for easier review:

1. **Microsoft Extensions** - All Microsoft.Extensions.* packages
2. **Microsoft Graph** - Graph API and Azure Identity packages
3. **Security Analyzers** - Static analysis and security scanning tools
4. **Test Frameworks** - Testing-related packages
5. **JSON/YAML Processing** - Serialization and data processing libraries

#### Version Constraints
- **Major Updates**: Blocked (require manual review)
- **Minor/Patch Updates**: Automatically approved
- **Pre-release Versions**: Ignored (except System.CommandLine)
- **Security Updates**: Always allowed regardless of version type

### Automated Update Workflow

The project includes a GitHub Actions workflow (`dependency-updates.yml`) that:

1. **Checks for Updates**: Scans for outdated and vulnerable packages
2. **Automated Updates**: Performs updates based on specified constraints
3. **Testing**: Validates updates through build and test execution
4. **PR Creation**: Creates pull requests with detailed change reports

#### Update Types
- **Patch**: Updates only patch versions (default)
- **Minor**: Updates patch and minor versions
- **All**: Updates all versions including major (manual trigger only)

## Security Measures

### Vulnerability Scanning

The project implements comprehensive security scanning:

1. **Package Vulnerability Detection**
   ```bash
   dotnet list package --vulnerable --include-transitive
   ```

2. **Dependency Analysis**
   ```bash
   dotnet list package --outdated
   ```

3. **Security Analyzers**
   - Microsoft.CodeAnalysis.NetAnalyzers
   - SonarAnalyzer.CSharp
   - SecurityCodeScan.VS2019

### Supply Chain Security

- **Package Signature Verification**: Enabled for all NuGet packages
- **Transitive Dependency Scanning**: Includes indirect dependencies
- **License Compliance**: Automated license checking and reporting
- **Secret Detection**: TruffleHog and GitLeaks integration

### Security Policies

1. **Security Updates**: Prioritized over feature updates
2. **Vulnerability Response**: Critical vulnerabilities addressed within 24 hours
3. **Dependency Pinning**: Major versions pinned to prevent breaking changes
4. **Regular Audits**: Weekly automated security scans

## Manual Dependency Management

### Checking for Updates

```bash
# Check for outdated packages
dotnet list package --outdated

# Check for vulnerable packages
dotnet list package --vulnerable --include-transitive

# Update specific package
dotnet add package <PackageName> --version <Version>
```

### Adding New Dependencies

1. **Evaluate Need**: Ensure the dependency addresses a genuine requirement
2. **Security Review**: Check package reputation and security history
3. **License Compatibility**: Verify license compatibility
4. **Add to Project**: Use `dotnet add package` command
5. **Update Documentation**: Update this document and project documentation

### Dependency Approval Process

1. **Proposal**: Submit dependency proposal with justification
2. **Security Review**: Automated and manual security assessment
3. **Impact Analysis**: Evaluate effect on project size and performance
4. **Testing**: Comprehensive testing with new dependency
5. **Documentation**: Update relevant documentation
6. **Approval**: Maintainer approval required for addition

## Best Practices

### Version Management

- **Semantic Versioning**: Follow SemVer principles
- **Version Constraints**: Use appropriate version ranges
- **Breaking Changes**: Carefully evaluate major version updates
- **Pinning**: Pin versions for production stability

### Security

- **Regular Updates**: Keep dependencies current
- **Vulnerability Monitoring**: Continuous security scanning
- **Minimal Dependencies**: Reduce attack surface
- **Trusted Sources**: Use only reputable package sources

### Performance

- **Bundle Size**: Monitor package impact on application size
- **Startup Time**: Evaluate dependency loading performance
- **Memory Usage**: Consider memory footprint of dependencies
- **Benchmarking**: Regular performance testing with BenchmarkDotNet

## Troubleshooting

### Common Issues

1. **Package Conflicts**
   ```bash
   # Clear package cache
   dotnet nuget locals all --clear
   
   # Restore packages
   dotnet restore --force
   ```

2. **Version Compatibility**
   ```bash
   # Check package compatibility
   dotnet list package --framework net8.0
   ```

3. **Build Failures After Updates**
   ```bash
   # Clean and rebuild
   dotnet clean
   dotnet restore
   dotnet build
   ```

### Recovery Procedures

1. **Rollback Strategy**: Maintain previous working versions
2. **Incremental Updates**: Update packages individually
3. **Testing**: Comprehensive testing after each update
4. **Documentation**: Record all changes and issues

## Monitoring and Reporting

### Metrics Tracked

- **Update Frequency**: Number of dependency updates per month
- **Security Issues**: Vulnerability count and resolution time
- **Build Health**: Success rate after dependency updates
- **Performance Impact**: Application performance metrics

### Reporting

- **Weekly Reports**: Automated dependency status reports
- **Security Alerts**: Immediate notification of vulnerabilities
- **Update Summaries**: Monthly summary of dependency changes
- **Performance Reports**: Quarterly performance impact analysis

## Future Enhancements

### Planned Improvements

1. **Advanced Vulnerability Scanning**: Integration with additional security tools
2. **Automated Rollback**: Automatic rollback on test failures
3. **Performance Regression Detection**: Automated performance monitoring
4. **Dependency Graph Visualization**: Visual dependency relationship mapping
5. **Custom Update Policies**: More granular update control

### Tool Integration

- **IDE Integration**: Enhanced Visual Studio/VS Code support
- **CI/CD Pipeline**: Deeper integration with build pipelines
- **Monitoring Tools**: Integration with application monitoring
- **Security Platforms**: Enhanced security platform integration

## Resources

### Documentation
- [Microsoft Graph SDK Documentation](https://docs.microsoft.com/en-us/graph/sdks/sdks-overview)
- [Azure Identity Documentation](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme)
- [Dependabot Configuration](https://docs.github.com/en/code-security/dependabot)

### Security Resources
- [OWASP Dependency Check](https://owasp.org/www-project-dependency-check/)
- [NuGet Security Best Practices](https://docs.microsoft.com/en-us/nuget/concepts/security-best-practices)
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)

### Tools
- [dotnet-outdated](https://github.com/dotnet-outdated/dotnet-outdated)
- [dotnet-project-licenses](https://github.com/tomchavakis/nuget-license)
- [Security Code Scan](https://security-code-scan.github.io/)

---

**Last Updated**: June 2025  
**Version**: 1.0  
**Maintainer**: CA_Scanner Team

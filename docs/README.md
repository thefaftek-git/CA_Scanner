

# CA Scanner Interactive Documentation

Welcome to the CA Scanner interactive documentation! This site provides comprehensive guidance on using the Conditional Access Policy management tool for Azure AD.

## Table of Contents
- [Quick Start](#quick-start)
- [Commands Reference](#commands-reference)
  - [Export Command](#export-command)
  - [Compare Command](#compare-command)
  - [Baseline Command](#baseline-command)
  - [Terraform Conversion](#terraform-conversion)
- [Tutorials](#tutorials)
  - [Basic Policy Export](#basic-policy-export)
  - [Policy Comparison](#policy-comparison)
  - [CI/CD Integration](#cicd-integration)
- [FAQ](#faq)
- [Contributing](#contributing)

## Quick Start

To get started with CA Scanner, you'll need to:

1. Install the .NET 8 SDK
2. Set up Azure credentials (see [Azure Setup Guide](https://github.com/thefaftek-git/CA_Scanner/blob/main/GITHUB_SECRETS_SETUP.md))
3. Clone this repository

```bash
# Clone the repository
git clone https://github.com/thefaftek-git/CA_Scanner.git

# Navigate to the project directory
cd CA_Scanner/ConditionalAccessExporter

# Build and run the application
dotnet build
dotnet run --help
```

## Commands Reference

### Export Command

Export all Conditional Access policies from your Azure AD tenant:

```bash
dotnet run export --output my-policies.json
```

**Options:**
- `--output <path>`: Specify output file path (default: timestamped JSON file)

### Compare Command

Compare Entra ID policies with reference JSON files:

```bash
dotnet run compare --reference-dir ./reference-policies
```

**Options:**
- `--reference-dir <dir>`: Directory containing reference JSON files (required)
- `--entra-file <file>`: Path to exported Entra policies JSON file
- `--output-dir <dir>`: Output directory for comparison reports (default: `comparison-reports`)
- `--formats <formats>`: Report formats to generate (default: `console json html`)

### Baseline Command

Generate baseline reference policies from current tenant:

```bash
dotnet run baseline --anonymize
```

**Options:**
- `--output-dir <dir>`: Directory to save baseline reference files (default: `reference-policies`)
- `--anonymize`: Remove tenant-specific identifiers
- `--filter-enabled-only`: Export only enabled policies

### Terraform Conversion

Convert between JSON and Terraform formats:

**JSON to Terraform:**

```bash
dotnet run json-to-terraform --input my-policies.json
```

**Terraform to JSON:**

```bash
dotnet run terraform --input my-terraform-files
```

## Tutorials

### Basic Policy Export

This tutorial walks you through exporting Conditional Access policies from your Azure AD tenant.

1. Set up your environment by following the [Quick Start](#quick-start) guide.
2. Run the export command:

```bash
dotnet run export --output my-policies.json
```

3. Verify the output file contains your policies.

### Policy Comparison

Learn how to compare live policies against reference files.

1. First, create a directory for your reference policies:

```bash
mkdir reference-policies
```

2. Copy or generate reference policy JSON files into this directory.
3. Run the comparison command:

```bash
dotnet run compare --reference-dir ./reference-policies
```

4. Review the output to identify differences.

### CI/CD Integration

Integrate CA Scanner with your CI/CD pipelines for automated policy validation.

1. Add a step in your pipeline to export policies:

```yaml
- name: Export Azure AD Policies
  run: dotnet run export --output policies.json
```

2. Compare against reference files and fail on differences:

```yaml
- name: Compare Policies
  run: dotnet run compare --reference-dir ./reference-policies --formats pipeline-json
```

3. Configure your pipeline to check the output for failures.

## FAQ

**Q: How do I set up Azure credentials?**

A: Follow the [Azure Setup Guide](https://github.com/thefaftek-git/CA_Scanner/blob/main/GITHUB_SECRETS_SETUP.md) to configure your environment variables.

**Q: Can I compare policies without exporting first?**

A: Yes, you can use the `--entra-file` option with the `compare` command to skip the export step.

**Q: How do I convert Terraform files to JSON?**

A: Use the `terraform` command followed by the path to your Terraform files:

```bash
dotnet run terraform --input my-terraform-files
```

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details on how to get involved.


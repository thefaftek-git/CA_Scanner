---
layout: default
title: Command Reference
parent: Documentation
nav_order: 2
---

# Command Reference

Detailed information about all available commands and options.

## Export Command

Export Conditional Access policies from Entra ID:

```bash
dotnet run export [--output <path>]
```

### Options

- `--output <path>`: Output file path (default: timestamped filename)

### Examples

Basic usage:

```bash
dotnet run export
```

Custom output path:

```bash
dotnet run export --output my-policies.json
```

## Baseline Command

Generate baseline reference policies from current tenant:

```bash
dotnet run baseline [options]
```

### Options

- `--output-dir <dir>`: Directory to save baseline reference files (default: "reference-policies")
- `--anonymize`: Remove tenant-specific identifiers (IDs, timestamps, tenant references) (default: false)
- `--filter-enabled-only`: Export only enabled policies (default: false)
- `--policy-names <names>`: Export specific policies by name (space or comma-separated)

### Examples

Basic usage:

```bash
dotnet run baseline
```

Anonymized output:

```bash
dotnet run baseline --anonymize
```

## Compare Command

Compare Entra policies with reference JSON files:

```bash
dotnet run compare [options]
```

### Options

- `--reference-dir <dir>` (REQUIRED): Directory containing reference JSON files
- `--entra-file <file>`: Path to exported Entra policies JSON file (optional, fetches live data if not provided)
- `--output-dir <dir>`: Output directory for comparison reports (default: "comparison-reports")
- `--formats <formats>`: Report formats to generate (space or comma-separated) (default: console|json|html)
- `--matching <ById|ByName|CustomMapping>`: Strategy for matching policies (default: ByName)
- `--case-sensitive`: Case sensitive policy name matching (default: false)
- `--explain`: Decode numeric values in console output with human-readable explanations (default: false)
- `--exit-on-differences`: Return non-zero exit codes based on comparison results (default: false)
- `--max-differences <count>`: Fail if more than specified number of policies differ
- `--fail-on <types>`: Fail on specific types of changes (comma or space-separated)
- `--ignore <types>`: Ignore specific types of differences (comma or space-separated)
- `--quiet`: Minimal output for pipeline usage (default: false)
- `--skip-validation`: Skip validation of reference files before comparison (default: false)

### Examples

Basic comparison:

```bash
dotnet run compare --reference-dir ./reference-policies
```

Custom formats:

```bash
dotnet run compare --reference-dir ./reference-policies --formats console json csv
```

## Terraform Commands

### Convert JSON to Terraform

Convert JSON conditional access policies to Terraform HCL:

```bash
dotnet run json-to-terraform [options]
```

#### Options

- `--input <file>` (REQUIRED): JSON file path containing conditional access policies
- `--output-dir <dir>`: Output directory for generated Terraform files (default: terraform-output)
- `--generate-variables`: Generate variables.tf file for reusable configurations (default: true)
- `--generate-provider`: Generate providers.tf file with version constraints (default: true)
- `--separate-files`: Generate separate .tf file for each policy (default: false)
- `--generate-module`: Generate Terraform module structure (default: false)
- `--include-comments`: Include descriptive comments in generated Terraform code (default: true)
- `--provider-version <version>`: AzureAD provider version constraint (default: ~> 2.0)

#### Examples

Basic conversion:

```bash
dotnet run json-to-terraform --input my-policies.json
```

### Convert Terraform to JSON

Convert Terraform conditional access policies to JSON:

```bash
dotnet run terraform [options]
```

#### Options

- `--input <path>` (REQUIRED): Terraform file or directory path containing conditional access policies
- `--output <path>`: Output file path for converted JSON (default: timestamped filename)
- `--validate`: Validate converted policies against Microsoft Graph schema (default: true)
- `--verbose`: Enable verbose logging during conversion (default: false)

#### Examples

Basic conversion:

```bash
dotnet run terraform --input my-terraform-files
```


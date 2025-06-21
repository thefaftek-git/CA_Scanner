---
layout: default
title: Quick Start Guide
parent: Documentation
nav_order: 1
---

# Quick Start Guide

Get up and running with CA Scanner in just a few steps.

## Prerequisites

1. **.NET 8 SDK**: Install from [Microsoft's website](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **Azure App Registration**: Create an app registration in Azure AD with `Policy.Read.All` permission
3. **Environment Variables**:
   ```bash
   export AZURE_TENANT_ID=your-tenant-id
   export AZURE_CLIENT_ID=your-client-id
   export AZURE_CLIENT_SECRET=your-client-secret
   ```

## Installation

Clone the repository and build the solution:

```bash
git clone https://github.com/thefaftek-git/CA_Scanner.git
cd CA_Scanner
dotnet build
```

## Basic Usage

### Export Policies

Export all Conditional Access policies from your Azure AD tenant:

```bash
cd ConditionalAccessExporter
dotnet run export --output my-policies.json
```

This will create a JSON file with all your policies.

### Compare Policies

Compare live policies against reference files:

```bash
dotnet run compare --reference-dir ./path-to-reference-policies
```

### Generate Baseline

Create baseline policy files from current tenant configurations:

```bash
dotnet run baseline --output-dir ./my-baselines --anonymize
```

## Next Steps

- Explore the [Command Reference](/docs/commands) for detailed information about all available commands and options.
- Learn advanced usage with our [tutorials](/docs/tutorials).
- Check out the [Examples & Use Cases](../EXAMPLES.md) for real-world scenarios.


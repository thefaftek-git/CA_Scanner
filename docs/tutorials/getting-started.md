

# Getting Started with CA_Scanner

Welcome to CA_Scanner! This tutorial will guide you through the process of setting up your development environment, running the application, and performing basic operations.

## Prerequisites

Before you begin, ensure you have the following installed:

1. **.NET 8.0 SDK**: You can install it using the provided script.
   ```bash
   ./dotnet-install.sh
   ```

2. **Git**: For version control.
   ```bash
   sudo apt-get install git
   ```

3. **Azure Environment**: An Azure subscription with Conditional Access policies and an app registration with `Policy.Read.All` permissions.

## Step 1: Clone the Repository

First, clone the CA_Scanner repository to your local machine.

```bash
git clone https://github.com/thefaftek-git/CA_Scanner.git
cd CA_Scanner
```

## Step 2: Set Up Azure Credentials

Create an Azure App Registration with the necessary permissions and set the following environment variables with your Azure credentials:

```bash
export AZURE_TENANT_ID=your-tenant-id
export AZURE_CLIENT_ID=your-client-id
export AZURE_CLIENT_SECRET=your-client-secret
```

## Step 3: Build the Solution

Build the solution to ensure everything is set up correctly.

```bash
dotnet build
```

## Step 4: Run the Application

Navigate to the `ConditionalAccessExporter` directory and run the application.

```bash
cd ConditionalAccessExporter
./run.sh
```

## Step 5: Export Policies

Use the `export` command to retrieve all Conditional Access policies from your Azure AD tenant.

```bash
dotnet run export --output-dir ./output
```

This command will create a directory named `output` containing JSON files for each policy.

## Step 6: Generate a Baseline

Create a baseline of your current policies for future comparisons.

```bash
dotnet run baseline --output-dir ./baseline
```

This command will create a directory named `baseline` containing JSON files for each policy.

## Step 7: Compare Policies

Compare your current policies against the baseline to identify any changes.

```bash
dotnet run compare --baseline-dir ./baseline --live-dir ./live --output-dir ./changes
```

This command will create a directory named `changes` containing a report of policy differences.

## Step 8: Generate a Report

Generate a detailed report of your policies in various formats (console, JSON, HTML, CSV).

```bash
dotnet run report --input-dir ./live --output-dir ./audit-report --format html
```

This command will create an HTML report in the `audit-report` directory detailing all policies and their configurations.

## Step 9: Remediate Policies

Quickly identify and remediate policy changes during an incident.

```bash
dotnet run compare --baseline-dir ./baseline --live-dir ./live --output-dir ./incident-report --remediate
```

This command will create a report of policy changes and perform automated remediation actions.

## Conclusion

Congratulations! You have successfully set up CA_Scanner, exported policies, generated a baseline, compared policies, generated a report, and remediated policies. For more advanced usage and examples, refer to the [EXAMPLES.md](EXAMPLES.md) file.


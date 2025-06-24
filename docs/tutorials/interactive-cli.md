


# Interactive CLI Tutorial

This tutorial will guide you through using the CA_Scanner CLI interactively. You'll learn how to perform various operations and customize the CLI experience.

## Prerequisites

Before you begin, ensure you have completed the [Getting Started Tutorial](getting-started.md) and have the CA_Scanner application running.

## Step 1: Basic Commands

### Export Policies

To export all Conditional Access policies from your Azure AD tenant, use the `export` command.

```bash
dotnet run export --output-dir ./output
```

This command will create a directory named `output` containing JSON files for each policy.

### Generate a Baseline

To create a baseline of your current policies for future comparisons, use the `baseline` command.

```bash
dotnet run baseline --output-dir ./baseline
```

This command will create a directory named `baseline` containing JSON files for each policy.

## Step 2: Customizing the CLI Experience

### Command-Line Options

CA_Scanner provides various command-line options to customize the CLI experience. Here are some examples:

- **Timeout**: Increase the timeout value for export operations.

  ```bash
  dotnet run export --timeout 300
  ```

- **Batch Size**: Limit the number of policies exported in each batch.

  ```bash
  dotnet run export --batch-size 50
  ```

- **Retry**: Retry failed policy exports.

  ```bash
  dotnet run export --retry 3
  ```

- **Parallel**: Run operations in parallel.

  ```bash
  dotnet run export --parallel 4
  ```

- **Memory Limit**: Limit memory usage.

  ```bash
  dotnet run export --memory-limit 2GB
  ```

- **Proxy**: Configure a proxy server.

  ```bash
  dotnet run export --proxy http://proxy.example.com:8080
  ```

- **Proxy Authentication**: Provide proxy authentication credentials.

  ```bash
  dotnet run export --proxy-auth user:password
  ```

## Step 3: Advanced Commands

### Compare Policies

To compare your current policies against the baseline, use the `compare` command.

```bash
dotnet run compare --baseline-dir ./baseline --live-dir ./live --output-dir ./changes
```

This command will create a directory named `changes` containing a report of policy differences.

### Generate a Report

To generate a detailed report of your policies in various formats (console, JSON, HTML, CSV), use the `report` command.

```bash
dotnet run report --input-dir ./live --output-dir ./audit-report --format html
```

This command will create an HTML report in the `audit-report` directory detailing all policies and their configurations.

### Remediate Policies

To quickly identify and remediate policy changes during an incident, use the `compare` command with the `--remediate` option.

```bash
dotnet run compare --baseline-dir ./baseline --live-dir ./live --output-dir ./incident-report --remediate
```

This command will create a report of policy changes and perform automated remediation actions.

## Step 4: Troubleshooting

If you encounter any issues, refer to the [TROUBLESHOOTING.md](TROUBLESHOOTING.md) file for solutions and common problems.

## Conclusion

Congratulations! You have successfully used the CA_Scanner CLI interactively. For more advanced usage and examples, refer to the [EXAMPLES.md](EXAMPLES.md) file.



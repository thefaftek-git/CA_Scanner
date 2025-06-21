---
layout: default
title: FAQ
parent: Documentation
nav_order: 4
---

# Frequently Asked Questions

## General Questions

### What is CA Scanner?

CA Scanner is a .NET 8 Azure Conditional Access Policy management tool designed for Azure administrators, DevOps engineers, and security teams. It provides comprehensive capabilities for managing and analyzing Conditional Access policies in Microsoft 365 environments.

### Is CA Scanner free to use?

Yes, CA Scanner is open-source and free to use. You can find the source code on [GitHub](https://github.com/thefaftek-git/CA_Scanner).

## Installation & Setup

### What are the system requirements?

- .NET 8 SDK
- Azure App Registration with appropriate permissions
- Environment variables for Azure credentials

### How do I set up an Azure App Registration?

1. Go to [Azure Portal](https://portal.azure.com/)
2. Navigate to **Azure Active Directory** > **App registrations**
3. Click **New registration** and create an app
4. After creating, go to **API permissions** and add `Policy.Read.All`
5. Click **Grant admin consent**

## Usage Questions

### How do I export policies?

Use the `export` command:

```bash
dotnet run export --output my-policies.json
```

### How do I compare policies?

Use the `compare` command with a reference directory:

```bash
dotnet run compare --reference-dir ./reference-policies
```

### What formats can I use for reports?

You can generate reports in console, JSON, HTML, and CSV formats.

## Terraform Integration

### Can I convert JSON policies to Terraform?

Yes, use the `json-to-terraform` command:

```bash
dotnet run json-to-terraform --input exported-policies.json
```

### Can I convert Terraform back to JSON?

Yes, use the `terraform` command:

```bash
dotnet run terraform --input path/to/terraform/files
```

## CI/CD Integration

### How can I integrate CA Scanner with my CI/CD pipeline?

See our [CI/CD Integration Tutorial](/tutorials.html#cicd-integration) for examples of how to use CA Scanner in GitHub Actions and other pipelines.

## Troubleshooting

### What if I encounter permission errors?

Ensure your app registration has the `Policy.Read.All` permission and admin consent has been granted.

### How do I handle authentication errors?

Verify that your tenant ID, client ID, and client secret are correct. Ensure the client secret hasn't expired and that the app registration is enabled.

## Contributing

### How can I contribute to CA Scanner?

Check out our [Contribution Guide](/contributing.html) for details on how you can help improve CA Scanner.


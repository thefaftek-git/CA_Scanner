

# Frequently Asked Questions (FAQ)

## General Information

### What is CA_Scanner?
CA_Scanner is a .NET 8 Azure Conditional Access Policy management tool designed for Azure administrators, DevOps engineers, and security teams. It provides comprehensive capabilities for managing and analyzing Conditional Access policies in Microsoft 365 environments.

### Why should I use CA_Scanner?
CA_Scanner helps you efficiently manage your Azure Conditional Access policies by providing features like:
- Exporting all policies from Azure AD
- Comparing live policies against reference JSON files
- Generating baseline policy files
- Converting between JSON and Terraform formats for Infrastructure as Code workflows
- Generating detailed reports in multiple formats

### Is CA_Scanner free to use?
Yes, CA_Scanner is open-source software released under the MIT license. You can use it freely in your projects.

## Getting Started

### What are the prerequisites for using CA_Scanner?
You need:
1. .NET 8 SDK installed on your system
2. An Azure App Registration with appropriate permissions (Policy.Read.All)
3. Environment variables set with your Azure credentials (AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET)

### How do I install CA_Scanner?
You can clone the repository from GitHub and build it using .NET CLI:
```bash
git clone https://github.com/thefaftek-git/CA_Scanner.git
cd CA_Scanner
dotnet build
```

### How do I set up my Azure credentials?
Follow the instructions in [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md) to create an Azure App Registration and configure your environment variables.

## Usage

### What is the basic usage of CA_Scanner?
The most common command is exporting policies:
```bash
cd ConditionalAccessExporter
dotnet run export
```
This will generate a JSON file with all your Conditional Access policies.

### How do I compare policies?
You can compare live policies against reference files using:
```bash
dotnet run compare --reference-dir ./policy-baselines
```

### Can I convert policies to Terraform format?
Yes, you can convert JSON policies to Terraform HCL using:
```bash
dotnet run json-to-terraform --input exported-policies.json --output-dir ./terraform
```

## Troubleshooting

### What if I encounter authentication errors?
Check that your Azure App Registration has the correct permissions and that your environment variables are set correctly. See [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md) for setup instructions.

### How do I handle large tenants with many policies?
For large tenants, consider using the `--batch-size` option to process policies in batches:
```bash
dotnet run export --batch-size 50
```

## Advanced Features

### Can CA_Scanner validate my policies against security best practices?
Yes, CA_Scanner includes a Policy Validation Engine that can assess your policies for compliance with security standards like NIST, ISO27001, and SOC2.

### Does CA_Scanner support CI/CD integration?
Yes, you can integrate CA_Scanner into your CI/CD pipelines to automate policy management tasks. See [CICD.md](CICD.md) for more information.

## Contributing

### How can I contribute to CA_Scanner?
We welcome contributions! Please see our [CONTRIBUTING.md](CONTRIBUTING.md) file for guidelines on how to get started.

### Where can I report bugs or request features?
You can open issues directly in the GitHub repository: https://github.com/thefaftek-git/CA_Scanner/issues

## Community

### Is there a community forum for CA_Scanner users?
Yes, you can join discussions and ask questions on our GitHub Discussions page: https://github.com/thefaftek-git/CA_Scanner/discussions

### Are there any user testimonials available?
Check out our [TESTIMONIALS.md](TESTIMONIALS.md) file for feedback from users who have successfully implemented CA_Scanner in their environments.

## License

### What is the licensing model for CA_Scanner?
CA_Scanner is released under the MIT license, which allows you to use, modify, and distribute the software freely. See [LICENSE](LICENSE) for details.

---
layout: default
title: Contribution Guide
parent: Documentation
nav_order: 5
---

# Contribution Guide

Thank you for considering contributing to CA Scanner! We welcome contributions from the community.

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [How Can I Help?](#how-can-i-help)
3. [Setting Up Your Development Environment](#setting-up-your-development-environment)
4. [Contribution Workflow](#contribution-workflow)
5. [Coding Guidelines](#coding-guidelines)
6. [Testing](#testing)
7. [Documentation](#documentation)

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](https://www.contributor-covenant.org/version/2/0/code_of_conduct/). By participating, you are expected to uphold this code.

## How Can I Help?

There are many ways you can contribute:

- Report bugs or suggest features
- Improve documentation
- Add new features
- Fix existing issues

## Setting Up Your Development Environment

1. **Clone the repository**:

   ```bash
   git clone https://github.com/thefaftek-git/CA_Scanner.git
   cd CA_Scanner
   ```

2. **Install .NET 8 SDK** if you haven't already:

   Download from [Microsoft's website](https://dotnet.microsoft.com/download/dotnet/8.0).

3. **Build the solution**:

   ```bash
   dotnet build
   ```

4. **Set up Azure App Registration** for testing (see [FAQ](/faq.html) for details).

## Contribution Workflow

1. Fork the repository and create a new branch from `main`:

   ```bash
   git checkout -b my-feature-branch
   ```

2. Make your changes and commit them with descriptive messages.

3. Push your branch to GitHub:

   ```bash
   git push origin my-feature-branch
   ```

4. Create a Pull Request against the `main` branch.

## Coding Guidelines

- Follow the existing code style and conventions.
- Write clean, readable code with appropriate comments.
- Keep methods small and focused on a single responsibility.
- Use meaningful variable and method names.

### Code Style

- Use 4 spaces for indentation
- Follow PascalCase for class names, camelCase for variables and methods
- Avoid magic numbers - use constants instead
- Use async/await patterns for asynchronous operations

## Testing

- Add unit tests for new features in the `ConditionalAccessExporter.Tests` project.
- Run all tests before submitting a PR:

  ```bash
  dotnet test
  ```

## Documentation

- Update or add documentation as needed.
- Follow the existing markdown style.
- Keep documentation clear and concise.

## Submitting Changes

1. Ensure your code follows the coding guidelines.
2. Add appropriate tests.
3. Update documentation if necessary.
4. Create a Pull Request with a detailed description of your changes.

Thank you for contributing to CA Scanner!


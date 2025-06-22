


# Basic Usage Tutorial

Welcome to the Basic Usage Tutorial for CA_Scanner! This guide will walk you through the fundamental steps to get started with CA_Scanner.

## Table of Contents

- [Introduction](#introduction)
- [Installation](#installation)
- [Configuration](#configuration)
- [Running CA_Scanner](#running-ca_scanner)
- [Basic Commands](#basic-commands)
- [Conclusion](#conclusion)

## Introduction

CA_Scanner is a powerful tool for managing and analyzing Conditional Access policies in Microsoft 365 environments. This tutorial will guide you through the basic usage of CA_Scanner.

## Installation

To install CA_Scanner, follow these steps:

1. **Clone the repository**:
   ```bash
   git clone https://github.com/thefaftek-git/CA_Scanner.git
   ```

2. **Navigate to the project directory**:
   ```bash
   cd CA_Scanner
   ```

3. **Install the .NET SDK**:
   ```bash
   ./dotnet-install.sh
   ```

4. **Build the project**:
   ```bash
   dotnet build
   ```

## Configuration

Before running CA_Scanner, you need to configure it with your Azure credentials. Set the following environment variables:

```bash
export AZURE_TENANT_ID=your-tenant-id-here
export AZURE_CLIENT_ID=your-client-id-here
export AZURE_CLIENT_SECRET=your-client-secret-here
```

## Running CA_Scanner

To run CA_Scanner, use the following command:

```bash
dotnet run --project ConditionalAccessExporter
```

## Basic Commands

Here are some basic commands to get you started with CA_Scanner:

- **Export policies**:
  ```bash
  dotnet run --project ConditionalAccessExporter -- export
  ```

- **Compare policies**:
  ```bash
  dotnet run --project ConditionalAccessExporter -- compare
  ```

- **Generate baseline**:
  ```bash
  dotnet run --project ConditionalAccessExporter -- baseline
  ```

## Conclusion

This tutorial covered the basic usage of CA_Scanner. For more advanced features and detailed information, refer to the [Advanced Features Tutorial](advanced-features.md) and the [API Reference](api-reference.md).




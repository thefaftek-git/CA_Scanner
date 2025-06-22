



# Advanced Features Tutorial

Welcome to the Advanced Features Tutorial for CA_Scanner! This guide will walk you through the advanced features and techniques to make the most of CA_Scanner.

## Table of Contents

- [Introduction](#introduction)
- [Policy Comparison](#policy-comparison)
- [Baseline Generation](#baseline-generation)
- [Terraform Conversion](#terraform-conversion)
- [Report Generation](#report-generation)
- [Remediation](#remediation)
- [Conclusion](#conclusion)

## Introduction

CA_Scanner offers advanced features to help you manage and analyze Conditional Access policies in Microsoft 365 environments. This tutorial will guide you through these advanced features.

## Policy Comparison

CA_Scanner allows you to compare live policies against reference JSON files with multiple matching strategies. To compare policies, use the following command:

```bash
dotnet run --project ConditionalAccessExporter -- compare
```

## Baseline Generation

You can create reference policy files from your current tenant configuration. To generate a baseline, use the following command:

```bash
dotnet run --project ConditionalAccessExporter -- baseline
```

## Terraform Conversion

CA_Scanner can convert between JSON and Terraform formats for Infrastructure as Code workflows. To convert policies, use the following command:

```bash
dotnet run --project ConditionalAccessExporter -- terraform-convert
```

## Report Generation

Generate detailed reports in multiple formats (console, JSON, HTML, CSV). To generate a report, use the following command:

```bash
dotnet run --project ConditionalAccessExporter -- report
```

## Remediation

Handle policy remediation workflows with comprehensive analysis. To remediate policies, use the following command:

```bash
dotnet run --project ConditionalAccessExporter -- remediate
```

## Conclusion

This tutorial covered the advanced features of CA_Scanner. For more detailed information and examples, refer to the [API Reference](api-reference.md) and the [Examples](examples.md).





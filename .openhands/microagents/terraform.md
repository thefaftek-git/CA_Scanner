---
triggers:
  - terraform
  - hcl
  - conversion
  - "terraform conversion"
  - "json to terraform"
  - "terraform to json"
  - azuread_conditional_access_policy
---

# Terraform Microagent

This microagent provides specialized guidance for Terraform-related tasks, including JSON-to-Terraform conversion, Terraform-to-JSON conversion, and cross-format comparisons within the CA_Scanner project.

## Overview

The CA_Scanner project includes comprehensive Terraform integration capabilities that enable bidirectional conversion between Microsoft Graph JSON conditional access policies and Terraform HCL format. The system focuses specifically on `azuread_conditional_access_policy` resources and provides cross-format comparison functionality.

## Terraform Integration Overview

- **Main Services**: TerraformConversionService.cs and TerraformParsingService.cs
- **Resource Type**: Focuses on `azuread_conditional_access_policy` resources
- **Conversion Directions**: Bidirectional (JSON ↔ Terraform)
- **Cross-Format Comparison**: Handle JSON vs Terraform comparisons

## Key Terraform Services

### 1. TerraformConversionService.cs
Converts Terraform HCL configurations to Microsoft Graph JSON format:
- Parses Terraform policy structures into Graph API-compatible JSON
- Handles field mapping from Terraform string values to JSON numeric codes
- Provides conversion logging and error tracking
- Supports batch conversion of multiple policies

### 2. TerraformParsingService.cs
Parses Terraform files and extracts conditional access policy configurations:
- Reads HCL files and extracts `azuread_conditional_access_policy` resources
- Handles Terraform-specific constructs (variables, locals, data sources)
- Validates Terraform syntax and structure
- Extracts policy metadata and resource dependencies

### 3. JsonToTerraformService.cs
Specialized JSON-to-Terraform conversion logic:
- Converts exported JSON policies to Terraform HCL format
- Generates proper Terraform resource blocks with correct syntax
- Handles complex nested structures (conditions, grant controls, session controls)
- Creates modular Terraform code with meaningful resource names
- Supports various output options (single file, separate files, module structure)

### 4. CrossFormatPolicyComparisonService.cs
Compares policies across JSON and Terraform formats:
- Normalizes both formats to common internal representation
- Performs semantic equivalence checking
- Generates unified comparison reports
- Handles format-specific nuances and differences

## Terraform Conversion Patterns

### Field Mapping: JSON ↔ Terraform

#### Built-in Controls
```
JSON: BuiltInControls: [1] → Terraform: built_in_controls = ["mfa"]
JSON: BuiltInControls: [2] → Terraform: built_in_controls = ["compliantDevice"]
JSON: BuiltInControls: [3] → Terraform: built_in_controls = ["domainJoinedDevice"]
JSON: BuiltInControls: [6] → Terraform: built_in_controls = ["block"]
```

#### Client App Types
```
JSON: ClientAppTypes: [0] → Terraform: client_app_types = ["browser"]
JSON: ClientAppTypes: [1] → Terraform: client_app_types = ["mobileAppsAndDesktopClients"]
JSON: ClientAppTypes: [2] → Terraform: client_app_types = ["exchangeActiveSync"]
JSON: ClientAppTypes: [3] → Terraform: client_app_types = ["other"]
```

#### Risk Levels
```
JSON: SignInRiskLevels: ["low"] → Terraform: sign_in_risk_levels = ["low"]
JSON: UserRiskLevels: ["medium"] → Terraform: user_risk_levels = ["medium"]
```

### Structure Mapping: Microsoft Graph JSON → Terraform Resource Blocks

Microsoft Graph JSON structure maps to Terraform `azuread_conditional_access_policy` resource with proper HCL syntax and nested block structures.

### Resource Naming
Generate meaningful Terraform resource names from policy display names:
- Sanitize special characters and spaces
- Convert to valid Terraform identifier format
- Ensure uniqueness across multiple policies

## Terraform Resource Structure

Understanding the azuread_conditional_access_policy resource structure:

```hcl
resource "azuread_conditional_access_policy" "example" {
  display_name = "Policy Name"
  state        = "enabled"
  
  conditions {
    applications {
      included_applications = ["All"]
      excluded_applications = []
    }
    users {
      included_users  = ["All"]
      excluded_users  = []
      included_groups = []
      excluded_groups = []
      included_roles  = []
      excluded_roles  = []
    }
    client_app_types = ["browser", "mobileAppsAndDesktopClients"]
    
    platforms {
      included_platforms = ["android", "iOS"]
      excluded_platforms = []
    }
    
    locations {
      included_locations = ["All"]
      excluded_locations = []
    }
    
    sign_in_risk_levels = ["low", "medium"]
    user_risk_levels    = ["high"]
  }
  
  grant_controls {
    operator          = "OR"
    built_in_controls = ["mfa"]
    custom_authentication_factors = []
    terms_of_use     = []
    
    authentication_strength {
      id           = "strength-id"
      display_name = "Custom Strength"
    }
  }
  
  session_controls {
    application_enforced_restrictions {
      enabled = true
    }
    
    cloud_app_security {
      cloud_app_security_type = "mcasConfigured"
      enabled                  = true
    }
    
    sign_in_frequency {
      enabled   = true
      type      = "hours"
      value     = 1
      frequency_interval = "timeBased"
    }
    
    persistent_browser {
      enabled = true
      mode    = "always"
    }
  }
}
```

## Common Terraform Tasks

### 1. JSON to Terraform Conversion
Convert exported conditional access policies to Terraform configurations:

```bash
# Convert JSON export to Terraform
dotnet run convert-to-terraform --input policies.json --output-dir ./terraform

# With specific options
dotnet run convert-to-terraform --input policies.json --output-dir ./terraform --separate-files --generate-variables
```

**Key Features:**
- Single file or separate file per policy
- Variable generation for reusable values
- Provider configuration generation
- Module structure creation
- Comment inclusion for documentation

### 2. Terraform to JSON Conversion
Parse existing Terraform and convert to JSON for comparison:

```bash
# Parse Terraform and convert to JSON
dotnet run convert-from-terraform --terraform-dir ./terraform --output policies.json

# Parse specific Terraform files
dotnet run convert-from-terraform --terraform-files ./main.tf,./policies.tf --output converted.json
```

**Capabilities:**
- HCL parsing and validation
- Variable resolution
- Resource extraction
- JSON schema compliance

### 3. Cross-Format Comparison
Compare live Azure policies against Terraform definitions:

```bash
# Cross-format comparison
dotnet run compare --reference-dir ./terraform-policies --entra-file live-policies.json

# Detailed comparison with matching options
dotnet run compare --source-dir ./terraform --reference-dir ./json-policies --format-agnostic --semantic-matching
```

**Comparison Features:**
- Format-agnostic policy matching
- Semantic equivalence checking
- Detailed diff reporting
- Configuration drift detection

### 4. Template Generation
Create reusable Terraform modules from policy JSON:

```bash
# Generate Terraform modules
dotnet run generate-terraform-module --input baseline-policies.json --module-name "baseline_ca_policies"
```

## Terraform-Specific Development Guidelines

### HCL Syntax Requirements
- Proper indentation (2 spaces)
- Correct block structure and nesting
- Valid Terraform identifiers for resource names
- Proper string escaping for special characters

### Provider Requirements
Include appropriate azuread provider version constraints:

```hcl
terraform {
  required_providers {
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.47.0"
    }
  }
}
```

### Variable Usage
Generate variables for tenant-specific values:

```hcl
variable "tenant_id" {
  description = "Azure AD Tenant ID"
  type        = string
}

variable "break_glass_group_id" {
  description = "Break glass group object ID"
  type        = string
}
```

### Resource Dependencies
Handle Terraform resource references and dependencies:

```hcl
resource "azuread_conditional_access_policy" "example" {
  # Reference other resources
  conditions {
    users {
      excluded_groups = [var.break_glass_group_id]
    }
  }
}
```

## Field Value Mappings (Terraform Context)

### Built-in Controls
Use string values instead of numeric codes:
- `"mfa"` - Multi-factor authentication
- `"compliantDevice"` - Compliant device required
- `"domainJoinedDevice"` - Domain joined device required
- `"approvedApplication"` - Approved application required
- `"compliantApplication"` - Compliant application required
- `"block"` - Block access

### Client App Types
Use descriptive strings:
- `"browser"` - Browser applications
- `"mobileAppsAndDesktopClients"` - Mobile apps and desktop clients
- `"exchangeActiveSync"` - Exchange ActiveSync clients
- `"other"` - Other clients

### Risk Levels
Use Azure AD risk level strings:
- `"low"` - Low risk
- `"medium"` - Medium risk
- `"high"` - High risk

### Application IDs
Handle special values and specific application GUIDs:
- `"All"` - All cloud applications
- `"None"` - No applications
- Specific GUIDs for individual applications

## Testing Terraform Features

### Conversion Tests
Test round-trip conversions to ensure data integrity:

```csharp
[Fact]
public async Task ConvertJsonToTerraform_ThenBackToJson_ShouldMaintainEquivalence()
{
    // JSON → Terraform → JSON conversion test
    var originalJson = await LoadTestPolicyJson();
    var terraformResult = await _jsonToTerraformService.ConvertAsync(originalJson);
    var backToJsonResult = await _terraformConversionService.ConvertToGraphJson(terraformResult);
    
    // Assert semantic equivalence
    AssertPoliciesEquivalent(originalJson, backToJsonResult);
}
```

### HCL Validation
Ensure generated Terraform is syntactically valid:

```csharp
[Fact]
public async Task GeneratedTerraform_ShouldBeValidHCL()
{
    var result = await _service.ConvertJsonToTerraformAsync(testJsonPath);
    var terraformContent = await File.ReadAllTextAsync(result.MainTerraformFile);
    
    // Validate HCL syntax
    var validationResult = await ValidateHclSyntax(terraformContent);
    Assert.True(validationResult.IsValid);
}
```

### Semantic Equivalence
Verify converted policies maintain same functionality:

```csharp
[Fact]
public async Task ConvertedPolicy_ShouldHaveSemanticEquivalence()
{
    // Test that Terraform policy enforces same access rules as JSON
    var jsonPolicy = CreateTestPolicy();
    var terraformPolicy = await ConvertToTerraform(jsonPolicy);
    
    AssertSemanticEquivalence(jsonPolicy, terraformPolicy);
}
```

### Provider Compatibility
Test with different azuread provider versions:

```bash
# Test with different provider versions
terraform init -upgrade
terraform plan -var-file="test.tfvars"
terraform validate
```

## Common Terraform Issues & Solutions

### HCL Formatting Issues
**Problem**: Generated Terraform has incorrect formatting
**Solution**: Use proper indentation (2 spaces), ensure block structure follows HCL conventions

### String Escaping Issues
**Problem**: Special characters in policy names cause syntax errors
**Solution**: Properly escape quotes, backslashes, and other special characters in HCL strings

### Resource Naming Issues
**Problem**: Invalid Terraform resource names generated from display names
**Solution**: Sanitize names by removing/replacing invalid characters, ensure uniqueness

### Provider Version Issues
**Problem**: Compatibility issues with different azuread provider versions
**Solution**: Specify version constraints, test with supported provider versions

## Future Enhancement Context

Reference `todo_tasks/future_enhancements.md` for planned Terraform features:

- **Task 2**: Terraform to JSON conversion (implements TerraformConversionService functionality)
- **Task 3**: JSON to Terraform conversion (implements JsonToTerraformService functionality)  
- **Task 4**: Cross-format comparison capabilities (implements CrossFormatPolicyComparisonService functionality)

These services are already implemented and provide the foundation for the planned enhancements.

## Models and Data Structures

### TerraformParseResult
Contains parsed Terraform configuration data:
- Policies: List of TerraformConditionalAccessPolicy objects
- Variables: Terraform variable definitions
- Locals: Local value definitions
- DataSources: Data source configurations
- Errors and Warnings: Parsing issues

### TerraformConditionalAccessPolicy
Represents a Terraform conditional access policy:
- ResourceName: Terraform resource identifier
- DisplayName: Policy display name
- State: Policy state (enabled/disabled)
- Conditions: Policy conditions (nested structure)
- GrantControls: Access grant controls
- SessionControls: Session control settings

### JsonToTerraformResult
Contains conversion results and metadata:
- ConvertedAt: Conversion timestamp
- SourcePath: Input file path
- OutputPath: Generated files location
- SuccessfulConversions: Count of successful conversions
- FailedConversions: Count of failed conversions
- GeneratedFiles: List of created Terraform files

## Benefits for OpenHands

This microagent helps OpenHands:

1. **Understand Terraform Architecture**: Navigate conversion services and their interactions
2. **Handle Field Mappings**: Correctly map between JSON numeric codes and Terraform string values
3. **Generate Valid HCL**: Create syntactically correct and semantically equivalent Terraform code
4. **Perform Cross-Format Operations**: Compare and convert between JSON and Terraform formats
5. **Follow Best Practices**: Implement Terraform conventions for Azure AD resources
6. **Troubleshoot Issues**: Identify and resolve common conversion and parsing problems
7. **Test Conversions**: Validate round-trip conversions and semantic equivalence
8. **Handle Complex Scenarios**: Work with nested structures, variables, and resource dependencies

The microagent provides comprehensive guidance for all Terraform-related operations within the CA_Scanner project, enabling effective development and maintenance of the conversion capabilities.

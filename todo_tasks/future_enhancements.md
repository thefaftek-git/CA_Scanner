# Future Enhancements for CA_Scanner

This document outlines planned enhancements and new features for the Conditional Access Policy Exporter application.

## 📋 Task List

### Task 1: Add Comparison Feature for Entra vs Static JSON Files

**Objective**: Implement functionality to compare the exported Conditional Access policies from Entra ID with static JSON files stored in a specified folder.

**Prompt**: 
Create a new feature that allows users to:
- Specify a directory containing reference JSON files with expected CA policy configurations
- Compare the live Entra ID policies against these reference files
- Generate a comparison report showing:
  - Policies that exist in Entra but not in reference files
  - Policies that exist in reference files but not in Entra
  - Policies that exist in both but have configuration differences
  - Detailed diff output highlighting specific configuration changes
- Support for flexible matching (by policy name, ID, or custom mapping)
- Output comparison results in multiple formats (JSON, HTML report, console output)

**Technical Requirements**:
- Add command line parameters for comparison mode
- Implement JSON diff algorithms
- Create comparison result models
- Add report generation functionality

---

### Task 2: Add Support for Terraform to CA Policy JSON Conversion

**Objective**: Implement functionality to convert Terraform conditional access policy configurations to the standard CA policy JSON format used by the application.

**Prompt**:
Develop a converter that can:
- Parse Terraform HCL files containing `azuread_conditional_access_policy` resources
- Extract policy configurations from Terraform state files
- Convert Terraform policy syntax to Microsoft Graph API JSON format
- Handle Terraform-specific constructs (variables, locals, data sources)
- Validate the converted JSON against the Microsoft Graph schema
- Support batch conversion of multiple Terraform files
- Provide detailed conversion logs and error reporting

**Technical Requirements**:
- Add HCL parsing library dependency
- Create Terraform-to-JSON mapping logic
- Implement schema validation
- Add CLI parameters for Terraform input processing

---

### Task 3: Add Support for CA Policy JSON to Terraform Conversion

**Objective**: Implement functionality to convert CA policy JSON files (exported from Entra or created manually) to Terraform HCL format.

**Prompt**:
Create a converter that can:
- Read CA policy JSON files (single or batch)
- Generate valid Terraform `azuread_conditional_access_policy` resource configurations
- Handle complex nested structures (conditions, grant controls, session controls)
- Generate appropriate Terraform variable files for reusable configurations
- Create modular Terraform code with proper resource naming conventions
- Include Terraform provider requirements and version constraints
- Generate commented Terraform code for better maintainability
- Support different output structures (single file, module structure, separate files per policy)

**Technical Requirements**:
- Implement JSON-to-HCL conversion logic
- Create Terraform code generation templates
- Add validation for generated Terraform syntax
- Support configurable output formatting options

---

### Task 4: Extend Comparison Feature for JSON and Terraform Files

**Objective**: Extend the comparison functionality from Task 1 to support comparing JSON files with Terraform files, enabling cross-format policy validation.

**Prompt**:
Enhance the comparison engine to:
- Compare CA policy JSON files against Terraform HCL files
- Convert both formats to a common internal representation for comparison
- Support bidirectional comparison (JSON vs Terraform and Terraform vs JSON)
- Generate unified comparison reports regardless of source format
- Handle format-specific nuances and differences
- Provide conversion suggestions when differences are found
- Support bulk comparison operations across mixed file types
- Include semantic equivalence checking (same policy intent, different syntax)

**Technical Requirements**:
- Extend the comparison engine with format-agnostic logic
- Create unified internal policy representation
- Implement cross-format normalization
- Add advanced diff algorithms for semantic comparison

---

## 🚀 Implementation Priorities

1. **Phase 1**: Task 1 (JSON vs JSON comparison) - Foundation for comparison functionality
2. **Phase 2**: Task 2 (Terraform to JSON) - Input format expansion
3. **Phase 3**: Task 3 (JSON to Terraform) - Output format expansion  
4. **Phase 4**: Task 4 (Cross-format comparison) - Advanced comparison features

## 📋 Additional Considerations

### Common Requirements for All Tasks:
- Maintain backward compatibility with existing functionality
- Add comprehensive unit tests for new features
- Update documentation and README files
- Follow existing code patterns and architecture
- Implement proper error handling and logging
- Add configuration options via command line and config files

### Performance Considerations:
- Optimize for large numbers of policies
- Implement parallel processing where applicable
- Add progress indicators for long-running operations
- Consider memory usage for large file processing

### User Experience:
- Provide clear command-line interface
- Add verbose/quiet output modes
- Include examples and usage documentation
- Implement dry-run modes for validation

### Testing Strategy:
- Create comprehensive test datasets
- Include edge cases and error scenarios
- Test with real-world Terraform and JSON configurations
- Validate against Microsoft Graph API specifications

---

## 📝 Notes

- Each task should include appropriate CLI parameters and help documentation
- Consider adding a web interface for easier use in the future
- Ensure all features work with the existing authentication and export functionality
- Plan for extensibility to support additional formats (PowerShell, ARM templates, etc.)
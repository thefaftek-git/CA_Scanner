using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Tests
{
    public class TerraformParsingServiceTests : IDisposable
    {
        private readonly TerraformParsingService _parsingService;
        private readonly string _testDirectory;

        public TerraformParsingServiceTests()
        {
            _parsingService = new TerraformParsingService();
            _testDirectory = Path.Combine(Path.GetTempPath(), "TerraformParsingServiceTests_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        #region ParseTerraformFileAsync Tests

        [Fact]
        public async Task ParseTerraformFileAsync_ValidTfFileWithSinglePolicy_ShouldParseSuccessfully()
        {
            // Arrange
            var tfContent = @"
resource ""azuread_conditional_access_policy"" ""test_policy"" {
  display_name = ""Test MFA Policy""
  state        = ""enabled""

  conditions {
    applications {
      include_applications = [""All""]
      exclude_applications = [""excluded-app-id""]
    }
    
    users {
      include_users = [""All""]
      exclude_users = [""admin-user-id""]
    }
    
    client_app_types = [""browser"", ""mobileAppsAndDesktopClients""]
  }

  grant_controls {
    operator          = ""OR""
    built_in_controls = [""mfa""]
  }
}";
            var filePath = CreateTestFile("single_policy.tf", tfContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(filePath, result.SourcePath);
            Assert.True(result.ParsedAt > DateTime.MinValue);
            Assert.Empty(result.Errors);
            Assert.Single(result.Policies);

            var policy = result.Policies.First();
            Assert.Equal("test_policy", policy.ResourceName);
            Assert.Equal("Test MFA Policy", policy.DisplayName);
            Assert.Equal("enabled", policy.State);
            Assert.NotNull(policy.Conditions);
            Assert.NotNull(policy.GrantControls);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_ValidTfFileWithMultiplePolicies_ShouldParseAll()
        {
            // Arrange
            var tfContent = @"
resource ""azuread_conditional_access_policy"" ""policy_one"" {
  display_name = ""Policy One""
  state        = ""enabled""
}

resource ""azuread_conditional_access_policy"" ""policy_two"" {
  display_name = ""Policy Two""
  state        = ""disabled""
}

resource ""azuread_conditional_access_policy"" ""policy_three"" {
  display_name = ""Policy Three""
  state        = ""report""
}";
            var filePath = CreateTestFile("multiple_policies.tf", tfContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.Equal(3, result.Policies.Count);
            
            Assert.Contains(result.Policies, p => p.ResourceName == "policy_one" && p.DisplayName == "Policy One");
            Assert.Contains(result.Policies, p => p.ResourceName == "policy_two" && p.DisplayName == "Policy Two");
            Assert.Contains(result.Policies, p => p.ResourceName == "policy_three" && p.DisplayName == "Policy Three");
        }

        [Fact]
        public async Task ParseTerraformFileAsync_ValidTfStateFile_ShouldParseSuccessfully()
        {
            // Arrange
            var stateContent = @"{
  ""version"": 4,
  ""terraform_version"": ""1.0.0"",
  ""resources"": [
    {
      ""mode"": ""managed"",
      ""type"": ""azuread_conditional_access_policy"",
      ""name"": ""test_policy"",
      ""instances"": [
        {
          ""attributes"": {
            ""display_name"": ""Test State Policy"",
            ""state"": ""enabled"",
            ""conditions"": {
              ""applications"": {
                ""include_applications"": [""All""],
                ""exclude_applications"": [""excluded-app""]
              },
              ""users"": {
                ""include_users"": [""All""]
              },
              ""client_app_types"": [""browser""]
            },
            ""grant_controls"": {
              ""operator"": ""OR"",
              ""built_in_controls"": [""mfa""]
            }
          }
        }
      ]
    }
  ]
}";
            var filePath = CreateTestFile("test.tfstate", stateContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.Single(result.Policies);
            
            var policy = result.Policies.First();
            Assert.Equal("test_policy", policy.ResourceName);
            Assert.Equal("Test State Policy", policy.DisplayName);
            Assert.Equal("enabled", policy.State);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_ValidTfStateBackupFile_ShouldParseSuccessfully()
        {
            // Arrange
            var stateContent = @"{
  ""version"": 4,
  ""resources"": [
    {
      ""type"": ""azuread_conditional_access_policy"",
      ""name"": ""backup_policy"",
      ""instances"": [
        {
          ""attributes"": {
            ""display_name"": ""Backup Policy"",
            ""state"": ""report""
          }
        }
      ]
    }
  ]
}";
            var filePath = CreateTestFile("test.tfstate.backup", stateContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.Single(result.Policies);
            Assert.Equal("backup_policy", result.Policies.First().ResourceName);
            Assert.Equal("Backup Policy", result.Policies.First().DisplayName);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_FileDoesNotExist_ShouldReturnError()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.tf");

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(nonExistentPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nonExistentPath, result.SourcePath);
            Assert.Single(result.Errors);
            Assert.Contains("File not found", result.Errors.First());
            Assert.Empty(result.Policies);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_EmptyFile_ShouldReturnEmptyPolicies()
        {
            // Arrange
            var filePath = CreateTestFile("empty.tf", "");

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Policies);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_TfFileWithSyntaxErrors_ShouldHandleGracefully()
        {
            // Arrange
            var tfContent = @"
resource ""azuread_conditional_access_policy"" ""broken_policy"" {
  display_name = ""Broken Policy
  state        = ""enabled""
  # Missing closing quote and brace
";
            var filePath = CreateTestFile("syntax_error.tf", tfContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            // The service should handle the malformed content gracefully
            // It might parse nothing or have errors, but shouldn't crash
            Assert.True(result.Policies.Count == 0 || result.Errors.Count > 0);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_TfFileWithUnsupportedResources_ShouldIgnoreOtherResources()
        {
            // Arrange
            var tfContent = @"
resource ""azuread_user"" ""test_user"" {
  user_principal_name = ""test@example.com""
}

resource ""azuread_conditional_access_policy"" ""test_policy"" {
  display_name = ""Test Policy""
  state        = ""enabled""
}

resource ""azuread_group"" ""test_group"" {
  display_name = ""Test Group""
}";
            var filePath = CreateTestFile("mixed_resources.tf", tfContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Policies);
            Assert.Equal("test_policy", result.Policies.First().ResourceName);
            Assert.Equal("Test Policy", result.Policies.First().DisplayName);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_UnknownFileExtension_ShouldAttemptHclParsing()
        {
            // Arrange
            var content = @"
resource ""azuread_conditional_access_policy"" ""test_policy"" {
  display_name = ""Test Policy""
  state        = ""enabled""
}";
            var filePath = CreateTestFile("unknown.txt", content);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result.Warnings, w => w.Contains("Unknown file type") && w.Contains("Attempting HCL parsing"));
            Assert.Single(result.Policies);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_TfFileWithVariableInterpolations_ShouldParseCorrectly()
        {
            // Arrange
            var tfContent = @"
variable ""policy_name"" {
  description = ""Name of the policy""
  type        = string
  default     = ""Variable Policy""
}

locals {
  app_ids = [""app1"", ""app2""]
  user_groups = [""group1""]
}

resource ""azuread_conditional_access_policy"" ""variable_policy"" {
  display_name = var.policy_name
  state        = ""enabled""
  
  conditions {
    applications {
      include_applications = local.app_ids
    }
    users {
      include_groups = local.user_groups
    }
  }
}";
            var filePath = CreateTestFile("variables_and_locals.tf", tfContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Policies);
            Assert.Single(result.Variables);
            Assert.Equal(2, result.Locals.Count);
            
            var variable = result.Variables.First();
            Assert.Equal("policy_name", variable.Name);
            Assert.Equal("string", variable.Type);
            Assert.Equal("Variable Policy", variable.DefaultValue);
            
            var locals = result.Locals;
            Assert.Contains(locals, l => l.Name == "app_ids");
            Assert.Contains(locals, l => l.Name == "user_groups");
        }

        [Fact]
        public async Task ParseTerraformFileAsync_TfFileWithComments_ShouldIgnoreComments()
        {
            // Arrange
            var tfContent = @"
# This is a comment
// This is another comment
resource ""azuread_conditional_access_policy"" ""commented_policy"" {
  # Policy configuration
  display_name = ""Commented Policy""  # Inline comment
  state        = ""enabled""
  
  # Conditions block
  conditions {
    // Applications configuration
    applications {
      include_applications = [""All""]  # Include all applications
    }
  }
}";
            var filePath = CreateTestFile("with_comments.tf", tfContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.Single(result.Policies);
            
            var policy = result.Policies.First();
            Assert.Equal("commented_policy", policy.ResourceName);
            Assert.Equal("Commented Policy", policy.DisplayName);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_FullyPopulatedPolicy_ShouldMapAllAttributes()
        {
            // Arrange
            var tfContent = @"
resource ""azuread_conditional_access_policy"" ""full_policy"" {
  display_name = ""Full Featured Policy""
  state        = ""enabled""

  conditions {
    applications {
      include_applications = [""All""]
      exclude_applications = [""excluded-app""]
      include_user_actions = [""urn:user:registersecurityinfo""]
      include_authentication_context_class_references = [""c1""]
    }
    
    users {
      include_users  = [""All""]
      exclude_users  = [""admin-user""]
      include_groups = [""employees-group""]
      exclude_groups = [""excluded-group""]
      include_roles  = [""global-admin""]
      exclude_roles  = [""excluded-role""]
    }
    
    client_app_types     = [""browser"", ""mobileAppsAndDesktopClients""]
    sign_in_risk_levels  = [""high"", ""medium""]
    user_risk_levels     = [""high""]
  }

  grant_controls {
    operator                        = ""OR""
    built_in_controls              = [""mfa"", ""compliantDevice""]
    custom_authentication_factors  = [""custom-factor""]
    terms_of_use                   = [""terms-id""]
  }
}";
            var filePath = CreateTestFile("full_policy.tf", tfContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.Single(result.Policies);
            
            var policy = result.Policies.First();
            Assert.Equal("full_policy", policy.ResourceName);
            Assert.Equal("Full Featured Policy", policy.DisplayName);
            Assert.Equal("enabled", policy.State);
            
            // Verify conditions
            Assert.NotNull(policy.Conditions);
            Assert.NotNull(policy.Conditions.Applications);
            Assert.NotNull(policy.Conditions.Users);
            Assert.NotNull(policy.Conditions.ClientAppTypes);
            Assert.NotNull(policy.Conditions.SignInRiskLevels);
            Assert.NotNull(policy.Conditions.UserRiskLevels);
            
            // Verify applications
            Assert.Contains("All", policy.Conditions.Applications.IncludeApplications!);
            Assert.Contains("excluded-app", policy.Conditions.Applications.ExcludeApplications!);
            
            // Verify users
            Assert.Contains("All", policy.Conditions.Users.IncludeUsers!);
            Assert.Contains("admin-user", policy.Conditions.Users.ExcludeUsers!);
            
            // Verify grant controls
            Assert.NotNull(policy.GrantControls);
            Assert.Equal("OR", policy.GrantControls.Operator);
            Assert.Contains("mfa", policy.GrantControls.BuiltInControls!);
            Assert.Contains("compliantDevice", policy.GrantControls.BuiltInControls!);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_PolicyWithMissingRequiredAttributes_ShouldHandleGracefully()
        {
            // Arrange
            var tfContent = @"
resource ""azuread_conditional_access_policy"" ""minimal_policy"" {
  # Missing display_name - this is typically required
  state = ""enabled""
  
  conditions {
    applications {
      include_applications = [""All""]
    }
  }
}";
            var filePath = CreateTestFile("missing_attributes.tf", tfContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Policies);
            
            var policy = result.Policies.First();
            Assert.Equal("minimal_policy", policy.ResourceName);
            Assert.True(string.IsNullOrEmpty(policy.DisplayName)); // Should handle missing display_name
            Assert.Equal("enabled", policy.State);
        }

        #endregion

        #region ParseTerraformDirectoryAsync Tests

        [Fact]
        public async Task ParseTerraformDirectoryAsync_DirectoryWithMultipleTfFiles_ShouldAggregateAll()
        {
            // Arrange
            var policy1Content = @"
resource ""azuread_conditional_access_policy"" ""policy1"" {
  display_name = ""Policy One""
  state        = ""enabled""
}";
            var policy2Content = @"
resource ""azuread_conditional_access_policy"" ""policy2"" {
  display_name = ""Policy Two""
  state        = ""disabled""
}";
            
            CreateTestFile("policy1.tf", policy1Content);
            CreateTestFile("policy2.tf", policy2Content);

            // Act
            var result = await _parsingService.ParseTerraformDirectoryAsync(_testDirectory);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testDirectory, result.SourcePath);
            Assert.Empty(result.Errors);
            Assert.Equal(2, result.Policies.Count);
            
            Assert.Contains(result.Policies, p => p.ResourceName == "policy1");
            Assert.Contains(result.Policies, p => p.ResourceName == "policy2");
        }

        [Fact]
        public async Task ParseTerraformDirectoryAsync_DirectoryWithMixedFileTypes_ShouldOnlyProcessTerraformFiles()
        {
            // Arrange
            var tfContent = @"
resource ""azuread_conditional_access_policy"" ""tf_policy"" {
  display_name = ""TF Policy""
  state        = ""enabled""
}";
            
            CreateTestFile("policy.tf", tfContent);
            CreateTestFile("readme.txt", "This is a readme file");
            CreateTestFile("config.json", @"{""key"": ""value""}");
            CreateTestFile("notes.md", "# Notes");

            // Act
            var result = await _parsingService.ParseTerraformDirectoryAsync(_testDirectory);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.Single(result.Policies);
            Assert.Equal("tf_policy", result.Policies.First().ResourceName);
        }

        [Fact]
        public async Task ParseTerraformDirectoryAsync_DirectoryWithSubdirectories_ShouldParseRecursively()
        {
            // Arrange
            var subDir = Path.Combine(_testDirectory, "subdirectory");
            Directory.CreateDirectory(subDir);
            
            var mainContent = @"
resource ""azuread_conditional_access_policy"" ""main_policy"" {
  display_name = ""Main Policy""
  state        = ""enabled""
}";
            var subContent = @"
resource ""azuread_conditional_access_policy"" ""sub_policy"" {
  display_name = ""Sub Policy""
  state        = ""enabled""
}";
            
            CreateTestFile("main.tf", mainContent);
            File.WriteAllText(Path.Combine(subDir, "sub.tf"), subContent);

            // Act
            var result = await _parsingService.ParseTerraformDirectoryAsync(_testDirectory);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.Equal(2, result.Policies.Count);
            
            Assert.Contains(result.Policies, p => p.ResourceName == "main_policy");
            Assert.Contains(result.Policies, p => p.ResourceName == "sub_policy");
        }

        [Fact]
        public async Task ParseTerraformDirectoryAsync_EmptyDirectory_ShouldReturnEmptyResults()
        {
            // Arrange
            var emptyDir = Path.Combine(_testDirectory, "empty");
            Directory.CreateDirectory(emptyDir);

            // Act
            var result = await _parsingService.ParseTerraformDirectoryAsync(emptyDir);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(emptyDir, result.SourcePath);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Policies);
            Assert.Empty(result.Variables);
            Assert.Empty(result.Locals);
            Assert.Empty(result.DataSources);
        }

        [Fact]
        public async Task ParseTerraformDirectoryAsync_DirectoryDoesNotExist_ShouldReturnError()
        {
            // Arrange
            var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");

            // Act
            var result = await _parsingService.ParseTerraformDirectoryAsync(nonExistentDir);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(nonExistentDir, result.SourcePath);
            Assert.Single(result.Errors);
            Assert.Contains("Directory not found", result.Errors.First());
            Assert.Empty(result.Policies);
        }

        [Fact]
        public async Task ParseTerraformDirectoryAsync_DirectoryWithValidAndInvalidFiles_ShouldParseValidOnesAndReportErrors()
        {
            // Arrange
            var validContent = @"
resource ""azuread_conditional_access_policy"" ""valid_policy"" {
  display_name = ""Valid Policy""
  state        = ""enabled""
}";
            var invalidStateContent = "{ invalid json content";
            
            CreateTestFile("valid.tf", validContent);
            CreateTestFile("invalid.tfstate", invalidStateContent);

            // Act
            var result = await _parsingService.ParseTerraformDirectoryAsync(_testDirectory);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Policies); // Only the valid one should be parsed
            Assert.Equal("valid_policy", result.Policies.First().ResourceName);
            
            // Should have errors for the invalid state file
            Assert.NotEmpty(result.Errors);
        }

        #endregion

        #region Helper Methods

        private string CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        #endregion

        #region Additional Edge Case Tests

        [Fact]
        public async Task ParseTerraformFileAsync_TfStateWithMultipleInstances_ShouldParseAll()
        {
            // Arrange
            var stateContent = @"{
  ""version"": 4,
  ""resources"": [
    {
      ""type"": ""azuread_conditional_access_policy"",
      ""name"": ""multi_instance_policy"",
      ""instances"": [
        {
          ""attributes"": {
            ""display_name"": ""Instance One"",
            ""state"": ""enabled""
          }
        },
        {
          ""attributes"": {
            ""display_name"": ""Instance Two"",
            ""state"": ""disabled""
          }
        }
      ]
    }
  ]
}";
            var filePath = CreateTestFile("multi_instances.tfstate", stateContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.Equal(2, result.Policies.Count);
            
            Assert.Contains(result.Policies, p => p.DisplayName == "Instance One");
            Assert.Contains(result.Policies, p => p.DisplayName == "Instance Two");
        }

        [Fact]
        public async Task ParseTerraformFileAsync_TfStateWithNoResources_ShouldReturnEmpty()
        {
            // Arrange
            var stateContent = @"{
  ""version"": 4,
  ""resources"": []
}";
            var filePath = CreateTestFile("empty_state.tfstate", stateContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Policies);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_TfFileWithVariablesOnly_ShouldParseVariables()
        {
            // Arrange
            var tfContent = @"
variable ""tenant_id"" {
  description = ""The tenant ID""
  type        = string
  sensitive   = true
}

variable ""policy_names"" {
  description = ""List of policy names""
  type        = list(string)
  default     = [""Policy1"", ""Policy2""]
}";
            var filePath = CreateTestFile("variables_only.tf", tfContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Policies);
            Assert.Equal(2, result.Variables.Count);
            
            var tenantIdVar = result.Variables.FirstOrDefault(v => v.Name == "tenant_id");
            Assert.NotNull(tenantIdVar);
            Assert.Equal("string", tenantIdVar.Type);
            Assert.True(tenantIdVar.Sensitive);
            
            var policyNamesVar = result.Variables.FirstOrDefault(v => v.Name == "policy_names");
            Assert.NotNull(policyNamesVar);
            Assert.Equal("list(string)", policyNamesVar.Type);
        }

        [Fact]
        public async Task ParseTerraformFileAsync_TfFileWithLocalsOnly_ShouldParseLocals()
        {
            // Arrange
            var tfContent = @"
locals {
  common_tags = {
    Environment = ""Production""
    Team        = ""Security""
  }
  
  app_ids = [""app1"", ""app2"", ""app3""]
  
  policy_prefix = ""CA-""
}";
            var filePath = CreateTestFile("locals_only.tf", tfContent);

            // Act
            var result = await _parsingService.ParseTerraformFileAsync(filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.Empty(result.Policies);
            Assert.Equal(3, result.Locals.Count);
            
            Assert.Contains(result.Locals, l => l.Name == "common_tags");
            Assert.Contains(result.Locals, l => l.Name == "app_ids");
            Assert.Contains(result.Locals, l => l.Name == "policy_prefix");
        }

        #endregion
    }
}
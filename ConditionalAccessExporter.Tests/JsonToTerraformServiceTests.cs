using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ConditionalAccessExporter.Tests
{
    public class JsonToTerraformServiceTests
    {
        private readonly JsonToTerraformService _service;

        public JsonToTerraformServiceTests()
        {
            _service = new JsonToTerraformService();
        }

        #region ConvertJsonToTerraformAsync Basic Tests

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithMinimalPolicy_ShouldGenerateCorrectHCL()
        {
            // Arrange
            var policy = CreateMinimalPolicy("Test Policy");
            var jsonExport = new JsonPolicyExport
            {
                ExportedAt = DateTime.UtcNow,
                TenantId = "test-tenant-id",
                Source = "Microsoft Graph API",
                PoliciesCount = 1,
                Policies = new List<JsonConditionalAccessPolicy> { policy }
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            var options = new TerraformOutputOptions
            {
                SeparateFilePerPolicy = false,
                GenerateVariables = false,
                GenerateProviderConfig = false,
                GenerateModuleStructure = false,
                IncludeComments = true
            };

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath, options);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(1, result.SuccessfulConversions);
                Assert.Equal(0, result.FailedConversions);
                Assert.True(result.GeneratedFiles.Any());
                Assert.Equal(tempPath, result.SourcePath);

                // Verify main.tf exists and contains expected content
                var mainTfPath = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("main.tf"));
                Assert.NotNull(mainTfPath);
                
                var content = await File.ReadAllTextAsync(mainTfPath);
                Assert.Contains("resource \"azuread_conditional_access_policy\"", content);
                Assert.Contains("display_name = \"Test Policy\"", content);
                Assert.Contains("state", content);
                Assert.Contains("\"enabled\"", content);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithMultiplePolicies_ShouldGenerateMultipleResources()
        {
            // Arrange
            var policies = new List<JsonConditionalAccessPolicy>
            {
                CreateMinimalPolicy("Policy 1"),
                CreateMinimalPolicy("Policy 2"),
                CreateMinimalPolicy("Policy 3")
            };

            var jsonExport = new JsonPolicyExport
            {
                ExportedAt = DateTime.UtcNow,
                Policies = policies,
                PoliciesCount = policies.Count
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            var options = new TerraformOutputOptions
            {
                SeparateFilePerPolicy = false,
                GenerateVariables = false,
                GenerateProviderConfig = false
            };

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath, options);

                // Assert
                Assert.Equal(3, result.SuccessfulConversions);
                Assert.Equal(0, result.FailedConversions);

                var mainTfPath = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("main.tf"));
                Assert.NotNull(mainTfPath);
                
                var content = await File.ReadAllTextAsync(mainTfPath);
                Assert.Equal(3, Regex.Matches(content, @"resource ""azuread_conditional_access_policy""").Count);
                Assert.Contains("Policy 1", content);
                Assert.Contains("Policy 2", content);
                Assert.Contains("Policy 3", content);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithEmptyPoliciesList_ShouldReturnError()
        {
            // Arrange
            var jsonExport = new JsonPolicyExport
            {
                ExportedAt = DateTime.UtcNow,
                Policies = new List<JsonConditionalAccessPolicy>(),
                PoliciesCount = 0
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            try
            {
                // Act
                var result = await _service.ConvertJsonToTerraformAsync(tempPath);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Errors.Any());
                Assert.Contains("No policies found", result.Errors.First());
                Assert.Equal(0, result.SuccessfulConversions);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithInvalidJson_ShouldHandleErrorGracefully()
        {
            // Arrange
            var tempPath = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempPath, "{ invalid json content");

            try
            {
                // Act
                var result = await _service.ConvertJsonToTerraformAsync(tempPath);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Errors.Any());
                Assert.Equal(0, result.SuccessfulConversions);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        #endregion

        #region Policy State Tests

        [Theory]
        [InlineData("enabled", "enabled")]
        [InlineData("disabled", "disabled")]
        [InlineData("enabledForReportingButNotEnforced", "enabledforreportingbutnotenforced")]
        public async Task ConvertJsonToTerraformAsync_WithDifferentStates_ShouldIncludeStateCorrectly(string inputState, string expectedOutputState)
        {
            // Arrange
            var policy = CreateMinimalPolicy("State Test Policy");
            policy.State = inputState;
            
            var jsonExport = new JsonPolicyExport
            {
                Policies = new List<JsonConditionalAccessPolicy> { policy },
                PoliciesCount = 1
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath);

                // Assert
                var mainTfPath = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("main.tf"));
                var content = await File.ReadAllTextAsync(mainTfPath!);
                Assert.Contains($"\"{expectedOutputState}\"", content);
                Assert.Contains("state", content);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        #endregion

        #region Options Tests

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithGenerateProvider_ShouldCreateProviderFile()
        {
            // Arrange
            var policy = CreateMinimalPolicy("Test Policy");
            var jsonExport = new JsonPolicyExport
            {
                Policies = new List<JsonConditionalAccessPolicy> { policy },
                PoliciesCount = 1
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            var options = new TerraformOutputOptions
            {
                GenerateProviderConfig = true,
                GenerateVariables = false
            };

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath, options);

                // Assert
                var providerFile = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("providers.tf"));
                Assert.NotNull(providerFile);
                
                var content = await File.ReadAllTextAsync(providerFile);
                Assert.Contains("terraform {", content);
                Assert.Contains("required_providers {", content);
                Assert.Contains("azuread", content);
                Assert.Contains("provider \"azuread\"", content);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithGenerateVariables_ShouldCreateVariablesFile()
        {
            // Arrange
            var policy = CreateMinimalPolicy("Test Policy");
            var jsonExport = new JsonPolicyExport
            {
                Policies = new List<JsonConditionalAccessPolicy> { policy },
                PoliciesCount = 1
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            var options = new TerraformOutputOptions
            {
                GenerateVariables = true,
                GenerateProviderConfig = false
            };

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath, options);

                // Assert
                var variablesFile = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("variables.tf"));
                Assert.NotNull(variablesFile);
                
                var content = await File.ReadAllTextAsync(variablesFile);
                Assert.Contains("variable", content);
                Assert.Contains("tenant_id", content);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConvertJsonToTerraformAsync_WithIncludeComments_ShouldGenerateAppropriateContent(bool includeComments)
        {
            // Arrange
            var policy = CreateMinimalPolicy("Test Policy");
            var jsonExport = new JsonPolicyExport
            {
                Policies = new List<JsonConditionalAccessPolicy> { policy },
                PoliciesCount = 1
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            var options = new TerraformOutputOptions
            {
                IncludeComments = includeComments,
                GenerateProviderConfig = false,
                GenerateVariables = false
            };

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath, options);

                // Assert
                var mainTfPath = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("main.tf"));
                var content = await File.ReadAllTextAsync(mainTfPath!);
                
                if (includeComments)
                {
                    Assert.Contains("#", content);
                    Assert.Contains("Policy:", content);
                }
                else
                {
                    // Should have much fewer comments
                    var commentLines = content.Split('\n').Count(line => line.Trim().StartsWith("#"));
                    Assert.True(commentLines < 5); // Minimal or no comments
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        #endregion

        #region Condition Tests

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithApplicationConditions_ShouldMapCorrectly()
        {
            // Arrange
            var policy = CreateMinimalPolicy("App Conditions Test");
            policy.Conditions = new JsonConditions
            {
                Applications = new JsonApplications
                {
                    IncludeApplications = new List<string> { "All", "app-id-1" },
                    ExcludeApplications = new List<string> { "excluded-app-id" },
                    IncludeUserActions = new List<string> { "urn:user:registersecurityinfo" }
                }
            };

            var jsonExport = new JsonPolicyExport
            {
                Policies = new List<JsonConditionalAccessPolicy> { policy },
                PoliciesCount = 1
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath);

                // Assert
                var mainTfPath = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("main.tf"));
                var content = await File.ReadAllTextAsync(mainTfPath!);
                
                Assert.Contains("conditions", content);
                Assert.Contains("applications", content);
                Assert.Contains("All", content);
                Assert.Contains("app-id-1", content);
                Assert.Contains("excluded-app-id", content);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithUserConditions_ShouldMapCorrectly()
        {
            // Arrange
            var policy = CreateMinimalPolicy("User Conditions Test");
            policy.Conditions = new JsonConditions
            {
                Users = new JsonUsers
                {
                    IncludeUsers = new List<string> { "All", "user-id-1" },
                    ExcludeUsers = new List<string> { "excluded-user-id" },
                    IncludeGroups = new List<string> { "group-id-1" },
                    ExcludeGroups = new List<string> { "excluded-group-id" }
                }
            };

            var jsonExport = new JsonPolicyExport
            {
                Policies = new List<JsonConditionalAccessPolicy> { policy },
                PoliciesCount = 1
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath);

                // Assert
                var mainTfPath = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("main.tf"));
                var content = await File.ReadAllTextAsync(mainTfPath!);
                
                Assert.Contains("conditions", content);
                Assert.Contains("users", content);
                Assert.Contains("user-id-1", content);
                Assert.Contains("group-id-1", content);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithGrantControls_ShouldMapCorrectly()
        {
            // Arrange
            var policy = CreateMinimalPolicy("Grant Controls Test");
            policy.GrantControls = new JsonGrantControls
            {
                Operator = "OR",
                BuiltInControls = new List<string> { "mfa", "compliantDevice" }
            };

            var jsonExport = new JsonPolicyExport
            {
                Policies = new List<JsonConditionalAccessPolicy> { policy },
                PoliciesCount = 1
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath);

                // Assert
                var mainTfPath = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("main.tf"));
                var content = await File.ReadAllTextAsync(mainTfPath!);
                
                Assert.Contains("grant_controls", content);
                Assert.Contains("OR", content);
                Assert.Contains("mfa", content);
                Assert.Contains("compliantDevice", content);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithSessionControls_ShouldMapCorrectly()
        {
            // Arrange
            var policy = CreateMinimalPolicy("Session Controls Test");
            policy.SessionControls = new JsonSessionControls
            {
                ApplicationEnforcedRestrictions = new JsonApplicationEnforcedRestrictions
                {
                    IsEnabled = true
                },
                SignInFrequency = new JsonSignInFrequency
                {
                    IsEnabled = true,
                    Type = "hours",
                    Value = 24
                }
            };

            var jsonExport = new JsonPolicyExport
            {
                Policies = new List<JsonConditionalAccessPolicy> { policy },
                PoliciesCount = 1
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath);

                // Assert
                var mainTfPath = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("main.tf"));
                var content = await File.ReadAllTextAsync(mainTfPath!);
                
                Assert.Contains("session_controls", content);
                Assert.Contains("hours", content);
                Assert.Contains("24", content);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithNullConditions_ShouldHandleGracefully()
        {
            // Arrange
            var policy = CreateMinimalPolicy("Null Conditions Test");
            policy.Conditions = null;

            var jsonExport = new JsonPolicyExport
            {
                Policies = new List<JsonConditionalAccessPolicy> { policy },
                PoliciesCount = 1
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath);

                // Assert
                Assert.Equal(1, result.SuccessfulConversions);
                Assert.Equal(0, result.FailedConversions);
                
                var mainTfPath = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("main.tf"));
                var content = await File.ReadAllTextAsync(mainTfPath!);
                
                // Should still contain the basic policy structure
                Assert.Contains("resource \"azuread_conditional_access_policy\"", content);
                Assert.Contains("display_name", content);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        [Fact]
        public async Task ConvertJsonToTerraformAsync_WithSpecialCharactersInDisplayName_ShouldSanitizeResourceName()
        {
            // Arrange
            var policy = CreateMinimalPolicy("Test Policy with Special@Characters & Spaces!");

            var jsonExport = new JsonPolicyExport
            {
                Policies = new List<JsonConditionalAccessPolicy> { policy },
                PoliciesCount = 1
            };

            var tempPath = Path.GetTempFileName();
            var jsonContent = JsonConvert.SerializeObject(jsonExport, Formatting.Indented);
            await File.WriteAllTextAsync(tempPath, jsonContent);

            JsonToTerraformResult? result = null;
            try
            {
                // Act
                result = await _service.ConvertJsonToTerraformAsync(tempPath);

                // Assert
                var mainTfPath = result.GeneratedFiles.FirstOrDefault(f => f.EndsWith("main.tf"));
                var content = await File.ReadAllTextAsync(mainTfPath!);
                
                // Resource name should be sanitized
                Assert.DoesNotContain("@", content.Split('"')[1]); // First quoted string should be the resource name
                Assert.DoesNotContain("&", content.Split('"')[1]);
                Assert.DoesNotContain("!", content.Split('"')[1]);
                
                // But display name should be preserved
                Assert.Contains("Test Policy with Special@Characters & Spaces!", content);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                if (result?.OutputPath != null && Directory.Exists(result.OutputPath))
                    Directory.Delete(result.OutputPath, true);
            }
        }

        #endregion

        #region Test Helper Methods

        private JsonConditionalAccessPolicy CreateMinimalPolicy(string displayName)
        {
            return new JsonConditionalAccessPolicy
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = displayName,
                State = "enabled",
                CreatedDateTime = DateTime.UtcNow.AddDays(-30),
                ModifiedDateTime = DateTime.UtcNow.AddDays(-1)
            };
        }

        #endregion
    }
}
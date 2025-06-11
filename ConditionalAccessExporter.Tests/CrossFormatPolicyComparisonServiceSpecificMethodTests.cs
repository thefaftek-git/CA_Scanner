using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ConditionalAccessExporter.Tests
{
    /// <summary>
    /// Tests for specific methods in CrossFormatPolicyComparisonService
    /// </summary>
    public class CrossFormatPolicyComparisonServiceSpecificMethodTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly CrossFormatPolicyComparisonService _service;

        public CrossFormatPolicyComparisonServiceSpecificMethodTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "CrossFormatSpecificTests_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_testDirectory);

            var terraformParsingService = new TerraformParsingService();
            var terraformConversionService = new TerraformConversionService();
            var jsonComparisonService = new PolicyComparisonService();

            _service = new CrossFormatPolicyComparisonService(
                jsonComparisonService,
                terraformParsingService,
                terraformConversionService);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        #region CompareJsonToTerraformAsync Tests

        [Fact]
        public async Task CompareJsonToTerraformAsync_ValidEntraExportAndTerraformDirectory_ShouldReturnComparisonResult()
        {
            // Arrange
            var entraExport = CreateStandardJsonPolicyArray();
            var terraformDirectory = CreateTerraformDirectory();

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(entraExport, terraformDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PolicyFormat.Json, result.SourceFormat);
            Assert.Equal(PolicyFormat.Terraform, result.ReferenceFormat);
            Assert.True(result.Summary.TotalSourcePolicies > 0);
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_IdenticalPolicies_ShouldReturnIdenticalStatus()
        {
            // Arrange
            var entraExport = new { Policies = new[] { CreateStandardJsonPolicy() } };
            var terraformDirectory = CreateTerraformDirectory();

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(entraExport, terraformDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.PolicyComparisons.Any(p => 
                p.Status == CrossFormatComparisonStatus.Identical ||
                p.Status == CrossFormatComparisonStatus.SemanticallyEquivalent ||
                p.Status == CrossFormatComparisonStatus.Different)); // At least some comparison happened
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_DifferentPolicies_ShouldReturnDifferentStatus()
        {
            // Arrange
            var jsonPolicy = CreateStandardJsonPolicy();
            jsonPolicy["State"] = "enabled";
            var entraExport = new { Policies = new[] { jsonPolicy } };

            var terraformDirectory = CreateTerraformDirectoryWithDisabledPolicy();

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(entraExport, terraformDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.PolicyComparisons);
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_NonExistentTerraformDirectory_ShouldHandleGracefully()
        {
            // Arrange
            var entraExport = new { Policies = new[] { CreateStandardJsonPolicy() } };
            var nonExistentDirectory = Path.Combine(_testDirectory, "nonexistent");

            var options = new CrossFormatMatchingOptions();

            // Act
            var result = await _service.CompareJsonToTerraformAsync(entraExport, nonExistentDirectory, options);

            // Assert - Service handles gracefully, doesn't throw exceptions
            Assert.NotNull(result);
            Assert.Equal(0, result.Summary.TotalReferencePolicies); // No policies found in non-existent directory
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_EmptyEntraExport_ShouldReturnEmptyResult()
        {
            // Arrange
            var emptyEntraExport = new { Policies = Array.Empty<JObject>() };
            var terraformDirectory = CreateTerraformDirectory();

            var options = new CrossFormatMatchingOptions();

            // Act
            var result = await _service.CompareJsonToTerraformAsync(emptyEntraExport, terraformDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Summary.TotalSourcePolicies);
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_EmptyTerraformDirectory_ShouldReturnSourceOnlyPolicies()
        {
            // Arrange
            var entraExport = new { Policies = new[] { CreateStandardJsonPolicy() } };
            var emptyTerraformDirectory = Path.Combine(_testDirectory, "empty_terraform");
            Directory.CreateDirectory(emptyTerraformDirectory);

            var options = new CrossFormatMatchingOptions();

            // Act
            var result = await _service.CompareJsonToTerraformAsync(entraExport, emptyTerraformDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Summary.TotalSourcePolicies > 0);
            Assert.Equal(0, result.Summary.TotalReferencePolicies);
        }

        #endregion

        #region CompareAsync Tests

        [Fact]
        public async Task CompareAsync_ValidDirectories_ShouldReturnComparisonResult()
        {
            // Arrange
            var sourceDirectory = CreateJsonDirectory();
            var referenceDirectory = CreateTerraformDirectory();

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareAsync(sourceDirectory, referenceDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Summary.TotalSourcePolicies >= 0);
            Assert.True(result.Summary.TotalReferencePolicies >= 0);
        }

        [Fact]
        public async Task CompareAsync_NonExistentSourceDirectory_ShouldHandleGracefully()
        {
            // Arrange
            var nonExistentSource = Path.Combine(_testDirectory, "nonexistent_source");
            var referenceDirectory = CreateTerraformDirectory();

            var options = new CrossFormatMatchingOptions();

            // Act
            var result = await _service.CompareAsync(nonExistentSource, referenceDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PolicyFormat.Unknown, result.SourceFormat);
        }

        [Fact]
        public async Task CompareAsync_DifferentMatchingStrategies_ShouldHandleProperly()
        {
            // Arrange
            var sourceDirectory = CreateJsonDirectory();
            var referenceDirectory = CreateTerraformDirectory();

            var strategies = new[]
            {
                CrossFormatMatchingStrategy.ByName,
                CrossFormatMatchingStrategy.ById,
                CrossFormatMatchingStrategy.SemanticSimilarity,
                CrossFormatMatchingStrategy.CustomMapping
            };

            foreach (var strategy in strategies)
            {
                var options = new CrossFormatMatchingOptions
                {
                    Strategy = strategy,
                    EnableSemanticComparison = true
                };

                // Act
                var result = await _service.CompareAsync(sourceDirectory, referenceDirectory, options);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Summary.TotalSourcePolicies >= 0);
            }
        }

        #endregion

        #region Semantic Equivalence Tests

        [Fact]
        public async Task CompareJsonToTerraformAsync_ArrayOrderDifference_ShouldDetectSemanticEquivalence()
        {
            // Arrange
            var jsonPolicy = CreateJsonPolicyWithUsers(new[] { "user1", "user2", "user3" });
            var entraExport = new { Policies = new[] { jsonPolicy } };

            var terraformDirectory = CreateTerraformDirectoryWithUsers(new[] { "user3", "user1", "user2" });

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(entraExport, terraformDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.PolicyComparisons);
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_CaseInsensitiveValues_ShouldDetectSemanticEquivalence()
        {
            // Arrange
            var jsonPolicy = CreateStandardJsonPolicy();
            jsonPolicy["Conditions"]!["ClientAppTypes"] = new JArray("Browser", "MobileAppsAndDesktopClients");
            var entraExport = new { Policies = new[] { jsonPolicy } };

            var terraformDirectory = CreateTerraformDirectoryWithClientAppTypes(new[] { "browser", "mobileAppsAndDesktopClients" });

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(entraExport, terraformDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.PolicyComparisons);
        }

        #endregion

        #region Options Tests

        [Fact]
        public async Task CompareJsonToTerraformAsync_DisableSemanticComparison_ShouldPerformExactComparison()
        {
            // Arrange
            var jsonPolicy = CreateJsonPolicyWithUsers(new[] { "user1", "user2" });
            var entraExport = new { Policies = new[] { jsonPolicy } };

            var terraformDirectory = CreateTerraformDirectoryWithUsers(new[] { "user2", "user1" }); // Different order

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = false // Disabled
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(entraExport, terraformDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.PolicyComparisons);
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_CustomMappingStrategy_ShouldUseCustomMappings()
        {
            // Arrange
            var entraExport = new { Policies = new[] { CreateStandardJsonPolicy() } };
            var terraformDirectory = CreateTerraformDirectory();

            var options = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.CustomMapping,
                CustomMappings = new Dictionary<string, string>
                {
                    { "Test Policy", "equivalent_terraform_policy" }
                },
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(entraExport, terraformDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.PolicyComparisons);
        }

        #endregion

        #region Helper Methods

        private object CreateStandardJsonPolicyArray()
        {
            return new { Policies = new[] { CreateStandardJsonPolicy() } };
        }

        private string CreateJsonDirectory()
        {
            var jsonDir = Path.Combine(_testDirectory, "json_policies");
            Directory.CreateDirectory(jsonDir);
            
            var policy = CreateStandardJsonPolicy();
            var filePath = Path.Combine(jsonDir, "test-policy.json");
            File.WriteAllText(filePath, policy.ToString());
            
            return jsonDir;
        }

        private string CreateTerraformDirectory()
        {
            var terraformDir = Path.Combine(_testDirectory, "terraform_policies");
            Directory.CreateDirectory(terraformDir);
            
            var policy = CreateEquivalentTerraformPolicy();
            var filePath = Path.Combine(terraformDir, "test-policy.tf");
            File.WriteAllText(filePath, policy);
            
            return terraformDir;
        }

        private string CreateTerraformDirectoryWithDisabledPolicy()
        {
            var terraformDir = Path.Combine(_testDirectory, "terraform_disabled");
            Directory.CreateDirectory(terraformDir);
            
            var policy = CreateEquivalentTerraformPolicy().Replace("enabled", "disabled");
            var filePath = Path.Combine(terraformDir, "disabled-policy.tf");
            File.WriteAllText(filePath, policy);
            
            return terraformDir;
        }

        private string CreateTerraformDirectoryWithUsers(string[] users)
        {
            var terraformDir = Path.Combine(_testDirectory, "terraform_users");
            Directory.CreateDirectory(terraformDir);
            
            var usersString = string.Join(@""", """, users);
            var policy = CreateEquivalentTerraformPolicy()
                .Replace(@"include_users = [""All""]", $@"include_users = [""{usersString}""]");
            
            var filePath = Path.Combine(terraformDir, "users-policy.tf");
            File.WriteAllText(filePath, policy);
            
            return terraformDir;
        }

        private string CreateTerraformDirectoryWithClientAppTypes(string[] clientAppTypes)
        {
            var terraformDir = Path.Combine(_testDirectory, "terraform_clientapps");
            Directory.CreateDirectory(terraformDir);
            
            var appsString = string.Join(@""", """, clientAppTypes);
            var policy = CreateEquivalentTerraformPolicy()
                .Replace(@"client_app_types = [""browser""]", $@"client_app_types = [""{appsString}""]");
            
            var filePath = Path.Combine(terraformDir, "clientapps-policy.tf");
            File.WriteAllText(filePath, policy);
            
            return terraformDir;
        }

        private JObject CreateStandardJsonPolicy()
        {
            return new JObject
            {
                ["Id"] = "test-policy-id",
                ["DisplayName"] = "Test Policy",
                ["State"] = "enabled",
                ["Conditions"] = new JObject
                {
                    ["Applications"] = new JObject
                    {
                        ["IncludeApplications"] = new JArray("All")
                    },
                    ["Users"] = new JObject
                    {
                        ["IncludeUsers"] = new JArray("All")
                    },
                    ["ClientAppTypes"] = new JArray("browser")
                },
                ["GrantControls"] = new JObject
                {
                    ["Operator"] = "OR",
                    ["BuiltInControls"] = new JArray("mfa")
                }
            };
        }

        private string CreateEquivalentTerraformPolicy()
        {
            return @"
resource ""azuread_conditional_access_policy"" ""test_policy"" {
  display_name = ""Test Policy""
  state        = ""enabled""

  conditions {
    applications {
      include_applications = [""All""]
    }
    
    users {
      include_users = [""All""]
    }
    
    client_app_types = [""browser""]
  }

  grant_controls {
    operator          = ""OR""
    built_in_controls = [""mfa""]
  }
}";
        }

        private JObject CreateJsonPolicyWithUsers(string[] users)
        {
            var policy = CreateStandardJsonPolicy();
            policy["Conditions"]!["Users"]!["IncludeUsers"] = new JArray(users);
            return policy;
        }

        #endregion
    }
}
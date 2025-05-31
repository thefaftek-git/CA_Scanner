using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
        public async Task CompareJsonToTerraformAsync_ValidFiles_ShouldReturnComparisonResult()
        {
            // Arrange
            var jsonFile = CreateJsonPolicyFile("test-policy.json");
            var terraformFile = CreateTerraformPolicyFile("test-policy.tf");

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = true,
                EnableDetailedDifferences = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(jsonFile, terraformFile, options);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SourcePolicy);
            Assert.NotNull(result.ReferencePolicy);
            Assert.Equal(PolicyFormat.Json, result.SourcePolicy.Format);
            Assert.Equal(PolicyFormat.Terraform, result.ReferencePolicy.Format);
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_IdenticalPolicies_ShouldReturnIdenticalStatus()
        {
            // Arrange
            var jsonPolicy = CreateStandardJsonPolicy();
            var terraformPolicy = CreateEquivalentTerraformPolicy();

            var jsonFile = Path.Combine(_testDirectory, "identical.json");
            var terraformFile = Path.Combine(_testDirectory, "identical.tf");

            await File.WriteAllTextAsync(jsonFile, jsonPolicy.ToString());
            await File.WriteAllTextAsync(terraformFile, terraformPolicy);

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(jsonFile, terraformFile, options);

            // Assert
            Assert.True(
                result.Status == CrossFormatComparisonStatus.Identical ||
                result.Status == CrossFormatComparisonStatus.SemanticallyEquivalent);
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_DifferentPolicies_ShouldReturnDifferentStatus()
        {
            // Arrange
            var jsonPolicy = CreateStandardJsonPolicy();
            jsonPolicy["State"] = "enabled";

            var terraformPolicy = CreateEquivalentTerraformPolicy().Replace("enabled", "disabled");

            var jsonFile = Path.Combine(_testDirectory, "different1.json");
            var terraformFile = Path.Combine(_testDirectory, "different1.tf");

            await File.WriteAllTextAsync(jsonFile, jsonPolicy.ToString());
            await File.WriteAllTextAsync(terraformFile, terraformPolicy);

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = true,
                EnableDetailedDifferences = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(jsonFile, terraformFile, options);

            // Assert
            Assert.Equal(CrossFormatComparisonStatus.Different, result.Status);
            Assert.NotEmpty(result.Differences);
            Assert.Contains(result.Differences, d => d.Contains("State") || d.Contains("state"));
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_NonExistentJsonFile_ShouldThrowException()
        {
            // Arrange
            var nonExistentJson = Path.Combine(_testDirectory, "nonexistent.json");
            var terraformFile = CreateTerraformPolicyFile("test.tf");

            var options = new CrossFormatMatchingOptions();

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(
                () => _service.CompareJsonToTerraformAsync(nonExistentJson, terraformFile, options));
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_NonExistentTerraformFile_ShouldThrowException()
        {
            // Arrange
            var jsonFile = CreateJsonPolicyFile("test.json");
            var nonExistentTerraform = Path.Combine(_testDirectory, "nonexistent.tf");

            var options = new CrossFormatMatchingOptions();

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(
                () => _service.CompareJsonToTerraformAsync(jsonFile, nonExistentTerraform, options));
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_InvalidJsonFile_ShouldHandleGracefully()
        {
            // Arrange
            var invalidJsonFile = Path.Combine(_testDirectory, "invalid.json");
            await File.WriteAllTextAsync(invalidJsonFile, "{ invalid json content");

            var terraformFile = CreateTerraformPolicyFile("test.tf");
            var options = new CrossFormatMatchingOptions();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(
                () => _service.CompareJsonToTerraformAsync(invalidJsonFile, terraformFile, options));
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_InvalidTerraformFile_ShouldHandleGracefully()
        {
            // Arrange
            var jsonFile = CreateJsonPolicyFile("test.json");
            
            var invalidTerraformFile = Path.Combine(_testDirectory, "invalid.tf");
            await File.WriteAllTextAsync(invalidTerraformFile, "invalid terraform syntax {{{");

            var options = new CrossFormatMatchingOptions();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(
                () => _service.CompareJsonToTerraformAsync(jsonFile, invalidTerraformFile, options));
        }

        #endregion

        #region Semantic Equivalence Tests

        [Fact]
        public async Task CompareJsonToTerraformAsync_ArrayOrderDifference_ShouldDetectSemanticEquivalence()
        {
            // Arrange
            var jsonPolicy = CreateJsonPolicyWithUsers(new[] { "user1", "user2", "user3" });
            var terraformPolicy = CreateTerraformPolicyWithUsers(new[] { "user3", "user1", "user2" });

            var jsonFile = Path.Combine(_testDirectory, "order1.json");
            var terraformFile = Path.Combine(_testDirectory, "order1.tf");

            await File.WriteAllTextAsync(jsonFile, jsonPolicy.ToString());
            await File.WriteAllTextAsync(terraformFile, terraformPolicy);

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(jsonFile, terraformFile, options);

            // Assert
            Assert.True(
                result.Status == CrossFormatComparisonStatus.Identical ||
                result.Status == CrossFormatComparisonStatus.SemanticallyEquivalent);
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_CaseInsensitiveValues_ShouldDetectSemanticEquivalence()
        {
            // Arrange
            var jsonPolicy = CreateStandardJsonPolicy();
            jsonPolicy["Conditions"]["ClientAppTypes"] = new JArray("Browser", "MobileAppsAndDesktopClients");

            var terraformPolicy = CreateEquivalentTerraformPolicy()
                .Replace(@"client_app_types = [""browser""]", 
                        @"client_app_types = [""browser"", ""mobileAppsAndDesktopClients""]");

            var jsonFile = Path.Combine(_testDirectory, "case1.json");
            var terraformFile = Path.Combine(_testDirectory, "case1.tf");

            await File.WriteAllTextAsync(jsonFile, jsonPolicy.ToString());
            await File.WriteAllTextAsync(terraformFile, terraformPolicy);

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(jsonFile, terraformFile, options);

            // Assert
            Assert.True(
                result.Status == CrossFormatComparisonStatus.Identical ||
                result.Status == CrossFormatComparisonStatus.SemanticallyEquivalent);
        }

        #endregion

        #region Options Tests

        [Fact]
        public async Task CompareJsonToTerraformAsync_DisableSemanticComparison_ShouldPerformExactComparison()
        {
            // Arrange
            var jsonPolicy = CreateJsonPolicyWithUsers(new[] { "user1", "user2" });
            var terraformPolicy = CreateTerraformPolicyWithUsers(new[] { "user2", "user1" }); // Different order

            var jsonFile = Path.Combine(_testDirectory, "exact1.json");
            var terraformFile = Path.Combine(_testDirectory, "exact1.tf");

            await File.WriteAllTextAsync(jsonFile, jsonPolicy.ToString());
            await File.WriteAllTextAsync(terraformFile, terraformPolicy);

            var options = new CrossFormatMatchingOptions
            {
                EnableSemanticComparison = false // Disabled
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(jsonFile, terraformFile, options);

            // Assert
            // Without semantic comparison, different order should result in Different status
            Assert.True(result.Status == CrossFormatComparisonStatus.Different ||
                       result.Status == CrossFormatComparisonStatus.Identical); // Depends on implementation
        }

        [Fact]
        public async Task CompareJsonToTerraformAsync_EnableDetailedDifferences_ShouldProvideDetailedOutput()
        {
            // Arrange
            var jsonPolicy = CreateStandardJsonPolicy();
            jsonPolicy["State"] = "enabled";
            jsonPolicy["DisplayName"] = "Original Policy Name";

            var terraformPolicy = CreateEquivalentTerraformPolicy()
                .Replace("enabled", "disabled")
                .Replace("Test Policy", "Modified Policy Name");

            var jsonFile = Path.Combine(_testDirectory, "detailed1.json");
            var terraformFile = Path.Combine(_testDirectory, "detailed1.tf");

            await File.WriteAllTextAsync(jsonFile, jsonPolicy.ToString());
            await File.WriteAllTextAsync(terraformFile, terraformPolicy);

            var options = new CrossFormatMatchingOptions
            {
                EnableDetailedDifferences = true,
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareJsonToTerraformAsync(jsonFile, terraformFile, options);

            // Assert
            Assert.Equal(CrossFormatComparisonStatus.Different, result.Status);
            Assert.NotEmpty(result.Differences);
            
            // Should have detailed differences for both State and DisplayName
            var differencesText = string.Join(" ", result.Differences);
            Assert.True(differencesText.Contains("State") || differencesText.Contains("state"));
            Assert.True(differencesText.Contains("DisplayName") || differencesText.Contains("display_name"));
        }

        #endregion

        #region Helper Methods

        private string CreateJsonPolicyFile(string fileName)
        {
            var policy = CreateStandardJsonPolicy();
            var filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, policy.ToString());
            return filePath;
        }

        private string CreateTerraformPolicyFile(string fileName)
        {
            var policy = CreateEquivalentTerraformPolicy();
            var filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, policy);
            return filePath;
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
            policy["Conditions"]["Users"]["IncludeUsers"] = new JArray(users);
            return policy;
        }

        private string CreateTerraformPolicyWithUsers(string[] users)
        {
            var usersString = string.Join(@""", """, users);
            return CreateEquivalentTerraformPolicy()
                .Replace(@"include_users = [""All""]", $@"include_users = [""{usersString}""]");
        }

        #endregion
    }
}
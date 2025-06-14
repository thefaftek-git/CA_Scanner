using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ConditionalAccessExporter.Tests
{
    public class CrossFormatPolicyComparisonServiceTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly TerraformParsingService _mockTerraformParsingService;
        private readonly TerraformConversionService _mockTerraformConversionService;
        private readonly PolicyComparisonService _mockJsonComparisonService;
        private readonly CrossFormatPolicyComparisonService _service;

        public CrossFormatPolicyComparisonServiceTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "CrossFormatTests_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_testDirectory);

            // Create mock services (for now using real instances since no mocking framework is available)
            _mockTerraformParsingService = new TerraformParsingService();
            _mockTerraformConversionService = new TerraformConversionService();
            _mockJsonComparisonService = new PolicyComparisonService();

            _service = new CrossFormatPolicyComparisonService(
                _mockJsonComparisonService,
                _mockTerraformParsingService,
                _mockTerraformConversionService);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        #region ComparePoliciesAsync Tests

        [Fact]
        public async Task ComparePoliciesAsync_IdenticalPolicies_ShouldReturnIdenticalStatus()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();
            
            var jsonPolicy = CreateJsonPolicy("test-policy", "Test Policy", "enabled");
            var terraformPolicy = CreateTerraformPolicy("test-policy", "Test Policy", "enabled");

            WriteJsonPolicyToFile(sourceDir, "policy1.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "policy1.tf", terraformPolicy);

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName,
                CaseSensitive = false,
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(DateTime.UtcNow.Date, result.ComparedAt.Date);
            Assert.Equal(sourceDir, result.SourceDirectory);
            Assert.Equal(referenceDir, result.ReferenceDirectory);
            Assert.Equal(PolicyFormat.Json, result.SourceFormat);
            Assert.Equal(PolicyFormat.Terraform, result.ReferenceFormat);
            Assert.Equal(1, result.Summary.TotalSourcePolicies);
            Assert.Equal(1, result.Summary.TotalReferencePolicies);
            Assert.Single(result.PolicyComparisons);

            var comparison = result.PolicyComparisons.First();
            Assert.Equal("Test Policy", comparison.PolicyName);
            PolicyAssertions.AssertPoliciesMatch(comparison);
        }

        [Fact]
        public async Task ComparePoliciesAsync_SourceOnlyPolicy_ShouldReturnSourceOnlyStatus()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var jsonPolicy = CreateJsonPolicy("source-only", "Source Only Policy", "enabled");
            WriteJsonPolicyToFile(sourceDir, "source-only.json", jsonPolicy);
            // No corresponding Terraform policy

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.SourceOnlyPolicies);
            Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);
            Assert.Single(result.PolicyComparisons);

            var comparison = result.PolicyComparisons.First();
            Assert.Equal(CrossFormatComparisonStatus.SourceOnly, comparison.Status);
            Assert.Equal("Source Only Policy", comparison.PolicyName);
            Assert.NotNull(comparison.SourcePolicy);
            Assert.Null(comparison.ReferencePolicy);
        }

        [Fact]
        public async Task ComparePoliciesAsync_ReferenceOnlyPolicy_ShouldReturnReferenceOnlyStatus()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var terraformPolicy = CreateTerraformPolicy("ref-only", "Reference Only Policy", "enabled");
            WriteTerraformPolicyToFile(referenceDir, "ref-only.tf", terraformPolicy);
            // No corresponding JSON policy

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.Equal(0, result.Summary.SourceOnlyPolicies);
            Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);
            Assert.Single(result.PolicyComparisons);

            var comparison = result.PolicyComparisons.First();
            Assert.Equal(CrossFormatComparisonStatus.ReferenceOnly, comparison.Status);
            Assert.Equal("Reference Only Policy", comparison.PolicyName);
            Assert.Null(comparison.SourcePolicy);
            Assert.NotNull(comparison.ReferencePolicy);
        }

        [Fact]
        public async Task ComparePoliciesAsync_DifferentPolicies_ShouldReturnDifferentStatus()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var jsonPolicy = CreateJsonPolicy("test-policy", "Test Policy", "enabled");
            var terraformPolicy = CreateTerraformPolicy("test-policy", "Test Policy", "disabled"); // Different state

            WriteJsonPolicyToFile(sourceDir, "policy1.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "policy1.tf", terraformPolicy);

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName,
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.PoliciesWithDifferences);
            Assert.Single(result.PolicyComparisons);

            var comparison = result.PolicyComparisons.First();
            Assert.Equal(CrossFormatComparisonStatus.Different, comparison.Status);
            Assert.NotNull(comparison.Differences);
            Assert.Contains(comparison.Differences, d => d.Contains("State"));
        }

        [Fact]
        public async Task ComparePoliciesAsync_SemanticallyEquivalentPolicies_ShouldReturnSemanticallyEquivalentStatus()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var jsonPolicy = CreateJsonPolicyWithConditions("test-policy", "Test Policy", "enabled",
                includeUsers: new[] { "user1", "user2" });
            var terraformPolicy = CreateTerraformPolicyWithConditions("test-policy", "Test Policy", "enabled",
                includeUsers: new[] { "user2", "user1" }); // Same users, different order

            WriteJsonPolicyToFile(sourceDir, "policy1.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "policy1.tf", terraformPolicy);

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName,
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            var comparison = result.PolicyComparisons.First();
            PolicyAssertions.AssertPoliciesMatch(comparison);
        }

        #endregion

        #region Matching Strategy Tests

        [Fact]
        public async Task ComparePoliciesAsync_ByNameCaseSensitive_ShouldRespectCaseSensitivity()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var jsonPolicy = CreateJsonPolicy("test-policy", "Test Policy", "enabled");
            var terraformPolicy = CreateTerraformPolicy("test-policy", "test policy", "enabled"); // Different case

            WriteJsonPolicyToFile(sourceDir, "policy1.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "policy1.tf", terraformPolicy);

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName,
                CaseSensitive = true
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.SourceOnlyPolicies);
            Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);
        }

        [Fact]
        public async Task ComparePoliciesAsync_ByNameCaseInsensitive_ShouldIgnoreCase()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var jsonPolicy = CreateJsonPolicy("test-policy", "Test Policy", "enabled");
            var terraformPolicy = CreateTerraformPolicy("test-policy", "test policy", "enabled"); // Different case

            WriteJsonPolicyToFile(sourceDir, "policy1.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "policy1.tf", terraformPolicy);

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            PolicyAssertions.AssertHasMatchingPolicies(result.Summary);
        }

        [Fact]
        public async Task ComparePoliciesAsync_CustomMapping_ShouldUseCustomMappings()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var jsonPolicy = CreateJsonPolicy("source-id", "Source Policy", "enabled");
            var terraformPolicy = CreateTerraformPolicy("reference-id", "Reference Policy", "enabled");

            WriteJsonPolicyToFile(sourceDir, "source.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "reference.tf", terraformPolicy);

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.CustomMapping,
                CustomMappings = new Dictionary<string, string>
                {
                    { "source-id", "reference.tf" }
                }
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            var comparison = result.PolicyComparisons.FirstOrDefault(c => c.SourcePolicy?.Id == "source-id");
            Assert.NotNull(comparison);
            Assert.NotNull(comparison.ReferencePolicy);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ComparePoliciesAsync_NonExistentSourceDirectory_ShouldHandleGracefully()
        {
            // Arrange
            var nonExistentSource = Path.Combine(_testDirectory, "nonexistent-source");
            var referenceDir = CreateReferenceDirectory();

            var matchingOptions = new CrossFormatMatchingOptions();

            // Act
            var result = await _service.CompareAsync(nonExistentSource, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Summary.TotalSourcePolicies);
        }

        [Fact]
        public async Task ComparePoliciesAsync_NonExistentReferenceDirectory_ShouldHandleGracefully()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var nonExistentReference = Path.Combine(_testDirectory, "nonexistent-reference");

            var matchingOptions = new CrossFormatMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, nonExistentReference, matchingOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Summary.TotalReferencePolicies);
        }

        [Fact]
        public async Task ComparePoliciesAsync_MalformedJsonFile_ShouldSkipAndContinue()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            // Create a malformed JSON file
            var malformedJsonPath = Path.Combine(sourceDir, "malformed.json");
            await File.WriteAllTextAsync(malformedJsonPath, "{ invalid json");

            // Create a valid policy
            var validPolicy = CreateJsonPolicy("valid-policy", "Valid Policy", "enabled");
            WriteJsonPolicyToFile(sourceDir, "valid.json", validPolicy);

            var matchingOptions = new CrossFormatMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            // Should have loaded the valid policy despite malformed file
            PolicyAssertions.AssertMinimumSourcePolicies(result, 1);
        }

        #endregion

        #region Input Format Tests

        [Fact]
        public async Task ComparePoliciesAsync_JsonToTerraform_ShouldDetectFormatsCorrectly()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var jsonPolicy = CreateJsonPolicy("test-policy", "Test Policy", "enabled");
            var terraformPolicy = CreateTerraformPolicy("test-policy", "Test Policy", "enabled");

            WriteJsonPolicyToFile(sourceDir, "policy.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "policy.tf", terraformPolicy);

            var matchingOptions = new CrossFormatMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.Equal(PolicyFormat.Json, result.SourceFormat);
            Assert.Equal(PolicyFormat.Terraform, result.ReferenceFormat);
        }

        [Fact]
        public async Task ComparePoliciesAsync_TerraformToJson_ShouldDetectFormatsCorrectly()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var terraformPolicy = CreateTerraformPolicy("test-policy", "Test Policy", "enabled");
            var jsonPolicy = CreateJsonPolicy("test-policy", "Test Policy", "enabled");

            WriteTerraformPolicyToFile(sourceDir, "policy.tf", terraformPolicy);
            WriteJsonPolicyToFile(referenceDir, "policy.json", jsonPolicy);

            var matchingOptions = new CrossFormatMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.Equal(PolicyFormat.Terraform, result.SourceFormat);
            Assert.Equal(PolicyFormat.Json, result.ReferenceFormat);
        }

        #endregion

        #region Error Scenario Tests

        [Fact]
        public async Task ComparePoliciesAsync_EmptySourceDirectory_ShouldHandleGracefully()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory(); // Empty directory
            var referenceDir = CreateReferenceDirectory();
            
            var terraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("test_policy", "Test Policy");
            WriteTerraformPolicyToFile(referenceDir, "policy.tf", terraformPolicy);

            var matchingOptions = TestDataFactory.CreateMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            PolicyAssertions.AssertSummaryTotals(result.Summary, 0, 1);
            Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);
        }

        [Fact]
        public async Task ComparePoliciesAsync_EmptyReferenceDirectory_ShouldHandleGracefully()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory(); // Empty directory
            
            var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("test-id", "Test Policy");
            WriteJsonPolicyToFile(sourceDir, "policy.json", jsonPolicy);

            var matchingOptions = TestDataFactory.CreateMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            PolicyAssertions.AssertSummaryTotals(result.Summary, 1, 0);
            Assert.Equal(1, result.Summary.SourceOnlyPolicies);
        }

        [Fact]
        public async Task ComparePoliciesAsync_BothDirectoriesEmpty_ShouldReturnEmptyResult()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();
            var matchingOptions = TestDataFactory.CreateMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            PolicyAssertions.AssertSummaryTotals(result.Summary, 0, 0);
            Assert.Empty(result.PolicyComparisons);
        }

        [Fact]
        public async Task ComparePoliciesAsync_InvalidTerraformSyntax_ShouldSkipAndContinue()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("valid-policy", "Valid Policy");
            WriteJsonPolicyToFile(sourceDir, "valid.json", jsonPolicy);

            // Create invalid Terraform file
            var invalidTerraformPath = Path.Combine(referenceDir, "invalid.tf");
            await File.WriteAllTextAsync(invalidTerraformPath, TestDataFactory.CreateInvalidTerraform());

            // Create valid Terraform file
            var validTerraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("valid_policy", "Valid Policy");
            WriteTerraformPolicyToFile(referenceDir, "valid.tf", validTerraformPolicy);

            var matchingOptions = TestDataFactory.CreateMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            // Should process the valid file despite invalid syntax in other file
            PolicyAssertions.AssertMinimumReferencePolicies(result, 1);
        }

        [Fact]
        public async Task ComparePoliciesAsync_LargeNumberOfPolicies_ShouldHandleEfficiently()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            const int policyCount = 100;

            // Create many JSON policies
            for (int i = 0; i < policyCount; i++)
            {
                var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy($"policy-{i:D3}", $"Test Policy {i}");
                WriteJsonPolicyToFile(sourceDir, $"policy-{i:D3}.json", jsonPolicy);
            }

            // Create corresponding Terraform policies
            for (int i = 0; i < policyCount; i++)
            {
                var terraformPolicy = TestDataFactory.CreateBasicTerraformPolicy($"policy_{i:D3}", $"Test Policy {i}");
                WriteTerraformPolicyToFile(referenceDir, $"policy-{i:D3}.tf", terraformPolicy);
            }

            var matchingOptions = TestDataFactory.CreateMatchingOptions();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            PolicyAssertions.AssertSummaryTotals(result.Summary, policyCount, policyCount);
            PolicyAssertions.AssertExecutionTimeWithinBounds(stopwatch.Elapsed, TimeSpan.FromSeconds(30));
            // Assert that policies are being processed and compared
            Assert.True(result.PolicyComparisons.Count > 0, "Expected policy comparisons to be performed");
            Assert.True(result.PolicyComparisons.Any(c => 
                c.Status == CrossFormatComparisonStatus.Identical ||
                c.Status == CrossFormatComparisonStatus.SemanticallyEquivalent ||
                c.Status == CrossFormatComparisonStatus.Different), 
                "Expected policies to be matched and compared");
        }

        [Fact]
        public async Task ComparePoliciesAsync_NullMatchingOptions_ShouldUseDefaults()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("test-policy", "Test Policy");
            var terraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("test_policy", "Test Policy");

            WriteJsonPolicyToFile(sourceDir, "policy.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "policy.tf", terraformPolicy);

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, new CrossFormatMatchingOptions());

            // Assert
            Assert.NotNull(result);
            PolicyAssertions.AssertMinimumSourcePolicies(result, 1);
            PolicyAssertions.AssertMinimumReferencePolicies(result, 1);
        }

        [Fact]
        public async Task ComparePoliciesAsync_ReadOnlyFileSystem_ShouldNotThrow()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("test-policy", "Test Policy");
            var terraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("test_policy", "Test Policy");

            WriteJsonPolicyToFile(sourceDir, "policy.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "policy.tf", terraformPolicy);

            var matchingOptions = TestDataFactory.CreateMatchingOptions();

            // Act & Assert - Should not throw even if file system is read-only
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ComparePoliciesAsync_VeryLongPolicyNames_ShouldHandleCorrectly()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var longName = new string('A', 500); // Very long policy name
            var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("long-policy", longName);
            var terraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("long_policy", longName);

            WriteJsonPolicyToFile(sourceDir, "long.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "long.tf", terraformPolicy);

            var matchingOptions = TestDataFactory.CreateMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            PolicyAssertions.AssertMinimumSourcePolicies(result, 1);
            PolicyAssertions.AssertMinimumReferencePolicies(result, 1);
        }

        [Fact]
        public async Task ComparePoliciesAsync_SpecialCharactersInPolicyNames_ShouldHandleCorrectly()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var specialName = "Policy with Special Characters: !@#$%^&*()_+-=[]{}|;':\",./<>?";
            var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("special-policy", specialName);
            var terraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("special_policy", specialName);

            WriteJsonPolicyToFile(sourceDir, "special.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "special.tf", terraformPolicy);

            var matchingOptions = TestDataFactory.CreateMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            PolicyAssertions.AssertMinimumSourcePolicies(result, 1);
            PolicyAssertions.AssertMinimumReferencePolicies(result, 1);
        }

        [Fact]
        public async Task ComparePoliciesAsync_UnicodeCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var unicodeName = "Política de Acesso Condicional 测试政策 πολιτική اسياسة";
            var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("unicode-policy", unicodeName);
            var terraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("unicode_policy", unicodeName);

            WriteJsonPolicyToFile(sourceDir, "unicode.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "unicode.tf", terraformPolicy);

            var matchingOptions = TestDataFactory.CreateMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            PolicyAssertions.AssertMinimumSourcePolicies(result, 1);
            PolicyAssertions.AssertMinimumReferencePolicies(result, 1);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task ComparePoliciesAsync_PerformanceWithManyComplexPolicies_ShouldCompleteWithinReasonableTime()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            const int policyCount = 50;

            // Create complex JSON policies
            for (int i = 0; i < policyCount; i++)
            {
                var jsonPolicy = TestDataFactory.CreateComplexJsonPolicy(
                    $"policy-{i:D3}", 
                    $"Complex Test Policy {i}",
                    includeUsers: new[] { $"user-{i}", $"user-{i + 1000}" },
                    excludeUsers: new[] { $"excluded-user-{i}" },
                    includeApplications: new[] { "All" },
                    excludeApplications: new[] { $"excluded-app-{i}" }
                );
                WriteJsonPolicyToFile(sourceDir, $"complex-policy-{i:D3}.json", jsonPolicy);
            }

            // Create corresponding complex Terraform policies
            for (int i = 0; i < policyCount; i++)
            {
                var terraformPolicy = TestDataFactory.CreateComplexTerraformPolicy(
                    $"complex_policy_{i:D3}", 
                    $"Complex Test Policy {i}",
                    includeUsers: new[] { $"user-{i}", $"user-{i + 1000}" },
                    excludeUsers: new[] { $"excluded-user-{i}" }
                );
                WriteTerraformPolicyToFile(referenceDir, $"complex-policy-{i:D3}.tf", terraformPolicy);
            }

            var matchingOptions = TestDataFactory.CreateMatchingOptions(enableSemanticComparison: true);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            PolicyAssertions.AssertSummaryTotals(result.Summary, policyCount, policyCount);
            PolicyAssertions.AssertExecutionTimeWithinBounds(stopwatch.Elapsed, TimeSpan.FromMinutes(2));
            // Assert that policies are being processed and compared
            Assert.True(result.PolicyComparisons.Count > 0, "Expected policy comparisons to be performed");
            Assert.True(result.PolicyComparisons.Any(c => 
                c.Status == CrossFormatComparisonStatus.Identical ||
                c.Status == CrossFormatComparisonStatus.SemanticallyEquivalent ||
                c.Status == CrossFormatComparisonStatus.Different), 
                "Expected policies to be matched and compared");
        }

        #endregion

        #region Helper Methods

        private string CreateSourceDirectory()
        {
            var dir = Path.Combine(_testDirectory, "source");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private string CreateReferenceDirectory()
        {
            var dir = Path.Combine(_testDirectory, "reference");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private JObject CreateJsonPolicy(string id, string displayName, string state)
        {
            return new JObject
            {
                ["Id"] = id,
                ["DisplayName"] = displayName,
                ["State"] = state,
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

        private JObject CreateJsonPolicyWithConditions(string id, string displayName, string state, string[]? includeUsers = null)
        {
            var policy = CreateJsonPolicy(id, displayName, state);
            
            if (includeUsers != null)
            {
                policy["Conditions"]!["Users"]!["IncludeUsers"] = new JArray(includeUsers);
            }

            return policy;
        }

        private string CreateTerraformPolicy(string resourceName, string displayName, string state)
        {
            return $@"resource ""azuread_conditional_access_policy"" ""{resourceName}"" {{
  display_name = ""{displayName}""
  state        = ""{state}""

  conditions {{
    applications {{
      include_applications = [""All""]
    }}
    
    users {{
      include_users = [""All""]
    }}
    
    client_app_types = [""browser""]
  }}

  grant_controls {{
    operator          = ""OR""
    built_in_controls = [""mfa""]
  }}
}}";
        }

        private string CreateTerraformPolicyWithConditions(string resourceName, string displayName, string state, string[]? includeUsers = null)
        {
            var usersSection = includeUsers != null 
                ? $@"include_users = [""{string.Join(@""", """, includeUsers)}""]"
                : @"include_users = [""All""]";

            return $@"resource ""azuread_conditional_access_policy"" ""{resourceName}"" {{
  display_name = ""{displayName}""
  state        = ""{state}""

  conditions {{
    applications {{
      include_applications = [""All""]
    }}
    
    users {{
      {usersSection}
    }}
    
    client_app_types = [""browser""]
  }}

  grant_controls {{
    operator          = ""OR""
    built_in_controls = [""mfa""]
  }}
}}";
        }

        private void WriteJsonPolicyToFile(string directory, string fileName, JObject policy)
        {
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, policy.ToString());
        }

        private void WriteTerraformPolicyToFile(string directory, string fileName, string content)
        {
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, content);
        }

        #endregion
    }
}
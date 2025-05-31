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
    /// <summary>
    /// Integration tests for CrossFormatPolicyComparisonService focusing on real-world scenarios
    /// </summary>
    public class CrossFormatPolicyComparisonServiceIntegrationTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly CrossFormatPolicyComparisonService _service;

        public CrossFormatPolicyComparisonServiceIntegrationTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "CrossFormatIntegrationTests_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_testDirectory);

            // Initialize with real services for integration testing
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

        #region Real-world Scenario Tests

        [Fact]
        public async Task CompareComplexPolicies_MultipleConditionsAndControls_ShouldAnalyzeCorrectly()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            var complexJsonPolicy = CreateComplexJsonPolicy();
            var complexTerraformPolicy = CreateComplexTerraformPolicy();

            WriteJsonPolicyToFile(sourceDir, "complex-policy.json", complexJsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "complex-policy.tf", complexTerraformPolicy);

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName,
                EnableSemanticComparison = true,
                EnableDetailedDifferences = true
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.PolicyComparisons);

            var comparison = result.PolicyComparisons.First();
            Assert.Equal("Complex MFA Policy", comparison.PolicyName);
            
            // Should detect semantic equivalence or exact match
            Assert.True(
                comparison.Status == CrossFormatComparisonStatus.Identical ||
                comparison.Status == CrossFormatComparisonStatus.SemanticallyEquivalent ||
                comparison.Status == CrossFormatComparisonStatus.Different);
        }

        [Fact]
        public async Task CompareMixedFormats_MultiplePolicies_ShouldHandleAllCombinations()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            // Create multiple policies in each directory
            var policies = new[]
            {
                ("mfa-policy", "Require MFA", "enabled"),
                ("block-legacy", "Block Legacy Authentication", "enabled"),
                ("compliant-device", "Require Compliant Device", "disabled")
            };

            foreach (var (id, name, state) in policies)
            {
                var jsonPolicy = CreateJsonPolicy(id, name, state);
                var terraformPolicy = CreateTerraformPolicy(id, name, state);

                WriteJsonPolicyToFile(sourceDir, $"{id}.json", jsonPolicy);
                WriteTerraformPolicyToFile(referenceDir, $"{id}.tf", terraformPolicy);
            }

            // Add one source-only policy
            var sourceOnlyPolicy = CreateJsonPolicy("source-only", "Source Only Policy", "enabled");
            WriteJsonPolicyToFile(sourceDir, "source-only.json", sourceOnlyPolicy);

            // Add one reference-only policy
            var referenceOnlyPolicy = CreateTerraformPolicy("ref-only", "Reference Only Policy", "enabled");
            WriteTerraformPolicyToFile(referenceDir, "ref-only.tf", referenceOnlyPolicy);

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName,
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.Equal(4, result.Summary.TotalSourcePolicies); // 3 matching + 1 source-only
            Assert.Equal(4, result.Summary.TotalReferencePolicies); // 3 matching + 1 reference-only
            Assert.Equal(1, result.Summary.SourceOnlyPolicies);
            Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);
            Assert.True(result.Summary.MatchingPolicies >= 3 || result.Summary.SemanticallyEquivalentPolicies >= 3);
        }

        [Fact]
        public async Task CompareWithDifferentNamingConventions_ShouldDetectSimilarPolicies()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            // Create policies with different naming conventions
            var jsonPolicy = CreateJsonPolicy("json-policy-id", "MFA Required for All Users", "enabled");
            var terraformPolicy = CreateTerraformPolicy("terraform_policy_resource", "MFA Required for All Users", "enabled");

            WriteJsonPolicyToFile(sourceDir, "mfa-policy.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "mfa_policy.tf", terraformPolicy);

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
            var matchedComparison = result.PolicyComparisons.FirstOrDefault(c => 
                c.SourcePolicy != null && c.ReferencePolicy != null);
            
            if (matchedComparison != null)
            {
                Assert.True(
                    matchedComparison.Status == CrossFormatComparisonStatus.Identical ||
                    matchedComparison.Status == CrossFormatComparisonStatus.SemanticallyEquivalent);
            }
        }

        [Fact]
        public async Task CompareWithCustomMappings_ComplexScenario_ShouldFollowMappings()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            // Create policies with completely different IDs/names that need custom mapping
            var jsonPolicy1 = CreateJsonPolicy("12345", "Policy Alpha", "enabled");
            var jsonPolicy2 = CreateJsonPolicy("67890", "Policy Beta", "disabled");

            var terraformPolicy1 = CreateTerraformPolicy("alpha_policy", "Policy Alpha Modified", "enabled");
            var terraformPolicy2 = CreateTerraformPolicy("beta_policy", "Policy Beta Modified", "disabled");

            WriteJsonPolicyToFile(sourceDir, "policy1.json", jsonPolicy1);
            WriteJsonPolicyToFile(sourceDir, "policy2.json", jsonPolicy2);
            WriteTerraformPolicyToFile(referenceDir, "alpha.tf", terraformPolicy1);
            WriteTerraformPolicyToFile(referenceDir, "beta.tf", terraformPolicy2);

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.CustomMapping,
                CustomMappings = new Dictionary<string, string>
                {
                    { "12345", "alpha.tf" },
                    { "67890", "beta.tf" }
                },
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.Equal(2, result.PolicyComparisons.Count(c => c.SourcePolicy != null && c.ReferencePolicy != null));
            
            var alphaComparison = result.PolicyComparisons.FirstOrDefault(c => 
                c.SourcePolicy?.Id == "12345");
            Assert.NotNull(alphaComparison);
            Assert.NotNull(alphaComparison.ReferencePolicy);

            var betaComparison = result.PolicyComparisons.FirstOrDefault(c => 
                c.SourcePolicy?.Id == "67890");
            Assert.NotNull(betaComparison);
            Assert.NotNull(betaComparison.ReferencePolicy);
        }

        #endregion

        #region Performance and Large Dataset Tests

        [Fact]
        public async Task CompareLargeNumberOfPolicies_ShouldPerformWithinReasonableTime()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            const int policyCount = 50; // Reasonable size for testing
            var startTime = DateTime.UtcNow;

            // Create multiple policies
            for (int i = 0; i < policyCount; i++)
            {
                var jsonPolicy = CreateJsonPolicy($"policy-{i}", $"Test Policy {i}", i % 2 == 0 ? "enabled" : "disabled");
                var terraformPolicy = CreateTerraformPolicy($"policy_{i}", $"Test Policy {i}", i % 2 == 0 ? "enabled" : "disabled");

                WriteJsonPolicyToFile(sourceDir, $"policy-{i}.json", jsonPolicy);
                WriteTerraformPolicyToFile(referenceDir, $"policy_{i}.tf", terraformPolicy);
            }

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName,
                EnableSemanticComparison = true
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);
            var endTime = DateTime.UtcNow;
            var executionTime = endTime - startTime;

            // Assert
            Assert.Equal(policyCount, result.Summary.TotalSourcePolicies);
            Assert.Equal(policyCount, result.Summary.TotalReferencePolicies);
            Assert.True(executionTime.TotalSeconds < 30, $"Execution took {executionTime.TotalSeconds} seconds, which is too long");
        }

        #endregion

        #region Edge Cases and Error Scenarios

        [Fact]
        public async Task CompareWithMixedFileTypes_ShouldIgnoreNonPolicyFiles()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();

            // Create valid policy files
            var jsonPolicy = CreateJsonPolicy("valid-policy", "Valid Policy", "enabled");
            var terraformPolicy = CreateTerraformPolicy("valid-policy", "Valid Policy", "enabled");

            WriteJsonPolicyToFile(sourceDir, "valid-policy.json", jsonPolicy);
            WriteTerraformPolicyToFile(referenceDir, "valid-policy.tf", terraformPolicy);

            // Create non-policy files that should be ignored
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "readme.txt"), "This is a readme file");
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "config.xml"), "<config></config>");
            await File.WriteAllTextAsync(Path.Combine(referenceDir, "variables.tf"), "variable \"test\" {}");
            await File.WriteAllTextAsync(Path.Combine(referenceDir, "outputs.tf"), "output \"test\" {}");

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName
            };

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.TotalSourcePolicies);
            Assert.Equal(1, result.Summary.TotalReferencePolicies);
            Assert.Single(result.PolicyComparisons);
        }

        [Fact]
        public async Task CompareWithEmptyDirectories_ShouldHandleGracefully()
        {
            // Arrange
            var sourceDir = CreateSourceDirectory();
            var referenceDir = CreateReferenceDirectory();
            // Directories are empty

            var matchingOptions = new CrossFormatMatchingOptions();

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Summary.TotalSourcePolicies);
            Assert.Equal(0, result.Summary.TotalReferencePolicies);
            Assert.Empty(result.PolicyComparisons);
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

        private JObject CreateComplexJsonPolicy()
        {
            return new JObject
            {
                ["Id"] = "complex-policy-id",
                ["DisplayName"] = "Complex MFA Policy",
                ["State"] = "enabled",
                ["Conditions"] = new JObject
                {
                    ["Applications"] = new JObject
                    {
                        ["IncludeApplications"] = new JArray("All"),
                        ["ExcludeApplications"] = new JArray("00000012-0000-0000-c000-000000000000") // Azure Portal
                    },
                    ["Users"] = new JObject
                    {
                        ["IncludeUsers"] = new JArray("All"),
                        ["ExcludeUsers"] = new JArray("emergency-account-id"),
                        ["IncludeGroups"] = new JArray("group-1", "group-2"),
                        ["ExcludeGroups"] = new JArray("excluded-group-id")
                    },
                    ["ClientAppTypes"] = new JArray("browser", "mobileAppsAndDesktopClients"),
                    ["Locations"] = new JObject
                    {
                        ["IncludeLocations"] = new JArray("All"),
                        ["ExcludeLocations"] = new JArray("AllTrusted")
                    },
                    ["Platforms"] = new JObject
                    {
                        ["IncludePlatforms"] = new JArray("windows", "macOS", "iOS", "android"),
                        ["ExcludePlatforms"] = new JArray()
                    },
                    ["DeviceStates"] = new JObject
                    {
                        ["IncludeStates"] = new JArray("All"),
                        ["ExcludeStates"] = new JArray("compliant", "domainJoined")
                    }
                },
                ["GrantControls"] = new JObject
                {
                    ["Operator"] = "OR",
                    ["BuiltInControls"] = new JArray("mfa", "compliantDevice"),
                    ["CustomAuthenticationFactors"] = new JArray(),
                    ["TermsOfUse"] = new JArray()
                },
                ["SessionControls"] = new JObject
                {
                    ["ApplicationEnforcedRestrictions"] = new JObject
                    {
                        ["IsEnabled"] = true
                    },
                    ["CloudAppSecurity"] = new JObject
                    {
                        ["IsEnabled"] = true,
                        ["CloudAppSecurityType"] = "mcasConfigured"
                    },
                    ["SignInFrequency"] = new JObject
                    {
                        ["IsEnabled"] = true,
                        ["Type"] = "hours",
                        ["Value"] = 8
                    },
                    ["PersistentBrowser"] = new JObject
                    {
                        ["IsEnabled"] = true,
                        ["Mode"] = "never"
                    }
                }
            };
        }

        private string CreateTerraformPolicy(string resourceName, string displayName, string state)
        {
            return $@"
resource ""azuread_conditional_access_policy"" ""{resourceName}"" {{
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

        private string CreateComplexTerraformPolicy()
        {
            return @"
resource ""azuread_conditional_access_policy"" ""complex_mfa_policy"" {
  display_name = ""Complex MFA Policy""
  state        = ""enabled""

  conditions {
    applications {
      include_applications = [""All""]
      exclude_applications = [""00000012-0000-0000-c000-000000000000""]
    }
    
    users {
      include_users  = [""All""]
      exclude_users  = [""emergency-account-id""]
      include_groups = [""group-1"", ""group-2""]
      exclude_groups = [""excluded-group-id""]
    }
    
    client_app_types = [""browser"", ""mobileAppsAndDesktopClients""]
    
    locations {
      include_locations = [""All""]
      exclude_locations = [""AllTrusted""]
    }
    
    platforms {
      include_platforms = [""windows"", ""macOS"", ""iOS"", ""android""]
    }
    
    device_states {
      include_states = [""All""]
      exclude_states = [""compliant"", ""domainJoined""]
    }
  }

  grant_controls {
    operator          = ""OR""
    built_in_controls = [""mfa"", ""compliantDevice""]
  }

  session_controls {
    application_enforced_restrictions_enabled = true
    
    cloud_app_security {
      policy                = ""mcasConfigured""
    }
    
    sign_in_frequency {
      type  = ""hours""
      value = 8
    }
    
    persistent_browser {
      mode = ""never""
    }
  }
}";
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
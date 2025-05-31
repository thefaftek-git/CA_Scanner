using ConditionalAccessExporter.Models;
using ConditionalAccessExporter.Services;
using Newtonsoft.Json.Linq;
using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace ConditionalAccessExporter.Tests
{
    public class PolicyComparisonServiceTests : IDisposable
    {
        private readonly PolicyComparisonService _service;
        private readonly string _tempDirectory;

        public PolicyComparisonServiceTests()
        {
            _service = new PolicyComparisonService();
            _tempDirectory = Path.GetTempPath();
        }

        #region Helper Methods

        private JObject CreateTestEntraPolicy(string id, string displayName, string state = "enabled")
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
                    }
                },
                ["GrantControls"] = new JObject
                {
                    ["Operator"] = "OR",
                    ["BuiltInControls"] = new JArray("mfa")
                }
            };
        }

        private object CreateTestEntraExport(string tenantId, params JObject[] policies)
        {
            return new
            {
                TenantId = tenantId,
                Policies = policies
            };
        }

        private string CreateTestReferenceDirectory(params (string fileName, JObject policy)[] policies)
        {
            var directory = Path.Combine(_tempDirectory, $"TestRef_{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);

            foreach (var (fileName, policy) in policies)
            {
                var filePath = Path.Combine(directory, fileName);
                File.WriteAllText(filePath, policy.ToString());
            }

            return directory;
        }

        private void CleanupDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        #endregion

        #region Test Case 1.1: Identical policies

        [Fact]
        public async Task CompareAsync_IdenticalPolicies_ShouldReturnIdenticalStatus()
        {
            // Arrange
            var policy1 = CreateTestEntraPolicy("policy-1", "Test Policy 1");
            var policy2 = CreateTestEntraPolicy("policy-2", "Test Policy 2");
            var entraExport = CreateTestEntraExport("tenant-123", policy1, policy2);

            var refDirectory = CreateTestReferenceDirectory(
                ("policy1.json", policy1),
                ("policy2.json", policy2)
            );

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(DateTime.UtcNow.Date, result.ComparedAt.Date);
                Assert.Equal("tenant-123", result.TenantId);
                Assert.Equal(refDirectory, result.ReferenceDirectory);

                // Summary assertions
                Assert.Equal(2, result.Summary.TotalEntraPolicies);
                Assert.Equal(2, result.Summary.TotalReferencePolicies);
                Assert.Equal(2, result.Summary.MatchingPolicies);
                Assert.Equal(0, result.Summary.PoliciesWithDifferences);
                Assert.Equal(0, result.Summary.EntraOnlyPolicies);
                Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);

                // Policy comparisons assertions
                Assert.Equal(2, result.PolicyComparisons.Count);
                Assert.All(result.PolicyComparisons, c => Assert.Equal(ComparisonStatus.Identical, c.Status));
                Assert.All(result.PolicyComparisons, c => Assert.Null(c.Differences));
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        #region Test Case 1.2: Entra has extra policy

        [Fact]
        public async Task CompareAsync_EntraHasExtraPolicy_ShouldReportEntraOnly()
        {
            // Arrange
            var policy1 = CreateTestEntraPolicy("policy-1", "Test Policy 1");
            var policy2 = CreateTestEntraPolicy("policy-2", "Test Policy 2");
            var extraPolicy = CreateTestEntraPolicy("policy-3", "Extra Policy");
            var entraExport = CreateTestEntraExport("tenant-123", policy1, policy2, extraPolicy);

            var refDirectory = CreateTestReferenceDirectory(
                ("policy1.json", policy1),
                ("policy2.json", policy2)
            );

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(3, result.Summary.TotalEntraPolicies);
                Assert.Equal(2, result.Summary.TotalReferencePolicies);
                Assert.Equal(2, result.Summary.MatchingPolicies);
                Assert.Equal(0, result.Summary.PoliciesWithDifferences);
                Assert.Equal(1, result.Summary.EntraOnlyPolicies);
                Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);

                var entraOnlyPolicy = result.PolicyComparisons.FirstOrDefault(c => c.Status == ComparisonStatus.EntraOnly);
                Assert.NotNull(entraOnlyPolicy);
                Assert.Equal("Extra Policy", entraOnlyPolicy.PolicyName);
                Assert.Equal("policy-3", entraOnlyPolicy.PolicyId);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        #region Test Case 1.3: Reference has extra policy

        [Fact]
        public async Task CompareAsync_ReferenceHasExtraPolicy_ShouldReportReferenceOnly()
        {
            // Arrange
            var policy1 = CreateTestEntraPolicy("policy-1", "Test Policy 1");
            var policy2 = CreateTestEntraPolicy("policy-2", "Test Policy 2");
            var extraRefPolicy = CreateTestEntraPolicy("policy-3", "Extra Ref Policy");
            var entraExport = CreateTestEntraExport("tenant-123", policy1, policy2);

            var refDirectory = CreateTestReferenceDirectory(
                ("policy1.json", policy1),
                ("policy2.json", policy2),
                ("policy3.json", extraRefPolicy)
            );

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(2, result.Summary.TotalEntraPolicies);
                Assert.Equal(3, result.Summary.TotalReferencePolicies);
                Assert.Equal(2, result.Summary.MatchingPolicies);
                Assert.Equal(0, result.Summary.PoliciesWithDifferences);
                Assert.Equal(0, result.Summary.EntraOnlyPolicies);
                Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);

                var refOnlyPolicy = result.PolicyComparisons.FirstOrDefault(c => c.Status == ComparisonStatus.ReferenceOnly);
                Assert.NotNull(refOnlyPolicy);
                Assert.Equal("Extra Ref Policy", refOnlyPolicy.PolicyName);
                Assert.Equal("policy3.json", refOnlyPolicy.ReferenceFileName);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        #region Test Case 1.4: One policy differs

        [Fact]
        public async Task CompareAsync_OnePolicyDiffers_ShouldReportDifferent()
        {
            // Arrange
            var entraPolicy = CreateTestEntraPolicy("policy-1", "Test Policy 1");
            var differentRefPolicy = CreateTestEntraPolicy("policy-1", "Test Policy 1");
            differentRefPolicy["State"] = "disabled"; // Make it different

            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);

            var refDirectory = CreateTestReferenceDirectory(
                ("policy1.json", differentRefPolicy)
            );

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(1, result.Summary.TotalEntraPolicies);
                Assert.Equal(1, result.Summary.TotalReferencePolicies);
                Assert.Equal(0, result.Summary.MatchingPolicies);
                Assert.Equal(1, result.Summary.PoliciesWithDifferences);
                Assert.Equal(0, result.Summary.EntraOnlyPolicies);
                Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);

                var differentPolicy = result.PolicyComparisons.First();
                Assert.Equal(ComparisonStatus.Different, differentPolicy.Status);
                Assert.NotNull(differentPolicy.Differences);
                Assert.Equal("Test Policy 1", differentPolicy.PolicyName);
                Assert.Equal("policy1.json", differentPolicy.ReferenceFileName);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        #region Test Case 1.5: Multiple differences

        [Fact]
        public async Task CompareAsync_MultipleDifferences_ShouldReportAllStatuses()
        {
            // Arrange
            var identicalPolicy = CreateTestEntraPolicy("policy-1", "Identical Policy");
            var differentPolicy = CreateTestEntraPolicy("policy-2", "Different Policy");
            var entraOnlyPolicy = CreateTestEntraPolicy("policy-3", "Entra Only Policy");

            var identicalRefPolicy = CreateTestEntraPolicy("policy-1", "Identical Policy");
            var differentRefPolicy = CreateTestEntraPolicy("policy-2", "Different Policy");
            differentRefPolicy["State"] = "disabled";
            var refOnlyPolicy = CreateTestEntraPolicy("policy-4", "Reference Only Policy");

            var entraExport = CreateTestEntraExport("tenant-123", identicalPolicy, differentPolicy, entraOnlyPolicy);

            var refDirectory = CreateTestReferenceDirectory(
                ("policy1.json", identicalRefPolicy),
                ("policy2.json", differentRefPolicy),
                ("policy4.json", refOnlyPolicy)
            );

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(3, result.Summary.TotalEntraPolicies);
                Assert.Equal(3, result.Summary.TotalReferencePolicies);
                Assert.Equal(1, result.Summary.MatchingPolicies);
                Assert.Equal(1, result.Summary.PoliciesWithDifferences);
                Assert.Equal(1, result.Summary.EntraOnlyPolicies);
                Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);

                Assert.Contains(result.PolicyComparisons, c => c.Status == ComparisonStatus.Identical);
                Assert.Contains(result.PolicyComparisons, c => c.Status == ComparisonStatus.Different);
                Assert.Contains(result.PolicyComparisons, c => c.Status == ComparisonStatus.EntraOnly);
                Assert.Contains(result.PolicyComparisons, c => c.Status == ComparisonStatus.ReferenceOnly);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        #region Test Case 1.6: Matching strategies

        [Fact]
        public async Task CompareAsync_MatchingByName_ShouldMatchCorrectly()
        {
            // Arrange
            var entraPolicy = CreateTestEntraPolicy("policy-id-1", "Test Policy Name");
            var refPolicy = CreateTestEntraPolicy("different-id-2", "Test Policy Name"); // Same name, different ID

            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);
            var refDirectory = CreateTestReferenceDirectory(("policy.json", refPolicy));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(0, result.Summary.MatchingPolicies);
                Assert.Equal(1, result.Summary.PoliciesWithDifferences); // Because IDs are different
                Assert.Equal(0, result.Summary.EntraOnlyPolicies);
                Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);

                var comparison = result.PolicyComparisons.First();
                Assert.Equal(ComparisonStatus.Different, comparison.Status); // Different because of different IDs
                Assert.Equal("Test Policy Name", comparison.PolicyName);
                Assert.Equal("policy.json", comparison.ReferenceFileName);
                Assert.NotNull(comparison.Differences); // Should have differences
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        [Fact]
        public async Task CompareAsync_MatchingById_ShouldMatchCorrectly()
        {
            // Arrange
            var entraPolicy = CreateTestEntraPolicy("same-id", "Entra Policy Name");
            var refPolicy = CreateTestEntraPolicy("same-id", "Different Policy Name"); // Same ID, different name

            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);
            var refDirectory = CreateTestReferenceDirectory(("policy.json", refPolicy));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ById,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(0, result.Summary.MatchingPolicies);
                Assert.Equal(1, result.Summary.PoliciesWithDifferences);

                var comparison = result.PolicyComparisons.First();
                Assert.Equal(ComparisonStatus.Different, comparison.Status);
                Assert.NotNull(comparison.Differences);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        [Fact]
        public async Task CompareAsync_MatchingById_DifferentIds_ShouldNotMatch()
        {
            // Arrange
            var entraPolicy = CreateTestEntraPolicy("id-1", "Same Policy Name");
            var refPolicy = CreateTestEntraPolicy("id-2", "Same Policy Name"); // Same name, different ID

            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);
            var refDirectory = CreateTestReferenceDirectory(("policy.json", refPolicy));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ById,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(0, result.Summary.MatchingPolicies);
                Assert.Equal(1, result.Summary.EntraOnlyPolicies);
                Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        #region Test Case 1.7: Case sensitivity

        [Fact]
        public async Task CompareAsync_CaseInsensitive_ShouldMatch()
        {
            // Arrange
            var entraPolicy = CreateTestEntraPolicy("policy-1", "My Policy");
            var refPolicy = CreateTestEntraPolicy("policy-1", "my policy"); // Different case

            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);
            var refDirectory = CreateTestReferenceDirectory(("policy.json", refPolicy));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                // Note: Even though names match case-insensitively, the content differs so it should be marked as Different
                Assert.Equal(1, result.Summary.PoliciesWithDifferences);
                Assert.Equal(0, result.Summary.MatchingPolicies);
                Assert.Equal(0, result.Summary.EntraOnlyPolicies);
                Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);

                var comparison = result.PolicyComparisons.First();
                Assert.Equal(ComparisonStatus.Different, comparison.Status);
                Assert.NotNull(comparison.Differences);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        [Fact]
        public async Task CompareAsync_CaseSensitive_ShouldNotMatch()
        {
            // Arrange
            var entraPolicy = CreateTestEntraPolicy("policy-1", "My Policy");
            var refPolicy = CreateTestEntraPolicy("policy-1", "my policy"); // Different case

            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);
            var refDirectory = CreateTestReferenceDirectory(("policy.json", refPolicy));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = true
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(0, result.Summary.MatchingPolicies);
                Assert.Equal(1, result.Summary.EntraOnlyPolicies);
                Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        #region Test Case 1.8: Custom mapping

        [Fact]
        public async Task CompareAsync_CustomMapping_ShouldMatchCorrectly()
        {
            // Arrange
            var entraPolicy = CreateTestEntraPolicy("entra-id", "Entra Policy Name");
            var refPolicy = CreateTestEntraPolicy("ref-id", "Reference Policy Name");

            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);
            var refDirectory = CreateTestReferenceDirectory(("ref-policy.json", refPolicy));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.CustomMapping,
                CustomMappings = new Dictionary<string, string>
                {
                    ["entra-id"] = "ref-policy.json"
                }
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(0, result.Summary.MatchingPolicies);
                Assert.Equal(1, result.Summary.PoliciesWithDifferences);

                var comparison = result.PolicyComparisons.First();
                Assert.Equal(ComparisonStatus.Different, comparison.Status);
                Assert.Equal("ref-policy.json", comparison.ReferenceFileName);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        [Fact]
        public async Task CompareAsync_CustomMapping_NonExistentReference_ShouldReportEntraOnly()
        {
            // Arrange
            var entraPolicy = CreateTestEntraPolicy("entra-id", "Entra Policy Name");

            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);
            var refDirectory = CreateTestReferenceDirectory(); // Empty

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.CustomMapping,
                CustomMappings = new Dictionary<string, string>
                {
                    ["entra-id"] = "non-existent.json"
                }
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(1, result.Summary.EntraOnlyPolicies);
                Assert.Equal(0, result.Summary.MatchingPolicies);

                var comparison = result.PolicyComparisons.First();
                Assert.Equal(ComparisonStatus.EntraOnly, comparison.Status);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        #region Test Case 1.9 & 1.10 & 1.11: Empty scenarios

        [Fact]
        public async Task CompareAsync_EmptyEntraExport_ShouldReportAllReferenceOnly()
        {
            // Arrange
            var refPolicy = CreateTestEntraPolicy("ref-1", "Reference Policy");
            var entraExport = CreateTestEntraExport("tenant-123"); // No policies

            var refDirectory = CreateTestReferenceDirectory(("policy.json", refPolicy));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(0, result.Summary.TotalEntraPolicies);
                Assert.Equal(1, result.Summary.TotalReferencePolicies);
                Assert.Equal(0, result.Summary.MatchingPolicies);
                Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);

                var comparison = result.PolicyComparisons.First();
                Assert.Equal(ComparisonStatus.ReferenceOnly, comparison.Status);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        [Fact]
        public async Task CompareAsync_EmptyReferenceDirectory_ShouldReportAllEntraOnly()
        {
            // Arrange
            var entraPolicy = CreateTestEntraPolicy("entra-1", "Entra Policy");
            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);

            var refDirectory = CreateTestReferenceDirectory(); // Empty

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(1, result.Summary.TotalEntraPolicies);
                Assert.Equal(0, result.Summary.TotalReferencePolicies);
                Assert.Equal(0, result.Summary.MatchingPolicies);
                Assert.Equal(1, result.Summary.EntraOnlyPolicies);

                var comparison = result.PolicyComparisons.First();
                Assert.Equal(ComparisonStatus.EntraOnly, comparison.Status);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        [Fact]
        public async Task CompareAsync_BothEmpty_ShouldReturnEmptyResults()
        {
            // Arrange
            var entraExport = CreateTestEntraExport("tenant-123"); // No policies
            var refDirectory = CreateTestReferenceDirectory(); // Empty

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(0, result.Summary.TotalEntraPolicies);
                Assert.Equal(0, result.Summary.TotalReferencePolicies);
                Assert.Equal(0, result.Summary.MatchingPolicies);
                Assert.Equal(0, result.Summary.PoliciesWithDifferences);
                Assert.Equal(0, result.Summary.EntraOnlyPolicies);
                Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);
                Assert.Empty(result.PolicyComparisons);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        #region Test Case 1.12: Malformed JSON handling

        [Fact]
        public async Task CompareAsync_MalformedJsonFiles_ShouldHandleGracefully()
        {
            // Arrange
            var entraPolicy = CreateTestEntraPolicy("entra-1", "Entra Policy");
            var validRefPolicy = CreateTestEntraPolicy("ref-1", "Valid Reference Policy");
            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);

            var refDirectory = Path.Combine(_tempDirectory, $"TestRef_{Guid.NewGuid():N}");
            Directory.CreateDirectory(refDirectory);

            // Create a valid JSON file
            File.WriteAllText(Path.Combine(refDirectory, "valid.json"), validRefPolicy.ToString());

            // Create a malformed JSON file
            File.WriteAllText(Path.Combine(refDirectory, "malformed.json"), "{ invalid json content");

            // Create a non-JSON file
            File.WriteAllText(Path.Combine(refDirectory, "not-json.txt"), "This is not JSON");

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(1, result.Summary.TotalEntraPolicies);
                Assert.Equal(1, result.Summary.TotalReferencePolicies); // Only valid JSON should be loaded
                Assert.Equal(0, result.Summary.MatchingPolicies);
                Assert.Equal(1, result.Summary.EntraOnlyPolicies);
                Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        #region Edge cases and error handling

        [Fact]
        public async Task CompareAsync_NonExistentReferenceDirectory_ShouldHandleGracefully()
        {
            // Arrange
            var entraPolicy = CreateTestEntraPolicy("entra-1", "Entra Policy");
            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);
            var nonExistentDirectory = Path.Combine(_tempDirectory, $"NonExistent_{Guid.NewGuid():N}");

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, nonExistentDirectory, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.TotalEntraPolicies);
            Assert.Equal(0, result.Summary.TotalReferencePolicies);
            Assert.Equal(1, result.Summary.EntraOnlyPolicies);
        }

        [Fact]
        public async Task CompareAsync_MalformedEntraExport_ShouldThrowException()
        {
            // Arrange
            var malformedEntraExport = "{ invalid json }";
            var refDirectory = CreateTestReferenceDirectory();

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act & Assert
                await Assert.ThrowsAsync<Newtonsoft.Json.JsonSerializationException>(
                    () => _service.CompareAsync(malformedEntraExport, refDirectory, matchingOptions));
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        [Fact]
        public async Task CompareAsync_EntraExportWithoutPoliciesArray_ShouldHandleGracefully()
        {
            // Arrange
            var entraExport = new { TenantId = "tenant-123" }; // No Policies array
            var refDirectory = CreateTestReferenceDirectory();

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(0, result.Summary.TotalEntraPolicies);
                Assert.Equal("tenant-123", result.TenantId);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        #region Complex nested structure comparison

        [Fact]
        public async Task CompareAsync_ComplexNestedDifferences_ShouldDetectCorrectly()
        {
            // Arrange
            var entraPolicy = new JObject
            {
                ["Id"] = "policy-1",
                ["DisplayName"] = "Complex Policy",
                ["State"] = "enabled",
                ["Conditions"] = new JObject
                {
                    ["Applications"] = new JObject
                    {
                        ["IncludeApplications"] = new JArray("app1", "app2"),
                        ["ExcludeApplications"] = new JArray("app3")
                    },
                    ["Users"] = new JObject
                    {
                        ["IncludeUsers"] = new JArray("user1"),
                        ["ExcludeUsers"] = new JArray("user2"),
                        ["IncludeGroups"] = new JArray("group1", "group2")
                    },
                    ["Locations"] = new JObject
                    {
                        ["IncludeLocations"] = new JArray("location1"),
                        ["ExcludeLocations"] = new JArray("location2")
                    }
                },
                ["GrantControls"] = new JObject
                {
                    ["Operator"] = "AND",
                    ["BuiltInControls"] = new JArray("mfa", "compliantDevice")
                },
                ["SessionControls"] = new JObject
                {
                    ["ApplicationEnforcedRestrictions"] = new JObject
                    {
                        ["IsEnabled"] = true
                    }
                }
            };

            var refPolicy = entraPolicy.DeepClone() as JObject;
            // Make a deep change
            refPolicy!["Conditions"]!["Users"]!["IncludeGroups"] = new JArray("group1", "group3"); // Different group

            var entraExport = CreateTestEntraExport("tenant-123", entraPolicy);
            var refDirectory = CreateTestReferenceDirectory(("policy.json", refPolicy));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            try
            {
                // Act
                var result = await _service.CompareAsync(entraExport, refDirectory, matchingOptions);

                // Assert
                Assert.Equal(1, result.Summary.PoliciesWithDifferences);
                var comparison = result.PolicyComparisons.First();
                Assert.Equal(ComparisonStatus.Different, comparison.Status);
                Assert.NotNull(comparison.Differences);
            }
            finally
            {
                CleanupDirectory(refDirectory);
            }
        }

        #endregion

        public void Dispose()
        {
            // Cleanup is handled in individual test methods
            GC.SuppressFinalize(this);
        }
    }
}
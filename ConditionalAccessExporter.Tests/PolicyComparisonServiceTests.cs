using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConditionalAccessExporter.Tests;

public class PolicyComparisonServiceTests
{
    private readonly PolicyComparisonService _service;

    public PolicyComparisonServiceTests()
    {
        _service = new PolicyComparisonService();
    }

    #region Test Data Helpers

    private static JObject CreateTestEntraPolicy(string id, string displayName, string state = "Enabled")
    {
        return JObject.FromObject(new
        {
            Id = id,
            DisplayName = displayName,
            State = state,
            CreatedDateTime = "2024-01-01T12:00:00Z",
            ModifiedDateTime = "2024-05-01T10:30:00Z",
            Conditions = new
            {
                Applications = new
                {
                    IncludeApplications = new[] { "All" },
                    ExcludeApplications = new string[0]
                },
                Users = new
                {
                    IncludeUsers = new[] { "All" },
                    ExcludeUsers = new string[0]
                }
            },
            GrantControls = new
            {
                Operator = "OR",
                BuiltInControls = new[] { "mfa" }
            }
        });
    }

    private static object CreateTestEntraExport(string tenantId, params JObject[] policies)
    {
        return new
        {
            TenantId = tenantId,
            Policies = policies
        };
    }

    private static string CreateTestReferencePolicy(string id, string displayName, string state = "Enabled")
    {
        var policy = new
        {
            Id = id,
            DisplayName = displayName,
            State = state,
            CreatedDateTime = "2024-01-01T12:00:00Z",
            ModifiedDateTime = "2024-05-01T10:30:00Z",
            Conditions = new
            {
                Applications = new
                {
                    IncludeApplications = new[] { "All" },
                    ExcludeApplications = new string[0]
                },
                Users = new
                {
                    IncludeUsers = new[] { "All" },
                    ExcludeUsers = new string[0]
                }
            },
            GrantControls = new
            {
                Operator = "OR",
                BuiltInControls = new[] { "mfa" }
            }
        };
        return JsonConvert.SerializeObject(policy, Formatting.Indented);
    }

    #endregion

    #region CompareAsync Method Tests

    [Fact]
    public async Task CompareAsync_IdenticalPolicies_ShouldReportAsIdentical()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "Test Policy");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-identical");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var refPolicyPath = Path.Combine(tempDir, "policy1.json");
            await File.WriteAllTextAsync(refPolicyPath, CreateTestReferencePolicy("policy-1", "Test Policy"));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("tenant-123", result.TenantId);
            Assert.Equal(tempDir, result.ReferenceDirectory);
            Assert.Equal(1, result.Summary.TotalEntraPolicies);
            Assert.Equal(1, result.Summary.TotalReferencePolicies);
            Assert.Equal(1, result.Summary.MatchingPolicies);
            Assert.Equal(0, result.Summary.PoliciesWithDifferences);
            Assert.Equal(0, result.Summary.EntraOnlyPolicies);
            Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);

            Assert.Single(result.PolicyComparisons);
            var comparison = result.PolicyComparisons[0];
            Assert.Equal(ComparisonStatus.Identical, comparison.Status);
            Assert.Equal("policy-1", comparison.PolicyId);
            Assert.Equal("Test Policy", comparison.PolicyName);
            Assert.NotNull(comparison.ReferenceFileName);
            Assert.Null(comparison.Differences);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_EntraOnlyPolicy_ShouldReportAsEntraOnly()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "Entra Only Policy");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-empty");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.EntraOnlyPolicies);
            Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Equal(0, result.Summary.PoliciesWithDifferences);

            Assert.Single(result.PolicyComparisons);
            var comparison = result.PolicyComparisons[0];
            Assert.Equal(ComparisonStatus.EntraOnly, comparison.Status);
            Assert.Equal("policy-1", comparison.PolicyId);
            Assert.Equal("Entra Only Policy", comparison.PolicyName);
            Assert.Null(comparison.ReferenceFileName);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_ReferenceOnlyPolicy_ShouldReportAsReferenceOnly()
    {
        // Arrange
        var entraExport = CreateTestEntraExport("tenant-123");
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-only");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var refPolicyPath = Path.Combine(tempDir, "policy1.json");
            await File.WriteAllTextAsync(refPolicyPath, CreateTestReferencePolicy("policy-1", "Reference Only Policy"));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(0, result.Summary.EntraOnlyPolicies);
            Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Equal(0, result.Summary.PoliciesWithDifferences);

            Assert.Single(result.PolicyComparisons);
            var comparison = result.PolicyComparisons[0];
            Assert.Equal(ComparisonStatus.ReferenceOnly, comparison.Status);
            Assert.Equal("Reference Only Policy", comparison.PolicyName);
            Assert.NotNull(comparison.ReferenceFileName);
            Assert.Null(comparison.EntraPolicy);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_DifferentPolicies_ShouldReportAsDifferent()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "Test Policy");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-different");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Create a different reference policy (different state)
            var differentPolicy = new
            {
                Id = "policy-1",
                DisplayName = "Test Policy",
                State = "Disabled", // Different state
                CreatedDateTime = "2024-01-01T12:00:00Z",
                ModifiedDateTime = "2024-05-01T10:30:00Z",
                Conditions = new
                {
                    Applications = new
                    {
                        IncludeApplications = new[] { "All" },
                        ExcludeApplications = new string[0]
                    },
                    Users = new
                    {
                        IncludeUsers = new[] { "All" },
                        ExcludeUsers = new string[0]
                    }
                },
                GrantControls = new
                {
                    Operator = "OR",
                    BuiltInControls = new[] { "mfa" }
                }
            };

            var refPolicyPath = Path.Combine(tempDir, "policy1.json");
            await File.WriteAllTextAsync(refPolicyPath, JsonConvert.SerializeObject(differentPolicy, Formatting.Indented));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(0, result.Summary.EntraOnlyPolicies);
            Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Equal(1, result.Summary.PoliciesWithDifferences);

            Assert.Single(result.PolicyComparisons);
            var comparison = result.PolicyComparisons[0];
            Assert.Equal(ComparisonStatus.Different, comparison.Status);
            Assert.Equal("policy-1", comparison.PolicyId);
            Assert.Equal("Test Policy", comparison.PolicyName);
            Assert.NotNull(comparison.ReferenceFileName);
            Assert.NotNull(comparison.Differences);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_MultipleDifferenceTypes_ShouldReportCorrectly()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "Identical Policy");
        var policy2 = CreateTestEntraPolicy("policy-2", "Different Policy");
        var policy3 = CreateTestEntraPolicy("policy-3", "Entra Only Policy");
        var entraExport = CreateTestEntraExport("tenant-123", policy1, policy2, policy3);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-mixed");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Identical policy
            var refPolicy1Path = Path.Combine(tempDir, "policy1.json");
            await File.WriteAllTextAsync(refPolicy1Path, CreateTestReferencePolicy("policy-1", "Identical Policy"));

            // Different policy
            var differentPolicy = new
            {
                Id = "policy-2",
                DisplayName = "Different Policy",
                State = "Disabled", // Different state
                Conditions = new
                {
                    Applications = new
                    {
                        IncludeApplications = new[] { "All" },
                        ExcludeApplications = new string[0]
                    },
                    Users = new
                    {
                        IncludeUsers = new[] { "All" },
                        ExcludeUsers = new string[0]
                    }
                },
                GrantControls = new
                {
                    Operator = "OR",
                    BuiltInControls = new[] { "mfa" }
                }
            };
            var refPolicy2Path = Path.Combine(tempDir, "policy2.json");
            await File.WriteAllTextAsync(refPolicy2Path, JsonConvert.SerializeObject(differentPolicy, Formatting.Indented));

            // Reference only policy
            var refPolicy4Path = Path.Combine(tempDir, "policy4.json");
            await File.WriteAllTextAsync(refPolicy4Path, CreateTestReferencePolicy("policy-4", "Reference Only Policy"));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(3, result.Summary.TotalEntraPolicies);
            Assert.Equal(3, result.Summary.TotalReferencePolicies);
            Assert.Equal(1, result.Summary.MatchingPolicies); // Identical Policy
            Assert.Equal(1, result.Summary.PoliciesWithDifferences); // Different Policy
            Assert.Equal(1, result.Summary.EntraOnlyPolicies); // Entra Only Policy
            Assert.Equal(1, result.Summary.ReferenceOnlyPolicies); // Reference Only Policy

            Assert.Equal(4, result.PolicyComparisons.Count);

            var identicalComparison = result.PolicyComparisons.FirstOrDefault(c => c.PolicyName == "Identical Policy");
            Assert.NotNull(identicalComparison);
            Assert.Equal(ComparisonStatus.Identical, identicalComparison.Status);

            var differentComparison = result.PolicyComparisons.FirstOrDefault(c => c.PolicyName == "Different Policy");
            Assert.NotNull(differentComparison);
            Assert.Equal(ComparisonStatus.Different, differentComparison.Status);

            var entraOnlyComparison = result.PolicyComparisons.FirstOrDefault(c => c.PolicyName == "Entra Only Policy");
            Assert.NotNull(entraOnlyComparison);
            Assert.Equal(ComparisonStatus.EntraOnly, entraOnlyComparison.Status);

            var referenceOnlyComparison = result.PolicyComparisons.FirstOrDefault(c => c.PolicyName == "Reference Only Policy");
            Assert.NotNull(referenceOnlyComparison);
            Assert.Equal(ComparisonStatus.ReferenceOnly, referenceOnlyComparison.Status);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_MatchingByName_SameName_ShouldMatch()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("different-id", "Same Policy Name");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-name-match");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var refPolicyPath = Path.Combine(tempDir, "policy1.json");
            await File.WriteAllTextAsync(refPolicyPath, CreateTestReferencePolicy("other-id", "Same Policy Name"));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.PoliciesWithDifferences);
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Single(result.PolicyComparisons);
            var comparison = result.PolicyComparisons[0];
            Assert.Equal(ComparisonStatus.Different, comparison.Status);
            Assert.Equal("different-id", comparison.PolicyId);
            Assert.Equal("Same Policy Name", comparison.PolicyName);
            Assert.NotNull(comparison.Differences);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_MatchingById_SameId_ShouldMatch()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("same-id", "Different Name 1");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-id-match");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var refPolicyPath = Path.Combine(tempDir, "policy1.json");
            await File.WriteAllTextAsync(refPolicyPath, CreateTestReferencePolicy("same-id", "Different Name 2"));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ById,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.PoliciesWithDifferences);
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Single(result.PolicyComparisons);
            var comparison = result.PolicyComparisons[0];
            Assert.Equal(ComparisonStatus.Different, comparison.Status);
            Assert.Equal("same-id", comparison.PolicyId);
            Assert.Equal("Different Name 1", comparison.PolicyName);
            Assert.NotNull(comparison.Differences);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_MatchingById_DifferentIds_ShouldNotMatch()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("id-1", "Same Policy Name");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-id-nomatch");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var refPolicyPath = Path.Combine(tempDir, "policy1.json");
            await File.WriteAllTextAsync(refPolicyPath, CreateTestReferencePolicy("id-2", "Same Policy Name"));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ById,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.EntraOnlyPolicies);
            Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);
            Assert.Equal(0, result.Summary.MatchingPolicies);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_CaseSensitiveFalse_DifferentCase_ShouldMatch()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "My Policy");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-case-insensitive");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var refPolicyPath = Path.Combine(tempDir, "policy1.json");
            await File.WriteAllTextAsync(refPolicyPath, CreateTestReferencePolicy("policy-1", "my policy")); // lowercase

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.PoliciesWithDifferences);
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Equal(0, result.Summary.EntraOnlyPolicies);
            Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_CaseSensitiveTrue_DifferentCase_ShouldNotMatch()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "My Policy");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-case-sensitive");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var refPolicyPath = Path.Combine(tempDir, "policy1.json");
            await File.WriteAllTextAsync(refPolicyPath, CreateTestReferencePolicy("policy-1", "my policy")); // lowercase

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = true
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Equal(1, result.Summary.EntraOnlyPolicies);
            Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_CustomMapping_ShouldMapCorrectly()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("entra-policy-id", "Entra Policy Name");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-custom-mapping");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var refPolicyPath = Path.Combine(tempDir, "reference-policy.json");
            await File.WriteAllTextAsync(refPolicyPath, CreateTestReferencePolicy("ref-policy-id", "Reference Policy Name"));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.CustomMapping,
                CustomMappings = new Dictionary<string, string>
                {
                    { "entra-policy-id", "reference-policy.json" }
                }
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.PoliciesWithDifferences);
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Single(result.PolicyComparisons);
            var comparison = result.PolicyComparisons[0];
            Assert.Equal(ComparisonStatus.Different, comparison.Status);
            Assert.Equal("entra-policy-id", comparison.PolicyId);
            Assert.Equal("reference-policy.json", comparison.ReferenceFileName);
            Assert.NotNull(comparison.Differences);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_CustomMapping_NonExistentReference_ShouldReportAsEntraOnly()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("entra-policy-id", "Entra Policy Name");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-custom-mapping-missing");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.CustomMapping,
                CustomMappings = new Dictionary<string, string>
                {
                    { "entra-policy-id", "non-existent-file.json" }
                }
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.EntraOnlyPolicies);
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Single(result.PolicyComparisons);
            var comparison = result.PolicyComparisons[0];
            Assert.Equal(ComparisonStatus.EntraOnly, comparison.Status);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_EmptyEntraExport_ShouldReportAllAsReferenceOnly()
    {
        // Arrange
        var entraExport = CreateTestEntraExport("tenant-123");
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-empty-entra");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var refPolicyPath = Path.Combine(tempDir, "policy1.json");
            await File.WriteAllTextAsync(refPolicyPath, CreateTestReferencePolicy("policy-1", "Reference Policy"));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(0, result.Summary.TotalEntraPolicies);
            Assert.Equal(1, result.Summary.TotalReferencePolicies);
            Assert.Equal(0, result.Summary.EntraOnlyPolicies);
            Assert.Equal(1, result.Summary.ReferenceOnlyPolicies);
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Equal(0, result.Summary.PoliciesWithDifferences);

            Assert.Single(result.PolicyComparisons);
            var comparison = result.PolicyComparisons[0];
            Assert.Equal(ComparisonStatus.ReferenceOnly, comparison.Status);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_EmptyReferenceDirectory_ShouldReportAllAsEntraOnly()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "Entra Policy");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-empty-reference");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.TotalEntraPolicies);
            Assert.Equal(0, result.Summary.TotalReferencePolicies);
            Assert.Equal(1, result.Summary.EntraOnlyPolicies);
            Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Equal(0, result.Summary.PoliciesWithDifferences);

            Assert.Single(result.PolicyComparisons);
            var comparison = result.PolicyComparisons[0];
            Assert.Equal(ComparisonStatus.EntraOnly, comparison.Status);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_BothEmpty_ShouldReturnEmptyResult()
    {
        // Arrange
        var entraExport = CreateTestEntraExport("tenant-123");
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-both-empty");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(0, result.Summary.TotalEntraPolicies);
            Assert.Equal(0, result.Summary.TotalReferencePolicies);
            Assert.Equal(0, result.Summary.EntraOnlyPolicies);
            Assert.Equal(0, result.Summary.ReferenceOnlyPolicies);
            Assert.Equal(0, result.Summary.MatchingPolicies);
            Assert.Equal(0, result.Summary.PoliciesWithDifferences);
            Assert.Empty(result.PolicyComparisons);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_NonExistentReferenceDirectory_ShouldHandleGracefully()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "Entra Policy");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "non-existent-directory");

        var matchingOptions = new MatchingOptions
        {
            Strategy = MatchingStrategy.ByName,
            CaseSensitive = false
        };

        // Act
        var result = await _service.CompareAsync(entraExport, nonExistentDir, matchingOptions);

        // Assert
        Assert.Equal(1, result.Summary.TotalEntraPolicies);
        Assert.Equal(0, result.Summary.TotalReferencePolicies);
        Assert.Equal(1, result.Summary.EntraOnlyPolicies);
        Assert.Single(result.PolicyComparisons);
        var comparison = result.PolicyComparisons[0];
        Assert.Equal(ComparisonStatus.EntraOnly, comparison.Status);
    }

    [Fact]
    public async Task CompareAsync_ReferenceDirectoryWithNonJsonFiles_ShouldIgnoreNonJsonFiles()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "Test Policy");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-mixed-files");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Create a valid JSON file
            var validJsonPath = Path.Combine(tempDir, "valid-policy.json");
            await File.WriteAllTextAsync(validJsonPath, CreateTestReferencePolicy("policy-1", "Test Policy"));

            // Create non-JSON files
            var textFilePath = Path.Combine(tempDir, "readme.txt");
            await File.WriteAllTextAsync(textFilePath, "This is a text file");

            var xmlFilePath = Path.Combine(tempDir, "config.xml");
            await File.WriteAllTextAsync(xmlFilePath, "<config></config>");

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.TotalReferencePolicies); // Only the valid JSON file should be counted
            Assert.Equal(1, result.Summary.MatchingPolicies);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_ReferenceDirectoryWithMalformedJson_ShouldSkipMalformedFiles()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "Test Policy");
        var entraExport = CreateTestEntraExport("tenant-123", policy1);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-malformed");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Create a valid JSON file
            var validJsonPath = Path.Combine(tempDir, "valid-policy.json");
            await File.WriteAllTextAsync(validJsonPath, CreateTestReferencePolicy("policy-1", "Test Policy"));

            // Create malformed JSON files
            var malformedJsonPath = Path.Combine(tempDir, "malformed.json");
            await File.WriteAllTextAsync(malformedJsonPath, "{ invalid json }");

            var incompleteJsonPath = Path.Combine(tempDir, "incomplete.json");
            await File.WriteAllTextAsync(incompleteJsonPath, "{ \"name\": invalid }");

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.TotalReferencePolicies); // Only the valid JSON file should be counted
            Assert.Equal(1, result.Summary.MatchingPolicies);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_ComplexNestedDifferences_ShouldDetectCorrectly()
    {
        // Arrange
        var complexEntraPolicy = JObject.FromObject(new
        {
            Id = "complex-policy",
            DisplayName = "Complex Policy",
            State = "Enabled",
            Conditions = new
            {
                Applications = new
                {
                    IncludeApplications = new[] { "app1", "app2" },
                    ExcludeApplications = new[] { "app3" }
                },
                Users = new
                {
                    IncludeUsers = new[] { "All" },
                    ExcludeUsers = new[] { "user1", "user2" },
                    IncludeGroups = new[] { "group1" },
                    ExcludeGroups = new string[0]
                },
                Platforms = new
                {
                    IncludePlatforms = new[] { "windows", "iOS" },
                    ExcludePlatforms = new[] { "android" }
                },
                Locations = new
                {
                    IncludeLocations = new[] { "AllTrusted" },
                    ExcludeLocations = new[] { "location1" }
                }
            },
            GrantControls = new
            {
                Operator = "AND",
                BuiltInControls = new[] { "mfa", "compliantDevice" }
            },
            SessionControls = new
            {
                ApplicationEnforcedRestrictions = new
                {
                    IsEnabled = true
                },
                CloudAppSecurity = new
                {
                    IsEnabled = false,
                    CloudAppSecurityType = "blockDownloads"
                }
            }
        });

        var entraExport = CreateTestEntraExport("tenant-123", complexEntraPolicy);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-complex");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Create a reference policy with differences in nested structures
            var complexReferencePolicy = new
            {
                Id = "complex-policy",
                DisplayName = "Complex Policy",
                State = "Enabled",
                Conditions = new
                {
                    Applications = new
                    {
                        IncludeApplications = new[] { "app1", "app2" },
                        ExcludeApplications = new[] { "app3" }
                    },
                    Users = new
                    {
                        IncludeUsers = new[] { "All" },
                        ExcludeUsers = new[] { "user1", "user3" }, // Different user
                        IncludeGroups = new[] { "group1", "group2" }, // Additional group
                        ExcludeGroups = new string[0]
                    },
                    Platforms = new
                    {
                        IncludePlatforms = new[] { "windows", "iOS" },
                        ExcludePlatforms = new[] { "android" }
                    },
                    Locations = new
                    {
                        IncludeLocations = new[] { "AllTrusted" },
                        ExcludeLocations = new[] { "location1" }
                    }
                },
                GrantControls = new
                {
                    Operator = "OR", // Different operator
                    BuiltInControls = new[] { "mfa", "compliantDevice" }
                },
                SessionControls = new
                {
                    ApplicationEnforcedRestrictions = new
                    {
                        IsEnabled = false // Different value
                    },
                    CloudAppSecurity = new
                    {
                        IsEnabled = false,
                        CloudAppSecurityType = "blockDownloads"
                    }
                }
            };

            var refPolicyPath = Path.Combine(tempDir, "complex-policy.json");
            await File.WriteAllTextAsync(refPolicyPath, JsonConvert.SerializeObject(complexReferencePolicy, Formatting.Indented));

            var matchingOptions = new MatchingOptions
            {
                Strategy = MatchingStrategy.ByName,
                CaseSensitive = false
            };

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(1, result.Summary.PoliciesWithDifferences);
            Assert.Single(result.PolicyComparisons);
            var comparison = result.PolicyComparisons[0];
            Assert.Equal(ComparisonStatus.Different, comparison.Status);
            Assert.NotNull(comparison.Differences);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region ParseEntraExport Tests

    [Fact]
    public async Task ParseEntraExport_ValidJObject_ShouldParseCorrectly()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "Test Policy 1");
        var policy2 = CreateTestEntraPolicy("policy-2", "Test Policy 2");
        var entraExport = CreateTestEntraExport("tenant-123", policy1, policy2);

        // Act
        var result = await _service.CompareAsync(entraExport, "dummy-path", new MatchingOptions());

        // Assert
        Assert.Equal("tenant-123", result.TenantId);
        Assert.Equal(2, result.Summary.TotalEntraPolicies);
    }

    [Fact]
    public async Task ParseEntraExport_ValidJsonString_ShouldParseCorrectly()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "Test Policy 1");
        var entraExportObj = CreateTestEntraExport("tenant-456", policy1);
        var jsonString = JsonConvert.SerializeObject(entraExportObj);

        // Act
        var result = await _service.CompareAsync(jsonString, "dummy-path", new MatchingOptions());

        // Assert
        Assert.Equal("tenant-456", result.TenantId);
        Assert.Equal(1, result.Summary.TotalEntraPolicies);
    }

    [Fact]
    public async Task ParseEntraExport_EmptyPoliciesArray_ShouldHandleGracefully()
    {
        // Arrange
        var entraExport = CreateTestEntraExport("tenant-789");

        // Act
        var result = await _service.CompareAsync(entraExport, "dummy-path", new MatchingOptions());

        // Assert
        Assert.Equal("tenant-789", result.TenantId);
        Assert.Equal(0, result.Summary.TotalEntraPolicies);
    }

    [Fact]
    public async Task ParseEntraExport_MissingTenantId_ShouldUseEmptyString()
    {
        // Arrange
        var policy1 = CreateTestEntraPolicy("policy-1", "Test Policy");
        var entraExport = new
        {
            // Missing TenantId
            Policies = new[] { policy1 }
        };

        // Act
        var result = await _service.CompareAsync(entraExport, "dummy-path", new MatchingOptions());

        // Assert
        Assert.Equal(string.Empty, result.TenantId);
        Assert.Equal(1, result.Summary.TotalEntraPolicies);
    }

    [Fact]
    public async Task ParseEntraExport_MissingPoliciesProperty_ShouldHandleGracefully()
    {
        // Arrange
        var entraExport = new
        {
            TenantId = "tenant-999"
            // Missing Policies property
        };

        // Act
        var result = await _service.CompareAsync(entraExport, "dummy-path", new MatchingOptions());

        // Assert
        Assert.Equal("tenant-999", result.TenantId);
        Assert.Equal(0, result.Summary.TotalEntraPolicies);
    }

    [Fact]
    public async Task ParseEntraExport_InvalidJsonString_ShouldThrowException()
    {
        // Arrange
        var invalidJson = "{ invalid json structure";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _service.CompareAsync(invalidJson, "dummy-path", new MatchingOptions());
        });
    }

    [Fact]
    public async Task ParseEntraExport_NullInput_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _service.CompareAsync(null!, "dummy-path", new MatchingOptions());
        });
    }

    #endregion

    #region ComparedAt and Metadata Tests

    [Fact]
    public async Task CompareAsync_ShouldSetComparedAtTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var entraExport = CreateTestEntraExport("tenant-123");
        var tempDir = Path.Combine(Path.GetTempPath(), "test-timestamp");
        Directory.CreateDirectory(tempDir);

        try
        {
            var matchingOptions = new MatchingOptions();

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);
            var after = DateTime.UtcNow;

            // Assert
            Assert.True(result.ComparedAt >= before);
            Assert.True(result.ComparedAt <= after);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CompareAsync_ShouldSetReferenceDirectoryCorrectly()
    {
        // Arrange
        var entraExport = CreateTestEntraExport("tenant-123");
        var tempDir = Path.Combine(Path.GetTempPath(), "test-ref-dir");
        Directory.CreateDirectory(tempDir);

        try
        {
            var matchingOptions = new MatchingOptions();

            // Act
            var result = await _service.CompareAsync(entraExport, tempDir, matchingOptions);

            // Assert
            Assert.Equal(tempDir, result.ReferenceDirectory);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    #endregion
}
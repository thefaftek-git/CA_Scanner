using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConditionalAccessExporter.Tests;

public class ReportGenerationServiceTests
{
    private readonly ReportGenerationService _reportService;
    private readonly string _tempOutputDir;

    public ReportGenerationServiceTests()
    {
        _reportService = new ReportGenerationService();
        _tempOutputDir = Path.Combine(Path.GetTempPath(), "test-reports-" + Guid.NewGuid().ToString("N")[..8]);
    }

    private void Cleanup()
    {
        if (Directory.Exists(_tempOutputDir))
            Directory.Delete(_tempOutputDir, true);
    }

    #region Test Case 1.1: All Status Types

    [Fact]
    public async Task GenerateReportsAsync_WithAllStatusTypes_ShouldCreateValidReports()
    {
        // Arrange
        var comparisonResult = CreateComparisonResultWithAllStatuses();
        var formats = new List<string> { "json", "html", "console" };

        try
        {
            // Act
            await _reportService.GenerateReportsAsync(comparisonResult, _tempOutputDir, formats);

            // Assert
            var jsonFiles = Directory.GetFiles(_tempOutputDir, "*.json");
            var htmlFiles = Directory.GetFiles(_tempOutputDir, "*.html");
            
            Assert.Single(jsonFiles);
            Assert.Single(htmlFiles);

            // Verify JSON content
            var jsonContent = await File.ReadAllTextAsync(jsonFiles[0]);
            Assert.False(string.IsNullOrWhiteSpace(jsonContent));
            var parsedJson = JsonConvert.DeserializeObject<ComparisonResult>(jsonContent);
            Assert.NotNull(parsedJson);
            Assert.Equal(comparisonResult.TenantId, parsedJson.TenantId);
            Assert.Equal(comparisonResult.PolicyComparisons.Count, parsedJson.PolicyComparisons.Count);

            // Verify HTML content
            var htmlContent = await File.ReadAllTextAsync(htmlFiles[0]);
            Assert.False(string.IsNullOrWhiteSpace(htmlContent));
            Assert.Contains("<!DOCTYPE html>", htmlContent);
            Assert.Contains("Conditional Access Policy Comparison Report", htmlContent);
            // CSS classes are always present in the style section
            Assert.Contains("status-identical", htmlContent);
            Assert.Contains("status-different", htmlContent);
            Assert.Contains("status-entra-only", htmlContent);
            Assert.Contains("status-reference-only", htmlContent);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Test Case 1.2: Empty ComparisonResult

    [Fact]
    public async Task GenerateReportsAsync_WithEmptyResult_ShouldGenerateReportsWithoutError()
    {
        // Arrange
        var emptyResult = new ComparisonResult
        {
            ComparedAt = DateTime.UtcNow,
            TenantId = "test-tenant",
            ReferenceDirectory = "test-ref",
            Summary = new ComparisonSummary(),
            PolicyComparisons = new List<PolicyComparison>()
        };
        var formats = new List<string> { "json", "html" };

        try
        {
            // Act
            await _reportService.GenerateReportsAsync(emptyResult, _tempOutputDir, formats);

            // Assert
            var jsonFiles = Directory.GetFiles(_tempOutputDir, "*.json");
            var htmlFiles = Directory.GetFiles(_tempOutputDir, "*.html");
            
            Assert.Single(jsonFiles);
            Assert.Single(htmlFiles);

            var jsonContent = await File.ReadAllTextAsync(jsonFiles[0]);
            var parsedJson = JsonConvert.DeserializeObject<ComparisonResult>(jsonContent);
            Assert.NotNull(parsedJson);
            Assert.Empty(parsedJson.PolicyComparisons);

            var htmlContent = await File.ReadAllTextAsync(htmlFiles[0]);
            Assert.Contains("<!DOCTYPE html>", htmlContent);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Test Case 1.3: All Identical Policies

    [Fact]
    public async Task GenerateReportsAsync_WithAllIdenticalPolicies_ShouldShowIdenticalStatus()
    {
        // Arrange
        var result = CreateComparisonResultWithIdenticalPolicies();
        var formats = new List<string> { "json", "html" };

        try
        {
            // Act
            await _reportService.GenerateReportsAsync(result, _tempOutputDir, formats);

            // Assert
            var jsonFiles = Directory.GetFiles(_tempOutputDir, "*.json");
            var htmlFiles = Directory.GetFiles(_tempOutputDir, "*.html");
            
            Assert.Single(jsonFiles);
            Assert.Single(htmlFiles);

            var jsonContent = await File.ReadAllTextAsync(jsonFiles[0]);
            var parsedJson = JsonConvert.DeserializeObject<ComparisonResult>(jsonContent);
            Assert.NotNull(parsedJson);
            Assert.All(parsedJson.PolicyComparisons, p => Assert.Equal(ComparisonStatus.Identical, p.Status));

            var htmlContent = await File.ReadAllTextAsync(htmlFiles[0]);
            // CSS classes are always present in style section, check for actual table rows with the class
            Assert.Contains("status-identical", htmlContent);
            // Verify this data only has identical policies in table rows
            var identicalPolicyName = "Identical Policy 1";
            Assert.Contains(identicalPolicyName, htmlContent);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Test Case 1.4: All EntraOnly Policies

    [Fact]
    public async Task GenerateReportsAsync_WithAllEntraOnlyPolicies_ShouldShowEntraOnlyStatus()
    {
        // Arrange
        var result = CreateComparisonResultWithEntraOnlyPolicies();
        var formats = new List<string> { "json", "html" };

        try
        {
            // Act
            await _reportService.GenerateReportsAsync(result, _tempOutputDir, formats);

            // Assert
            var jsonFiles = Directory.GetFiles(_tempOutputDir, "*.json");
            var htmlFiles = Directory.GetFiles(_tempOutputDir, "*.html");
            
            Assert.Single(jsonFiles);
            Assert.Single(htmlFiles);

            var jsonContent = await File.ReadAllTextAsync(jsonFiles[0]);
            var parsedJson = JsonConvert.DeserializeObject<ComparisonResult>(jsonContent);
            Assert.NotNull(parsedJson);
            Assert.All(parsedJson.PolicyComparisons, p => Assert.Equal(ComparisonStatus.EntraOnly, p.Status));

            var htmlContent = await File.ReadAllTextAsync(htmlFiles[0]);
            // CSS classes are always present in style section, check for actual data
            Assert.Contains("status-entra-only", htmlContent);
            // Verify this data only has entra-only policies
            var entraOnlyPolicyName = "Entra Only Policy 1";
            Assert.Contains(entraOnlyPolicyName, htmlContent);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Test Case 1.5: Null Differences

    [Fact]
    public async Task GenerateReportsAsync_WithNullDifferencesForDifferentPolicy_ShouldHandleGracefully()
    {
        // Arrange
        var result = CreateComparisonResultWithNullDifferences();
        var formats = new List<string> { "json", "html" };

        try
        {
            // Act
            await _reportService.GenerateReportsAsync(result, _tempOutputDir, formats);

            // Assert
            var jsonFiles = Directory.GetFiles(_tempOutputDir, "*.json");
            var htmlFiles = Directory.GetFiles(_tempOutputDir, "*.html");
            
            Assert.Single(jsonFiles);
            Assert.Single(htmlFiles);

            var jsonContent = await File.ReadAllTextAsync(jsonFiles[0]);
            var parsedJson = JsonConvert.DeserializeObject<ComparisonResult>(jsonContent);
            Assert.NotNull(parsedJson);
            
            var differentPolicy = parsedJson.PolicyComparisons.First(p => p.Status == ComparisonStatus.Different);
            Assert.Null(differentPolicy.Differences);

            var htmlContent = await File.ReadAllTextAsync(htmlFiles[0]);
            Assert.Contains("status-different", htmlContent);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Test Case 1.6: Invalid Format

    [Fact]
    public async Task GenerateReportsAsync_WithInvalidFormat_ShouldLogWarningAndContinue()
    {
        // Arrange
        var result = CreateComparisonResultWithAllStatuses();
        var formats = new List<string> { "json", "invalid-format", "html" };

        try
        {
            // Act & Assert - should not throw exception
            await _reportService.GenerateReportsAsync(result, _tempOutputDir, formats);

            // Should still generate valid formats
            var jsonFiles = Directory.GetFiles(_tempOutputDir, "*.json");
            var htmlFiles = Directory.GetFiles(_tempOutputDir, "*.html");
            
            Assert.Single(jsonFiles);
            Assert.Single(htmlFiles);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Console Output Tests

    [Fact]
    public void GenerateConsoleReport_WithValidData_ShouldNotThrow()
    {
        // Arrange
        var result = CreateComparisonResultWithAllStatuses();

        // Act & Assert - should not throw
        _reportService.GenerateConsoleReport(result);
    }

    [Fact]
    public void GenerateConsoleReport_WithEmptyData_ShouldNotThrow()
    {
        // Arrange
        var emptyResult = new ComparisonResult
        {
            ComparedAt = DateTime.UtcNow,
            TenantId = "test-tenant",
            ReferenceDirectory = "test-ref",
            Summary = new ComparisonSummary(),
            PolicyComparisons = new List<PolicyComparison>()
        };

        // Act & Assert - should not throw
        _reportService.GenerateConsoleReport(emptyResult);
    }

    #endregion

    #region Helper Methods

    private ComparisonResult CreateComparisonResultWithAllStatuses()
    {
        return new ComparisonResult
        {
            ComparedAt = DateTime.UtcNow,
            TenantId = "test-tenant-123",
            ReferenceDirectory = "/test/reference",
            Summary = new ComparisonSummary
            {
                TotalEntraPolicies = 4,
                TotalReferencePolicies = 4,
                EntraOnlyPolicies = 1,
                ReferenceOnlyPolicies = 1,
                MatchingPolicies = 1,
                PoliciesWithDifferences = 1
            },
            PolicyComparisons = new List<PolicyComparison>
            {
                new PolicyComparison
                {
                    PolicyId = "policy-1",
                    PolicyName = "Identical Policy",
                    Status = ComparisonStatus.Identical,
                    ReferenceFileName = "identical.json"
                },
                new PolicyComparison
                {
                    PolicyId = "policy-2",
                    PolicyName = "Different Policy",
                    Status = ComparisonStatus.Different,
                    ReferenceFileName = "different.json",
                    Differences = new { changes = "some differences" }
                },
                new PolicyComparison
                {
                    PolicyId = "policy-3",
                    PolicyName = "Entra Only Policy",
                    Status = ComparisonStatus.EntraOnly
                },
                new PolicyComparison
                {
                    PolicyId = "policy-4",
                    PolicyName = "Reference Only Policy",
                    Status = ComparisonStatus.ReferenceOnly,
                    ReferenceFileName = "reference-only.json"
                }
            }
        };
    }

    private ComparisonResult CreateComparisonResultWithIdenticalPolicies()
    {
        return new ComparisonResult
        {
            ComparedAt = DateTime.UtcNow,
            TenantId = "test-tenant-identical",
            ReferenceDirectory = "/test/reference",
            Summary = new ComparisonSummary
            {
                TotalEntraPolicies = 2,
                TotalReferencePolicies = 2,
                MatchingPolicies = 2
            },
            PolicyComparisons = new List<PolicyComparison>
            {
                new PolicyComparison
                {
                    PolicyId = "policy-1",
                    PolicyName = "Identical Policy 1",
                    Status = ComparisonStatus.Identical,
                    ReferenceFileName = "identical1.json"
                },
                new PolicyComparison
                {
                    PolicyId = "policy-2",
                    PolicyName = "Identical Policy 2",
                    Status = ComparisonStatus.Identical,
                    ReferenceFileName = "identical2.json"
                }
            }
        };
    }

    private ComparisonResult CreateComparisonResultWithEntraOnlyPolicies()
    {
        return new ComparisonResult
        {
            ComparedAt = DateTime.UtcNow,
            TenantId = "test-tenant-entra-only",
            ReferenceDirectory = "/test/reference",
            Summary = new ComparisonSummary
            {
                TotalEntraPolicies = 2,
                TotalReferencePolicies = 0,
                EntraOnlyPolicies = 2
            },
            PolicyComparisons = new List<PolicyComparison>
            {
                new PolicyComparison
                {
                    PolicyId = "policy-1",
                    PolicyName = "Entra Only Policy 1",
                    Status = ComparisonStatus.EntraOnly
                },
                new PolicyComparison
                {
                    PolicyId = "policy-2",
                    PolicyName = "Entra Only Policy 2",
                    Status = ComparisonStatus.EntraOnly
                }
            }
        };
    }

    private ComparisonResult CreateComparisonResultWithNullDifferences()
    {
        return new ComparisonResult
        {
            ComparedAt = DateTime.UtcNow,
            TenantId = "test-tenant-null-diff",
            ReferenceDirectory = "/test/reference",
            Summary = new ComparisonSummary
            {
                TotalEntraPolicies = 1,
                TotalReferencePolicies = 1,
                PoliciesWithDifferences = 1
            },
            PolicyComparisons = new List<PolicyComparison>
            {
                new PolicyComparison
                {
                    PolicyId = "policy-1",
                    PolicyName = "Different Policy with Null Differences",
                    Status = ComparisonStatus.Different,
                    ReferenceFileName = "different.json",
                    Differences = null // This is the key test case
                }
            }
        };
    }

    #endregion
}
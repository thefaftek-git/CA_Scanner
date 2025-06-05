using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

namespace ConditionalAccessExporter.Tests
{
    [Collection("Console Output Tests")]
    public class ReportGenerationServiceTests : IDisposable
    {
        private readonly ReportGenerationService _reportService;
        private readonly string _tempDirectory;

        public ReportGenerationServiceTests()
        {
            _reportService = new ReportGenerationService();
            _tempDirectory = Path.Combine(Path.GetTempPath(), "ReportGenerationTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        #region Test Data Creation Methods

        private ComparisonResult CreateComparisonResultWithAllStatuses()
        {
            return new ComparisonResult
            {
                ComparedAt = new DateTime(2024, 5, 30, 12, 0, 0, DateTimeKind.Utc),
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
                        PolicyId = "identical-policy-123",
                        PolicyName = "Identical Policy",
                        Status = ComparisonStatus.Identical,
                        ReferenceFileName = "identical-policy.json"
                    },
                    new PolicyComparison
                    {
                        PolicyId = "different-policy-456",
                        PolicyName = "Different Policy",
                        Status = ComparisonStatus.Different,
                        ReferenceFileName = "different-policy.json",
                        Differences = new { path = "/conditions/users", type = "modified" }
                    },
                    new PolicyComparison
                    {
                        PolicyId = "entra-only-789",
                        PolicyName = "Entra Only Policy",
                        Status = ComparisonStatus.EntraOnly
                    },
                    new PolicyComparison
                    {
                        PolicyId = "reference-only-101",
                        PolicyName = "Reference Only Policy",
                        Status = ComparisonStatus.ReferenceOnly,
                        ReferenceFileName = "reference-only.json"
                    }
                }
            };
        }

        private ComparisonResult CreateEmptyComparisonResult()
        {
            return new ComparisonResult
            {
                ComparedAt = DateTime.UtcNow,
                TenantId = "empty-tenant",
                ReferenceDirectory = "/empty/reference",
                Summary = new ComparisonSummary(),
                PolicyComparisons = new List<PolicyComparison>()
            };
        }

        private ComparisonResult CreateAllIdenticalComparisonResult()
        {
            return new ComparisonResult
            {
                ComparedAt = DateTime.UtcNow,
                TenantId = "identical-tenant",
                ReferenceDirectory = "/identical/reference",
                Summary = new ComparisonSummary
                {
                    TotalEntraPolicies = 3,
                    TotalReferencePolicies = 3,
                    MatchingPolicies = 3
                },
                PolicyComparisons = new List<PolicyComparison>
                {
                    new PolicyComparison
                    {
                        PolicyId = "policy-1",
                        PolicyName = "Policy One",
                        Status = ComparisonStatus.Identical,
                        ReferenceFileName = "policy-1.json"
                    },
                    new PolicyComparison
                    {
                        PolicyId = "policy-2",
                        PolicyName = "Policy Two",
                        Status = ComparisonStatus.Identical,
                        ReferenceFileName = "policy-2.json"
                    },
                    new PolicyComparison
                    {
                        PolicyId = "policy-3",
                        PolicyName = "Policy Three",
                        Status = ComparisonStatus.Identical,
                        ReferenceFileName = "policy-3.json"
                    }
                }
            };
        }

        private ComparisonResult CreateAllEntraOnlyComparisonResult()
        {
            return new ComparisonResult
            {
                ComparedAt = DateTime.UtcNow,
                TenantId = "entra-only-tenant",
                ReferenceDirectory = "/entra-only/reference",
                Summary = new ComparisonSummary
                {
                    TotalEntraPolicies = 2,
                    EntraOnlyPolicies = 2
                },
                PolicyComparisons = new List<PolicyComparison>
                {
                    new PolicyComparison
                    {
                        PolicyId = "entra-policy-1",
                        PolicyName = "Entra Policy One",
                        Status = ComparisonStatus.EntraOnly
                    },
                    new PolicyComparison
                    {
                        PolicyId = "entra-policy-2",
                        PolicyName = "Entra Policy Two",
                        Status = ComparisonStatus.EntraOnly
                    }
                }
            };
        }

        private ComparisonResult CreateDifferentWithNullDifferences()
        {
            return new ComparisonResult
            {
                ComparedAt = DateTime.UtcNow,
                TenantId = "null-diff-tenant",
                ReferenceDirectory = "/null-diff/reference",
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
                        PolicyId = "null-diff-policy",
                        PolicyName = "Policy with Null Differences",
                        Status = ComparisonStatus.Different,
                        ReferenceFileName = "null-diff-policy.json",
                        Differences = null
                    }
                }
            };
        }

        #endregion

        #region JSON Report Tests

        [Fact]
        public async Task GenerateJsonReport_WithAllStatusTypes_ShouldCreateValidJsonFile()
        {
            // Arrange
            var result = CreateComparisonResultWithAllStatuses();
            var formats = new List<string> { "json" };

            // Act
            await _reportService.GenerateReportsAsync(result, _tempDirectory, formats);

            // Assert
            var jsonFiles = Directory.GetFiles(_tempDirectory, "*.json");
            Assert.Single(jsonFiles);

            var jsonContent = await File.ReadAllTextAsync(jsonFiles[0]);
            Assert.False(string.IsNullOrEmpty(jsonContent));

            // Verify it's valid JSON
            var deserializedResult = JsonConvert.DeserializeObject<ComparisonResult>(jsonContent);
            Assert.NotNull(deserializedResult);
            Assert.Equal(result.TenantId, deserializedResult.TenantId);
            Assert.Equal(result.PolicyComparisons.Count, deserializedResult.PolicyComparisons.Count);
        }

        [Fact]
        public async Task GenerateJsonReport_WithEmptyResult_ShouldCreateValidJsonFile()
        {
            // Arrange
            var result = CreateEmptyComparisonResult();
            var formats = new List<string> { "json" };

            // Act
            await _reportService.GenerateReportsAsync(result, _tempDirectory, formats);

            // Assert
            var jsonFiles = Directory.GetFiles(_tempDirectory, "*.json");
            Assert.Single(jsonFiles);

            var jsonContent = await File.ReadAllTextAsync(jsonFiles[0]);
            var deserializedResult = JsonConvert.DeserializeObject<ComparisonResult>(jsonContent);
            Assert.NotNull(deserializedResult);
            Assert.Empty(deserializedResult.PolicyComparisons);
        }

        #endregion

        #region HTML Report Tests

        [Fact]
        public async Task GenerateHtmlReport_WithAllStatusTypes_ShouldCreateValidHtmlFile()
        {
            // Arrange
            var result = CreateComparisonResultWithAllStatuses();
            var formats = new List<string> { "html" };

            // Act
            await _reportService.GenerateReportsAsync(result, _tempDirectory, formats);

            // Assert
            var htmlFiles = Directory.GetFiles(_tempDirectory, "*.html");
            Assert.Single(htmlFiles);

            var htmlContent = await File.ReadAllTextAsync(htmlFiles[0]);
            Assert.False(string.IsNullOrEmpty(htmlContent));

            // Verify HTML structure
            Assert.Contains("<!DOCTYPE html>", htmlContent);
            Assert.Contains("<html>", htmlContent);
            Assert.Contains("<title>Conditional Access Policy Comparison Report</title>", htmlContent);
            Assert.Contains("</html>", htmlContent);

            // Verify content includes summary data
            Assert.Contains(result.TenantId, htmlContent);
            Assert.Contains("Total Entra Policies", htmlContent);
            Assert.Contains("4", htmlContent); // Total policies count

            // Verify all policy statuses are represented
            Assert.Contains("Identical Policy", htmlContent);
            Assert.Contains("Different Policy", htmlContent);
            Assert.Contains("Entra Only Policy", htmlContent);
            Assert.Contains("Reference Only Policy", htmlContent);

            // Verify CSS classes are applied
            Assert.Contains("status-identical", htmlContent);
            Assert.Contains("status-different", htmlContent);
            Assert.Contains("status-entra-only", htmlContent);
            Assert.Contains("status-reference-only", htmlContent);
        }

        [Fact]
        public async Task GenerateHtmlReport_WithEmptyResult_ShouldCreateValidHtmlFile()
        {
            // Arrange
            var result = CreateEmptyComparisonResult();
            var formats = new List<string> { "html" };

            // Act
            await _reportService.GenerateReportsAsync(result, _tempDirectory, formats);

            // Assert
            var htmlFiles = Directory.GetFiles(_tempDirectory, "*.html");
            Assert.Single(htmlFiles);

            var htmlContent = await File.ReadAllTextAsync(htmlFiles[0]);
            Assert.Contains("<!DOCTYPE html>", htmlContent);
            Assert.Contains(result.TenantId, htmlContent);
        }

        #endregion

        #region CSV Report Tests

        [Fact]
        public async Task GenerateCsvReport_WithAllStatusTypes_ShouldCreateValidCsvFile()
        {
            // Arrange
            var result = CreateComparisonResultWithAllStatuses();
            var formats = new List<string> { "csv" };

            // Act
            await _reportService.GenerateReportsAsync(result, _tempDirectory, formats);

            // Assert
            var csvFiles = Directory.GetFiles(_tempDirectory, "*.csv");
            Assert.Single(csvFiles);

            var csvContent = await File.ReadAllTextAsync(csvFiles[0]);
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Verify header
            Assert.Contains("PolicyName,PolicyId,Status,ReferenceFile,HasDifferences", lines[0]);

            // Verify data rows (should have 4 policies + header)
            Assert.Equal(5, lines.Length);

            // Verify specific policy data
            Assert.Contains("Identical Policy", csvContent);
            Assert.Contains("Different Policy", csvContent);
            Assert.Contains("Entra Only Policy", csvContent);
            Assert.Contains("Reference Only Policy", csvContent);

            // Verify differences column
            Assert.Contains("True", csvContent); // Different policy should have differences
            Assert.Contains("False", csvContent); // Other policies should not
        }

        #endregion

        #region Console Report Tests

        [Fact]
        public void GenerateConsoleReport_WithAllStatusTypes_ShouldProduceStructuredOutput()
        {
            // Arrange
            var result = CreateComparisonResultWithAllStatuses();
            var originalOut = Console.Out;
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            try
            {
                // Act
                _reportService.GenerateConsoleReport(result);

                // Assert
                var output = consoleOutput.ToString();
                
                // Verify header
                Assert.Contains("CONDITIONAL ACCESS POLICY COMPARISON REPORT", output);
                Assert.Contains("Compared At:", output);
                Assert.Contains("Tenant ID: test-tenant-123", output);

                // Verify summary section
                Assert.Contains("SUMMARY:", output);
                Assert.Contains("Total Entra Policies: 4", output);
                Assert.Contains("Total Reference Policies: 4", output);
                Assert.Contains("Policies only in Entra: 1", output);
                Assert.Contains("Policies only in Reference: 1", output);
                Assert.Contains("Matching Policies: 1", output);
                Assert.Contains("Policies with Differences: 1", output);

                // Verify detailed sections
                Assert.Contains("POLICIES ONLY IN ENTRA:", output);
                Assert.Contains("Entra Only Policy", output);
                
                Assert.Contains("POLICIES ONLY IN REFERENCE:", output);
                Assert.Contains("Reference Only Policy", output);
                
                Assert.Contains("POLICIES WITH DIFFERENCES:", output);
                Assert.Contains("Different Policy", output);
                
                Assert.Contains("IDENTICAL POLICIES:", output);
                Assert.Contains("✓ Identical Policy", output);
            }
            finally
            {
                Console.SetOut(originalOut);
                consoleOutput.Dispose();
            }
        }

        [Fact]
        public void GenerateConsoleReport_WithAllIdentical_ShouldShowAllIdentical()
        {
            // Arrange
            var result = CreateAllIdenticalComparisonResult();
            var originalOut = Console.Out;
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            try
            {
                // Act
                _reportService.GenerateConsoleReport(result);

                // Assert
                var output = consoleOutput.ToString();
                
                Assert.Contains("Matching Policies: 3", output);
                Assert.Contains("IDENTICAL POLICIES:", output);
                Assert.Contains("✓ Policy One", output);
                Assert.Contains("✓ Policy Two", output);
                Assert.Contains("✓ Policy Three", output);
                
                // Should not contain other sections
                Assert.DoesNotContain("POLICIES ONLY IN ENTRA:", output);
                Assert.DoesNotContain("POLICIES ONLY IN REFERENCE:", output);
                Assert.DoesNotContain("POLICIES WITH DIFFERENCES:", output);
            }
            finally
            {
                Console.SetOut(originalOut);
                consoleOutput.Dispose();
            }
        }

        [Fact]
        public void GenerateConsoleReport_WithAllEntraOnly_ShouldShowEntraOnlySection()
        {
            // Arrange
            var result = CreateAllEntraOnlyComparisonResult();
            var originalOut = Console.Out;
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            try
            {
                // Act
                _reportService.GenerateConsoleReport(result);

                // Assert
                var output = consoleOutput.ToString();
                
                Assert.Contains("Policies only in Entra: 2", output);
                Assert.Contains("POLICIES ONLY IN ENTRA:", output);
                Assert.Contains("• Entra Policy One", output);
                Assert.Contains("• Entra Policy Two", output);
                
                // Should not contain other sections
                Assert.DoesNotContain("POLICIES ONLY IN REFERENCE:", output);
                Assert.DoesNotContain("POLICIES WITH DIFFERENCES:", output);
                Assert.DoesNotContain("IDENTICAL POLICIES:", output);
            }
            finally
            {
                Console.SetOut(originalOut);
                consoleOutput.Dispose();
            }
        }

        [Fact]
        public void GenerateConsoleReport_WithEmptyResult_ShouldShowEmptyReport()
        {
            // Arrange
            var result = CreateEmptyComparisonResult();
            var originalOut = Console.Out;
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            try
            {
                // Act
                _reportService.GenerateConsoleReport(result);

                // Assert
                var output = consoleOutput.ToString();
                
                Assert.Contains("CONDITIONAL ACCESS POLICY COMPARISON REPORT", output);
                Assert.Contains("Total Entra Policies: 0", output);
                Assert.Contains("Total Reference Policies: 0", output);
                
                // Should not contain policy sections
                Assert.DoesNotContain("POLICIES ONLY IN ENTRA:", output);
                Assert.DoesNotContain("POLICIES ONLY IN REFERENCE:", output);
                Assert.DoesNotContain("POLICIES WITH DIFFERENCES:", output);
                Assert.DoesNotContain("IDENTICAL POLICIES:", output);
            }
            finally
            {
                Console.SetOut(originalOut);
                consoleOutput.Dispose();
            }
        }

        #endregion

        #region Multiple Format Tests

        [Fact]
        public async Task GenerateReports_WithMultipleFormats_ShouldCreateAllFiles()
        {
            // Arrange
            var result = CreateComparisonResultWithAllStatuses();
            var formats = new List<string> { "json", "html", "csv" };

            // Act
            await _reportService.GenerateReportsAsync(result, _tempDirectory, formats);

            // Assert
            var jsonFiles = Directory.GetFiles(_tempDirectory, "*.json");
            var htmlFiles = Directory.GetFiles(_tempDirectory, "*.html");
            var csvFiles = Directory.GetFiles(_tempDirectory, "*.csv");

            Assert.Single(jsonFiles);
            Assert.Single(htmlFiles);
            Assert.Single(csvFiles);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GenerateReports_WithInvalidFormat_ShouldHandleGracefully()
        {
            // Arrange
            var result = CreateComparisonResultWithAllStatuses();
            var formats = new List<string> { "invalid-format", "json" };
            var originalOut = Console.Out;
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            try
            {
                // Act
                await _reportService.GenerateReportsAsync(result, _tempDirectory, formats);

                // Assert
                var output = consoleOutput.ToString();
                Assert.Contains("Warning: Unknown report format 'invalid-format'", output);
                
                // Should still create the valid format
                var jsonFiles = Directory.GetFiles(_tempDirectory, "*.json");
                Assert.Single(jsonFiles);
            }
            finally
            {
                Console.SetOut(originalOut);
                consoleOutput.Dispose();
            }
        }

        [Fact]
        public async Task GenerateReports_WithNullDifferences_ShouldHandleGracefully()
        {
            // Arrange
            var result = CreateDifferentWithNullDifferences();
            var formats = new List<string> { "json", "html", "csv" };

            // Act & Assert - Should not throw
            await _reportService.GenerateReportsAsync(result, _tempDirectory, formats);

            // Verify files were created
            Assert.True(Directory.GetFiles(_tempDirectory, "*.json").Length > 0);
            Assert.True(Directory.GetFiles(_tempDirectory, "*.html").Length > 0);
            Assert.True(Directory.GetFiles(_tempDirectory, "*.csv").Length > 0);
        }

        [Fact]
        public async Task GenerateReports_CreatesOutputDirectory_WhenNotExists()
        {
            // Arrange
            var result = CreateComparisonResultWithAllStatuses();
            var formats = new List<string> { "json" };
            var nonExistentDirectory = Path.Combine(_tempDirectory, "new-directory");

            // Act
            await _reportService.GenerateReportsAsync(result, nonExistentDirectory, formats);

            // Assert
            Assert.True(Directory.Exists(nonExistentDirectory));
            var jsonFiles = Directory.GetFiles(nonExistentDirectory, "*.json");
            Assert.Single(jsonFiles);
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        #endregion
    }
}
using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;

namespace ConditionalAccessExporter.Tests
{
    public class CrossFormatReportGenerationServiceTests : IDisposable
    {
        private readonly CrossFormatReportGenerationService _reportService;
        private readonly string _tempDirectory;

        public CrossFormatReportGenerationServiceTests()
        {
            _reportService = new CrossFormatReportGenerationService();
            _tempDirectory = Path.Combine(Path.GetTempPath(), "CrossFormatReportGenerationTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        #region Test Data Creation Methods

        private CrossFormatComparisonResult CreateCrossFormatResultWithAllStatuses()
        {
            return new CrossFormatComparisonResult
            {
                ComparedAt = new DateTime(2024, 5, 30, 12, 0, 0, DateTimeKind.Utc),
                SourceDirectory = "/test/source",
                ReferenceDirectory = "/test/reference",
                SourceFormat = PolicyFormat.Json,
                ReferenceFormat = PolicyFormat.Terraform,
                Summary = new CrossFormatComparisonSummary
                {
                    TotalSourcePolicies = 5,
                    TotalReferencePolicies = 5,
                    MatchingPolicies = 1,
                    SemanticallyEquivalentPolicies = 1,
                    PoliciesWithDifferences = 1,
                    SourceOnlyPolicies = 1,
                    ReferenceOnlyPolicies = 1
                },
                PolicyComparisons = new List<CrossFormatPolicyComparison>
                {
                    new CrossFormatPolicyComparison
                    {
                        PolicyId = "identical-policy-123",
                        PolicyName = "Identical Policy",
                        Status = CrossFormatComparisonStatus.Identical,
                        SourcePolicy = CreateNormalizedPolicy("identical-policy-123", "Identical Policy", PolicyFormat.Json, "identical.json"),
                        ReferencePolicy = CreateNormalizedPolicy("identical-policy-123", "Identical Policy", PolicyFormat.Terraform, "identical.tf")
                    },
                    new CrossFormatPolicyComparison
                    {
                        PolicyId = "equivalent-policy-456",
                        PolicyName = "Semantically Equivalent Policy",
                        Status = CrossFormatComparisonStatus.SemanticallyEquivalent,
                        SourcePolicy = CreateNormalizedPolicy("equivalent-policy-456", "Semantically Equivalent Policy", PolicyFormat.Json, "equivalent.json"),
                        ReferencePolicy = CreateNormalizedPolicy("equivalent-policy-456", "Semantically Equivalent Policy", PolicyFormat.Terraform, "equivalent.tf"),
                        SemanticAnalysis = new SemanticAnalysisResult
                        {
                            IsSemanticallyEquivalent = true,
                            SimilarityScore = 0.95,
                            SemanticInsights = new List<string> { "Minor formatting differences", "Equivalent access controls" }
                        }
                    },
                    new CrossFormatPolicyComparison
                    {
                        PolicyId = "different-policy-789",
                        PolicyName = "Different Policy",
                        Status = CrossFormatComparisonStatus.Different,
                        SourcePolicy = CreateNormalizedPolicy("different-policy-789", "Different Policy", PolicyFormat.Json, "different.json"),
                        ReferencePolicy = CreateNormalizedPolicy("different-policy-789", "Different Policy", PolicyFormat.Terraform, "different.tf"),
                        Differences = new List<string> { "User conditions differ", "Grant controls mismatch" },
                        ConversionSuggestions = new List<string> { "Update user inclusion list", "Align MFA requirements" }
                    },
                    new CrossFormatPolicyComparison
                    {
                        PolicyId = "source-only-101",
                        PolicyName = "Source Only Policy",
                        Status = CrossFormatComparisonStatus.SourceOnly,
                        SourcePolicy = CreateNormalizedPolicy("source-only-101", "Source Only Policy", PolicyFormat.Json, "source-only.json")
                    },
                    new CrossFormatPolicyComparison
                    {
                        PolicyId = "reference-only-202",
                        PolicyName = "Reference Only Policy",
                        Status = CrossFormatComparisonStatus.ReferenceOnly,
                        ReferencePolicy = CreateNormalizedPolicy("reference-only-202", "Reference Only Policy", PolicyFormat.Terraform, "reference-only.tf")
                    }
                }
            };
        }

        private CrossFormatComparisonResult CreateEmptyCrossFormatResult()
        {
            return new CrossFormatComparisonResult
            {
                ComparedAt = DateTime.UtcNow,
                SourceDirectory = "/empty/source",
                ReferenceDirectory = "/empty/reference",
                SourceFormat = PolicyFormat.Json,
                ReferenceFormat = PolicyFormat.Terraform,
                Summary = new CrossFormatComparisonSummary(),
                PolicyComparisons = new List<CrossFormatPolicyComparison>()
            };
        }

        private CrossFormatComparisonResult CreateSemanticallyEquivalentResult()
        {
            return new CrossFormatComparisonResult
            {
                ComparedAt = DateTime.UtcNow,
                SourceDirectory = "/semantic/source",
                ReferenceDirectory = "/semantic/reference",
                SourceFormat = PolicyFormat.Json,
                ReferenceFormat = PolicyFormat.Terraform,
                Summary = new CrossFormatComparisonSummary
                {
                    TotalSourcePolicies = 2,
                    TotalReferencePolicies = 2,
                    SemanticallyEquivalentPolicies = 2
                },
                PolicyComparisons = new List<CrossFormatPolicyComparison>
                {
                    new CrossFormatPolicyComparison
                    {
                        PolicyId = "semantic-policy-1",
                        PolicyName = "Semantic Policy One",
                        Status = CrossFormatComparisonStatus.SemanticallyEquivalent,
                        SourcePolicy = CreateNormalizedPolicy("semantic-policy-1", "Semantic Policy One", PolicyFormat.Json, "semantic1.json"),
                        ReferencePolicy = CreateNormalizedPolicy("semantic-policy-1", "Semantic Policy One", PolicyFormat.Terraform, "semantic1.tf"),
                        SemanticAnalysis = new SemanticAnalysisResult
                        {
                            IsSemanticallyEquivalent = true,
                            SimilarityScore = 0.88,
                            SemanticInsights = new List<string> { "Functionally equivalent", "Different syntax only" }
                        }
                    },
                    new CrossFormatPolicyComparison
                    {
                        PolicyId = "semantic-policy-2",
                        PolicyName = "Semantic Policy Two",
                        Status = CrossFormatComparisonStatus.SemanticallyEquivalent,
                        SourcePolicy = CreateNormalizedPolicy("semantic-policy-2", "Semantic Policy Two", PolicyFormat.Json, "semantic2.json"),
                        ReferencePolicy = CreateNormalizedPolicy("semantic-policy-2", "Semantic Policy Two", PolicyFormat.Terraform, "semantic2.tf"),
                        SemanticAnalysis = new SemanticAnalysisResult
                        {
                            IsSemanticallyEquivalent = true,
                            SimilarityScore = 0.92,
                            SemanticInsights = new List<string> { "Identical logic", "Format differences only" }
                        }
                    }
                }
            };
        }

        private CrossFormatComparisonResult CreateResultWithConversionSuggestions()
        {
            return new CrossFormatComparisonResult
            {
                ComparedAt = DateTime.UtcNow,
                SourceDirectory = "/conversion/source",
                ReferenceDirectory = "/conversion/reference",
                SourceFormat = PolicyFormat.Json,
                ReferenceFormat = PolicyFormat.Terraform,
                Summary = new CrossFormatComparisonSummary
                {
                    TotalSourcePolicies = 1,
                    TotalReferencePolicies = 1,
                    PoliciesWithDifferences = 1
                },
                PolicyComparisons = new List<CrossFormatPolicyComparison>
                {
                    new CrossFormatPolicyComparison
                    {
                        PolicyId = "conversion-policy",
                        PolicyName = "Policy with Conversion Suggestions",
                        Status = CrossFormatComparisonStatus.Different,
                        SourcePolicy = CreateNormalizedPolicy("conversion-policy", "Policy with Conversion Suggestions", PolicyFormat.Json, "conversion.json"),
                        ReferencePolicy = CreateNormalizedPolicy("conversion-policy", "Policy with Conversion Suggestions", PolicyFormat.Terraform, "conversion.tf"),
                        Differences = new List<string> { "Application scope differs", "Session controls mismatch" },
                        ConversionSuggestions = new List<string> 
                        { 
                            "Consider using azuread_conditional_access_policy resource",
                            "Update application_filter block",
                            "Align session_controls configuration"
                        }
                    }
                }
            };
        }

        private NormalizedPolicy CreateNormalizedPolicy(string id, string displayName, PolicyFormat format, string sourceFile)
        {
            return new NormalizedPolicy
            {
                Id = id,
                DisplayName = displayName,
                State = "Enabled",
                SourceFormat = format,
                SourceFile = sourceFile,
                NormalizedConditions = new NormalizedConditions
                {
                    Users = new NormalizedUsers
                    {
                        IncludeUsers = new List<string> { "All" }
                    },
                    Applications = new NormalizedApplications
                    {
                        IncludeApplications = new List<string> { "All" }
                    }
                },
                NormalizedGrantControls = new NormalizedGrantControls
                {
                    Operator = "OR",
                    BuiltInControls = new List<string> { "mfa" }
                }
            };
        }

        #endregion

        #region JSON Report Tests

        [Fact]
        public async Task GenerateJsonReport_WithAllStatuses_ShouldCreateValidJsonFile()
        {
            // Arrange
            var result = CreateCrossFormatResultWithAllStatuses();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Json);

            // Assert
            Assert.True(File.Exists(filePath));
            Assert.Contains("cross-format-comparison-", Path.GetFileName(filePath));
            Assert.EndsWith(".json", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath);
            Assert.False(string.IsNullOrEmpty(jsonContent));

            // Verify it's valid JSON
            var deserializedResult = JsonConvert.DeserializeObject<CrossFormatComparisonResult>(jsonContent);
            Assert.NotNull(deserializedResult);
            Assert.Equal(result.SourceDirectory, deserializedResult.SourceDirectory);
            Assert.Equal(result.PolicyComparisons.Count, deserializedResult.PolicyComparisons.Count);
        }

        [Fact]
        public async Task GenerateJsonReport_WithEmptyResult_ShouldCreateValidJsonFile()
        {
            // Arrange
            var result = CreateEmptyCrossFormatResult();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Json);

            // Assert
            Assert.True(File.Exists(filePath));
            var jsonContent = await File.ReadAllTextAsync(filePath);
            var deserializedResult = JsonConvert.DeserializeObject<CrossFormatComparisonResult>(jsonContent);
            Assert.NotNull(deserializedResult);
            Assert.Empty(deserializedResult.PolicyComparisons);
        }

        #endregion

        #region HTML Report Tests

        [Fact]
        public async Task GenerateHtmlReport_WithAllStatuses_ShouldCreateValidHtmlFile()
        {
            // Arrange
            var result = CreateCrossFormatResultWithAllStatuses();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Html);

            // Assert
            Assert.True(File.Exists(filePath));
            Assert.EndsWith(".html", filePath);

            var htmlContent = await File.ReadAllTextAsync(filePath);
            Assert.False(string.IsNullOrEmpty(htmlContent));

            // Verify HTML structure
            Assert.Contains("<!DOCTYPE html>", htmlContent);
            Assert.Contains("<html>", htmlContent);
            Assert.Contains("<title>Cross-Format Policy Comparison Report</title>", htmlContent);
            Assert.Contains("</html>", htmlContent);

            // Verify content includes source and reference info
            Assert.Contains("/test/source", htmlContent);
            Assert.Contains("/test/reference", htmlContent);
            Assert.Contains("Json", htmlContent);
            Assert.Contains("Terraform", htmlContent);

            // Verify summary data
            Assert.Contains("Total Source Policies", htmlContent);
            Assert.Contains("Total Reference Policies", htmlContent);
            Assert.Contains("Semantically Equivalent Policies", htmlContent);

            // Verify all policy statuses are represented
            Assert.Contains("Identical Policy", htmlContent);
            Assert.Contains("Semantically Equivalent Policy", htmlContent);
            Assert.Contains("Different Policy", htmlContent);
            Assert.Contains("Source Only Policy", htmlContent);
            Assert.Contains("Reference Only Policy", htmlContent);

            // Verify CSS classes are applied
            Assert.Contains("status-identical", htmlContent);
            Assert.Contains("status-equivalent", htmlContent);
            Assert.Contains("status-different", htmlContent);
            Assert.Contains("status-source-only", htmlContent);
            Assert.Contains("status-reference-only", htmlContent);

            // Verify differences and suggestions are displayed
            Assert.Contains("Differences:", htmlContent);
            Assert.Contains("User conditions differ", htmlContent);
            Assert.Contains("Conversion Suggestions:", htmlContent);
            Assert.Contains("Update user inclusion list", htmlContent);
        }

        [Fact]
        public async Task GenerateHtmlReport_WithSemanticallyEquivalent_ShouldDisplaySemanticInfo()
        {
            // Arrange
            var result = CreateSemanticallyEquivalentResult();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Html);

            // Assert
            var htmlContent = await File.ReadAllTextAsync(filePath);
            
            Assert.Contains("Semantically Equivalent Policies", htmlContent);
            Assert.Contains("2", htmlContent); // Count should be 2
            Assert.Contains("Semantic Policy One", htmlContent);
            Assert.Contains("Semantic Policy Two", htmlContent);
        }

        [Fact]
        public async Task GenerateHtmlReport_WithConversionSuggestions_ShouldDisplaySuggestions()
        {
            // Arrange
            var result = CreateResultWithConversionSuggestions();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Html);

            // Assert
            var htmlContent = await File.ReadAllTextAsync(filePath);
            
            Assert.Contains("Conversion Suggestions:", htmlContent);
            Assert.Contains("azuread_conditional_access_policy", htmlContent);
            Assert.Contains("application_filter block", htmlContent);
            Assert.Contains("session_controls configuration", htmlContent);
        }

        #endregion

        #region Markdown Report Tests

        [Fact]
        public async Task GenerateMarkdownReport_WithAllStatuses_ShouldCreateValidMarkdownFile()
        {
            // Arrange
            var result = CreateCrossFormatResultWithAllStatuses();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Markdown);

            // Assert
            Assert.True(File.Exists(filePath));
            Assert.EndsWith(".md", filePath);

            var markdownContent = await File.ReadAllTextAsync(filePath);
            Assert.False(string.IsNullOrEmpty(markdownContent));

            // Verify Markdown structure
            Assert.Contains("# Cross-Format Policy Comparison Report", markdownContent);
            Assert.Contains("## Comparison Summary", markdownContent);
            Assert.Contains("## Policy Comparisons", markdownContent);

            // Verify metadata
            Assert.Contains("**Generated:**", markdownContent);
            Assert.Contains("**Source:**", markdownContent);
            Assert.Contains("**Reference:**", markdownContent);

            // Verify summary table
            Assert.Contains("| Metric | Count |", markdownContent);
            Assert.Contains("|--------|-------|", markdownContent);
            Assert.Contains("| Total Source Policies |", markdownContent);
            Assert.Contains("| Semantically Equivalent Policies |", markdownContent);

            // Verify all policy statuses are represented
            Assert.Contains("### Identical Policy - Identical", markdownContent);
            Assert.Contains("### Semantically Equivalent Policy - SemanticallyEquivalent", markdownContent);
            Assert.Contains("### Different Policy - Different", markdownContent);
            Assert.Contains("### Source Only Policy - SourceOnly", markdownContent);
            Assert.Contains("### Reference Only Policy - ReferenceOnly", markdownContent);

            // Verify differences and suggestions formatting
            Assert.Contains("**Differences:**", markdownContent);
            Assert.Contains("- User conditions differ", markdownContent);
            Assert.Contains("**Conversion Suggestions:**", markdownContent);
            Assert.Contains("- Update user inclusion list", markdownContent);
        }

        [Fact]
        public async Task GenerateMarkdownReport_WithSemanticallyEquivalent_ShouldFormatCorrectly()
        {
            // Arrange
            var result = CreateSemanticallyEquivalentResult();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Markdown);

            // Assert
            var markdownContent = await File.ReadAllTextAsync(filePath);
            
            Assert.Contains("| Semantically Equivalent Policies | 2 |", markdownContent);
            Assert.Contains("### Semantic Policy One - SemanticallyEquivalent", markdownContent);
            Assert.Contains("### Semantic Policy Two - SemanticallyEquivalent", markdownContent);
        }

        [Fact]
        public async Task GenerateMarkdownReport_ValidatesMarkdownSyntax()
        {
            // Arrange
            var result = CreateCrossFormatResultWithAllStatuses();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Markdown);

            // Assert
            var markdownContent = await File.ReadAllTextAsync(filePath);
            
            // Check for proper table formatting
            var lines = markdownContent.Split('\n');
            var tableHeaderLine = lines.FirstOrDefault(l => l.Contains("| Metric | Count |"));
            Assert.NotNull(tableHeaderLine);
            
            var tableSeparatorLine = lines.FirstOrDefault(l => l.Contains("|--------|-------|"));
            Assert.NotNull(tableSeparatorLine);

            // Check for proper heading hierarchy
            Assert.Contains("# Cross-Format Policy Comparison Report", markdownContent);
            Assert.Contains("## Comparison Summary", markdownContent);
            Assert.Contains("## Policy Comparisons", markdownContent);
            
            // Verify proper list formatting
            Assert.Contains("- User conditions differ", markdownContent);
            Assert.Contains("- Update user inclusion list", markdownContent);
        }

        #endregion

        #region CSV Report Tests

        [Fact]
        public async Task GenerateCsvReport_WithAllStatuses_ShouldCreateValidCsvFile()
        {
            // Arrange
            var result = CreateCrossFormatResultWithAllStatuses();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Csv);

            // Assert
            Assert.True(File.Exists(filePath));
            Assert.EndsWith(".csv", filePath);

            var csvContent = await File.ReadAllTextAsync(filePath);
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Verify header
            Assert.Contains("PolicyName,Status,SourceFile,SourceFormat,ReferenceFile,ReferenceFormat,DifferenceCount,HasSuggestions", lines[0]);

            // Verify data rows (should have 5 policies + header)
            Assert.Equal(6, lines.Length);

            // Verify specific policy data
            Assert.Contains("Identical Policy", csvContent);
            Assert.Contains("Semantically Equivalent Policy", csvContent);
            Assert.Contains("Different Policy", csvContent);
            Assert.Contains("Source Only Policy", csvContent);
            Assert.Contains("Reference Only Policy", csvContent);

            // Verify format information
            Assert.Contains("Json", csvContent);
            Assert.Contains("Terraform", csvContent);

            // Verify differences and suggestions columns
            Assert.Contains("2,True", csvContent); // Different policy should have 2 differences and suggestions
            Assert.Contains("0,False", csvContent); // Other policies should have no differences or suggestions
        }

        [Fact]
        public async Task GenerateCsvReport_WithConversionSuggestions_ShouldReflectInHasSuggestions()
        {
            // Arrange
            var result = CreateResultWithConversionSuggestions();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Csv);

            // Assert
            var csvContent = await File.ReadAllTextAsync(filePath);
            
            Assert.Contains("2,True", csvContent); // Should have 2 differences and suggestions
            Assert.Contains("Policy with Conversion Suggestions", csvContent);
        }

        #endregion

        #region Default Format Tests

        [Fact]
        public async Task GenerateReport_WithDefaultFormat_ShouldCreateJsonReport()
        {
            // Arrange
            var result = CreateCrossFormatResultWithAllStatuses();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory); // No format specified

            // Assert
            Assert.True(File.Exists(filePath));
            Assert.EndsWith(".json", filePath);

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var deserializedResult = JsonConvert.DeserializeObject<CrossFormatComparisonResult>(jsonContent);
            Assert.NotNull(deserializedResult);
        }

        [Fact]
        public async Task GenerateReport_WithUnknownFormat_ShouldDefaultToJson()
        {
            // Arrange
            var result = CreateCrossFormatResultWithAllStatuses();

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, _tempDirectory, (ReportFormat)999); // Invalid format

            // Assert
            Assert.True(File.Exists(filePath));
            Assert.EndsWith(".json", filePath);
        }

        #endregion

        #region Content Accuracy Tests

        [Fact]
        public async Task GenerateReports_AllFormats_ShouldContainSameDataAccuracy()
        {
            // Arrange
            var result = CreateCrossFormatResultWithAllStatuses();

            // Act
            var jsonPath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Json);
            var htmlPath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Html);
            var markdownPath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Markdown);
            var csvPath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Csv);

            // Assert
            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var htmlContent = await File.ReadAllTextAsync(htmlPath);
            var markdownContent = await File.ReadAllTextAsync(markdownPath);
            var csvContent = await File.ReadAllTextAsync(csvPath);

            // Verify all formats contain the key data points
            var testValues = new[]
            {
                "Identical Policy",
                "Semantically Equivalent Policy", 
                "Different Policy",
                "Source Only Policy",
                "Reference Only Policy"
            };

            foreach (var value in testValues)
            {
                Assert.Contains(value, jsonContent);
                Assert.Contains(value, htmlContent);
                Assert.Contains(value, markdownContent);
                Assert.Contains(value, csvContent);
            }

            // Verify summary counts are consistent
            Assert.Contains("5", jsonContent); // Total policies
            Assert.Contains("5", htmlContent);
            Assert.Contains("5", markdownContent);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GenerateReport_WithEmptyResult_ShouldHandleGracefully()
        {
            // Arrange
            var result = CreateEmptyCrossFormatResult();

            // Act & Assert - Should not throw
            var jsonPath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Json);
            var htmlPath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Html);
            var markdownPath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Markdown);
            var csvPath = await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Csv);

            // Verify files were created
            Assert.True(File.Exists(jsonPath));
            Assert.True(File.Exists(htmlPath));
            Assert.True(File.Exists(markdownPath));
            Assert.True(File.Exists(csvPath));
        }

        [Fact]
        public async Task GenerateReport_CreatesOutputDirectory_WhenNotExists()
        {
            // Arrange
            var result = CreateCrossFormatResultWithAllStatuses();
            var nonExistentDirectory = Path.Combine(_tempDirectory, "new-directory");

            // Act
            var filePath = await _reportService.GenerateReportAsync(result, nonExistentDirectory, ReportFormat.Json);

            // Assert
            Assert.True(Directory.Exists(nonExistentDirectory));
            Assert.True(File.Exists(filePath));
        }

        #endregion

        #region Console Output Tests

        [Fact]
        public async Task GenerateReports_ShouldLogFileGeneration()
        {
            // Arrange
            var result = CreateCrossFormatResultWithAllStatuses();
            var originalOut = Console.Out;
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            try
            {
                // Act
                await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Json);
                await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Html);
                await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Markdown);
                await _reportService.GenerateReportAsync(result, _tempDirectory, ReportFormat.Csv);

                // Assert
                var output = consoleOutput.ToString();
                Assert.Contains("Cross-format comparison report generated:", output);
                Assert.Contains("HTML cross-format comparison report generated:", output);
                Assert.Contains("Markdown cross-format comparison report generated:", output);
                Assert.Contains("CSV cross-format comparison report generated:", output);
            }
            finally
            {
                Console.SetOut(originalOut);
                consoleOutput.Dispose();
            }
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
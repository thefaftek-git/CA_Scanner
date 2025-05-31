using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConditionalAccessExporter.Tests;

public class CrossFormatReportGenerationServiceComprehensiveTests
{
    private readonly CrossFormatReportGenerationService _reportService;
    private readonly string _tempOutputDir;

    public CrossFormatReportGenerationServiceComprehensiveTests()
    {
        _reportService = new CrossFormatReportGenerationService();
        _tempOutputDir = Path.Combine(Path.GetTempPath(), "test-cross-format-reports-" + Guid.NewGuid().ToString("N")[..8]);
    }

    private void Cleanup()
    {
        if (Directory.Exists(_tempOutputDir))
            Directory.Delete(_tempOutputDir, true);
    }

    #region Test Case 2.1: All Status Types

    [Fact]
    public async Task GenerateReportAsync_WithAllStatusTypes_Json_ShouldCreateValidReport()
    {
        // Arrange
        var comparisonResult = CreateCrossFormatComparisonResultWithAllStatuses();

        try
        {
            Directory.CreateDirectory(_tempOutputDir);
            
            // Act
            var reportPath = await _reportService.GenerateReportAsync(comparisonResult, _tempOutputDir, ReportFormat.Json);

            // Assert
            Assert.True(File.Exists(reportPath));
            var jsonContent = await File.ReadAllTextAsync(reportPath);
            Assert.False(string.IsNullOrWhiteSpace(jsonContent));
            
            var parsedJson = JsonConvert.DeserializeObject<CrossFormatComparisonResult>(jsonContent);
            Assert.NotNull(parsedJson);
            Assert.Equal(comparisonResult.SourceFormat, parsedJson.SourceFormat);
            Assert.Equal(comparisonResult.ReferenceFormat, parsedJson.ReferenceFormat);
            Assert.Equal(comparisonResult.PolicyComparisons.Count, parsedJson.PolicyComparisons.Count);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task GenerateReportAsync_WithAllStatusTypes_Html_ShouldCreateValidReport()
    {
        // Arrange
        var comparisonResult = CreateCrossFormatComparisonResultWithAllStatuses();

        try
        {
            Directory.CreateDirectory(_tempOutputDir);
            
            // Act
            var reportPath = await _reportService.GenerateReportAsync(comparisonResult, _tempOutputDir, ReportFormat.Html);

            // Assert
            Assert.True(File.Exists(reportPath));
            var htmlContent = await File.ReadAllTextAsync(reportPath);
            Assert.False(string.IsNullOrWhiteSpace(htmlContent));
            
            Assert.Contains("<!DOCTYPE html>", htmlContent);
            Assert.Contains("Cross-Format Policy Comparison Report", htmlContent);
            Assert.Contains("status-identical", htmlContent);
            Assert.Contains("status-equivalent", htmlContent);
            Assert.Contains("status-different", htmlContent);
            Assert.Contains("status-source-only", htmlContent);
            Assert.Contains("status-reference-only", htmlContent);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task GenerateReportAsync_WithAllStatusTypes_Markdown_ShouldCreateValidReport()
    {
        // Arrange
        var comparisonResult = CreateCrossFormatComparisonResultWithAllStatuses();

        try
        {
            Directory.CreateDirectory(_tempOutputDir);
            
            // Act
            var reportPath = await _reportService.GenerateReportAsync(comparisonResult, _tempOutputDir, ReportFormat.Markdown);

            // Assert
            Assert.True(File.Exists(reportPath));
            var markdownContent = await File.ReadAllTextAsync(reportPath);
            Assert.False(string.IsNullOrWhiteSpace(markdownContent));
            
            Assert.Contains("# Cross-Format Policy Comparison Report", markdownContent);
            Assert.Contains("| Metric | Count |", markdownContent);
            Assert.Contains("## Policy Comparisons", markdownContent);
            Assert.Contains("### Identical Policy", markdownContent);
            Assert.Contains("### Semantically Equivalent Policy", markdownContent);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task GenerateReportAsync_WithAllStatusTypes_Csv_ShouldCreateValidReport()
    {
        // Arrange
        var comparisonResult = CreateCrossFormatComparisonResultWithAllStatuses();

        try
        {
            Directory.CreateDirectory(_tempOutputDir);
            
            // Act
            var reportPath = await _reportService.GenerateReportAsync(comparisonResult, _tempOutputDir, ReportFormat.Csv);

            // Assert
            Assert.True(File.Exists(reportPath));
            var csvContent = await File.ReadAllTextAsync(reportPath);
            Assert.False(string.IsNullOrWhiteSpace(csvContent));
            
            Assert.Contains("PolicyName,Status,SourceFile,SourceFormat,ReferenceFile,ReferenceFormat,DifferenceCount,HasSuggestions", csvContent);
            Assert.Contains("Identical", csvContent);
            Assert.Contains("SemanticallyEquivalent", csvContent);
            Assert.Contains("Different", csvContent);
            Assert.Contains("SourceOnly", csvContent);
            Assert.Contains("ReferenceOnly", csvContent);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Test Case 2.2: Semantically Equivalent Policies

    [Fact]
    public async Task GenerateReportAsync_WithSemanticallyEquivalentPolicies_ShouldShowSemanticInfo()
    {
        // Arrange
        var result = CreateComparisonResultWithSemanticallyEquivalentPolicies();

        try
        {
            Directory.CreateDirectory(_tempOutputDir);
            
            // Act - Test HTML for rich semantic display
            var htmlReportPath = await _reportService.GenerateReportAsync(result, _tempOutputDir, ReportFormat.Html);
            var markdownReportPath = await _reportService.GenerateReportAsync(result, _tempOutputDir, ReportFormat.Markdown);

            // Assert
            var htmlContent = await File.ReadAllTextAsync(htmlReportPath);
            Assert.Contains("SemanticallyEquivalent", htmlContent);
            // The service doesn't display detailed similarity scores in HTML - just the status
            
            var markdownContent = await File.ReadAllTextAsync(markdownReportPath);
            Assert.Contains("SemanticallyEquivalent", markdownContent);
            // The service doesn't display detailed similarity scores in Markdown - just the status
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Test Case 2.3: Conversion Suggestions

    [Fact]
    public async Task GenerateReportAsync_WithConversionSuggestions_ShouldDisplaySuggestions()
    {
        // Arrange
        var result = CreateComparisonResultWithConversionSuggestions();

        try
        {
            Directory.CreateDirectory(_tempOutputDir);
            
            // Act
            var htmlReportPath = await _reportService.GenerateReportAsync(result, _tempOutputDir, ReportFormat.Html);
            var markdownReportPath = await _reportService.GenerateReportAsync(result, _tempOutputDir, ReportFormat.Markdown);

            // Assert
            var htmlContent = await File.ReadAllTextAsync(htmlReportPath);
            Assert.Contains("Conversion Suggestions:", htmlContent);
            Assert.Contains("Consider updating the display name format", htmlContent);
            Assert.Contains("Update grant controls structure", htmlContent);

            var markdownContent = await File.ReadAllTextAsync(markdownReportPath);
            Assert.Contains("**Conversion Suggestions:**", markdownContent);
            Assert.Contains("- Consider updating the display name format", markdownContent);
            Assert.Contains("- Update grant controls structure", markdownContent);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Test Case 2.4: Empty CrossFormatComparisonResult

    [Fact]
    public async Task GenerateReportAsync_WithEmptyResult_ShouldGenerateReportsWithoutError()
    {
        // Arrange
        var emptyResult = new CrossFormatComparisonResult
        {
            ComparedAt = DateTime.UtcNow,
            SourceDirectory = "test-source",
            ReferenceDirectory = "test-reference",
            SourceFormat = PolicyFormat.Json,
            ReferenceFormat = PolicyFormat.Terraform,
            Summary = new CrossFormatComparisonSummary(),
            PolicyComparisons = new List<CrossFormatPolicyComparison>()
        };

        try
        {
            Directory.CreateDirectory(_tempOutputDir);
            
            // Act
            var jsonReportPath = await _reportService.GenerateReportAsync(emptyResult, _tempOutputDir, ReportFormat.Json);
            var htmlReportPath = await _reportService.GenerateReportAsync(emptyResult, _tempOutputDir, ReportFormat.Html);

            // Assert
            Assert.True(File.Exists(jsonReportPath));
            Assert.True(File.Exists(htmlReportPath));

            var jsonContent = await File.ReadAllTextAsync(jsonReportPath);
            var parsedJson = JsonConvert.DeserializeObject<CrossFormatComparisonResult>(jsonContent);
            Assert.NotNull(parsedJson);
            Assert.Empty(parsedJson.PolicyComparisons);

            var htmlContent = await File.ReadAllTextAsync(htmlReportPath);
            Assert.Contains("<!DOCTYPE html>", htmlContent);
            Assert.Contains("Cross-Format Policy Comparison Report", htmlContent);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Test Case 2.5: Invalid Format

    [Fact]
    public async Task GenerateReportAsync_WithInvalidFormat_ShouldDefaultToJson()
    {
        // Arrange
        var result = CreateCrossFormatComparisonResultWithAllStatuses();

        try
        {
            Directory.CreateDirectory(_tempOutputDir);
            
            // Act - Using cast to invalid enum value to simulate unknown format
            var reportPath = await _reportService.GenerateReportAsync(result, _tempOutputDir, (ReportFormat)999);

            // Assert - Should default to JSON
            Assert.True(File.Exists(reportPath));
            var content = await File.ReadAllTextAsync(reportPath);
            
            // Should be valid JSON
            var parsedJson = JsonConvert.DeserializeObject<CrossFormatComparisonResult>(content);
            Assert.NotNull(parsedJson);
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Format-Specific Tests

    [Fact]
    public async Task GenerateReportAsync_HtmlFormat_ShouldContainCssAndStructure()
    {
        // Arrange
        var result = CreateCrossFormatComparisonResultWithAllStatuses();

        try
        {
            Directory.CreateDirectory(_tempOutputDir);
            
            // Act
            var reportPath = await _reportService.GenerateReportAsync(result, _tempOutputDir, ReportFormat.Html);

            // Assert
            var htmlContent = await File.ReadAllTextAsync(reportPath);
            
            // Check HTML structure
            Assert.Contains("<!DOCTYPE html>", htmlContent);
            Assert.Contains("<html>", htmlContent);
            Assert.Contains("<head>", htmlContent);
            Assert.Contains("<style>", htmlContent);
            Assert.Contains("<body>", htmlContent);
            
            // Check CSS classes
            Assert.Contains(".status-identical", htmlContent);
            Assert.Contains(".status-equivalent", htmlContent);
            Assert.Contains(".status-different", htmlContent);
            Assert.Contains(".differences", htmlContent);
            Assert.Contains(".suggestions", htmlContent);
            
            // Check content sections
            Assert.Contains("Cross-Format Policy Comparison Report", htmlContent);
            Assert.Contains("Summary", htmlContent);
            Assert.Contains("Policy Comparisons", htmlContent);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task GenerateReportAsync_MarkdownFormat_ShouldContainValidMarkdownSyntax()
    {
        // Arrange
        var result = CreateCrossFormatComparisonResultWithAllStatuses();

        try
        {
            Directory.CreateDirectory(_tempOutputDir);
            
            // Act
            var reportPath = await _reportService.GenerateReportAsync(result, _tempOutputDir, ReportFormat.Markdown);

            // Assert
            var markdownContent = await File.ReadAllTextAsync(reportPath);
            
            // Check Markdown syntax
            Assert.Contains("# Cross-Format Policy Comparison Report", markdownContent);
            Assert.Contains("## Comparison Summary", markdownContent); // Note: "Comparison Summary" not just "Summary"
            Assert.Contains("## Policy Comparisons", markdownContent);
            Assert.Contains("### ", markdownContent); // Policy section headers
            Assert.Contains("| Metric | Count |", markdownContent); // Table headers
            Assert.Contains("|--------|-------|", markdownContent); // Table separator
            Assert.Contains("**Generated:**", markdownContent); // Bold text
            Assert.Contains("**Source:**", markdownContent);
            Assert.Contains("**Reference:**", markdownContent);
        }
        finally
        {
            Cleanup();
        }
    }

    [Fact]
    public async Task GenerateReportAsync_CsvFormat_ShouldContainValidCsvStructure()
    {
        // Arrange
        var result = CreateCrossFormatComparisonResultWithAllStatuses();

        try
        {
            Directory.CreateDirectory(_tempOutputDir);
            
            // Act
            var reportPath = await _reportService.GenerateReportAsync(result, _tempOutputDir, ReportFormat.Csv);

            // Assert
            var csvContent = await File.ReadAllTextAsync(reportPath);
            
            // Split into lines for validation
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.True(lines.Length >= 2); // Header + at least one data row
            
            // Check header
            var header = lines[0];
            Assert.Contains("PolicyName", header);
            Assert.Contains("Status", header);
            Assert.Contains("SourceFile", header);
            Assert.Contains("SourceFormat", header);
            Assert.Contains("ReferenceFile", header);
            Assert.Contains("ReferenceFormat", header);
            Assert.Contains("DifferenceCount", header);
            Assert.Contains("HasSuggestions", header);
            
            // Check data rows contain CSV structure (commas, quotes)
            foreach (var line in lines.Skip(1))
            {
                Assert.Contains(',', line);
                // CSV should properly quote fields
                Assert.True(line.Contains('"') || !line.Contains(','));
            }
        }
        finally
        {
            Cleanup();
        }
    }

    #endregion

    #region Helper Methods

    private CrossFormatComparisonResult CreateCrossFormatComparisonResultWithAllStatuses()
    {
        return new CrossFormatComparisonResult
        {
            ComparedAt = DateTime.UtcNow,
            SourceDirectory = "/test/source",
            ReferenceDirectory = "/test/reference",
            SourceFormat = PolicyFormat.Json,
            ReferenceFormat = PolicyFormat.Terraform,
            Summary = new CrossFormatComparisonSummary
            {
                TotalSourcePolicies = 5,
                TotalReferencePolicies = 5,
                SourceOnlyPolicies = 1,
                ReferenceOnlyPolicies = 1,
                MatchingPolicies = 1,
                SemanticallyEquivalentPolicies = 1,
                PoliciesWithDifferences = 1
            },
            PolicyComparisons = new List<CrossFormatPolicyComparison>
            {
                new CrossFormatPolicyComparison
                {
                    PolicyId = "policy-1",
                    PolicyName = "Identical Policy",
                    Status = CrossFormatComparisonStatus.Identical,
                    SourcePolicy = new NormalizedPolicy
                    {
                        Id = "policy-1",
                        DisplayName = "Identical Policy",
                        SourceFormat = PolicyFormat.Json,
                        SourceFile = "identical.json"
                    },
                    ReferencePolicy = new NormalizedPolicy
                    {
                        Id = "policy-1",
                        DisplayName = "Identical Policy",
                        SourceFormat = PolicyFormat.Terraform,
                        SourceFile = "identical.tf"
                    }
                },
                new CrossFormatPolicyComparison
                {
                    PolicyId = "policy-2",
                    PolicyName = "Semantically Equivalent Policy",
                    Status = CrossFormatComparisonStatus.SemanticallyEquivalent,
                    SourcePolicy = new NormalizedPolicy
                    {
                        Id = "policy-2",
                        DisplayName = "Semantically Equivalent Policy",
                        SourceFormat = PolicyFormat.Json,
                        SourceFile = "semantic.json"
                    },
                    ReferencePolicy = new NormalizedPolicy
                    {
                        Id = "policy-2",
                        DisplayName = "Semantically Equivalent Policy",
                        SourceFormat = PolicyFormat.Terraform,
                        SourceFile = "semantic.tf"
                    },
                    SemanticAnalysis = new SemanticAnalysisResult
                    {
                        IsSemanticallyEquivalent = true,
                        SimilarityScore = 0.85,
                        SemanticInsights = new List<string> { "Minor formatting differences" }
                    }
                },
                new CrossFormatPolicyComparison
                {
                    PolicyId = "policy-3",
                    PolicyName = "Different Policy",
                    Status = CrossFormatComparisonStatus.Different,
                    SourcePolicy = new NormalizedPolicy
                    {
                        Id = "policy-3",
                        DisplayName = "Different Policy",
                        SourceFormat = PolicyFormat.Json,
                        SourceFile = "different.json"
                    },
                    ReferencePolicy = new NormalizedPolicy
                    {
                        Id = "policy-3",
                        DisplayName = "Different Policy",
                        SourceFormat = PolicyFormat.Terraform,
                        SourceFile = "different.tf"
                    },
                    Differences = new List<string> { "Grant controls differ", "User conditions differ" }
                },
                new CrossFormatPolicyComparison
                {
                    PolicyId = "policy-4",
                    PolicyName = "Source Only Policy",
                    Status = CrossFormatComparisonStatus.SourceOnly,
                    SourcePolicy = new NormalizedPolicy
                    {
                        Id = "policy-4",
                        DisplayName = "Source Only Policy",
                        SourceFormat = PolicyFormat.Json,
                        SourceFile = "source-only.json"
                    }
                },
                new CrossFormatPolicyComparison
                {
                    PolicyId = "policy-5",
                    PolicyName = "Reference Only Policy",
                    Status = CrossFormatComparisonStatus.ReferenceOnly,
                    ReferencePolicy = new NormalizedPolicy
                    {
                        Id = "policy-5",
                        DisplayName = "Reference Only Policy",
                        SourceFormat = PolicyFormat.Terraform,
                        SourceFile = "reference-only.tf"
                    }
                }
            }
        };
    }

    private CrossFormatComparisonResult CreateComparisonResultWithSemanticallyEquivalentPolicies()
    {
        return new CrossFormatComparisonResult
        {
            ComparedAt = DateTime.UtcNow,
            SourceDirectory = "/test/source",
            ReferenceDirectory = "/test/reference",
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
                    PolicyId = "policy-1",
                    PolicyName = "Semantic Policy 1",
                    Status = CrossFormatComparisonStatus.SemanticallyEquivalent,
                    SourcePolicy = new NormalizedPolicy
                    {
                        Id = "policy-1",
                        DisplayName = "Semantic Policy 1",
                        SourceFormat = PolicyFormat.Json,
                        SourceFile = "semantic1.json"
                    },
                    ReferencePolicy = new NormalizedPolicy
                    {
                        Id = "policy-1",
                        DisplayName = "Semantic Policy 1",
                        SourceFormat = PolicyFormat.Terraform,
                        SourceFile = "semantic1.tf"
                    },
                    SemanticAnalysis = new SemanticAnalysisResult
                    {
                        IsSemanticallyEquivalent = true,
                        SimilarityScore = 0.85,
                        SemanticInsights = new List<string> { "Functionally equivalent with minor syntax differences" }
                    }
                },
                new CrossFormatPolicyComparison
                {
                    PolicyId = "policy-2",
                    PolicyName = "Semantic Policy 2",
                    Status = CrossFormatComparisonStatus.SemanticallyEquivalent,
                    SourcePolicy = new NormalizedPolicy
                    {
                        Id = "policy-2",
                        DisplayName = "Semantic Policy 2",
                        SourceFormat = PolicyFormat.Json,
                        SourceFile = "semantic2.json"
                    },
                    ReferencePolicy = new NormalizedPolicy
                    {
                        Id = "policy-2",
                        DisplayName = "Semantic Policy 2",
                        SourceFormat = PolicyFormat.Terraform,
                        SourceFile = "semantic2.tf"
                    },
                    SemanticAnalysis = new SemanticAnalysisResult
                    {
                        IsSemanticallyEquivalent = true,
                        SimilarityScore = 0.92,
                        SemanticInsights = new List<string> { "Equivalent functionality" }
                    }
                }
            }
        };
    }

    private CrossFormatComparisonResult CreateComparisonResultWithConversionSuggestions()
    {
        return new CrossFormatComparisonResult
        {
            ComparedAt = DateTime.UtcNow,
            SourceDirectory = "/test/source",
            ReferenceDirectory = "/test/reference",
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
                    PolicyId = "policy-1",
                    PolicyName = "Policy with Suggestions",
                    Status = CrossFormatComparisonStatus.Different,
                    SourcePolicy = new NormalizedPolicy
                    {
                        Id = "policy-1",
                        DisplayName = "Policy with Suggestions",
                        SourceFormat = PolicyFormat.Json,
                        SourceFile = "suggestions.json"
                    },
                    ReferencePolicy = new NormalizedPolicy
                    {
                        Id = "policy-1",
                        DisplayName = "Policy with Suggestions",
                        SourceFormat = PolicyFormat.Terraform,
                        SourceFile = "suggestions.tf"
                    },
                    Differences = new List<string> { "Display name format differs", "Grant controls structure differs" },
                    ConversionSuggestions = new List<string>
                    {
                        "Consider updating the display name format to match Terraform conventions",
                        "Update grant controls structure to use list format instead of array",
                        "Consider consolidating user inclusion rules"
                    }
                }
            }
        };
    }

    #endregion
}
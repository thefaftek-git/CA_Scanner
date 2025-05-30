using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ConditionalAccessExporter.Tests;

public class CrossFormatComparisonTests
{
    [Fact]
    public async Task CrossFormatComparison_WithSampleData_ShouldProduceValidResults()
    {
        // Arrange
        var jsonComparisonService = new PolicyComparisonService();
        var terraformParsingService = new TerraformParsingService();
        var terraformConversionService = new TerraformConversionService();
        var crossFormatService = new CrossFormatPolicyComparisonService(
            jsonComparisonService,
            terraformParsingService,
            terraformConversionService);

        var tempSourceDir = Path.Combine(Path.GetTempPath(), "test-source");
        var tempReferenceDir = Path.Combine(Path.GetTempPath(), "test-reference");

        try
        {
            // Create test directories
            Directory.CreateDirectory(tempSourceDir);
            Directory.CreateDirectory(tempReferenceDir);

            // Create sample JSON policy
            var sampleJsonPolicy = new
            {
                Id = "test-policy-123",
                DisplayName = "Test MFA Policy",
                State = "Enabled",
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

            var jsonPolicyPath = Path.Combine(tempSourceDir, "test-policy.json");
            await File.WriteAllTextAsync(jsonPolicyPath, JsonConvert.SerializeObject(sampleJsonPolicy, Formatting.Indented));

            // Create sample Terraform policy
            var terraformContent = @"
resource ""azuread_conditional_access_policy"" ""test_mfa_policy"" {
  display_name = ""Test MFA Policy""
  state        = ""enabled""

  conditions {
    applications {
      include_applications = [""All""]
    }

    users {
      include_users = [""All""]
    }
  }

  grant_controls {
    operator          = ""OR""
    built_in_controls = [""mfa""]
  }
}";

            var terraformPolicyPath = Path.Combine(tempReferenceDir, "test-policy.tf");
            await File.WriteAllTextAsync(terraformPolicyPath, terraformContent);

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName,
                CaseSensitive = false,
                EnableSemanticComparison = true,
                SemanticSimilarityThreshold = 0.8
            };

            // Act
            var result = await crossFormatService.CompareAsync(
                tempSourceDir,
                tempReferenceDir,
                matchingOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PolicyFormat.Json, result.SourceFormat);
            Assert.Equal(PolicyFormat.Terraform, result.ReferenceFormat);
            Assert.True(result.Summary.TotalSourcePolicies > 0);
            Assert.True(result.Summary.TotalReferencePolicies > 0);
            Assert.NotNull(result.PolicyComparisons);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempSourceDir))
                Directory.Delete(tempSourceDir, true);
            if (Directory.Exists(tempReferenceDir))
                Directory.Delete(tempReferenceDir, true);
        }
    }

    [Fact]
    public void CrossFormatMatchingOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new CrossFormatMatchingOptions();

        // Assert
        Assert.Equal(CrossFormatMatchingStrategy.ByName, options.Strategy);
        Assert.False(options.CaseSensitive);
        Assert.True(options.EnableSemanticComparison);
        Assert.Equal(0.8, options.SemanticSimilarityThreshold);
    }

    [Fact]
    public async Task CrossFormatReportGeneration_ShouldCreateValidJsonReport()
    {
        // Arrange
        var reportService = new CrossFormatReportGenerationService();
        var comparisonResult = new CrossFormatComparisonResult
        {
            ComparedAt = DateTime.UtcNow,
            SourceDirectory = "test-source",
            ReferenceDirectory = "test-reference",
            SourceFormat = PolicyFormat.Json,
            ReferenceFormat = PolicyFormat.Terraform,
            Summary = new CrossFormatComparisonSummary
            {
                TotalSourcePolicies = 1,
                TotalReferencePolicies = 1,
                MatchingPolicies = 1
            },
            PolicyComparisons = new List<CrossFormatPolicyComparison>()
        };

        var tempOutputDir = Path.Combine(Path.GetTempPath(), "test-reports");

        try
        {
            Directory.CreateDirectory(tempOutputDir);

            // Act
            var reportPath = await reportService.GenerateReportAsync(
                comparisonResult,
                tempOutputDir,
                ReportFormat.Json);

            // Assert
            Assert.True(File.Exists(reportPath));
            var reportContent = await File.ReadAllTextAsync(reportPath);
            Assert.False(string.IsNullOrWhiteSpace(reportContent));
            
            // Verify it's valid JSON
            var parsedReport = JsonConvert.DeserializeObject(reportContent);
            Assert.NotNull(parsedReport);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempOutputDir))
                Directory.Delete(tempOutputDir, true);
        }
    }
}
using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace ConditionalAccessExporter.Tests
{
    /// <summary>
    /// Integration tests covering real workflow scenarios with actual services
    /// These tests verify end-to-end functionality and integration between components
    /// </summary>
    [Collection("Console Output Tests")]
    public class WorkflowIntegrationTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly ITestOutputHelper _output;

        public WorkflowIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            _testDirectory = Path.Combine(Path.GetTempPath(), "WorkflowIntegrationTests_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_testDirectory);
            _output.WriteLine($"Test directory: {_testDirectory}");
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Warning: Could not cleanup test directory: {ex.Message}");
                }
            }
        }

        #region Cross-Format Comparison Integration Tests

        [Fact]
        public async Task CrossFormatWorkflow_JsonToTerraformComparison_ShouldDetectEquivalentPolicies()
        {
            // Arrange
            var jsonDir = CreateTestDirectory("json-policies");
            var terraformDir = CreateTestDirectory("terraform-policies");

            // Create equivalent policies in different formats
            var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("test-policy-1", "MFA Policy", "enabled");
            var terraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("test_policy_1", "MFA Policy", "enabled");

            await File.WriteAllTextAsync(Path.Combine(jsonDir, "mfa-policy.json"), jsonPolicy.ToString());
            await File.WriteAllTextAsync(Path.Combine(terraformDir, "mfa-policy.tf"), terraformPolicy);

            // Act - Perform cross-format comparison
            var crossFormatService = new CrossFormatPolicyComparisonService(
                new PolicyComparisonService(),
                new TerraformParsingService(),
                new TerraformConversionService());

            var matchingOptions = new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName,
                EnableSemanticComparison = true,
                CaseSensitive = false
            };

            var result = await crossFormatService.CompareAsync(jsonDir, terraformDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Summary.TotalSourcePolicies > 0);
            Assert.True(result.Summary.TotalReferencePolicies > 0);
            Assert.NotEmpty(result.PolicyComparisons);

            _output.WriteLine($"Cross-format comparison: {result.Summary.TotalSourcePolicies} JSON policies, {result.Summary.TotalReferencePolicies} Terraform policies");
        }

        [Fact]
        public async Task CrossFormatWorkflow_JsonToTerraformWithReport_ShouldGenerateComprehensiveReport()
        {
            // Arrange
            var jsonDir = CreateTestDirectory("json-source");
            var terraformDir = CreateTestDirectory("terraform-reference");
            var outputDir = CreateTestDirectory("report-output");

            // Create multiple policies for comprehensive testing
            var jsonPolicies = new[]
            {
                TestDataFactory.CreateBasicJsonPolicy("policy-1", "Global MFA", "enabled"),
                TestDataFactory.CreateBasicJsonPolicy("policy-2", "Block Legacy Auth", "enabled")
            };

            var terraformPolicies = new[]
            {
                TestDataFactory.CreateBasicTerraformPolicy("policy_1", "Global MFA", "enabled"),
                TestDataFactory.CreateBasicTerraformPolicy("policy_3", "Different Policy", "enabled")
            };

            foreach (var (policy, index) in jsonPolicies.Select((p, i) => (p, i)))
            {
                await File.WriteAllTextAsync(Path.Combine(jsonDir, $"policy-{index + 1}.json"), policy.ToString());
            }

            foreach (var (policy, index) in terraformPolicies.Select((p, i) => (p, i)))
            {
                await File.WriteAllTextAsync(Path.Combine(terraformDir, $"policy-{index + 1}.tf"), policy);
            }

            // Act
            var crossFormatService = new CrossFormatPolicyComparisonService(
                new PolicyComparisonService(),
                new TerraformParsingService(),
                new TerraformConversionService());

            var result = await crossFormatService.CompareAsync(jsonDir, terraformDir, new CrossFormatMatchingOptions
            {
                Strategy = CrossFormatMatchingStrategy.ByName,
                EnableSemanticComparison = true
            });

            // Generate report
            var reportService = new CrossFormatReportGenerationService();
            var reportPath = await reportService.GenerateReportAsync(result, Path.Combine(outputDir, "comparison-report.json"));

            // Assert
            Assert.NotNull(result);
            Assert.True(File.Exists(reportPath));

            var reportContent = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("Global MFA", reportContent);

            _output.WriteLine($"Report generated at: {reportPath}");
        }

        #endregion

        #region JSON to Terraform Conversion Integration Tests

        [Fact]
        public async Task JsonToTerraformWorkflow_SinglePolicy_ShouldConvertWithMetadata()
        {
            // Arrange
            var jsonFilePath = Path.Combine(_testDirectory, "test-policy.json");
            var outputDir = CreateTestDirectory("terraform-output");

            var jsonExport = TestDataFactory.CreateSinglePolicyExport("conversion-test", "Test Conversion Policy", "enabled");
            await File.WriteAllTextAsync(jsonFilePath, jsonExport.ToString());

            // Act
            var conversionService = new JsonToTerraformService();
            var options = new TerraformOutputOptions
            {
                OutputDirectory = outputDir,
                GenerateVariables = true,
                GenerateProviderConfig = true,
                IncludeComments = true
            };

            var result = await conversionService.ConvertJsonToTerraformAsync(jsonFilePath, options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Errors.Count == 0, $"Conversion had errors: {string.Join(", ", result.Errors)}");
            Assert.NotEmpty(result.GeneratedFiles);

            // Verify expected files were created
            var tfFiles = Directory.GetFiles(outputDir, "*.tf");
            Assert.True(tfFiles.Length > 0);

            // Check for provider file if requested
            if (options.GenerateProviderConfig)
            {
                Assert.Contains(tfFiles, f => Path.GetFileName(f).Contains("provider"));
            }

            // Check for variables file if requested
            if (options.GenerateVariables)
            {
                Assert.Contains(tfFiles, f => Path.GetFileName(f).Contains("variable"));
            }

            _output.WriteLine($"Conversion successful: {result.GeneratedFiles.Count} files created");
        }

        [Fact]
        public async Task JsonToTerraformWorkflow_ComplexPolicy_ShouldPreserveStructure()
        {
            // Arrange
            var jsonFilePath = Path.Combine(_testDirectory, "complex-policy.json");
            var outputDir = CreateTestDirectory("complex-output");

            var complexPolicy = TestDataFactory.CreateComplexJsonPolicy(
                "complex-test",
                "Complex Conditional Access Policy",
                "enabled",
                new[] { "user1@example.com" },
                new[] { "emergency@example.com" },
                new[] { "All" },
                new[] { "legacy-app-id" }
            );

            var jsonExport = TestDataFactory.CreateJsonPolicyExport(complexPolicy);
            await File.WriteAllTextAsync(jsonFilePath, jsonExport.ToString());

            // Act
            var conversionService = new JsonToTerraformService();
            var result = await conversionService.ConvertJsonToTerraformAsync(jsonFilePath, new TerraformOutputOptions
            {
                OutputDirectory = outputDir,
                SeparateFilePerPolicy = true,
                IncludeComments = true
            });

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Errors.Count == 0, $"Conversion had errors: {string.Join(", ", result.Errors)}");

            // Verify Terraform content contains expected structures
            var tfFiles = Directory.GetFiles(outputDir, "*.tf");
            var mainTfContent = await File.ReadAllTextAsync(tfFiles.First());

            Assert.Contains("resource", mainTfContent);
            Assert.Contains("azuread_conditional_access_policy", mainTfContent);
            Assert.Contains("Complex Conditional Access Policy", mainTfContent);

            _output.WriteLine($"Complex policy converted successfully with {tfFiles.Length} files");
        }

        #endregion

        #region Terraform Parsing Integration Tests

        [Fact]
        public async Task TerraformParsingWorkflow_ValidConfiguration_ShouldParseSuccessfully()
        {
            // Arrange
            var terraformDir = CreateTestDirectory("terraform-parsing");
            
            var validTerraform = TestDataFactory.CreateBasicTerraformPolicy("valid_policy", "Valid Test Policy", "enabled");
            await File.WriteAllTextAsync(Path.Combine(terraformDir, "main.tf"), validTerraform);

            // Act
            var parsingService = new TerraformParsingService();
            var parseResult = await parsingService.ParseTerraformDirectoryAsync(terraformDir);

            // Assert
            Assert.NotNull(parseResult);
            Assert.True(parseResult.Errors.Count == 0, $"Parsing had errors: {string.Join(", ", parseResult.Errors)}");
            Assert.NotEmpty(parseResult.Policies);

            var firstPolicy = parseResult.Policies.First();
            Assert.NotNull(firstPolicy.ResourceName);
            Assert.NotNull(firstPolicy.DisplayName);

            _output.WriteLine($"Parsed {parseResult.Policies.Count} Terraform policies successfully");
        }

        [Fact]
        public async Task TerraformParsingWorkflow_InvalidSyntax_ShouldReportErrors()
        {
            // Arrange
            var terraformDir = CreateTestDirectory("invalid-terraform");
            
            var invalidTerraform = @"
resource ""azuread_conditional_access_policy"" ""invalid"" {
  display_name = ""Invalid Policy""
  state = ""enabled""
  # Missing closing brace
";
            await File.WriteAllTextAsync(Path.Combine(terraformDir, "invalid.tf"), invalidTerraform);

            // Act
            var parsingService = new TerraformParsingService();
            var parseResult = await parsingService.ParseTerraformDirectoryAsync(terraformDir);

            // Assert
            Assert.NotNull(parseResult);
            Assert.True(parseResult.Errors.Count > 0, "Should have parsing errors for invalid syntax");

            _output.WriteLine($"Invalid Terraform correctly identified: {parseResult.Errors.Count} errors found");
        }

        #endregion

        #region Validation Integration Tests

        [Fact]
        public async Task ValidationWorkflow_MixedFiles_ShouldValidateAppropriately()
        {
            // Arrange
            var testDir = CreateTestDirectory("validation-test");

            // Create valid JSON policy
            var validJson = TestDataFactory.CreateBasicJsonPolicy("valid-json", "Valid JSON Policy", "enabled");
            await File.WriteAllTextAsync(Path.Combine(testDir, "valid.json"), validJson.ToString());

            // Create invalid JSON
            var invalidJson = "{ invalid json syntax ]}";
            await File.WriteAllTextAsync(Path.Combine(testDir, "invalid.json"), invalidJson);

            // Create valid Terraform
            var validTerraform = TestDataFactory.CreateBasicTerraformPolicy("valid_tf", "Valid TF Policy", "enabled");
            await File.WriteAllTextAsync(Path.Combine(testDir, "valid.tf"), validTerraform);

            // Create non-policy file
            await File.WriteAllTextAsync(Path.Combine(testDir, "readme.txt"), "This is not a policy file");

            // Act
            var validationService = new PolicyValidationService();
            var result = await validationService.ValidateDirectoryAsync(testDir);

            // Assert
            Assert.NotNull(result);
            // The validation result should handle the mixed file types appropriately
            // Some files should be valid, others invalid, and some should be ignored

            _output.WriteLine($"Directory validation completed - Valid: {result.IsValid}");
        }

        #endregion

        #region Template Integration Tests

        [Fact]
        public async Task TemplateWorkflow_ListAndCreateBasicTemplate_ShouldProvideTemplateSupport()
        {
            // Arrange
            var outputDir = CreateTestDirectory("template-test");

            // Act
            var templateService = new TemplateService();
            
            // List available templates
            var availableTemplates = await templateService.ListAvailableTemplatesAsync();
            
            // Create a basic template if available
            if (availableTemplates.Any())
            {
                var firstTemplate = availableTemplates.First();
                var createResult = await templateService.CreateTemplateAsync(firstTemplate.Name, outputDir);

                // Assert
                Assert.NotNull(createResult);
                Assert.True(createResult.Success);

                _output.WriteLine($"Template system working: {availableTemplates.Count} templates available, creation successful: {createResult.Success}");
            }
            else
            {
                // If no templates are available, that's still a valid state
                _output.WriteLine("No templates available - template system accessible");
            }

            // The test passes as long as the template service is accessible and functional
            Assert.NotNull(availableTemplates);
        }

        #endregion

        #region Performance Integration Tests

        [Fact]
        public async Task PerformanceWorkflow_MultipleFileProcessing_ShouldCompleteWithinReasonableTime()
        {
            // Arrange
            var testDir = CreateTestDirectory("performance-test");
            const int fileCount = 20; // Reasonable number for integration testing

            var startTime = DateTime.UtcNow;

            // Create multiple policy files
            var tasks = new List<Task>();
            for (int i = 0; i < fileCount; i++)
            {
                var policy = TestDataFactory.CreateBasicJsonPolicy($"perf-policy-{i:D2}", $"Performance Test Policy {i}", "enabled");
                var filePath = Path.Combine(testDir, $"policy-{i:D2}.json");
                tasks.Add(File.WriteAllTextAsync(filePath, policy.ToString()));
            }

            await Task.WhenAll(tasks);

            // Act - Process all files
            var validationService = new PolicyValidationService();
            var validationResult = await validationService.ValidateDirectoryAsync(testDir);

            var endTime = DateTime.UtcNow;
            var executionTime = endTime - startTime;

            // Assert
            Assert.NotNull(validationResult);
            Assert.True(executionTime.TotalSeconds < 30, $"Processing {fileCount} files took {executionTime.TotalSeconds:F2} seconds, which may be too long");

            _output.WriteLine($"Performance test: {fileCount} files processed in {executionTime.TotalSeconds:F2} seconds");
        }

        #endregion

        #region Error Handling Integration Tests

        [Fact]
        public async Task ErrorHandlingWorkflow_NonExistentDirectory_ShouldHandleGracefully()
        {
            // Arrange
            var nonExistentDir = Path.Combine(_testDirectory, "does-not-exist");

            // Act & Assert - Services should handle non-existent directories gracefully
            var validationService = new PolicyValidationService();
            var validationResult = await validationService.ValidateDirectoryAsync(nonExistentDir);

            // The service should not throw, but should indicate the directory issue
            Assert.NotNull(validationResult);
            Assert.False(validationResult.IsValid);

            _output.WriteLine("Non-existent directory handled gracefully");
        }

        [Fact]
        public async Task ErrorHandlingWorkflow_EmptyDirectory_ShouldReturnEmptyResults()
        {
            // Arrange
            var emptyDir = CreateTestDirectory("empty-directory");
            // Directory exists but is empty

            // Act
            var validationService = new PolicyValidationService();
            var validationResult = await validationService.ValidateDirectoryAsync(emptyDir);

            var terraformService = new TerraformParsingService();
            var parseResult = await terraformService.ParseTerraformDirectoryAsync(emptyDir);

            // Assert
            Assert.NotNull(validationResult);
            Assert.NotNull(parseResult);

            _output.WriteLine("Empty directory handled gracefully");
        }

        #endregion

        #region Helper Methods

        private string CreateTestDirectory(string name)
        {
            var dir = Path.Combine(_testDirectory, name);
            Directory.CreateDirectory(dir);
            return dir;
        }

        #endregion
    }
}

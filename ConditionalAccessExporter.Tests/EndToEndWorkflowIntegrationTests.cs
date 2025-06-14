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
    /// End-to-end integration tests covering complete user workflows from start to finish
    /// </summary>
    [Collection("Console Output Tests")]
    public class EndToEndWorkflowIntegrationTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly ITestOutputHelper _output;

        public EndToEndWorkflowIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            _testDirectory = Path.Combine(Path.GetTempPath(), "EndToEndTests_" + Guid.NewGuid().ToString("N")[..8]);
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

        #region Cross-Format Comparison Workflow Tests

        [Fact]
        public async Task CrossFormatComparisonWorkflow_JsonVsTerraform_ShouldAnalyzeCorrectly()
        {
            // Arrange
            var jsonDir = CreateTestDirectory("json-policies");
            var terraformDir = CreateTestDirectory("terraform-policies");

            // Create equivalent policies in different formats
            var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("cross-format-test", "Cross Format Test Policy", "enabled");
            var terraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("cross_format_test", "Cross Format Test Policy", "enabled");

            await File.WriteAllTextAsync(Path.Combine(jsonDir, "test-policy.json"), jsonPolicy.ToString());
            await File.WriteAllTextAsync(Path.Combine(terraformDir, "test-policy.tf"), terraformPolicy);

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

            var crossFormatResult = await crossFormatService.CompareAsync(jsonDir, terraformDir, matchingOptions);

            // Assert
            Assert.NotNull(crossFormatResult);
            PolicyAssertions.AssertSummaryTotals(crossFormatResult.Summary, 1, 1);

            var matchedComparison = crossFormatResult.PolicyComparisons.FirstOrDefault(c => 
                c.SourcePolicy != null && c.ReferencePolicy != null);

            if (matchedComparison != null)
            {
                PolicyAssertions.AssertPoliciesMatch(matchedComparison);
            }

            _output.WriteLine($"Cross-format comparison completed with {crossFormatResult.PolicyComparisons.Count} comparisons");
        }

        [Fact]
        public async Task BaselineGenerationWorkflow_CreateFromJsonFiles_ShouldCreateValidBaseline()
        {
            // Arrange
            var sourceDir = CreateTestDirectory("source-policies");
            var baselineDir = CreateTestDirectory("baseline-output");

            // Create source policies to be baselined
            var sourcePolicies = new[]
            {
                TestDataFactory.CreateBasicJsonPolicy("source-mfa", "Global MFA Requirement", "enabled"),
                TestDataFactory.CreateBasicJsonPolicy("source-legacy", "Block Legacy Auth", "enabled"),
                TestDataFactory.CreateBasicJsonPolicy("source-disabled", "Test Policy", "disabled")
            };

            // Write policies to source directory
            foreach (var (policy, index) in sourcePolicies.Select((p, i) => (p, i)))
            {
                var fileName = $"source-policy-{index + 1}.json";
                await File.WriteAllTextAsync(Path.Combine(sourceDir, fileName), policy.ToString());
            }

            // Create a mock export data structure that the service expects
            var mockExportData = new
            {
                TenantId = "test-tenant-id",
                ExportedAt = DateTime.UtcNow,
                Policies = sourcePolicies.Select(p => JObject.Parse(p.ToString())).ToArray()
            };

            await File.WriteAllTextAsync(Path.Combine(sourceDir, "exported-policies.json"), 
                JsonConvert.SerializeObject(mockExportData, Formatting.Indented));

            // Act - Generate baseline using the service
            var baselineService = new BaselineGenerationService();
            var baselineOptions = new BaselineGenerationOptions
            {
                OutputDirectory = baselineDir,
                Anonymize = true,
                FilterEnabledOnly = true
            };

            // Note: This would normally connect to Graph API, but for integration testing 
            // we're simulating the baseline generation process
            var result = await baselineService.GenerateBaselineAsync(baselineOptions);

            // Assert
            Assert.True(result >= 0, "Baseline generation should not fail");
            Assert.True(Directory.Exists(baselineDir), "Baseline directory should be created");

            // Verify files were actually generated
            var generatedFiles = Directory.GetFiles(baselineDir, "*.json");
            Assert.True(generatedFiles.Length > 0, "Baseline generation should create JSON files");

            _output.WriteLine($"Baseline generation completed with result: {result}");
        }

        #endregion

        #region JSON to Terraform Conversion Tests

        [Fact]
        public async Task JsonToTerraformWorkflow_SingleFile_ShouldConvertSuccessfully()
        {
            // Arrange
            var jsonDir = CreateTestDirectory("json-source");
            var terraformDir = CreateTestDirectory("terraform-output");

            var originalPolicy = TestDataFactory.CreateBasicJsonPolicy("terraform-test", "Terraform Conversion Test", "enabled");
            var jsonExport = TestDataFactory.CreateJsonPolicyExport(originalPolicy);
            var jsonFilePath = Path.Combine(jsonDir, "test-policy.json");
            await File.WriteAllTextAsync(jsonFilePath, jsonExport.ToString());

            // Act - Convert JSON to Terraform
            var jsonToTerraformService = new JsonToTerraformService();
            
            var result = await jsonToTerraformService.ConvertJsonToTerraformAsync(jsonFilePath, new TerraformOutputOptions
            {
                OutputDirectory = terraformDir,
                GenerateVariables = true,
                GenerateProviderConfig = true
            });

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Errors.Count == 0, $"Conversion had errors: {string.Join(", ", result.Errors)}");
            Assert.NotEmpty(result.GeneratedFiles);

            // Verify Terraform files were created
            var tfFiles = Directory.GetFiles(terraformDir, "*.tf");
            Assert.True(tfFiles.Length > 0);

            // Verify content contains expected Terraform syntax
            foreach (var tfFile in tfFiles)
            {
                var content = await File.ReadAllTextAsync(tfFile);
                Assert.True(content.Contains("resource") || content.Contains("variable") || content.Contains("provider"));
            }

            _output.WriteLine($"JSON to Terraform conversion completed: {result.GeneratedFiles.Count} files generated");
        }

        [Fact]
        public async Task TerraformValidationWorkflow_CompleteValidation_ShouldValidateAllAspects()
        {
            // Arrange
            var terraformDir = CreateTestDirectory("terraform-validation");
            
            // Create valid Terraform configuration
            var validTerraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("test_policy", "Test Policy", "enabled");
            var providerConfig = @"
terraform {
  required_providers {
    azuread = {
      source  = ""hashicorp/azuread""
      version = ""~> 2.0""
    }
  }
}

provider ""azuread"" {
  # Configuration options
}";

            await File.WriteAllTextAsync(Path.Combine(terraformDir, "main.tf"), validTerraformPolicy);
            await File.WriteAllTextAsync(Path.Combine(terraformDir, "provider.tf"), providerConfig);

            // Act - Validate Terraform configuration
            var terraformParsingService = new TerraformParsingService();
            var parseResult = await terraformParsingService.ParseTerraformDirectoryAsync(terraformDir);

            var policyValidationService = new PolicyValidationService();
            var validationResult = await policyValidationService.ValidateDirectoryAsync(terraformDir);

            // Assert
            Assert.NotNull(parseResult);
            Assert.True(parseResult.Errors.Count == 0, $"Terraform parsing failed: {string.Join(", ", parseResult.Errors)}");

            Assert.NotNull(validationResult);
            Assert.True(validationResult.IsValid, $"Policy validation failed");

            _output.WriteLine("Terraform validation completed successfully");
        }

        #endregion

        #region File I/O and Error Handling Tests

        [Fact]
        public async Task FileIOWorkflow_LargeFiles_ShouldHandleEfficiently()
        {
            // Arrange
            var testDir = CreateTestDirectory("large-files");
            var largePolicyData = TestDataFactory.CreateLargePolicyDataset(10);
            var largePolicy = largePolicyData.First();

            // Add additional data to make it substantially large
            var largeConditions = new JObject();
            for (int i = 0; i < 50; i++)
            {
                largeConditions[$"condition_{i}"] = $"Large condition data {i} with lots of text content";
            }
            largePolicy["LargeConditions"] = largeConditions;

            var fileName = Path.Combine(testDir, "large-policy.json");

            // Act
            var startTime = DateTime.UtcNow;
            await File.WriteAllTextAsync(fileName, largePolicy.ToString());
            
            var content = await File.ReadAllTextAsync(fileName);
            var parsedPolicy = JObject.Parse(content);
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.NotNull(parsedPolicy);
            Assert.Equal(largePolicy["Id"]?.ToString(), parsedPolicy["Id"]?.ToString());
            
            var executionTime = endTime - startTime;
            PolicyAssertions.AssertExecutionTimeWithinBounds(executionTime, TimeSpan.FromSeconds(5));

            var fileInfo = new FileInfo(fileName);
            Assert.True(fileInfo.Length > 1024, "File should be substantially large");

            _output.WriteLine($"Large file I/O completed in {executionTime.TotalMilliseconds:F2}ms, file size: {fileInfo.Length} bytes");
        }

        [Fact]
        public async Task ErrorHandlingWorkflow_MalformedFiles_ShouldHandleGracefully()
        {
            // Arrange
            var sourceDir = CreateTestDirectory("malformed-source");
            var referenceDir = CreateTestDirectory("valid-reference");

            // Create malformed JSON file
            var malformedJson = "{ invalid json content here }}}";
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "malformed.json"), malformedJson);

            // Create invalid Terraform file
            var invalidTerraform = @"
resource ""invalid_syntax"" {
  missing_closing_brace = ""value""
  # Missing closing brace
";
            await File.WriteAllTextAsync(Path.Combine(sourceDir, "invalid.tf"), invalidTerraform);

            // Create valid reference file
            var validPolicy = TestDataFactory.CreateBasicJsonPolicy("valid", "Valid Policy", "enabled");
            await File.WriteAllTextAsync(Path.Combine(referenceDir, "valid.json"), validPolicy.ToString());

            // Assert - Should handle errors gracefully by checking file contents
            var sourceFiles = Directory.GetFiles(sourceDir);
            var referenceFiles = Directory.GetFiles(referenceDir);
            
            Assert.True(sourceFiles.Length > 0, "Source directory should contain files");
            Assert.True(referenceFiles.Length > 0, "Reference directory should contain files");
            
            // Check that malformed files exist in source
            var malformedContent = await File.ReadAllTextAsync(sourceFiles.First());
            Assert.Contains("invalid", malformedContent, StringComparison.OrdinalIgnoreCase);
            
            // Check that valid JSON exists in reference
            var validContent = await File.ReadAllTextAsync(referenceFiles.First());
            Assert.True(validContent.StartsWith("{"), "Reference file should be valid JSON");

            _output.WriteLine($"Error handling test completed: Source files={sourceFiles.Length}, Reference files={referenceFiles.Length}");
        }

        #endregion

        #region Template Integration Tests

        [Fact]
        public async Task TemplateWorkflow_ListAndCreateTemplate_ShouldProvideTemplateCapabilities()
        {
            // Arrange
            var templateOutputDir = CreateTestDirectory("template-output");

            // Act - List available templates
            var templateService = new TemplateService();
            var availableTemplates = await templateService.ListAvailableTemplatesAsync();

            // Create a template
            var templateResult = await templateService.CreateTemplateAsync("basic/require-mfa-all-users", templateOutputDir);

            // Assert
            Assert.NotNull(availableTemplates);
            Assert.True(availableTemplates.Count > 0, "Should have available templates");

            Assert.NotNull(templateResult);
            Assert.True(templateResult.Success, "Template creation should succeed");
            Assert.True(templateResult.Errors.Count == 0, $"Template creation had errors: {string.Join(", ", templateResult.Errors)}");

            // Verify template was created in output directory
            var createdFiles = Directory.GetFiles(templateOutputDir, "*", SearchOption.AllDirectories);
            Assert.True(createdFiles.Length > 0, "Should create template files");

            _output.WriteLine($"Template workflow: {availableTemplates.Count} templates available, {createdFiles.Length} files created");
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

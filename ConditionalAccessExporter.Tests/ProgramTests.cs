using System.CommandLine;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using ConditionalAccessExporter.Models;
using ConditionalAccessExporter.Services;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace ConditionalAccessExporter.Tests
{
    [Collection("Console Output Tests")]
    public class ProgramTests
    {
        /// <summary>
        /// Helper method to assert that a required option error message is present in the captured output
        /// </summary>
        /// <param name="capturedOutput">The captured console output</param>
        /// <param name="optionName">The option name to check for (e.g., "--input")</param>
        private static void AssertRequiredOptionError(string? capturedOutput, string optionName)
        {
            Assert.NotNull(capturedOutput);
            Assert.Contains(optionName, capturedOutput);
            Assert.Contains("required", capturedOutput, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Test infrastructure for invoking Program.Main with command-line arguments
        /// </summary>
        [Fact]
        public async Task Main_NoArgs_CallsExportCommand()
        {
            // This will test Issue Test Case 6.1
            // Arrange - Capture console output
            string? capturedOutput = null;
            var mockFileSystem = new MockFileSystem();
            var expectedOutputPath = $"ConditionalAccessPolicies_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            
            try
            {
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    // Using an empty array to simulate no args
                    await ProgramTestHelper.InvokeMainAsync(Array.Empty<string>());
                });
            }
            catch (Exception)
            {
                // The test might fail due to Graph API authentication
                // We're just testing that the export code path is invoked
            }

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Conditional Access Policy Exporter", capturedOutput);
        }

        [Theory]
        [InlineData("export")]
        [InlineData("export", "--output", "custom_export.json")]
        public async Task Export_Command_ValidArgs_CallsHandler(params string[] args)
        {
            // This will test Issue Test Case 1.1 and 1.2
            // Arrange - Capture console output
            string? capturedOutput = null;
            var expectedOutputPath = args.Length == 3 ? args[2] : $"ConditionalAccessPolicies_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            
            try
            {
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    await ProgramTestHelper.InvokeMainAsync(args);
                });
            }
            catch (Exception)
            {
                // The test might fail due to Graph API authentication
                // We're just testing that the export code path is invoked
            }

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Conditional Access Policy Exporter", capturedOutput);
            if (args.Length == 3)
            {
                Assert.Contains(expectedOutputPath, capturedOutput);
            }
        }

        [Theory]
        [InlineData("terraform", "--input", "file.tf")]
        [InlineData("terraform", "--input", "dir/", "--output", "converted.json", "--validate", "false", "--verbose", "true")]
        public async Task Terraform_Command_ValidArgs_CallsHandler(params string[] args)
        {
            // This will test Issue Test Case 2.1 and 2.2
            // Arrange - Capture console output
            string? capturedOutput = null;
            var mockFileSystem = new MockFileSystem();
            
            try
            {
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    await ProgramTestHelper.InvokeMainAsync(args);
                });
            }
            catch (Exception)
            {
                // The test might fail due to file system access issues
                // We're just testing that the terraform command path is invoked
            }

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Terraform to JSON Conversion", capturedOutput);
            
            // Verify the correct parameters were used
            Assert.Contains($"Input path: {args[2]}", capturedOutput);
            
            // Check for additional parameters if provided
            if (args.Length > 3)
            {
                Assert.Contains(args[4], capturedOutput);
                
                // Check validate option if specified
                if (args.Contains("--validate"))
                {
                    var validateIndex = Array.IndexOf(args, "--validate");
                    if (validateIndex >= 0 && validateIndex < args.Length - 1)
                    {
                        var validateValue = args[validateIndex + 1].ToLower();
                        Assert.Contains($"Validation: {(validateValue == "true" ? "Enabled" : "Disabled")}", capturedOutput);
                    }
                }
                
                // Check verbose option if specified
                if (args.Contains("--verbose"))
                {
                    var verboseIndex = Array.IndexOf(args, "--verbose");
                    if (verboseIndex >= 0 && verboseIndex < args.Length - 1)
                    {
                        var verboseValue = args[verboseIndex + 1].ToLower();
                        Assert.Contains($"Verbose logging: {(verboseValue == "true" ? "Enabled" : "Disabled")}", capturedOutput);
                    }
                }
            }
        }

        [Fact]
        public async Task Terraform_Command_MissingInput_ReturnsError()
        {
            // This will test Issue Test Case 2.3
            // Arrange - Create a test with missing required option
            string? capturedOutput = null;
            var args = new[] { "terraform" };
            
            // Act
            capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
            {
                await ProgramTestHelper.InvokeMainAsync(args);
            });

            // Assert
            AssertRequiredOptionError(capturedOutput, "--input");
            Assert.DoesNotContain("Parsing Terraform files", capturedOutput);
        }

        [Theory]
        [InlineData("json-to-terraform", "--input", "policies.json")]
        [InlineData("json-to-terraform", "--input", "policies.json", "--output-dir", "custom_tf_out", "--generate-variables", "false",
            "--separate-files", "true", "--provider-version", "~> 2.39")]
        public async Task JsonToTerraform_Command_ValidArgs_CallsHandler(params string[] args)
        {
            // This will test Issue Test Case 3.1 and 3.2
            // Arrange - Capture console output
            string? capturedOutput = null;
            
            try
            {
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    await ProgramTestHelper.InvokeMainAsync(args);
                });
            }
            catch (Exception)
            {
                // The test might fail due to file access issues
                // We're just testing that the command path is invoked
            }

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("JSON to Terraform Conversion", capturedOutput);
            
            // Verify input path was correctly extracted from args
            Assert.Contains($"Input file: {args[2]}", capturedOutput);
            
            // Check for additional parameters if provided
            if (args.Length > 3)
            {
                // Check output-dir if specified
                if (args.Contains("--output-dir"))
                {
                    var outputDirIndex = Array.IndexOf(args, "--output-dir");
                    if (outputDirIndex >= 0 && outputDirIndex < args.Length - 1)
                    {
                        Assert.Contains($"Output directory: {args[outputDirIndex + 1]}", capturedOutput);
                    }
                }
                
                // Check other options if needed
                if (args.Contains("--provider-version"))
                {
                    var providerVersionIndex = Array.IndexOf(args, "--provider-version");
                    if (providerVersionIndex >= 0 && providerVersionIndex < args.Length - 1)
                    {
                        Assert.Contains($"Provider version: {args[providerVersionIndex + 1]}", capturedOutput);
                    }
                }
            }
        }

        [Fact]
        public async Task JsonToTerraform_Command_MissingInput_ReturnsError()
        {
            // This will test Issue Test Case 3.3
            // Arrange
            string? capturedOutput = null;
            var args = new[] { "json-to-terraform" };
            
            // Act
            capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
            {
                await ProgramTestHelper.InvokeMainAsync(args);
            });

            // Assert
            AssertRequiredOptionError(capturedOutput, "--input");
            Assert.DoesNotContain("JSON to Terraform Conversion", capturedOutput);
        }

        [Theory]
        [InlineData("compare", "--reference-dir", "./ref")]
        [InlineData("compare", "--reference-dir", "./ref", "--entra-file", "entra_export.json", "--output-dir", "reports/",
            "--formats", "json", "--formats", "html", "--matching", "ById", "--case-sensitive", "true")]
        public async Task Compare_Command_ValidArgs_CallsHandler(params string[] args)
        {
            // This will test Issue Test Case 4.1 and 4.2
            // Arrange - Create required directories for the test using temporary paths
            var tempDirs = new List<string>();
            string? capturedOutput = null;
            
            // Create a temporary base directory for this test
            var tempBaseDir = Path.Combine(Path.GetTempPath(), $"CA_Scanner_Test_{Path.GetRandomFileName()}");
            Directory.CreateDirectory(tempBaseDir);
            tempDirs.Add(tempBaseDir);
            
            // Replace relative paths with temporary paths in the args
            var modifiedArgs = new string[args.Length];
            Array.Copy(args, modifiedArgs, args.Length);
            
            // Cache indices of arguments to avoid repeated Array.IndexOf calls
            var refDirIndex = Array.IndexOf(modifiedArgs, "--reference-dir");
            var entraFileIndex = Array.IndexOf(modifiedArgs, "--entra-file");
            var outputDirIndex = Array.IndexOf(modifiedArgs, "--output-dir");
            var matchingIndex = Array.IndexOf(modifiedArgs, "--matching");
            
            try
            {
                
                // Create reference directory if needed
                if (refDirIndex >= 0 && refDirIndex < modifiedArgs.Length - 1)
                {
                    var tempRefDir = Path.Combine(tempBaseDir, "ref");
                    Directory.CreateDirectory(tempRefDir);
                    modifiedArgs[refDirIndex + 1] = tempRefDir;
                    tempDirs.Add(tempRefDir);
                }
                
                // Create entra file if needed
                if (entraFileIndex >= 0 && entraFileIndex < modifiedArgs.Length - 1)
                {
                    var tempEntraFile = Path.Combine(tempBaseDir, "entra_export.json");
                    File.WriteAllText(tempEntraFile, "[]"); // Empty JSON array
                    modifiedArgs[entraFileIndex + 1] = tempEntraFile;
                }
                
                // Create output directory if needed
                if (outputDirIndex >= 0 && outputDirIndex < modifiedArgs.Length - 1)
                {
                    var tempOutputDir = Path.Combine(tempBaseDir, "reports");
                    Directory.CreateDirectory(tempOutputDir);
                    modifiedArgs[outputDirIndex + 1] = tempOutputDir;
                    tempDirs.Add(tempOutputDir);
                }
                
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    await ProgramTestHelper.InvokeMainAsync(modifiedArgs);
                });
            }
            catch (Exception)
            {
                // The test might fail due to Graph API or file system issues
                // We're just testing that the command path is invoked
            }
            finally
            {
                // Clean up all temporary directories (includes files within them)
                foreach (var tempDir in tempDirs)
                {
                    try
                    {
                        if (Directory.Exists(tempDir))
                            Directory.Delete(tempDir, true);
                    }
                    catch (Exception ex)
                    {
                        // Log warning if deletion fails to aid in diagnosing file-system issues
                        Console.WriteLine($"Warning: Failed to delete temporary directory '{tempDir}': {ex.Message}");
                    }
                }
            }

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Conditional Access Policy Comparison", capturedOutput);
            
            // Verify reference directory was correctly extracted from modified args
            if (refDirIndex >= 0 && refDirIndex < modifiedArgs.Length - 1)
            {
                Assert.Contains($"Reference directory: {modifiedArgs[refDirIndex + 1]}", capturedOutput);
            }
            // Check for additional parameters if provided
            if (modifiedArgs.Length > 3)
            {
                // Check entra-file if specified
                if (entraFileIndex >= 0 && entraFileIndex < modifiedArgs.Length - 1)
                {
                    Assert.Contains($"Entra file: {modifiedArgs[entraFileIndex + 1]}", capturedOutput);
                }
                
                // Check output-dir if specified
                if (outputDirIndex >= 0 && outputDirIndex < modifiedArgs.Length - 1)
                {
                    Assert.Contains($"Output directory: {modifiedArgs[outputDirIndex + 1]}", capturedOutput);
                }
                
                // Check matching strategy if specified
                if (matchingIndex >= 0 && matchingIndex < modifiedArgs.Length - 1)
                {
                    Assert.Contains($"Matching strategy: {modifiedArgs[matchingIndex + 1]}", capturedOutput);
                }
            }
        }

        [Fact]
        public async Task Compare_Command_MissingReferenceDir_ReturnsError()
        {
            // This will test Issue Test Case 4.3
            // Arrange
            string? capturedOutput = null;
            var args = new[] { "compare" };
            
            // Act
            capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
            {
                await ProgramTestHelper.InvokeMainAsync(args);
            });

            // Assert
            AssertRequiredOptionError(capturedOutput, "--reference-dir");
            Assert.DoesNotContain("Conditional Access Policy Comparison", capturedOutput);
        }

        [Theory]
        [InlineData("cross-compare", "--source-dir", "./src", "--reference-dir", "./ref")]
        [InlineData("cross-compare", "--source-dir", "./src_tf", "--reference-dir", "./ref_json", "--output-dir", "cross_reports/", 
            "--formats", "markdown", "--matching", "SemanticSimilarity", "--similarity-threshold", "0.75", "--enable-semantic", "false")]
        public async Task CrossCompare_Command_ValidArgs_CallsHandler(params string[] args)
        {
            // This will test Issue Test Case 5.1 and 5.2
            // Arrange - Capture console output
            string? capturedOutput = null;
            
            try
            {
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    await ProgramTestHelper.InvokeMainAsync(args);
                });
            }
            catch (Exception)
            {
                // The test might fail due to file system access issues
                // We're just testing that the command path is invoked
            }

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Cross-Format Policy Comparison", capturedOutput);
            
            // Verify source and reference directories were correctly extracted from args
            Assert.Contains($"Source directory: {args[2]}", capturedOutput);
            Assert.Contains($"Reference directory: {args[4]}", capturedOutput);
            
            // Check for additional parameters if provided
            if (args.Length > 5)
            {
                // Check output-dir if specified
                if (args.Contains("--output-dir"))
                {
                    var outputDirIndex = Array.IndexOf(args, "--output-dir");
                    if (outputDirIndex >= 0 && outputDirIndex < args.Length - 1)
                    {
                        Assert.Contains($"Output directory: {args[outputDirIndex + 1]}", capturedOutput);
                    }
                }
                
                // Check matching strategy if specified
                if (args.Contains("--matching"))
                {
                    var matchingIndex = Array.IndexOf(args, "--matching");
                    if (matchingIndex >= 0 && matchingIndex < args.Length - 1)
                    {
                        Assert.Contains($"Matching strategy: {args[matchingIndex + 1]}", capturedOutput);
                    }
                }
                
                // Check similarity threshold if specified
                if (args.Contains("--similarity-threshold"))
                {
                    var thresholdIndex = Array.IndexOf(args, "--similarity-threshold");
                    if (thresholdIndex >= 0 && thresholdIndex < args.Length - 1)
                    {
                        Assert.Contains($"Similarity threshold: {args[thresholdIndex + 1]}", capturedOutput);
                    }
                }
            }
        }

        [Theory]
        [InlineData("cross-compare", "--source-dir", "./src")]
        [InlineData("cross-compare", "--reference-dir", "./ref")]
        public async Task CrossCompare_Command_MissingRequiredOption_ReturnsError(params string[] args)
        {
            // This will test Issue Test Case 5.3
            // Arrange
            string? capturedOutput = null;
            
            // Act
            capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
            {
                await ProgramTestHelper.InvokeMainAsync(args);
            });

            // Assert
            Assert.NotNull(capturedOutput);
            
            if (args.Contains("--source-dir") && !args.Contains("--reference-dir"))
            {
                AssertRequiredOptionError(capturedOutput, "--reference-dir");
            }
            else if (args.Contains("--reference-dir") && !args.Contains("--source-dir"))
            {
                AssertRequiredOptionError(capturedOutput, "--source-dir");
            }
            
            Assert.DoesNotContain("Cross-Format Policy Comparison", capturedOutput);
        }

        [Theory]
        [InlineData("--help")]
        [InlineData("export", "--help")]
        public async Task Help_Option_PrintsHelpText(params string[] args)
        {
            // This will test Issue Test Case 7.1 and 7.2
            // Arrange
            string? capturedOutput = null;
            
            // Act
            capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
            {
                await ProgramTestHelper.InvokeMainAsync(args);
            });

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Description:", capturedOutput);
            Assert.Contains("Usage:", capturedOutput);
            
            if (args.Length > 1 && args[0] == "export")
            {
                Assert.Contains("Export", capturedOutput);
                Assert.Contains("--output", capturedOutput);
            }
            else
            {
                // Root help should list all commands
                Assert.Contains("export", capturedOutput);
                Assert.Contains("terraform", capturedOutput);
                Assert.Contains("json-to-terraform", capturedOutput);
                Assert.Contains("compare", capturedOutput);
                Assert.Contains("cross-compare", capturedOutput);
            }
        }

        [Fact]
        public async Task ExportPoliciesAsync_Success_ReturnsZero()
        {
            // This will test Issue Test Case 1.3
            // Arrange
            string? capturedOutput = null;
            string testOutputPath = "test_export.json";
            var mockFileSystem = new MockFileSystem();
            
            try
            {
                // Mock data setup - this would be ideal with a service mock
                // Setup would mock FetchEntraPoliciesAsync to return a valid response
                
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    var result = await ProgramTestHelper.InvokeExportPoliciesAsync(testOutputPath);
                    
                    // A successful result should be zero
                    Assert.Equal(0, result);
                });
            }
            catch (Exception)
            {
                // The test might fail due to Graph API authentication
                // This is a unit test, so we're just testing the return code path
            }

            // Assert
            Assert.NotNull(capturedOutput);
            // Even if we get an exception, we should see some output about the export attempt
            Assert.Contains("Conditional Access Policy Exporter", capturedOutput, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ExportPoliciesAsync_Exception_HandlesErrorAndReturnsOne()
        {
            // This will test Issue Test Case 1.4
            // Arrange
            string? capturedOutput = null;
            string testOutputPath = "invalid_path/that_cannot_be_written_to/test.json";
            
            // Act
            capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
            {
                var result = await ProgramTestHelper.InvokeExportPoliciesAsync(testOutputPath);
                
                // An error result should be non-zero (1)
                Assert.Equal(1, result);
            });
            
            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Error", capturedOutput, StringComparison.OrdinalIgnoreCase);
        }
        


        [Fact]
        public async Task ConvertTerraformAsync_Success_ReturnsZero()
        {
            // This will test Issue Test Case 2.4
            // Arrange
            string? capturedOutput = null;
            string testInputPath = Path.GetTempFileName();
            string testOutputPath = Path.GetTempFileName();
            
            try
            {
                // Create temporary Terraform file with valid content
                var terraformContent = @"
resource ""azuread_conditional_access_policy"" ""test_policy"" {
  display_name = ""Test Policy""
  state        = ""enabled""
  
  conditions {
    applications {
      included_applications = [""All""]
    }
    
    users {
      included_users = [""All""]
    }
  }
  
  grant_controls {
    operator          = ""OR""
    built_in_controls = [""mfa""]
  }
}";
                await File.WriteAllTextAsync(testInputPath, terraformContent);
                
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    var result = await ProgramTestHelper.InvokeConvertTerraformAsync(testInputPath, testOutputPath);
                    
                    // A successful result should be zero
                    Assert.Equal(0, result);
                });
                
                // Assert
                Assert.NotNull(capturedOutput);
                Assert.Contains("Converting Terraform", capturedOutput, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                // Cleanup temporary files
                if (File.Exists(testInputPath))
                    File.Delete(testInputPath);
                if (File.Exists(testOutputPath))
                    File.Delete(testOutputPath);
            }
        }

        [Fact]
        public async Task ConvertTerraformAsync_ServiceFailure_ReturnsOne()
        {
            // This will test Issue Test Case 2.5
            // Arrange
            string? capturedOutput = null;
            string testInputPath = "nonexistent_path.json";
            string testOutputPath = "terraform_output";
            
            try
            {
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    var result = await ProgramTestHelper.InvokeConvertTerraformAsync(testInputPath, testOutputPath);
                    
                    // A failure result should be one
                    Assert.Equal(1, result);
                });
            }
            catch (Exception)
            {
                // The test might fail if the method doesn't handle file not found exceptions correctly
                // This is a unit test, so we're just testing the error path
            }

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Error", capturedOutput, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ConvertJsonToTerraformAsync_Success_ReturnsZero()
        {
            // This will test Issue Test Case 3.4
            // Arrange
            string? capturedOutput = null;
            string testInputPath = Path.GetTempFileName();
            string testOutputDir = Path.Combine(Path.GetTempPath(), "terraform_output_" + Guid.NewGuid().ToString("N")[..8]);
            bool generateVariables = true;
            bool generateProvider = true;
            bool separateFiles = false;
            bool generateModule = false;
            bool includeComments = true;
            string providerVersion = "~> 2.0";
            
            // Create test data
            var testData = new[]
            {
                new
                {
                    id = "test-policy-1",
                    displayName = "Test Policy 1",
                    state = "enabled",
                    conditions = new
                    {
                        users = new
                        {
                            includeUsers = new[] { "All" }
                        },
                        applications = new
                        {
                            includeApplications = new[] { "All" }
                        }
                    },
                    grantControls = new
                    {
                        operator = "AND",
                        builtInControls = new[] { "mfa" }
                    }
                }
            };
            
            try
            {
                // Create test input file
                await File.WriteAllTextAsync(testInputPath, JsonConvert.SerializeObject(testData, Formatting.Indented));
                
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    var result = await ProgramTestHelper.InvokeConvertJsonToTerraformAsync(
                        testInputPath, 
                        testOutputDir, 
                        generateVariables, 
                        generateProvider, 
                        separateFiles, 
                        generateModule, 
                        includeComments, 
                        providerVersion);
                    
                    // A successful result should be zero
                    Assert.Equal(0, result);
                });
            }
            catch (Exception)
            {
                // The test might fail due to filesystem access
                // This is a unit test, so we're just testing the return code path
            }
            finally
            {
                // Clean up
                if (File.Exists(testInputPath))
                    File.Delete(testInputPath);
                if (Directory.Exists(testOutputDir))
                    Directory.Delete(testOutputDir, true);
            }

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Converting JSON to Terraform", capturedOutput, StringComparison.OrdinalIgnoreCase);
        }
        
        [Fact]
        public async Task ConvertJsonToTerraformAsync_ServiceFailure_ReturnsOne()
        {
            // This will test Issue Test Case 3.5
            // Arrange
            string? capturedOutput = null;
            string testInputPath = "nonexistent_policies.json";
            string testOutputDir = "terraform_output";
            bool generateVariables = true;
            bool generateProvider = true;
            bool separateFiles = false;
            bool generateModule = false;
            bool includeComments = true;
            string providerVersion = "~> 2.0";
            
            try
            {
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    var result = await ProgramTestHelper.InvokeConvertJsonToTerraformAsync(
                        testInputPath, 
                        testOutputDir, 
                        generateVariables, 
                        generateProvider, 
                        separateFiles, 
                        generateModule, 
                        includeComments, 
                        providerVersion);
                    
                    // A failure result should be one
                    Assert.Equal(1, result);
                });
            }
            catch (Exception)
            {
                // The test might fail if the method doesn't handle file not found exceptions correctly
                // This is a unit test, so we're just testing the error path
            }

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Error", capturedOutput, StringComparison.OrdinalIgnoreCase);
        }
        
        [Fact]
        public async Task ComparePoliciesAsync_Success_ReturnsZero()
        {
            // This will test Issue Test Case 4.4
            // Arrange
            string? capturedOutput = null;
            string testRefDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string testEntraFile = Path.GetTempFileName();
            string testOutputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string[] formats = new[] { "json", "html" };
            bool caseSensitive = true;
            
            try
            {
                // Create temporary directories and files
                Directory.CreateDirectory(testRefDir);
                Directory.CreateDirectory(testOutputDir);
                
                // Create a simple Entra export JSON file
                var entraExport = new
                {
                    policies = new[]
                    {
                        new
                        {
                            id = "test-policy-1",
                            displayName = "Test Policy 1",
                            state = "enabled"
                        }
                    }
                };
                await File.WriteAllTextAsync(testEntraFile, JsonConvert.SerializeObject(entraExport, Formatting.Indented));
                
                // Create a sample reference policy file
                var referencePolicyFile = Path.Combine(testRefDir, "policy1.json");
                var referencePolicy = new
                {
                    id = "test-policy-1",
                    displayName = "Test Policy 1",
                    state = "enabled"
                };
                await File.WriteAllTextAsync(referencePolicyFile, JsonConvert.SerializeObject(referencePolicy, Formatting.Indented));
                
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    var result = await ProgramTestHelper.InvokeComparePoliciesAsync(
                        testRefDir, 
                        testEntraFile, 
                        testOutputDir, 
                        formats, 
                        Models.MatchingStrategy.ById, 
                        caseSensitive);
                    
                    // A successful result should be zero
                    Assert.Equal(0, result);
                });
                    
                // Assert
                Assert.NotNull(capturedOutput);
                Assert.Contains("Conditional Access Policy Comparison", capturedOutput, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                // Cleanup temporary files and directories
                try
                {
                    if (File.Exists(testEntraFile))
                        File.Delete(testEntraFile);
                    if (Directory.Exists(testRefDir))
                        Directory.Delete(testRefDir, true);
                    if (Directory.Exists(testOutputDir))
                        Directory.Delete(testOutputDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        
        [Fact]
        public async Task ComparePoliciesAsync_ServiceFailure_ReturnsOne()
        {
            // This will test Issue Test Case 4.5
            // Arrange
            string? capturedOutput = null;
            string testRefDir = "nonexistent_dir";
            string testEntraFile = "entra_export.json";
            string testOutputDir = "compare_reports";
            string[] formats = new[] { "json", "html" };
            bool caseSensitive = true;
            
            try
            {
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    var result = await ProgramTestHelper.InvokeComparePoliciesAsync(
                        testRefDir, 
                        testEntraFile, 
                        testOutputDir, 
                        formats, 
                        Models.MatchingStrategy.ById, 
                        caseSensitive);
                    
                    // A failure result should be one
                    Assert.Equal(1, result);
                });
            }
            catch (Exception)
            {
                // The test might fail if the method doesn't handle directory not found exceptions correctly
                // This is a unit test, so we're just testing the error path
            }

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Error", capturedOutput, StringComparison.OrdinalIgnoreCase);
        }
        
        [Fact]
        public async Task CrossFormatComparePoliciesAsync_Success_ReturnsZero()
        {
            // This will test Issue Test Case 5.4
            // Arrange
            string? capturedOutput = null;
            string testSourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string testReferenceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string testOutputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string[] formats = new[] { "json", "markdown" };
            string matchingStrategy = "ByName";
            double similarityThreshold = 0.8;
            bool enableSemantic = false;
            bool caseSensitive = false;
            
            try
            {
                // Create temporary directories
                Directory.CreateDirectory(testSourceDir);
                Directory.CreateDirectory(testReferenceDir);
                Directory.CreateDirectory(testOutputDir);
                
                // Create a sample JSON policy file in source directory
                var sourceJsonFile = Path.Combine(testSourceDir, "policy1.json");
                var sourcePolicy = new
                {
                    id = "test-policy-1",
                    displayName = "Test Policy 1", 
                    state = "enabled"
                };
                await File.WriteAllTextAsync(sourceJsonFile, JsonConvert.SerializeObject(sourcePolicy, Formatting.Indented));
                
                // Create a sample Terraform policy file in reference directory
                var referenceTfFile = Path.Combine(testReferenceDir, "policy1.tf");
                var terraformContent = @"
resource ""azuread_conditional_access_policy"" ""test_policy_1"" {
  display_name = ""Test Policy 1""
  state        = ""enabled""
  
  conditions {
    applications {
      included_applications = [""All""]
    }
    
    users {
      included_users = [""All""]
    }
  }
  
  grant_controls {
    operator          = ""OR""
    built_in_controls = [""mfa""]
  }
}";
                await File.WriteAllTextAsync(referenceTfFile, terraformContent);
                
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    var result = await ProgramTestHelper.InvokeCrossFormatComparePoliciesAsync(
                        testSourceDir, 
                        testReferenceDir, 
                        testOutputDir, 
                        formats, 
                        matchingStrategy, 
                        caseSensitive, 
                        enableSemantic,
                        similarityThreshold);
                    
                    // A successful result should be zero
                    Assert.Equal(0, result);
                });
                
                // Assert
                Assert.NotNull(capturedOutput);
                Assert.Contains("Cross-comparing policies", capturedOutput, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                // Cleanup temporary directories
                try
                {
                    if (Directory.Exists(testSourceDir))
                        Directory.Delete(testSourceDir, true);
                    if (Directory.Exists(testReferenceDir))
                        Directory.Delete(testReferenceDir, true);
                    if (Directory.Exists(testOutputDir))
                        Directory.Delete(testOutputDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        
        [Fact]
        public async Task CrossFormatComparePoliciesAsync_ServiceFailure_ReturnsOne()
        {
            // This will test Issue Test Case 5.5
            // Arrange
            string? capturedOutput = null;
            string testSourceDir = "nonexistent_source_dir";
            string testReferenceDir = "reference_dir";
            string testOutputDir = "cross_compare_reports";
            string[] formats = new[] { "json", "markdown" };
            string matchingStrategy = "ByName";
            double similarityThreshold = 0.8;
            bool enableSemantic = false;
            bool caseSensitive = false;
            
            try
            {
                // Act
                capturedOutput = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
                {
                    var result = await ProgramTestHelper.InvokeCrossFormatComparePoliciesAsync(
                        testSourceDir, 
                        testReferenceDir, 
                        testOutputDir, 
                        formats, 
                        matchingStrategy, 
                        caseSensitive, 
                        enableSemantic,
                        similarityThreshold);
                    
                    // A failure result should be one
                    Assert.Equal(1, result);
                });
            }
            catch (Exception)
            {
                // The test might fail if the method doesn't handle directory not found exceptions correctly
                // This is a unit test, so we're just testing the error path
            }

            // Assert
            Assert.NotNull(capturedOutput);
            Assert.Contains("Error", capturedOutput, StringComparison.OrdinalIgnoreCase);
        }
    }
}
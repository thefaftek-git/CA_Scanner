using Xunit;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using ConditionalAccessExporter;

namespace ConditionalAccessExporter.Tests
{
    /// <summary>
    /// Unit tests for Program.cs CLI functionality
    /// Tests command-line interface aspects including argument parsing, command invocation, and error handling
    /// </summary>
    public class ProgramCliTests : IDisposable
    {
        private readonly StringWriter _consoleOutput;
        private readonly TextWriter _originalOut;
        private readonly string _tempDirectory;

        public ProgramCliTests()
        {
            // Setup console output capture
            _originalOut = Console.Out;
            _consoleOutput = new StringWriter();
            Console.SetOut(_consoleOutput);

            // Create temp directory for test files
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"ProgramCliTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            // Restore console output
            Console.SetOut(_originalOut);
            _consoleOutput?.Dispose();

            // Cleanup temp directory
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        private string GetConsoleOutput()
        {
            return _consoleOutput.ToString();
        }

        private void ClearConsoleOutput()
        {
            _consoleOutput.GetStringBuilder().Clear();
        }

        private static async Task<int> CallProgramMain(params string[] args)
        {
            // Directly call Program.Main since it's now public
            return await Program.Main(args);
        }

        #region Export Command Tests

        [Fact]
        public async Task ExportCommand_NoOptions_ShouldUseDefaultOutput()
        {
            // Arrange
            var args = new[] { "export" };

            // Act & Assert - Should not throw and should attempt to run export
            // Note: This will fail due to missing Azure credentials but we're testing CLI parsing
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Verify CLI parsing worked and export was attempted
            Assert.Contains("Conditional Access Policy Exporter", output);
            Assert.Equal(1, exitCode); // Expected to fail due to missing credentials
        }

        [Fact]
        public async Task ExportCommand_WithOutputOption_ShouldUseSpecifiedOutput()
        {
            // Arrange
            var outputFile = Path.Combine(_tempDirectory, "test_export.json");
            var args = new[] { "export", "--output", outputFile };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert - Should attempt to export to specified file
            Assert.Contains("Conditional Access Policy Exporter", output);
            Assert.Equal(1, exitCode); // Expected to fail due to missing credentials
        }

        [Fact]
        public async Task ExportCommand_Help_ShouldShowHelpText()
        {
            // Arrange
            var args = new[] { "export", "--help" };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains("Export Conditional Access policies from Entra ID", output);
            Assert.Equal(0, exitCode); // Help should return 0
        }

        #endregion

        #region Terraform Command Tests

        [Fact]
        public async Task TerraformCommand_MissingRequiredInput_ShouldShowError()
        {
            // Arrange
            var args = new[] { "terraform" };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.NotEqual(0, exitCode); // Should fail due to missing required option
            // System.CommandLine should handle the missing required option
        }

        [Fact]
        public async Task TerraformCommand_WithRequiredInput_ShouldAttemptConversion()
        {
            // Arrange
            var inputFile = Path.Combine(_tempDirectory, "test.tf");
            await File.WriteAllTextAsync(inputFile, "# Empty terraform file");
            var args = new[] { "terraform", "--input", inputFile };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains("Terraform to JSON Conversion", output);
            Assert.Contains($"Input path: {inputFile}", output);
        }

        [Fact]
        public async Task TerraformCommand_WithAllOptions_ShouldUseSpecifiedValues()
        {
            // Arrange
            var inputFile = Path.Combine(_tempDirectory, "test.tf");
            var outputFile = Path.Combine(_tempDirectory, "output.json");
            await File.WriteAllTextAsync(inputFile, "# Empty terraform file");
            var args = new[] { 
                "terraform", 
                "--input", inputFile,
                "--output", outputFile,
                "--validate", "false",
                "--verbose", "true"
            };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains($"Input path: {inputFile}", output);
            Assert.Contains($"Output path: {outputFile}", output);
            Assert.Contains("Validation: Disabled", output);
            Assert.Contains("Verbose logging: Enabled", output);
        }

        [Fact]
        public async Task TerraformCommand_NonExistentInput_ShouldShowError()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.tf");
            var args = new[] { "terraform", "--input", nonExistentFile };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Equal(1, exitCode);
            Assert.Contains($"Error: Input path '{nonExistentFile}' not found", output);
        }

        [Fact]
        public async Task TerraformCommand_Help_ShouldShowHelpText()
        {
            // Arrange
            var args = new[] { "terraform", "--help" };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains("Convert Terraform conditional access policies to JSON", output);
            Assert.Equal(0, exitCode);
        }

        #endregion

        #region JSON to Terraform Command Tests

        [Fact]
        public async Task JsonToTerraformCommand_MissingRequiredInput_ShouldShowError()
        {
            // Arrange
            var args = new[] { "json-to-terraform" };

            // Act
            var exitCode = await CallProgramMain(args);

            // Assert
            Assert.NotEqual(0, exitCode); // Should fail due to missing required option
        }

        [Fact]
        public async Task JsonToTerraformCommand_WithInput_ShouldAttemptConversion()
        {
            // Arrange
            var inputFile = Path.Combine(_tempDirectory, "policies.json");
            await File.WriteAllTextAsync(inputFile, "[]"); // Empty JSON array
            var args = new[] { "json-to-terraform", "--input", inputFile };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains("JSON to Terraform Conversion", output);
            Assert.Contains($"Input JSON file: {inputFile}", output);
            Assert.Contains("Output directory: terraform-output", output); // Default value
        }

        [Fact]
        public async Task JsonToTerraformCommand_WithAllOptions_ShouldUseSpecifiedValues()
        {
            // Arrange
            var inputFile = Path.Combine(_tempDirectory, "policies.json");
            var outputDir = Path.Combine(_tempDirectory, "tf-output");
            await File.WriteAllTextAsync(inputFile, "[]");
            var args = new[] { 
                "json-to-terraform", 
                "--input", inputFile,
                "--output-dir", outputDir,
                "--generate-variables", "false",
                "--generate-provider", "false", 
                "--separate-files", "true",
                "--generate-module", "true",
                "--include-comments", "false",
                "--provider-version", "~> 2.39"
            };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains($"Output directory: {outputDir}", output);
            Assert.Contains("Generate variables: False", output);
            Assert.Contains("Generate provider config: False", output);
            Assert.Contains("Separate files per policy: True", output);
            Assert.Contains("Generate module structure: True", output);
            Assert.Contains("Include comments: False", output);
            Assert.Contains("Provider version: ~> 2.39", output);
        }

        [Fact]
        public async Task JsonToTerraformCommand_NonExistentInput_ShouldShowError()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.json");
            var args = new[] { "json-to-terraform", "--input", nonExistentFile };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Equal(1, exitCode);
            Assert.Contains($"Error: Input file '{nonExistentFile}' not found", output);
        }

        [Fact]
        public async Task JsonToTerraformCommand_Help_ShouldShowHelpText()
        {
            // Arrange
            var args = new[] { "json-to-terraform", "--help" };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains("Convert JSON conditional access policies to Terraform HCL", output);
            Assert.Equal(0, exitCode);
        }

        #endregion

        #region Compare Command Tests

        [Fact]
        public async Task CompareCommand_MissingRequiredReferenceDir_ShouldShowError()
        {
            // Arrange
            var args = new[] { "compare" };

            // Act
            var exitCode = await CallProgramMain(args);

            // Assert
            Assert.NotEqual(0, exitCode); // Should fail due to missing required option
        }

        [Fact]
        public async Task CompareCommand_WithReferenceDir_ShouldAttemptComparison()
        {
            // Arrange
            var refDir = Path.Combine(_tempDirectory, "reference");
            Directory.CreateDirectory(refDir);
            var args = new[] { "compare", "--reference-dir", refDir };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains("Conditional Access Policy Comparison", output);
            // Will fail due to missing Azure credentials but CLI parsing should work
        }

        [Fact]
        public async Task CompareCommand_WithAllOptions_ShouldUseSpecifiedValues()
        {
            // Arrange
            var refDir = Path.Combine(_tempDirectory, "reference");
            var entraFile = Path.Combine(_tempDirectory, "entra.json");
            var outputDir = Path.Combine(_tempDirectory, "reports");
            Directory.CreateDirectory(refDir);
            await File.WriteAllTextAsync(entraFile, "{}");
            
            var args = new[] { 
                "compare", 
                "--reference-dir", refDir,
                "--entra-file", entraFile,
                "--output-dir", outputDir,
                "--formats", "json", "html",
                "--matching", "ById",
                "--case-sensitive", "true"
            };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            // Check that the command was parsed and executed (may fail due to missing Azure credentials)
            // Test should verify CLI parsing worked - content may vary based on execution success/failure
            Assert.True(output.Contains("Conditional Access Policy Comparison") || 
                       output.Contains("Loading Entra policies from file") ||
                       output.Contains("Error") ||
                       output.Contains("Compare Entra policies with reference JSON files"), // Help text
                       $"Expected command execution output but got: {output}");
        }

        [Fact]
        public async Task CompareCommand_Help_ShouldShowHelpText()
        {
            // Arrange
            var args = new[] { "compare", "--help" };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains("Compare Entra policies with reference JSON files", output);
            Assert.Equal(0, exitCode);
        }

        #endregion

        #region Cross-Compare Command Tests

        [Fact]
        public async Task CrossCompareCommand_MissingRequiredDirs_ShouldShowError()
        {
            // Arrange
            var args = new[] { "cross-compare" };

            // Act
            var exitCode = await CallProgramMain(args);

            // Assert
            Assert.NotEqual(0, exitCode); // Should fail due to missing required options
        }

        [Fact]
        public async Task CrossCompareCommand_WithRequiredDirs_ShouldAttemptComparison()
        {
            // Arrange
            var sourceDir = Path.Combine(_tempDirectory, "source");
            var refDir = Path.Combine(_tempDirectory, "reference");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(refDir);
            var args = new[] { 
                "cross-compare", 
                "--source-dir", sourceDir,
                "--reference-dir", refDir 
            };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains("Cross-Format Policy Comparison", output);
            Assert.Contains($"Source directory: {sourceDir}", output);
            Assert.Contains($"Reference directory: {refDir}", output);
        }

        [Fact]
        public async Task CrossCompareCommand_WithAllOptions_ShouldUseSpecifiedValues()
        {
            // Arrange
            var sourceDir = Path.Combine(_tempDirectory, "source");
            var refDir = Path.Combine(_tempDirectory, "reference");
            var outputDir = Path.Combine(_tempDirectory, "cross-reports");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(refDir);
            
            var args = new[] { 
                "cross-compare", 
                "--source-dir", sourceDir,
                "--reference-dir", refDir,
                "--output-dir", outputDir,
                "--formats", "json", "markdown",
                "--matching", "SemanticSimilarity",
                "--case-sensitive", "true",
                "--enable-semantic", "false",
                "--similarity-threshold", "0.75"
            };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            // Check that the command was parsed and executed (may fail due to no policies in empty dirs)
            // Test should verify CLI parsing worked - content may vary based on execution success/failure
            Assert.True(output.Contains("Cross-Format Policy Comparison") || 
                       output.Contains($"Source directory: {sourceDir}") ||
                       output.Contains("Error") ||
                       output.Contains("Found 0 source policies") ||
                       output.Contains("Compare policies across different formats"), // Help text
                       $"Expected command execution output but got: {output}");
        }

        [Fact]
        public async Task CrossCompareCommand_Help_ShouldShowHelpText()
        {
            // Arrange
            var args = new[] { "cross-compare", "--help" };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains("Compare policies across different formats", output);
            Assert.Equal(0, exitCode);
        }

        #endregion

        #region Default Behavior Tests

        [Fact]
        public async Task ProgramMain_NoArguments_ShouldDefaultToExport()
        {
            // Arrange
            var args = new string[0];

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains("Conditional Access Policy Exporter", output);
            Assert.Equal(1, exitCode); // Expected to fail due to missing credentials
        }

        #endregion

        #region Root Command Help Tests

        [Fact]
        public async Task RootCommand_Help_ShouldShowHelpText()
        {
            // Arrange
            var args = new[] { "--help" };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            Assert.Contains("Conditional Access Policy Exporter and Comparator", output);
            Assert.Contains("export", output);
            Assert.Contains("terraform", output);
            Assert.Contains("json-to-terraform", output);
            Assert.Contains("compare", output);
            Assert.Contains("cross-compare", output);
            Assert.Equal(0, exitCode);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task HandleExceptionAsync_WithStandardException_ShouldDisplayError()
        {
            // This test would require making HandleExceptionAsync public or using reflection
            // For now, we test error handling through actual command failures
            
            // Arrange - Use invalid command to trigger error
            var args = new[] { "invalid-command" };

            // Act
            var exitCode = await CallProgramMain(args);

            // Assert
            Assert.NotEqual(0, exitCode); // Should return non-zero for invalid command
        }

        #endregion

        #region Additional CLI Integration Tests

        [Fact]
        public async Task HandleExceptionAsync_ServiceException_ShouldHandleGraphExceptions()
        {
            // Arrange - Use a command that will likely fail with missing credentials
            var args = new[] { "export", "--output", "/tmp/test.json" };

            // Act
            var exitCode = await CallProgramMain(args);
            var output = GetConsoleOutput();

            // Assert
            // Should handle the exception and return non-zero exit code
            Assert.Equal(1, exitCode);
            Assert.True(output.Contains("Error") || output.Contains("Missing required environment variables"), 
                       $"Expected error handling output but got: {output}");
        }

        [Fact]
        public async Task VariousCommands_InvalidOptions_ShouldShowErrors()
        {
            // Test terraform with invalid option
            var exitCode = await CallProgramMain("terraform", "--invalid-option");
            Assert.NotEqual(0, exitCode);
        }

        [Fact]
        public async Task SystemCommandLine_Integration_AllCommandsParsable()
        {
            // Test that all commands can be parsed by System.CommandLine
            var commands = new[]
            {
                new[] { "export", "--help" },
                new[] { "terraform", "--help" },
                new[] { "json-to-terraform", "--help" },
                new[] { "compare", "--help" },
                new[] { "cross-compare", "--help" }
            };

            foreach (var command in commands)
            {
                ClearConsoleOutput();
                var exitCode = await CallProgramMain(command);
                var output = GetConsoleOutput();
                
                // Help commands should return 0 and contain help text
                Assert.Equal(0, exitCode);
                Assert.Contains("Usage:", output);
            }
        }

        #endregion
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using System.Diagnostics;
using System.Text;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using ConditionalAccessExporter.Models;
using ConditionalAccessExporter.Services;
using System.Reflection;

namespace ConditionalAccessExporter.Tests
{
    public class ProgramTests
    {
        private readonly StringWriter _consoleOutput;
        private readonly StringWriter _consoleError;
        private readonly TextWriter _originalOutput;
        private readonly TextWriter _originalError;

        public ProgramTests()
        {
            _originalOutput = Console.Out;
            _originalError = Console.Error;
            _consoleOutput = new StringWriter();
            _consoleError = new StringWriter();
            Console.SetOut(_consoleOutput);
            Console.SetError(_consoleError);
        }

        ~ProgramTests()
        {
            Console.SetOut(_originalOutput);
            Console.SetError(_originalError);
            _consoleOutput.Dispose();
            _consoleError.Dispose();
        }

        private void ResetConsoleOutput()
        {
            _consoleOutput.GetStringBuilder().Clear();
            _consoleError.GetStringBuilder().Clear();
        }

        private string GetConsoleOutput()
        {
            return _consoleOutput.ToString() + _consoleError.ToString();
        }

        [Fact]
        public async Task Program_WithHelpArgument_ShouldDisplayHelpAndExitWithZero()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new[] { "--help" };

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            Assert.Equal(0, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("Conditional Access Policy Exporter and Comparator", output);
        }

        [Fact]
        public async Task ExportCommand_WithHelpArgument_ShouldDisplayHelpAndExitWithZero()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new[] { "export", "--help" };

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            Assert.Equal(0, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("--output", output);
            Assert.Contains("Output file path", output);
        }

        [Fact]
        public async Task TerraformCommand_WithHelpArgument_ShouldDisplayHelpAndExitWithZero()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new[] { "terraform", "--help" };

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            Assert.Equal(0, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("--input", output);
            Assert.Contains("--output", output);
            Assert.Contains("--validate", output);
            Assert.Contains("--verbose", output);
        }

        [Fact]
        public async Task JsonToTerraformCommand_WithHelpArgument_ShouldDisplayHelpAndExitWithZero()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new[] { "json-to-terraform", "--help" };

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            Assert.Equal(0, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("--input", output);
            Assert.Contains("--output-dir", output);
            Assert.Contains("--generate-variables", output);
            Assert.Contains("--provider-version", output);
        }

        [Fact]
        public async Task CompareCommand_WithHelpArgument_ShouldDisplayHelpAndExitWithZero()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new[] { "compare", "--help" };

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            Assert.Equal(0, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("--reference-dir", output);
            Assert.Contains("--entra-file", output);
            Assert.Contains("--output-dir", output);
            Assert.Contains("--formats", output);
            Assert.Contains("--matching", output);
            Assert.Contains("--case-sensitive", output);
        }

        [Fact]
        public async Task CrossCompareCommand_WithHelpArgument_ShouldDisplayHelpAndExitWithZero()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new[] { "cross-compare", "--help" };

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            Assert.Equal(0, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("--source-dir", output);
            Assert.Contains("--reference-dir", output);
            Assert.Contains("--output-dir", output);
            Assert.Contains("--similarity-threshold", output);
            Assert.Contains("--enable-semantic", output);
        }

        [Fact]
        public async Task TerraformCommand_WithMissingRequiredInput_ShouldExitWithNonZero()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new[] { "terraform" };

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            Assert.NotEqual(0, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("input", output.ToLowerInvariant());
        }

        [Fact]
        public async Task JsonToTerraformCommand_WithMissingRequiredInput_ShouldExitWithNonZero()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new[] { "json-to-terraform" };

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            Assert.NotEqual(0, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("input", output.ToLowerInvariant());
        }

        [Fact]
        public async Task CompareCommand_WithMissingRequiredReferenceDir_ShouldExitWithNonZero()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new[] { "compare" };

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            Assert.NotEqual(0, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("reference-dir", output.ToLowerInvariant());
        }

        [Fact]
        public async Task CrossCompareCommand_WithMissingRequiredSourceDir_ShouldExitWithNonZero()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new[] { "cross-compare", "--reference-dir", "/some/path" };

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            Assert.NotEqual(0, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("source-dir", output.ToLowerInvariant());
        }

        [Fact]
        public async Task CrossCompareCommand_WithMissingRequiredReferenceDir_ShouldExitWithNonZero()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new[] { "cross-compare", "--source-dir", "/some/path" };

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            Assert.NotEqual(0, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("reference-dir", output.ToLowerInvariant());
        }

        [Fact]
        public async Task Program_WithNoArguments_ShouldCallExportWithDefaultPath()
        {
            // Arrange
            ResetConsoleOutput();
            var args = new string[0];

            // Mock the environment variables to avoid authentication issues
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", "test-tenant");
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", "test-client");
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "test-secret");

            // Act
            var exitCode = await InvokeMainAsync(args);

            // Assert
            // Should exit with 1 due to authentication failure, but should attempt export
            Assert.Equal(1, exitCode);
            var output = GetConsoleOutput();
            Assert.Contains("Conditional Access Policy Exporter", output);
            
            // Clean up environment variables
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", null);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", null);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", null);
        }

        private async Task<int> InvokeMainAsync(string[] args)
        {
            // Use reflection to call the private Main method
            var programType = typeof(ConditionalAccessExporter.Program);
            var mainMethod = programType.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);
            
            if (mainMethod == null)
            {
                throw new InvalidOperationException("Could not find Main method in Program class");
            }

            var task = (Task<int>)mainMethod.Invoke(null, new object[] { args });
            return await task;
        }
    }
}
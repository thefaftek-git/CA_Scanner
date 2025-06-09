using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Tests
{
    /// <summary>
    /// Tests for the refactored RemediatePoliciesAsync method to ensure
    /// the refactoring maintains existing functionality while improving readability
    /// </summary>
    public class RemediatePoliciesAsyncTests
    {
        [Fact]
        public async Task RemediatePoliciesAsync_WithoutCredentials_ReturnsErrorExitCode()
        {
            // Arrange
            var originalTenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var originalClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var originalClientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            
            // Clear environment variables to simulate missing credentials
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", null);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", null);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", null);
            
            var tempDir = Path.Combine(Path.GetTempPath(), "test_remediation_" + Guid.NewGuid().ToString("N")[..8]);
            
            try
            {
                // Act
                var result = await ProgramTestHelper.InvokeRemediatePoliciesAsync(
                    analysisOnly: true,
                    interactive: false,
                    riskLevel: "All",
                    scriptFormat: "PowerShell",
                    outputDir: tempDir,
                    includeImpactAnalysis: false,
                    dryRun: true,
                    backup: false);
                
                // Assert
                Assert.Equal(1, result); // Should return error exit code when credentials are missing
            }
            finally
            {
                // Restore environment variables
                Environment.SetEnvironmentVariable("AZURE_TENANT_ID", originalTenantId);
                Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", originalClientId);
                Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", originalClientSecret);
                
                // Clean up temp directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task RemediatePoliciesAsync_CreatesOutputDirectory()
        {
            // Arrange
            var originalTenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var originalClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var originalClientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            
            // Clear environment variables to trigger early exit after directory creation
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", null);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", null);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", null);
            
            var tempDir = Path.Combine(Path.GetTempPath(), "test_remediation_" + Guid.NewGuid().ToString("N")[..8]);
            
            try
            {
                // Act
                await ProgramTestHelper.InvokeRemediatePoliciesAsync(
                    analysisOnly: true,
                    interactive: false,
                    riskLevel: "All",
                    scriptFormat: "PowerShell",
                    outputDir: tempDir,
                    includeImpactAnalysis: false,
                    dryRun: true,
                    backup: false);
                
                // Assert
                Assert.True(Directory.Exists(tempDir), "Output directory should be created");
            }
            finally
            {
                // Restore environment variables
                Environment.SetEnvironmentVariable("AZURE_TENANT_ID", originalTenantId);
                Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", originalClientId);
                Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", originalClientSecret);
                
                // Clean up temp directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Theory]
        [InlineData("Low")]
        [InlineData("Medium")]
        [InlineData("High")]
        [InlineData("Critical")]
        [InlineData("All")]
        public async Task RemediatePoliciesAsync_AcceptsValidRiskLevels(string riskLevel)
        {
            // Arrange
            var originalTenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var originalClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var originalClientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            
            // Clear environment variables to trigger early exit
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", null);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", null);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", null);
            
            var tempDir = Path.Combine(Path.GetTempPath(), "test_remediation_" + Guid.NewGuid().ToString("N")[..8]);
            
            try
            {
                // Act & Assert - Should not throw exceptions for valid risk levels
                var result = await ProgramTestHelper.InvokeRemediatePoliciesAsync(
                    analysisOnly: true,
                    interactive: false,
                    riskLevel: riskLevel,
                    scriptFormat: "PowerShell",
                    outputDir: tempDir,
                    includeImpactAnalysis: false,
                    dryRun: true,
                    backup: false);
                
                // Should return error code due to missing credentials, but not due to invalid risk level
                Assert.Equal(1, result);
            }
            finally
            {
                // Restore environment variables
                Environment.SetEnvironmentVariable("AZURE_TENANT_ID", originalTenantId);
                Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", originalClientId);
                Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", originalClientSecret);
                
                // Clean up temp directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}

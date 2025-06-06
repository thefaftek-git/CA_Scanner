using ConditionalAccessExporter.Services;
using Xunit;

namespace ConditionalAccessExporter.Tests
{
    public class BaselineGenerationServiceTests
    {
        [Fact]
        public void BaselineGenerationOptions_DefaultConstructor_SetsDefaults()
        {
            // Arrange & Act
            var options = new BaselineGenerationOptions();

            // Assert
            Assert.Equal(string.Empty, options.OutputDirectory);
            Assert.False(options.Anonymize);
            Assert.False(options.FilterEnabledOnly);
            Assert.Null(options.PolicyNames);
        }

        [Fact]
        public void BaselineGenerationOptions_PropertiesSetCorrectly()
        {
            // Arrange
            var options = new BaselineGenerationOptions
            {
                OutputDirectory = "/test/path",
                Anonymize = true,
                FilterEnabledOnly = true,
                PolicyNames = new List<string> { "Policy1", "Policy2" }
            };

            // Assert
            Assert.Equal("/test/path", options.OutputDirectory);
            Assert.True(options.Anonymize);
            Assert.True(options.FilterEnabledOnly);
            Assert.NotNull(options.PolicyNames);
            Assert.Equal(2, options.PolicyNames.Count);
            Assert.Contains("Policy1", options.PolicyNames);
            Assert.Contains("Policy2", options.PolicyNames);
        }

        [Fact]
        public void BaselineGenerationService_Constructor_CreatesInstance()
        {
            // Arrange & Act
            var service = new BaselineGenerationService();

            // Assert
            Assert.NotNull(service);
        }
    }
}

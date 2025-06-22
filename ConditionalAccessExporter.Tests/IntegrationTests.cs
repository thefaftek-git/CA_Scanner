
using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Graph;
using ConditionalAccessExporter.Services;

namespace ConditionalAccessExporter.Tests
{
    public class IntegrationTests
    {
        private readonly Mock<IGraphServiceClient> _mockGraphClient;
        private readonly PolicyComparisonService _service;

        public IntegrationTests()
        {
            _mockGraphClient = new Mock<IGraphServiceClient>();
            _service = new PolicyComparisonService(_mockGraphClient.Object);
        }

        [Fact]
        public async Task ComparePoliciesAsync_WithMockedGraphApi_ShouldReturnExpectedResult()
        {
            // Arrange
            var sourceDir = "path/to/source";
            var referenceDir = "path/to/reference";
            var matchingOptions = new MatchingOptions();

            // Mock Graph API responses
            _mockGraphClient.Setup(client => client.ConditionalAccessPolicies.Request().GetAsync())
                .ReturnsAsync(new ConditionalAccessPolicyCollectionPage
                {
                    new ConditionalAccessPolicy
                    {
                        Id = "policy1",
                        DisplayName = "Test Policy"
                    }
                });

            // Act
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Summary.TotalSourcePolicies);
        }
    }
}

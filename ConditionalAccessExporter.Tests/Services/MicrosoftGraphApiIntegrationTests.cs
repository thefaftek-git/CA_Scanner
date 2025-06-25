using System;
using System.Net.Http;
using System.Threading.Tasks;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConditionalAccessExporter.Tests
{
    [Collection("ConsoleOutputTestCollection")]
    public class MicrosoftGraphApiIntegrationTests : IDisposable
    {
        private readonly HttpClient _mockHttpClient;
        private readonly Mock<ILogger<ConditionalAccessPolicyService>> _loggerMock;

        public MicrosoftGraphApiIntegrationTests()
        {
            // Create mock policies for testing
            var testPolicies = TestDataFactory.CreateLargePolicyDataset(5);

            // Initialize the mock HTTP client with our test policies
            _mockHttpClient = MicrosoftGraphMock.CreateMockHttpClient(testPolicies.ToArray());

            // Setup a mock logger
            _loggerMock = new Mock<ILogger<ConditionalAccessPolicyService>>();
        }

        public void Dispose()
        {
            _mockHttpClient.Dispose();
        }

        [Fact]
        public async Task GetAllPoliciesAsync_WithValidCredentials_ReturnsAllPolicies()
        {
            // Arrange
            var service = new ConditionalAccessPolicyService(_mockHttpClient, _loggerMock.Object);

            // Act
            var result = await service.GetAllPoliciesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(5, result.Policies.Count); // Should match our test dataset size
        }

        [Fact]
        public async Task GetPolicyByIdAsync_WithExistingId_ReturnsCorrectPolicy()
        {
            // Arrange
            var service = new ConditionalAccessPolicyService(_mockHttpClient, _loggerMock.Object);
            var policyId = "policy-0001"; // From our test dataset

            // Act
            var result = await service.GetPolicyByIdAsync(policyId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(policyId, result.Policy.Id);
        }

        [Fact]
        public async Task GetPolicyByIdAsync_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            var service = new ConditionalAccessPolicyService(_mockHttpClient, _loggerMock.Object);
            var nonExistingId = "non-existing-id";

            // Act
            var result = await service.GetPolicyByIdAsync(nonExistingId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreatePolicyAsync_WithValidPolicy_CreatesSuccessfully()
        {
            // Arrange
            var service = new ConditionalAccessPolicyService(_mockHttpClient, _loggerMock.Object);
            var policy = TestDataFactory.CreateBasicJsonPolicy("new-policy-id", "New Policy");

            // Act
            var result = await service.CreatePolicyAsync(policy);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task UpdatePolicyAsync_WithValidPolicy_UpdatesSuccessfully()
        {
            // Arrange
            var service = new ConditionalAccessPolicyService(_mockHttpClient, _loggerMock.Object);
            var policyId = "policy-0001"; // From our test dataset
            var updatedPolicy = TestDataFactory.CreateBasicJsonPolicy(policyId, "Updated Policy", "disabled");

            // Act
            var result = await service.UpdatePolicyAsync(updatedPolicy);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task DeletePolicyAsync_WithExistingId_DeletesSuccessfully()
        {
            // Arrange
            var service = new ConditionalAccessPolicyService(_mockHttpClient, _loggerMock.Object);
            var policyId = "policy-0001"; // From our test dataset

            // Act
            var result = await service.DeletePolicyAsync(policyId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task GetAllPoliciesAsync_WithPerformanceMeasurement_TakesReasonableTime()
        {
            // Arrange
            var service = new ConditionalAccessPolicyService(_mockHttpClient, _loggerMock.Object);

            // Act
            var startTime = DateTime.UtcNow;
            var result = await service.GetAllPoliciesAsync();
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.True(result.Success);
            Assert.InRange((endTime - startTime).TotalSeconds, 0, 5); // Should complete in under 5 seconds
        }

        [Fact]
        public async Task GetAllPoliciesAsync_WithLargeDataset_ReturnsCompleteResults()
        {
            // Arrange
            var largeDatasetClient = MicrosoftGraphMock.CreateMockHttpClient(TestDataFactory.CreateLargePolicyDataset(100).ToArray());
            var service = new ConditionalAccessPolicyService(largeDatasetClient, _loggerMock.Object);

            try
            {
                // Act
                var result = await service.GetAllPoliciesAsync();

                // Assert
                Assert.True(result.Success);
                Assert.Equal(100, result.Policies.Count); // Should match our large dataset size
            }
            finally
            {
                largeDatasetClient.Dispose();
            }
        }

        [Fact]
        public async Task GetAllPoliciesAsync_WithEmptyResponse_HandlesGracefully()
        {
            // Arrange
            var emptyResponseClient = MicrosoftGraphMock.CreateMockHttpClient(Array.Empty<JObject>());
            var service = new ConditionalAccessPolicyService(emptyResponseClient, _loggerMock.Object);

            try
            {
                // Act
                var result = await service.GetAllPoliciesAsync();

                // Assert
                Assert.True(result.Success);
                Assert.Empty(result.Policies);
            }
            finally
            {
                emptyResponseClient.Dispose();
            }
        }

        [Fact]
        public async Task GetAllPoliciesAsync_WithErrorResponse_HandlesErrors()
        {
            // Arrange
            var errorHandlerMock = new Mock<HttpMessageHandler>();
            errorHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.InternalServerError,
                    Content = new StringContent("Internal server error")
                });

            var service = new ConditionalAccessPolicyService(errorHandlerMock.Object, _loggerMock.Object);

            // Act
            var result = await service.GetAllPoliciesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("error", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
    }
}

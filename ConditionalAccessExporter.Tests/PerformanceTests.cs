

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using ConditionalAccessExporter.Services;

namespace ConditionalAccessExporter.Tests
{
    public class PerformanceTests
    {
        private readonly PolicyComparisonService _service;

        public PerformanceTests()
        {
            _service = new PolicyComparisonService();
        }

        [Fact]
        public async Task ComparePoliciesAsync_LargeNumberOfPolicies_ShouldHandleEfficiently()
        {
            // Arrange
            const int policyCount = 1000;
            var sourceDir = "path/to/source";
            var referenceDir = "path/to/reference";
            var matchingOptions = new MatchingOptions();

            // Create many policies
            for (int i = 0; i < policyCount; i++)
            {
                var policy = TestDataFactory.CreateBasicJsonPolicy($"policy-{i:D3}", $"Test Policy {i}");
                WriteJsonPolicyToFile(sourceDir, $"policy-{i:D3}.json", policy);
            }

            // Act with timing
            var stopwatch = Stopwatch.StartNew();
            var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);
            stopwatch.Stop();

            // Assert performance bounds
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"Execution time exceeded 30 seconds: {stopwatch.Elapsed}");
            Assert.Equal(policyCount, result.Summary.TotalSourcePolicies);
        }
    }
}


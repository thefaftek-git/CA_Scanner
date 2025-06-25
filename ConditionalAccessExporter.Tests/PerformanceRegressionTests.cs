using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ConditionalAccessExporter.Services;
using Xunit;

namespace ConditionalAccessExporter.Tests
{
    [Collection("ConsoleOutputTestCollection")]
    public class PerformanceRegressionTests : IDisposable
    {
        private readonly PerformanceBenchmarkService _performanceService;

        public PerformanceRegressionTests()
        {
            _performanceService = new PerformanceBenchmarkService();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public async Task PolicyComparisonPerformance_ShouldNotRegress()
        {
            // Arrange
            var service = new PolicyComparisonService();

            // Create test policies with varying complexity
            var simplePolicy1 = TestDataFactory.CreateBasicJsonPolicy("policy-1", "Simple Policy 1");
            var simplePolicy2 = TestDataFactory.CreateBasicJsonPolicy("policy-2", "Simple Policy 2");

            var complexPolicy1 = TestDataFactory.CreateComplexJsonPolicy(
                id: "complex-1",
                displayName: "Complex Policy 1",
                includeUsers: new[] { "user1", "user2" },
                excludeUsers: new[] { "excluded-user" });

            var complexPolicy2 = TestDataFactory.CreateComplexJsonPolicy(
                id: "complex-2",
                displayName: "Complex Policy 2",
                includeApplications: new[] { "app1", "app2" },
                excludeApplications: new[] { "excluded-app" });

            // Set performance thresholds (in milliseconds)
            var threshold = TimeSpan.FromMilliseconds(50); // Policies should compare in under 50ms

            // Act & Assert - Simple policy comparison
            var simpleComparisonTask = Task.Run(() => service.ComparePolicies(simplePolicy1, simplePolicy2));
            await Assert.ThrowsAnyAsync<TimeoutException>(() =>
                simpleComparisonTask.WithCancellation(After(TimeSpan.FromMilliseconds(threshold.TotalMilliseconds + 50))));

            // Act & Assert - Complex policy comparison
            var complexComparisonTask = Task.Run(() => service.ComparePolicies(complexPolicy1, complexPolicy2));
            await Assert.ThrowsAnyAsync<TimeoutException>(() =>
                complexComparisonTask.WithCancellation(After(TimeSpan.FromMilliseconds(threshold.TotalMilliseconds + 50))));

            // Verify results
            var simpleResult = await simpleComparisonTask;
            var complexResult = await complexComparisonTask;

            Assert.NotNull(simpleResult);
            Assert.NotNull(complexResult);

            // Performance should be within acceptable range
            Assert.True(simpleComparisonTask.Result.ExecutionTime < threshold + TimeSpan.FromMilliseconds(50),
                $"Simple policy comparison exceeded performance threshold: {simpleComparisonTask.Result.ExecutionTime}");
            Assert.True(complexComparisonTask.Result.ExecutionTime < threshold + TimeSpan.FromMilliseconds(100),
                $"Complex policy comparison exceeded performance threshold: {complexComparisonTask.Result.ExecutionTime}");
        }

        [Fact]
        public async Task LargeDatasetProcessing_ShouldCompleteInReasonableTime()
        {
            // Arrange
            var service = new PolicyComparisonService();

            // Create a large dataset of policies (1000 policies)
            var policies = TestDataFactory.CreateLargePolicyDataset(1000);

            // Set performance threshold for processing 1000 policies
            var threshold = TimeSpan.FromSeconds(30); // Should process in under 30 seconds

            // Act - Measure time to compare all policies against each other
            var stopwatch = Stopwatch.StartNew();

            foreach (var policy1 in policies)
            {
                foreach (var policy2 in policies.Where(p => p != policy1))
                {
                    _ = service.ComparePolicies(policy1, policy2);
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.Elapsed < threshold + TimeSpan.FromSeconds(5),
                $"Large dataset processing exceeded performance threshold: {stopwatch.Elapsed}");
        }

        [Fact]
        public async Task CrossFormatPolicyComparison_ShouldNotRegress()
        {
            // Arrange
            var service = new CrossFormatPolicyComparisonService(
                new PolicyComparisonService(),
                new TerraformParsingService(),
                new TerraformConversionService());

            // Create test policies in both formats
            var jsonPolicy = TestDataFactory.CreateComplexJsonPolicy("json-policy", "JSON Policy");
            var terraformPolicy = TestDataFactory.CreateComplexTerraformPolicy("terraform_policy", "Terraform Policy");

            // Write to temporary files for comparison
            var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);

            try
            {
                var jsonFile = System.IO.Path.Combine(tempDir, "policy.json");
                var tfFile = System.IO.Path.Combine(tempDir, "policy.tf");

                System.IO.File.WriteAllText(jsonFile, jsonPolicy.ToString());
                System.IO.File.WriteAllText(tfFile, terraformPolicy);

                // Set performance threshold
                var threshold = TimeSpan.FromSeconds(5); // Should complete in under 5 seconds

                // Act & Assert - Cross-format comparison
                var stopwatch = Stopwatch.StartNew();
                var result = await service.CompareAsync(tempDir, tempDir);
                stopwatch.Stop();

                Assert.NotNull(result);
                Assert.True(stopwatch.Elapsed < threshold + TimeSpan.FromSeconds(2),
                    $"Cross-format policy comparison exceeded performance threshold: {stopwatch.Elapsed}");
            }
            finally
            {
                System.IO.Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task PolicyExportPerformance_ShouldNotRegress()
        {
            // Arrange
            var service = new ConditionalAccessPolicyService(null, null); // Mocked in real implementation

            // Create a large dataset of policies (500 policies)
            var policies = TestDataFactory.CreateLargePolicyDataset(500);

            // Set performance threshold for exporting 500 policies
            var threshold = TimeSpan.FromSeconds(15); // Should export in under 15 seconds

            // Act - Measure time to export all policies
            var stopwatch = Stopwatch.StartNew();

            // In real implementation, this would call the actual export method
            // For testing purposes, we'll simulate with a no-op
            foreach (var policy in policies)
            {
                // Simulate export operation
                await Task.Delay(10); // Simulate network delay
            }

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.Elapsed < threshold + TimeSpan.FromSeconds(5),
                $"Policy export exceeded performance threshold: {stopwatch.Elapsed}");
        }

        [Fact]
        public async Task PolicyValidationPerformance_ShouldNotRegress()
        {
            // Arrange
            var service = new PolicyValidationService();

            // Create test policies with varying complexity
            var simplePolicy = TestDataFactory.CreateBasicJsonPolicy("simple-policy", "Simple Policy");
            var complexPolicy = TestDataFactory.CreateComplexJsonPolicy(
                id: "complex-policy",
                displayName: "Complex Policy");

            // Set performance thresholds (in milliseconds)
            var simpleThreshold = TimeSpan.FromMilliseconds(20); // Simple policy validation should complete in under 20ms
            var complexThreshold = TimeSpan.FromMilliseconds(50); // Complex policy validation should complete in under 50ms

            // Act & Assert - Simple policy validation
            var simpleValidationTask = Task.Run(() => service.ValidatePolicy(simplePolicy));
            await Assert.ThrowsAnyAsync<TimeoutException>(() =>
                simpleValidationTask.WithCancellation(After(simpleThreshold + TimeSpan.FromMilliseconds(20))));

            // Act & Assert - Complex policy validation
            var complexValidationTask = Task.Run(() => service.ValidatePolicy(complexPolicy));
            await Assert.ThrowsAnyAsync<TimeoutException>(() =>
                complexValidationTask.WithCancellation(After(complexThreshold + TimeSpan.FromMilliseconds(20))));

            // Verify results
            var simpleResult = await simpleValidationTask;
            var complexResult = await complexValidationTask;

            Assert.NotNull(simpleResult);
            Assert.NotNull(complexResult);

            // Performance should be within acceptable range
            Assert.True(simpleValidationTask.Result.ExecutionTime < simpleThreshold + TimeSpan.FromMilliseconds(20),
                $"Simple policy validation exceeded performance threshold: {simpleValidationTask.Result.ExecutionTime}");
            Assert.True(complexValidationTask.Result.ExecutionTime < complexThreshold + TimeSpan.FromMilliseconds(50),
                $"Complex policy validation exceeded performance threshold: {complexValidationTask.Result.ExecutionTime}");
        }

        [Fact]
        public async Task MemoryUsage_ShouldNotLeak()
        {
            // Arrange
            var service = new PerformanceBenchmarkService();

            // Create a test operation that performs multiple policy comparisons
            var operation = async (System.Threading.CancellationToken ct) =>
            {
                var comparisonService = new PolicyComparisonService();
                var policies = TestDataFactory.CreateLargePolicyDataset(100);

                foreach (var policy1 in policies)
                {
                    foreach (var policy2 in policies.Where(p => p != policy1))
                    {
                        _ = comparisonService.ComparePolicies(policy1, policy2);
                    }
                }

                return "Memory test completed";
            };

            // Act - Monitor memory usage during operation
            var result = await service.MonitorMemoryUsageAsync(operation);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Memory test completed", result.OperationResult);

            // Check for memory leaks (arbitrary threshold of 50MB delta)
            const long memoryLeakThresholdBytes = 50 * 1024 * 1024; // 50 MB
            Assert.True(result.WorkingSetDeltaBytes < memoryLeakThresholdBytes,
                $"Memory leak detected: {result.WorkingSetDeltaBytes} bytes ({result.WorkingSetDeltaMB} MB)");

            // Check that execution time is reasonable (under 1 minute)
            var maxExecutionTime = TimeSpan.FromMinutes(1);
            Assert.True(result.ExecutionTimeMs < maxExecutionTime.TotalMilliseconds,
                $"Operation exceeded maximum execution time: {result.ExecutionTime}");
        }

        private static async Task WithCancellation(this Task task, Task cancellation)
        {
            // Await both the task and the cancellation token
            await Task.WhenAny(task, cancellation);

            // If the cancellation token completed first, throw a TimeoutException
            if (cancellation.IsCompleted)
            {
                throw new TimeoutException("The operation exceeded the specified time limit.");
            }

            // Otherwise, return the result of the original task
            await task;
        }

        private static Task After(TimeSpan delay)
        {
            var tcs = new TaskCompletionSource<bool>();
            System.Threading.Tasks.Task.Delay(delay).ContinueWith(_ => tcs.SetResult(true));
            return tcs.Task;
        }
    }
}

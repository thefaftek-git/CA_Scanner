


using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Utils;
using Xunit;

namespace ConditionalAccessExporter.Tests;

[Collection("ConsoleOutputTestCollection")]
public class PerformanceBenchmarkServiceTests
{
    [Fact]
    public async Task MonitorMemoryUsageAsync_WithSimpleOperation_ReturnsValidResults()
    {
        // Arrange
        var service = new PerformanceBenchmarkService();
        var operation = async (CancellationToken ct) =>
        {
            await Task.Delay(100, ct);
            return "Test completed";
        };

        // Act
        var result = await service.MonitorMemoryUsageAsync(operation, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Test completed", result.OperationResult);
        Assert.True(result.ExecutionTimeMs >= 90); // Should be around 100ms, allow for variance
        Assert.True(result.ExecutionTimeMs <= 200);
        Assert.NotEqual(0, result.InitialMemoryBytes);
        Assert.NotEqual(0, result.FinalMemoryBytes);
    }

    [Fact]
    public async Task MonitorMemoryUsageAsync_WithException_HandlesGracefully()
    {
        // Arrange
        var service = new PerformanceBenchmarkService();
        var operation = async (CancellationToken ct) =>
        {
            await Task.Delay(50, ct);
            throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162 // Unreachable code detected
            return "Should not reach here";
#pragma warning restore CS0162 // Unreachable code detected
        };

        // Act
        var result = await service.MonitorMemoryUsageAsync<string>(operation, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Test exception", result.ErrorMessage);
        Assert.Null(result.OperationResult);
        Assert.True(result.ExecutionTimeMs >= 40); // Should be around 50ms, allow for variance
        Assert.NotEqual(0, result.InitialMemoryBytes);
        Assert.NotEqual(0, result.FinalMemoryBytes);
    }

    [Fact]
    public async Task MonitorMemoryUsageAsync_WithCancellation_HandlesCancellation()
    {
        // Arrange
        var service = new PerformanceBenchmarkService();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel after 50ms

        var operation = async (CancellationToken ct) =>
        {
            await Task.Delay(200, ct); // This should be cancelled
            return "Should not complete";
        };

        // Act
        var result = await service.MonitorMemoryUsageAsync<string>(operation, cts.Token);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("canceled", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.ExecutionTimeMs < 150); // Should be cancelled before 200ms
    }

    [Fact]
    public async Task RunPerformanceRegressionTestsAsync_WithValidThresholds_ReturnsResults()
    {
        // Arrange
        var service = new PerformanceBenchmarkService();
        var thresholds = new Dictionary<string, TimeSpan>
        {
            ["PolicyComparison"] = TimeSpan.FromSeconds(1),
            ["FileIO"] = TimeSpan.FromSeconds(1)
        };

        // Act
        var result = await service.RunPerformanceRegressionTestsAsync(thresholds, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("PolicyComparison", result.Results.Keys);
        Assert.Contains("FileIO", result.Results.Keys);

        foreach (var testResult in result.Results.Values)
        {
            Assert.NotNull(testResult.TestName);
            Assert.True(testResult.ExecutionTime >= TimeSpan.Zero);
            Assert.True(testResult.Threshold > TimeSpan.Zero);
        }
    }

    [Fact]
    public async Task RunPerformanceRegressionTestsAsync_WithStrictThresholds_DetectsFailures()
    {
        // Arrange
        var service = new PerformanceBenchmarkService();
        var veryStrictThresholds = new Dictionary<string, TimeSpan>
        {
            ["PolicyComparison"] = TimeSpan.FromMilliseconds(1), // Very strict threshold
            ["FileIO"] = TimeSpan.FromMilliseconds(1)
        };

        // Act
        var result = await service.RunPerformanceRegressionTestsAsync(veryStrictThresholds, CancellationToken.None);

        // Assert
        Assert.True(result.Success); // The test execution itself should succeed
        Assert.False(result.AllTestsPassed); // But the performance tests should fail due to strict thresholds

        foreach (var testResult in result.Results.Values)
        {
            Assert.False(testResult.Passed); // All tests should fail with very strict thresholds
        }
    }

    [Fact]
    public void MemoryUsageResult_FormattedProperties_ReturnValidStrings()
    {
        // Arrange
        var result = new MemoryUsageResult
        {
            ManagedMemoryDeltaBytes = 1024 * 1024, // 1 MB
            WorkingSetDeltaBytes = 2 * 1024 * 1024, // 2 MB
            MemoryLeakIndicatorBytes = 512 * 1024 // 0.5 MB
        };

        // Act & Assert
        Assert.Equal("1.00", result.ManagedMemoryDeltaMB);
        Assert.Equal("2.00", result.WorkingSetDeltaMB);
        Assert.Equal("0.50", result.MemoryLeakIndicatorMB);
    }

    [Fact]
    public void RegressionTestResult_FormattedResult_ReturnsValidString()
    {
        // Arrange
        var testResult = new RegressionTestResult
        {
            TestName = "TestOperation",
            ExecutionTime = TimeSpan.FromMilliseconds(500),
            Threshold = TimeSpan.FromSeconds(1),
            Passed = true
        };

        // Act
        var formatted = testResult.FormattedResult;

        // Assert
        Assert.Contains("TestOperation", formatted);
        Assert.Contains("500ms", formatted);
        Assert.Contains("1000ms", formatted);
        Assert.Contains("PASS", formatted);
    }

    [Fact]
    public async Task PerformanceBenchmarkService_WithProgressReporter_ReportsProgress()
    {
        // Arrange
        var progressMessages = new List<string>();
        var progressReporter = new Progress<string>(message => progressMessages.Add(message));
        var service = new PerformanceBenchmarkService(progressReporter);
        
        var thresholds = new Dictionary<string, TimeSpan>
        {
            ["PolicyComparison"] = TimeSpan.FromSeconds(5)
        };

        // Act
        var result = await service.RunPerformanceRegressionTestsAsync(thresholds, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(progressMessages);
        Assert.Contains(progressMessages, msg => msg.Contains("regression"));
    }
}





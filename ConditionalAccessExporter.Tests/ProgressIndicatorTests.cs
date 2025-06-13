




using ConditionalAccessExporter.Utils;
using Xunit;

namespace ConditionalAccessExporter.Tests;

[Collection("ConsoleOutputTestCollection")]
public class ProgressIndicatorTests
{
    [Fact]
    public void ProgressIndicator_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        using var progressIndicator = new ProgressIndicator("Test Operation", 100);

        // Assert - Constructor should not throw
        Assert.True(true); // If we reach here, constructor succeeded
    }

    [Fact]
    public void ProgressIndicator_IncrementProgress_UpdatesCorrectly()
    {
        // Arrange
        var progressReports = new List<ProgressInfo>();
        var progressReporter = new Progress<ProgressInfo>(info => progressReports.Add(info));
        
        using var progressIndicator = new ProgressIndicator("Test Operation", 100, progressReporter);

        // Act
        progressIndicator.IncrementProgress(10);
        progressIndicator.ForceUpdate();
        
        Thread.Sleep(100); // Allow time for the update to be processed

        // Assert
        Assert.NotEmpty(progressReports);
        var latestReport = progressReports.Last();
        Assert.Equal("Test Operation", latestReport.OperationName);
        Assert.Equal(10, latestReport.CompletedItems);
        Assert.Equal(100, latestReport.TotalItems);
        Assert.Equal(10.0, latestReport.PercentageComplete);
    }

    [Fact]
    public void ProgressIndicator_SetProgress_UpdatesCorrectly()
    {
        // Arrange
        var progressReports = new List<ProgressInfo>();
        var progressReporter = new Progress<ProgressInfo>(info => progressReports.Add(info));
        
        using var progressIndicator = new ProgressIndicator("Test Operation", 50, progressReporter);

        // Act
        progressIndicator.SetProgress(25);
        progressIndicator.ForceUpdate();
        
        Thread.Sleep(100); // Allow time for the update to be processed

        // Assert
        Assert.NotEmpty(progressReports);
        var latestReport = progressReports.Last();
        Assert.Equal(25, latestReport.CompletedItems);
        Assert.Equal(50.0, latestReport.PercentageComplete);
    }

    [Fact]
    public void ProgressInfo_FormattedProperties_ReturnValidStrings()
    {
        // Arrange
        var progressInfo = new ProgressInfo
        {
            OperationName = "Test Operation",
            CompletedItems = 25,
            TotalItems = 100,
            PercentageComplete = 25.0,
            ElapsedTime = TimeSpan.FromMinutes(2),
            EstimatedTimeRemaining = TimeSpan.FromMinutes(6),
            ItemsPerSecond = 5.5,
            MemoryUsageBytes = 50 * 1024 * 1024, // 50 MB
            MemoryDeltaBytes = 10 * 1024 * 1024  // 10 MB
        };

        // Act & Assert
        Assert.Equal("Test Operation: 25/100 (25.0%)", progressInfo.FormattedProgress);
        Assert.Equal("Elapsed: 00:02:00", progressInfo.FormattedElapsedTime);
        Assert.Equal("ETA: 00:06:00", progressInfo.FormattedEstimatedTime);
        Assert.Equal("Rate: 5.5 items/sec", progressInfo.FormattedThroughput);
        Assert.Equal("Memory: 50.0 MB", progressInfo.FormattedMemoryUsage);
        Assert.Equal("Delta: +10.0 MB", progressInfo.FormattedMemoryDelta);
    }

    [Fact]
    public void ProgressInfo_ToString_ReturnsComprehensiveString()
    {
        // Arrange
        var progressInfo = new ProgressInfo
        {
            OperationName = "Test Operation",
            CompletedItems = 50,
            TotalItems = 100,
            PercentageComplete = 50.0,
            ElapsedTime = TimeSpan.FromMinutes(1),
            EstimatedTimeRemaining = TimeSpan.FromMinutes(1),
            ItemsPerSecond = 10.0,
            MemoryUsageBytes = 100 * 1024 * 1024,
            MemoryDeltaBytes = 5 * 1024 * 1024
        };

        // Act
        var result = progressInfo.ToString();

        // Assert
        Assert.Contains("Test Operation", result);
        Assert.Contains("50/100", result);
        Assert.Contains("50.0%", result);
        Assert.Contains("00:01:00", result);
        Assert.Contains("10.0 items/sec", result);
        Assert.Contains("100.0 MB", result);
        Assert.Contains("+5.0 MB", result);
    }

    [Fact]
    public void ConsoleProgressReporter_Report_DoesNotThrow()
    {
        // Arrange
        var reporter = new ConsoleProgressReporter();
        var progressInfo = new ProgressInfo
        {
            OperationName = "Test",
            CompletedItems = 1,
            TotalItems = 10,
            PercentageComplete = 10.0
        };

        // Act & Assert - Should not throw
        reporter.Report(progressInfo);
        reporter.WriteLine("Test message");
        reporter.Complete();
    }

    [Fact]
    public void PerformanceThreshold_CheckThresholds_DetectsViolations()
    {
        // Arrange
        var threshold = new PerformanceThreshold
        {
            Name = "Test Threshold",
            MaxExecutionTime = TimeSpan.FromMinutes(1),
            MinItemsPerSecond = 10.0,
            MaxMemoryUsageBytes = 100 * 1024 * 1024, // 100 MB
            MaxMemoryDeltaBytes = 50 * 1024 * 1024   // 50 MB
        };

        var progressInfo = new ProgressInfo
        {
            ElapsedTime = TimeSpan.FromMinutes(2), // Exceeds max execution time
            ItemsPerSecond = 5.0, // Below minimum throughput
            MemoryUsageBytes = 200 * 1024 * 1024, // Exceeds max memory
            MemoryDeltaBytes = 100 * 1024 * 1024  // Exceeds max delta
        };

        // Act
        var alerts = threshold.CheckThresholds(progressInfo);

        // Assert
        Assert.Equal(4, alerts.Count); // Should have 4 violations
        
        Assert.Contains(alerts, a => a.AlertType == PerformanceAlertType.ExecutionTimeExceeded);
        Assert.Contains(alerts, a => a.AlertType == PerformanceAlertType.ThroughputBelowThreshold);
        Assert.Contains(alerts, a => a.AlertType == PerformanceAlertType.MemoryUsageExceeded);
        Assert.Contains(alerts, a => a.AlertType == PerformanceAlertType.MemoryLeakDetected);

        foreach (var alert in alerts)
        {
            Assert.Equal("Test Threshold", alert.ThresholdName);
            Assert.NotEmpty(alert.Message);
            Assert.True(alert.Timestamp <= DateTime.UtcNow);
        }
    }

    [Fact]
    public void PerformanceThreshold_CheckThresholds_NoViolations_ReturnsEmpty()
    {
        // Arrange
        var threshold = new PerformanceThreshold
        {
            Name = "Test Threshold",
            MaxExecutionTime = TimeSpan.FromMinutes(5),
            MinItemsPerSecond = 1.0,
            MaxMemoryUsageBytes = 500 * 1024 * 1024,
            MaxMemoryDeltaBytes = 100 * 1024 * 1024
        };

        var progressInfo = new ProgressInfo
        {
            ElapsedTime = TimeSpan.FromMinutes(1), // Within limits
            ItemsPerSecond = 5.0, // Above minimum
            MemoryUsageBytes = 50 * 1024 * 1024, // Within limits
            MemoryDeltaBytes = 10 * 1024 * 1024  // Within limits
        };

        // Act
        var alerts = threshold.CheckThresholds(progressInfo);

        // Assert
        Assert.Empty(alerts);
    }

    [Fact]
    public void PerformanceThresholdMonitor_CheckProgress_CallsProgressReporter()
    {
        // Arrange
        var alerts = new List<PerformanceAlert>();
        var alertReporter = new Progress<PerformanceAlert>(alert => alerts.Add(alert));
        
        var thresholds = new Dictionary<string, PerformanceThreshold>
        {
            ["Test"] = new PerformanceThreshold
            {
                Name = "Test",
                MaxExecutionTime = TimeSpan.FromSeconds(1)
            }
        };

        var monitor = new PerformanceThresholdMonitor(thresholds, alertReporter);
        
        var progressInfo = new ProgressInfo
        {
            ElapsedTime = TimeSpan.FromMinutes(1) // Exceeds threshold
        };

        // Act
        monitor.CheckProgress(progressInfo);

        // Wait a moment for async processing
        Thread.Sleep(100);

        // Assert
        Assert.NotEmpty(alerts);
        Assert.Equal(PerformanceAlertType.ExecutionTimeExceeded, alerts[0].AlertType);
    }

    [Fact]
    public void ProgressInfo_EstimatedTimeRemaining_CalculatesCorrectly()
    {
        // Arrange
        var progressInfo = new ProgressInfo
        {
            EstimatedTimeRemaining = null // No ETA
        };

        // Act & Assert
        Assert.Equal("ETA: Calculating...", progressInfo.FormattedEstimatedTime);

        // Test with actual ETA
        progressInfo.EstimatedTimeRemaining = TimeSpan.FromMinutes(5);
        Assert.Equal("ETA: 00:05:00", progressInfo.FormattedEstimatedTime);
    }

    [Fact]
    public void ProgressInfo_NegativeMemoryDelta_FormatsCorrectly()
    {
        // Arrange
        var progressInfo = new ProgressInfo
        {
            MemoryDeltaBytes = -5 * 1024 * 1024 // -5 MB (memory decreased)
        };

        // Act & Assert
        Assert.Equal("Delta: -5.0 MB", progressInfo.FormattedMemoryDelta);
    }

    [Fact]
    public void ProgressIndicator_Dispose_StopsUpdates()
    {
        // Arrange
        var progressReports = new List<ProgressInfo>();
        var progressReporter = new Progress<ProgressInfo>(info => progressReports.Add(info));
        
        var progressIndicator = new ProgressIndicator("Test", 100, progressReporter, TimeSpan.FromMilliseconds(50));
        
        // Act
        progressIndicator.IncrementProgress(10);
        Thread.Sleep(200); // Let some updates happen
        var countBeforeDispose = progressReports.Count;
        
        progressIndicator.Dispose();
        Thread.Sleep(200); // Wait to see if more updates happen
        var countAfterDispose = progressReports.Count;

        // Assert
        Assert.True(countBeforeDispose > 0); // Should have had some updates
        // Count might increase by 1 due to final update in Dispose, but shouldn't continue growing
        Assert.True(countAfterDispose <= countBeforeDispose + 1);
    }
}







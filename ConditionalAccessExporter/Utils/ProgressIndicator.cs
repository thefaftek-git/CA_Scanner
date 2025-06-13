

using System.Diagnostics;

namespace ConditionalAccessExporter.Utils;

/// <summary>
/// Provides progress indication and performance monitoring for long-running operations
/// </summary>
public class ProgressIndicator : IDisposable
{
    private readonly string _operationName;
    private readonly int _totalItems;
    private readonly IProgress<ProgressInfo>? _progress;
    private readonly Stopwatch _stopwatch;
    private readonly Timer _updateTimer;
    private int _completedItems;
    private long _initialMemory;
    private bool _disposed;

    public ProgressIndicator(
        string operationName, 
        int totalItems, 
        IProgress<ProgressInfo>? progress = null,
        TimeSpan? updateInterval = null)
    {
        _operationName = operationName;
        _totalItems = totalItems;
        _progress = progress;
        _stopwatch = Stopwatch.StartNew();
        
        // Record initial memory usage (only force GC for accurate benchmarking when needed)
        // In normal operations, avoid the overhead of forcing garbage collection
        _initialMemory = GC.GetTotalMemory(forceFullCollection: false);
        
        var interval = updateInterval ?? TimeSpan.FromMilliseconds(500);
        _updateTimer = new Timer(UpdateProgress, null, interval, interval);
        
        ReportProgress();
    }

    /// <summary>
    /// Updates the progress counter
    /// </summary>
    public void IncrementProgress(int increment = 1)
    {
        Interlocked.Add(ref _completedItems, increment);
    }

    /// <summary>
    /// Sets the progress to a specific value
    /// </summary>
    public void SetProgress(int completedItems)
    {
        Interlocked.Exchange(ref _completedItems, completedItems);
    }

    /// <summary>
    /// Forces an immediate progress update
    /// </summary>
    public void ForceUpdate()
    {
        UpdateProgress(null);
    }

    private void UpdateProgress(object? state)
    {
        ReportProgress();
    }

    private void ReportProgress()
    {
        if (_progress == null) return;

        var completed = _completedItems;
        var percentage = _totalItems > 0 ? (double)completed / _totalItems * 100 : 0;
        var elapsed = _stopwatch.Elapsed;
        
        // Calculate estimated time remaining
        TimeSpan? estimatedTimeRemaining = null;
        if (completed > 0 && completed < _totalItems)
        {
            var avgTimePerItem = elapsed.TotalMilliseconds / completed;
            var remainingItems = _totalItems - completed;
            estimatedTimeRemaining = TimeSpan.FromMilliseconds(avgTimePerItem * remainingItems);
        }

        // Calculate throughput
        var itemsPerSecond = completed > 0 && elapsed.TotalSeconds > 0 
            ? completed / elapsed.TotalSeconds 
            : 0;

        // Calculate memory usage
        var currentMemory = GC.GetTotalMemory(false);
        var memoryDelta = currentMemory - _initialMemory;

        var progressInfo = new ProgressInfo
        {
            OperationName = _operationName,
            CompletedItems = completed,
            TotalItems = _totalItems,
            PercentageComplete = percentage,
            ElapsedTime = elapsed,
            EstimatedTimeRemaining = estimatedTimeRemaining,
            ItemsPerSecond = itemsPerSecond,
            MemoryUsageBytes = currentMemory,
            MemoryDeltaBytes = memoryDelta
        };

        _progress.Report(progressInfo);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _updateTimer?.Dispose();
        _stopwatch?.Stop();
        
        // Final progress update
        ReportProgress();
        
        _disposed = true;
    }
}

/// <summary>
/// Progress information for long-running operations
/// </summary>
public class ProgressInfo
{
    public string OperationName { get; set; } = string.Empty;
    public int CompletedItems { get; set; }
    public int TotalItems { get; set; }
    public double PercentageComplete { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public double ItemsPerSecond { get; set; }
    public long MemoryUsageBytes { get; set; }
    public long MemoryDeltaBytes { get; set; }

    public string FormattedProgress => $"{OperationName}: {CompletedItems}/{TotalItems} ({PercentageComplete:F1}%)";
    
    public string FormattedElapsedTime => $"Elapsed: {ElapsedTime:hh\\:mm\\:ss}";
    
    public string FormattedEstimatedTime => EstimatedTimeRemaining.HasValue 
        ? $"ETA: {EstimatedTimeRemaining.Value:hh\\:mm\\:ss}" 
        : "ETA: Calculating...";
    
    public string FormattedThroughput => $"Rate: {ItemsPerSecond:F1} items/sec";
    
    public string FormattedMemoryUsage => $"Memory: {MemoryUsageBytes / 1024.0 / 1024.0:F1} MB";
    
    public string FormattedMemoryDelta => $"Delta: {(MemoryDeltaBytes >= 0 ? "+" : "")}{MemoryDeltaBytes / 1024.0 / 1024.0:F1} MB";

    public override string ToString()
    {
        var parts = new[]
        {
            FormattedProgress,
            FormattedElapsedTime,
            FormattedEstimatedTime,
            FormattedThroughput,
            FormattedMemoryUsage,
            FormattedMemoryDelta
        };
        
        return string.Join(" | ", parts);
    }
}

/// <summary>
/// Console-based progress reporter
/// </summary>
public class ConsoleProgressReporter : IProgress<ProgressInfo>
{
    private readonly object _lockObject = new();
    private int _lastLineLength = 0;

    public void Report(ProgressInfo value)
    {
        lock (_lockObject)
        {
            // Clear the previous line
            if (_lastLineLength > 0)
            {
                Console.Write('\r' + new string(' ', _lastLineLength) + '\r');
            }

            // Write the new progress line
            var progressLine = value.ToString();
            Console.Write(progressLine);
            _lastLineLength = progressLine.Length;
        }
    }

    public void WriteLine(string message)
    {
        lock (_lockObject)
        {
            // Clear current progress line
            if (_lastLineLength > 0)
            {
                Console.Write('\r' + new string(' ', _lastLineLength) + '\r');
                _lastLineLength = 0;
            }

            // Write the message
            Console.WriteLine(message);
        }
    }

    public void Complete()
    {
        lock (_lockObject)
        {
            if (_lastLineLength > 0)
            {
                Console.WriteLine(); // Move to next line
                _lastLineLength = 0;
            }
        }
    }
}

/// <summary>
/// Performance threshold monitoring
/// </summary>
public class PerformanceThresholdMonitor
{
    private readonly Dictionary<string, PerformanceThreshold> _thresholds;
    private readonly IProgress<PerformanceAlert>? _alertReporter;

    public PerformanceThresholdMonitor(
        Dictionary<string, PerformanceThreshold> thresholds,
        IProgress<PerformanceAlert>? alertReporter = null)
    {
        _thresholds = thresholds;
        _alertReporter = alertReporter;
    }

    public void CheckProgress(ProgressInfo progressInfo)
    {
        foreach (var threshold in _thresholds.Values)
        {
            var alerts = threshold.CheckThresholds(progressInfo);
            foreach (var alert in alerts)
            {
                _alertReporter?.Report(alert);
            }
        }
    }
}

/// <summary>
/// Performance threshold configuration
/// </summary>
public class PerformanceThreshold
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan? MaxExecutionTime { get; set; }
    public double? MinItemsPerSecond { get; set; }
    public long? MaxMemoryUsageBytes { get; set; }
    public long? MaxMemoryDeltaBytes { get; set; }

    public List<PerformanceAlert> CheckThresholds(ProgressInfo progressInfo)
    {
        var alerts = new List<PerformanceAlert>();

        if (MaxExecutionTime.HasValue && progressInfo.ElapsedTime > MaxExecutionTime.Value)
        {
            alerts.Add(new PerformanceAlert
            {
                ThresholdName = Name,
                AlertType = PerformanceAlertType.ExecutionTimeExceeded,
                Message = $"Execution time ({progressInfo.ElapsedTime:hh\\:mm\\:ss}) exceeded threshold ({MaxExecutionTime.Value:hh\\:mm\\:ss})",
                ActualValue = progressInfo.ElapsedTime.TotalMilliseconds,
                ThresholdValue = MaxExecutionTime.Value.TotalMilliseconds
            });
        }

        if (MinItemsPerSecond.HasValue && progressInfo.ItemsPerSecond < MinItemsPerSecond.Value)
        {
            alerts.Add(new PerformanceAlert
            {
                ThresholdName = Name,
                AlertType = PerformanceAlertType.ThroughputBelowThreshold,
                Message = $"Throughput ({progressInfo.ItemsPerSecond:F1} items/sec) below threshold ({MinItemsPerSecond.Value:F1} items/sec)",
                ActualValue = progressInfo.ItemsPerSecond,
                ThresholdValue = MinItemsPerSecond.Value
            });
        }

        if (MaxMemoryUsageBytes.HasValue && progressInfo.MemoryUsageBytes > MaxMemoryUsageBytes.Value)
        {
            alerts.Add(new PerformanceAlert
            {
                ThresholdName = Name,
                AlertType = PerformanceAlertType.MemoryUsageExceeded,
                Message = $"Memory usage ({progressInfo.MemoryUsageBytes / 1024.0 / 1024.0:F1} MB) exceeded threshold ({MaxMemoryUsageBytes.Value / 1024.0 / 1024.0:F1} MB)",
                ActualValue = progressInfo.MemoryUsageBytes,
                ThresholdValue = MaxMemoryUsageBytes.Value
            });
        }

        if (MaxMemoryDeltaBytes.HasValue && progressInfo.MemoryDeltaBytes > MaxMemoryDeltaBytes.Value)
        {
            alerts.Add(new PerformanceAlert
            {
                ThresholdName = Name,
                AlertType = PerformanceAlertType.MemoryLeakDetected,
                Message = $"Memory delta ({progressInfo.MemoryDeltaBytes / 1024.0 / 1024.0:F1} MB) exceeded threshold ({MaxMemoryDeltaBytes.Value / 1024.0 / 1024.0:F1} MB)",
                ActualValue = progressInfo.MemoryDeltaBytes,
                ThresholdValue = MaxMemoryDeltaBytes.Value
            });
        }

        return alerts;
    }
}

/// <summary>
/// Performance alert information
/// </summary>
public class PerformanceAlert
{
    public string ThresholdName { get; set; } = string.Empty;
    public PerformanceAlertType AlertType { get; set; }
    public string Message { get; set; } = string.Empty;
    public double ActualValue { get; set; }
    public double ThresholdValue { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of performance alerts
/// </summary>
public enum PerformanceAlertType
{
    ExecutionTimeExceeded,
    ThroughputBelowThreshold,
    MemoryUsageExceeded,
    MemoryLeakDetected
}



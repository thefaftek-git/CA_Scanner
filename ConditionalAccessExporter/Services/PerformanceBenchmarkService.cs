using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace ConditionalAccessExporter.Services;

/// <summary>
/// Service for running performance benchmarks and monitoring system performance
/// </summary>
public class PerformanceBenchmarkService
{
    private readonly IProgress<string>? _progressReporter;

    public PerformanceBenchmarkService(IProgress<string>? progressReporter = null)
    {
        _progressReporter = progressReporter;
    }

    /// <summary>
    /// Runs all available benchmarks
    /// </summary>
    public Task<BenchmarkExecutionResult> RunAllBenchmarksAsync(CancellationToken cancellationToken = default)
    {
        var result = new BenchmarkExecutionResult();
        
        try
        {
            _progressReporter?.Report("Starting performance benchmarks...");
            
            // Run policy processing benchmarks
            _progressReporter?.Report("Running policy processing benchmarks...");
            var policyBenchmarks = BenchmarkRunner.Run<PolicyProcessingBenchmarks>(
                ManualConfig.Create(DefaultConfig.Instance)
                    .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance)));
            
            // Run file I/O benchmarks
            _progressReporter?.Report("Running file I/O benchmarks...");
            var fileBenchmarks = BenchmarkRunner.Run<FileIOBenchmarks>(
                ManualConfig.Create(DefaultConfig.Instance)
                    .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance)));
            
            // Run memory usage benchmarks
            _progressReporter?.Report("Running memory usage benchmarks...");
            var memoryBenchmarks = BenchmarkRunner.Run<MemoryUsageBenchmarks>(
                ManualConfig.Create(DefaultConfig.Instance)
                    .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance)));

            result.PolicyBenchmarkResults = policyBenchmarks;
            result.FileIOBenchmarkResults = fileBenchmarks;
            result.MemoryBenchmarkResults = memoryBenchmarks;
            result.Success = true;
            
            _progressReporter?.Report("All benchmarks completed successfully.");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _progressReporter?.Report($"Benchmark execution failed: {ex.Message}");
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Monitors memory usage during operation execution
    /// </summary>
    public async Task<MemoryUsageResult> MonitorMemoryUsageAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var result = new MemoryUsageResult();
        var process = Process.GetCurrentProcess();
        
        // Record initial memory state
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        result.InitialMemoryBytes = GC.GetTotalMemory(false);
        result.InitialWorkingSetBytes = process.WorkingSet64;
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Execute the operation
            var operationResult = await operation(cancellationToken);
            result.OperationResult = operationResult;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            
            // Record final memory state
            result.FinalMemoryBytes = GC.GetTotalMemory(false);
            result.FinalWorkingSetBytes = process.WorkingSet64;
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            
            // Calculate memory differences
            result.ManagedMemoryDeltaBytes = result.FinalMemoryBytes - result.InitialMemoryBytes;
            result.WorkingSetDeltaBytes = result.FinalWorkingSetBytes - result.InitialWorkingSetBytes;
            
            // Force garbage collection and measure again for more accurate results
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            result.PostGCMemoryBytes = GC.GetTotalMemory(false);
            result.MemoryLeakIndicatorBytes = result.PostGCMemoryBytes - result.InitialMemoryBytes;
        }
        
        return result;
    }

    /// <summary>
    /// Runs performance regression tests
    /// </summary>
    public async Task<PerformanceRegressionResult> RunPerformanceRegressionTestsAsync(
        Dictionary<string, TimeSpan> performanceThresholds,
        CancellationToken cancellationToken = default)
    {
        var result = new PerformanceRegressionResult();
        
        try
        {
            _progressReporter?.Report("Running performance regression tests...");
            
            // Test policy comparison performance
            if (performanceThresholds.ContainsKey("PolicyComparison"))
            {
                var threshold = performanceThresholds["PolicyComparison"];
                var comparisonTime = await MeasurePolicyComparisonPerformance(cancellationToken);
                
                result.Results["PolicyComparison"] = new RegressionTestResult
                {
                    TestName = "PolicyComparison",
                    ExecutionTime = comparisonTime,
                    Threshold = threshold,
                    Passed = comparisonTime <= threshold
                };
            }
            
            // Test file I/O performance
            if (performanceThresholds.ContainsKey("FileIO"))
            {
                var threshold = performanceThresholds["FileIO"];
                var fileIOTime = await MeasureFileIOPerformance(cancellationToken);
                
                result.Results["FileIO"] = new RegressionTestResult
                {
                    TestName = "FileIO",
                    ExecutionTime = fileIOTime,
                    Threshold = threshold,
                    Passed = fileIOTime <= threshold
                };
            }
            
            result.AllTestsPassed = result.Results.Values.All(r => r.Passed);
            result.Success = true;
            
            _progressReporter?.Report($"Performance regression tests completed. Passed: {result.AllTestsPassed}");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _progressReporter?.Report($"Performance regression tests failed: {ex.Message}");
        }
        
        return result;
    }

    private async Task<TimeSpan> MeasurePolicyComparisonPerformance(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Simulate policy comparison work
        await Task.Delay(100, cancellationToken); // Placeholder for actual policy comparison
        
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    private async Task<TimeSpan> MeasureFileIOPerformance(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Simulate file I/O work
        await Task.Delay(50, cancellationToken); // Placeholder for actual file I/O
        
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }
}

/// <summary>
/// Result of benchmark execution
/// </summary>
public class BenchmarkExecutionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public object? PolicyBenchmarkResults { get; set; }
    public object? FileIOBenchmarkResults { get; set; }
    public object? MemoryBenchmarkResults { get; set; }
}

/// <summary>
/// Result of memory usage monitoring
/// </summary>
public class MemoryUsageResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public object? OperationResult { get; set; }
    public long ExecutionTimeMs { get; set; }
    
    // Memory measurements
    public long InitialMemoryBytes { get; set; }
    public long FinalMemoryBytes { get; set; }
    public long PostGCMemoryBytes { get; set; }
    public long InitialWorkingSetBytes { get; set; }
    public long FinalWorkingSetBytes { get; set; }
    
    // Calculated deltas
    public long ManagedMemoryDeltaBytes { get; set; }
    public long WorkingSetDeltaBytes { get; set; }
    public long MemoryLeakIndicatorBytes { get; set; }
    
    // Formatted properties for display
    public string ManagedMemoryDeltaMB => (ManagedMemoryDeltaBytes / 1024.0 / 1024.0).ToString("F2");
    public string WorkingSetDeltaMB => (WorkingSetDeltaBytes / 1024.0 / 1024.0).ToString("F2");
    public string MemoryLeakIndicatorMB => (MemoryLeakIndicatorBytes / 1024.0 / 1024.0).ToString("F2");
}

/// <summary>
/// Result of performance regression testing
/// </summary>
public class PerformanceRegressionResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool AllTestsPassed { get; set; }
    public Dictionary<string, RegressionTestResult> Results { get; set; } = new();
}

/// <summary>
/// Individual regression test result
/// </summary>
public class RegressionTestResult
{
    public string TestName { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public TimeSpan Threshold { get; set; }
    public bool Passed { get; set; }
    
    public string FormattedResult => $"{TestName}: {ExecutionTime.TotalMilliseconds:F0}ms (Threshold: {Threshold.TotalMilliseconds:F0}ms) - {(Passed ? "PASS" : "FAIL")}";
}

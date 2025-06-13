

# Performance Benchmarking and Monitoring Guide

This document provides comprehensive guidance on using the performance benchmarking and monitoring capabilities of CA_Scanner.

## Overview

CA_Scanner includes a complete performance benchmarking suite designed to:
- **Monitor Performance**: Track execution time, memory usage, and throughput
- **Detect Regressions**: Automatically identify performance degradations in CI/CD
- **Optimize Operations**: Provide insights for performance improvements
- **Report Progress**: Show real-time progress for long-running operations

## Quick Start

### Running All Benchmarks
```bash
# Run comprehensive benchmark suite
dotnet run --project ConditionalAccessExporter benchmark

# Run specific benchmark types
dotnet run --project ConditionalAccessExporter benchmark --type benchmarks
dotnet run --project ConditionalAccessExporter benchmark --type regression
dotnet run --project ConditionalAccessExporter benchmark --type memory

# Save results to file
dotnet run --project ConditionalAccessExporter benchmark --output results.txt
```

### Available Benchmark Types

| Type | Description | Purpose |
|------|-------------|---------|
| `all` | Run all benchmark categories | Comprehensive performance analysis |
| `benchmarks` | BenchmarkDotNet performance tests | Detailed performance measurements |
| `regression` | Performance regression tests | CI/CD quality gates |
| `memory` | Memory usage monitoring | Memory leak detection |

## Benchmark Categories

### 1. Policy Processing Benchmarks

Tests performance of core policy operations:

```bash
# Benchmarks include:
# - JSON serialization/deserialization
# - Policy comparison operations
# - Terraform conversion processes
# - Policy validation
# - Parallel processing scenarios
```

**Key Metrics:**
- **Throughput**: Policies processed per second
- **Latency**: Time per individual operation
- **Memory Usage**: Memory consumption patterns
- **Scalability**: Performance with varying dataset sizes

### 2. File I/O Benchmarks

Measures file system operation performance:

```bash
# Benchmarks include:
# - Synchronous vs asynchronous file operations
# - Small vs large file processing
# - Multiple file handling
# - Streaming vs buffered operations
```

**Key Metrics:**
- **Read/Write Speed**: MB/s throughput
- **IOPS**: Input/output operations per second
- **Memory Efficiency**: Memory usage during I/O
- **Concurrency Performance**: Parallel file operations

### 3. Memory Usage Benchmarks

Analyzes memory consumption patterns:

```bash
# Benchmarks include:
# - Object allocation patterns
# - Garbage collection impact
# - Memory leak detection
# - Large dataset handling
```

**Key Metrics:**
- **Memory Allocation**: Bytes allocated per operation
- **GC Pressure**: Garbage collection frequency
- **Memory Leaks**: Post-GC memory retention
- **Working Set**: Total process memory usage

## Performance Regression Testing

### Setting Up Thresholds

Define performance thresholds for automated testing:

```csharp
var thresholds = new Dictionary<string, TimeSpan>
{
    ["PolicyComparison"] = TimeSpan.FromSeconds(5),
    ["FileIO"] = TimeSpan.FromSeconds(2),
    ["JsonSerialization"] = TimeSpan.FromSeconds(1),
    ["TerraformConversion"] = TimeSpan.FromSeconds(3)
};
```

### CI/CD Integration

Add performance regression tests to your CI/CD pipeline:

```yaml
# Example GitHub Actions workflow step
- name: Run Performance Tests
  run: |
    dotnet run --project ConditionalAccessExporter benchmark --type regression
  env:
    PERFORMANCE_THRESHOLD_POLICY_COMPARISON: 5000  # 5 seconds in ms
    PERFORMANCE_THRESHOLD_FILE_IO: 2000            # 2 seconds in ms
```

### Interpreting Results

Performance regression test results include:

```
✓ PolicyComparison: 1250ms (Threshold: 5000ms) - PASS
✗ FileIO: 3500ms (Threshold: 2000ms) - FAIL
✓ JsonSerialization: 750ms (Threshold: 1000ms) - PASS
```

**Status Indicators:**
- ✓ **PASS**: Operation completed within threshold
- ✗ **FAIL**: Operation exceeded performance threshold

## Memory Monitoring

### Real-time Memory Tracking

Monitor memory usage during operations:

```csharp
var benchmarkService = new PerformanceBenchmarkService();

var result = await benchmarkService.MonitorMemoryUsageAsync(async ct =>
{
    // Your operation here
    await ProcessPoliciesAsync(ct);
    return "Operation completed";
}, cancellationToken);

Console.WriteLine($"Memory Delta: {result.ManagedMemoryDeltaMB} MB");
Console.WriteLine($"Execution Time: {result.ExecutionTimeMs}ms");
```

### Memory Metrics

| Metric | Description | Purpose |
|--------|-------------|---------|
| **Initial Memory** | Memory usage before operation | Baseline measurement |
| **Final Memory** | Memory usage after operation | Total consumption |
| **Memory Delta** | Difference between final and initial | Operation impact |
| **Post-GC Memory** | Memory after garbage collection | Leak detection |
| **Working Set** | Total process memory | System impact |

### Memory Leak Detection

The system automatically detects potential memory leaks:

```
Memory Usage Analysis:
  Initial Memory: 45.2 MB
  Final Memory: 67.8 MB
  Memory Delta: +22.6 MB
  Post-GC Memory: 46.1 MB
  Memory Leak Indicator: +0.9 MB ⚠️
```

**Leak Indicators:**
- **< 1 MB**: Normal operation
- **1-10 MB**: Monitor closely
- **> 10 MB**: Investigate potential leak

## Progress Indicators

### Real-time Progress Reporting

Long-running operations display real-time progress:

```
Policy Processing: 150/500 (30.0%) | Elapsed: 00:02:15 | ETA: 00:05:30 | Rate: 1.1 items/sec | Memory: 67.3 MB | Delta: +12.4 MB
```

**Progress Components:**
- **Progress**: Completed/Total items and percentage
- **Elapsed Time**: Time since operation started
- **ETA**: Estimated time to completion
- **Rate**: Items processed per second
- **Memory**: Current memory usage
- **Delta**: Memory change since start

### Custom Progress Indicators

Implement custom progress tracking:

```csharp
var progressReporter = new ConsoleProgressReporter();
var progress = new Progress<ProgressInfo>(info => progressReporter.Report(info));

using var progressIndicator = new ProgressIndicator("Processing Policies", 1000, progress);

for (int i = 0; i < 1000; i++)
{
    // Process item
    await ProcessPolicyAsync(policies[i]);
    progressIndicator.IncrementProgress();
}

progressReporter.Complete();
```

## Performance Thresholds

### Configurable Thresholds

Set performance thresholds for monitoring:

```csharp
var thresholds = new Dictionary<string, PerformanceThreshold>
{
    ["PolicyProcessing"] = new PerformanceThreshold
    {
        Name = "Policy Processing",
        MaxExecutionTime = TimeSpan.FromMinutes(5),
        MinItemsPerSecond = 2.0,
        MaxMemoryUsageBytes = 500 * 1024 * 1024, // 500 MB
        MaxMemoryDeltaBytes = 100 * 1024 * 1024  // 100 MB
    }
};
```

### Alert Types

The system generates alerts for threshold violations:

| Alert Type | Description | Action Required |
|------------|-------------|-----------------|
| **ExecutionTimeExceeded** | Operation took longer than expected | Check for performance bottlenecks |
| **ThroughputBelowThreshold** | Processing rate is too slow | Optimize algorithms or increase resources |
| **MemoryUsageExceeded** | Memory consumption is too high | Review memory usage patterns |
| **MemoryLeakDetected** | Potential memory leak identified | Investigate object lifecycle management |

## Best Practices

### 1. Regular Benchmarking

- Run benchmarks after significant code changes
- Include benchmarks in CI/CD pipelines
- Track performance trends over time
- Set realistic performance thresholds

### 2. Memory Management

- Monitor memory usage in production scenarios
- Use streaming for large datasets
- Implement proper disposal patterns
- Minimize object allocations in hot paths

### 3. Performance Optimization

- Profile code to identify bottlenecks
- Use asynchronous operations for I/O
- Implement caching where appropriate
- Consider parallel processing for large datasets

### 4. CI/CD Integration

- Fail builds on performance regressions
- Generate performance reports
- Track performance metrics over time
- Alert on threshold violations

## Troubleshooting

### Common Issues

**Benchmark Failures:**
```bash
# Check .NET 8 compatibility
dotnet --version

# Verify BenchmarkDotNet dependencies
dotnet restore

# Run with verbose output
dotnet run benchmark --verbosity normal
```

**Memory Issues:**
```bash
# Check available system memory
free -h

# Monitor during execution
dotnet run benchmark --type memory
```

**Performance Degradation:**
```bash
# Run regression tests
dotnet run benchmark --type regression

# Compare with baseline
dotnet run benchmark --output current.txt
diff baseline.txt current.txt
```

### Debug Mode vs Release Mode

Performance benchmarks should be run in **Release** mode:

```bash
# Build in Release mode
dotnet build --configuration Release

# Run benchmarks in Release mode
dotnet run --project ConditionalAccessExporter --configuration Release benchmark
```

## Integration Examples

### GitHub Actions

```yaml
name: Performance Tests
on: [push, pull_request]

jobs:
  performance:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Run Performance Benchmarks
        run: |
          dotnet run --project ConditionalAccessExporter --configuration Release benchmark --type regression
      
      - name: Upload Results
        uses: actions/upload-artifact@v3
        with:
          name: performance-results
          path: benchmark-results.txt
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Performance Tests'
  inputs:
    command: 'run'
    projects: 'ConditionalAccessExporter'
    arguments: '--configuration Release benchmark --type all --output $(Agent.TempDirectory)/perf-results.txt'

- task: PublishTestResults@2
  displayName: 'Publish Performance Results'
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '$(Agent.TempDirectory)/perf-results.txt'
```

## Advanced Configuration

### Environment Variables

Configure benchmarking behavior:

```bash
# Performance thresholds (in milliseconds)
export PERF_THRESHOLD_POLICY_COMPARISON=5000
export PERF_THRESHOLD_FILE_IO=2000
export PERF_THRESHOLD_JSON_SERIALIZATION=1000

# Memory thresholds (in bytes)
export PERF_MAX_MEMORY_USAGE=536870912      # 512 MB
export PERF_MAX_MEMORY_DELTA=104857600      # 100 MB

# Benchmark configuration
export BENCHMARK_WARMUP_COUNT=3
export BENCHMARK_ITERATION_COUNT=10
export BENCHMARK_INVOCATION_COUNT=1
```

### Custom Benchmarks

Create custom benchmarks for specific scenarios:

```csharp
[MemoryDiagnoser]
[SimpleJob]
public class CustomPolicyBenchmarks
{
    [Benchmark]
    public async Task ProcessLargeDataset()
    {
        // Your custom benchmark logic
    }
}
```

## Conclusion

The performance benchmarking suite provides comprehensive insights into CA_Scanner's performance characteristics. Use these tools to:

- **Monitor** application performance continuously
- **Detect** performance regressions early
- **Optimize** critical code paths
- **Ensure** scalability requirements are met

Regular benchmarking and performance monitoring help maintain high-quality, performant applications throughout the development lifecycle.

For additional support or questions about performance benchmarking, refer to the main project documentation or open an issue in the project repository.



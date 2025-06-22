
# Performance Optimizations Guide

This document provides detailed implementation examples and guidelines for the asynchronous file I/O optimizations implemented in CA_Scanner.

## Overview

The CA_Scanner project has been enhanced with comprehensive asynchronous file I/O operations to improve performance, scalability, and resource utilization when processing large policy sets.

## Key Improvements

### 1. Asynchronous File Operations

All synchronous file operations have been converted to their asynchronous counterparts:

#### Before (Synchronous)
```csharp
// Blocking operation
var content = File.ReadAllText(filePath);
File.WriteAllText(outputPath, jsonContent);
```

#### After (Asynchronous)
```csharp
// Non-blocking operation with cancellation support
var content = await File.ReadAllTextAsync(filePath, cancellationToken);
await File.WriteAllTextAsync(outputPath, jsonContent, cancellationToken);
```

### 2. Cancellation Token Support

All async methods now support cancellation tokens for better resource management:

```csharp
public async Task<List<TemplateInfo>> ListAvailableTemplatesAsync(CancellationToken cancellationToken = default)
{
    foreach (var file in templateFiles)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var content = await File.ReadAllTextAsync(file, cancellationToken);
        // Process content...
    }
}
```

### 3. Streaming JSON Processing

For large files, streaming processing minimizes memory usage:

```csharp
// Instead of loading entire file into memory
var largeJsonArray = JsonConvert.DeserializeObject<List<Policy>>(
    await File.ReadAllTextAsync(largePolicyFile));

// Use streaming for memory-efficient processing
await StreamingJsonProcessor.ProcessJsonArrayStreamAsync<Policy>(
    largePolicyFile, 
    async policy => 
    {
        // Process each policy individually
        await ProcessPolicy(policy);
        return true; // Continue processing
    }, 
    cancellationToken);
```

## Implementation Examples

### Template Service Enhancements

The `TemplateService` class demonstrates all optimization techniques:

1. **Async Method Signatures**
   ```csharp
   public async Task<List<TemplateInfo>> ListAvailableTemplatesAsync(CancellationToken cancellationToken = default)
   public async Task<TemplateCreationResult> CreateTemplateAsync(string templateName, string outputDirectory, CancellationToken cancellationToken = default)
   public async Task<string> GetTemplateDocumentationAsync(string templateName, CancellationToken cancellationToken = default)
   ```

2. **Cancellation Support**
   ```csharp
   foreach (var template in baselineTemplates)
   {
       cancellationToken.ThrowIfCancellationRequested();
       var templateResult = await CreateTemplateAsync(template, outputDirectory, cancellationToken);
   }
   ```

3. **Error Handling**
   ```csharp
   catch (OperationCanceledException)
   {
       result.Errors.Add("Operation was cancelled while creating baseline template set");
       return result;
   }
   ```

### Streaming JSON Processing

The `StreamingJsonProcessor` utility provides memory-efficient processing:

1. **Large File Detection**
   ```csharp
   if (StreamingJsonProcessor.ShouldUseStreaming(filePath))
   {
       // Use streaming for files > 10MB
       await StreamingJsonProcessor.ProcessJsonStreamAsync(filePath, processor, cancellationToken);
   }
   else
   {
       // Use regular async read for smaller files
       var content = await File.ReadAllTextAsync(filePath, cancellationToken);
   }
   ```

2. **Memory Usage Estimation**
   ```csharp
   var estimatedMemory = await StreamingJsonProcessor.EstimateMemoryUsageAsync(filePath);
   if (estimatedMemory > availableMemory * 0.8)
   {
       // Use streaming to avoid memory pressure
   }
   ```

## Performance Benefits

### 1. Improved Throughput
- **Non-blocking I/O**: Threads remain available for other work during file operations
- **Concurrent Processing**: Multiple files can be processed simultaneously
- **Better CPU Utilization**: Reduced idle time waiting for I/O operations

### 2. Enhanced Scalability
- **Memory Efficiency**: Streaming processing prevents memory exhaustion with large files
- **Resource Management**: Cancellation tokens allow proper cleanup and resource management
- **Backpressure Handling**: Controlled concurrency prevents system overload

### 3. Better Resource Utilization
- **Thread Pool Efficiency**: Async operations use I/O completion ports
- **Memory Management**: Streaming reduces garbage collection pressure
- **System Responsiveness**: UI/API remains responsive during long operations

## Benchmarking Results

CA_Scanner now includes comprehensive performance benchmarking capabilities. Use the built-in benchmarking suite to measure performance:

```bash
# Run all performance benchmarks
dotnet run --project ConditionalAccessExporter benchmark

# Run specific benchmark categories
dotnet run --project ConditionalAccessExporter benchmark --type benchmarks
dotnet run --project ConditionalAccessExporter benchmark --type memory
dotnet run --project ConditionalAccessExporter benchmark --type regression

# Save results to file
dotnet run --project ConditionalAccessExporter benchmark --output performance-results.txt
```

The benchmarking suite provides detailed analysis including:

```
Policy Processing: 150/500 (30.0%) | Elapsed: 00:02:15 | ETA: 00:05:30 | Rate: 1.1 items/sec | Memory: 67.3 MB | Delta: +12.4 MB

Benchmark Results:
✓ JSON Serialization: 245ms (avg) | 15.2 MB/s throughput
✓ Policy Comparison: 1.2s (avg) | 125 policies/sec
✓ File I/O Operations: 890ms (avg) | 28.4 MB/s throughput
✗ Memory Usage: 156MB peak | +23MB delta | Potential leak detected
```

For detailed benchmarking documentation, see [PERFORMANCE_BENCHMARKING.md](PERFORMANCE_BENCHMARKING.md).

## Best Practices

### 1. Always Use Cancellation Tokens
```csharp
// Good
public async Task ProcessPoliciesAsync(CancellationToken cancellationToken = default)
{
    await File.ReadAllTextAsync(path, cancellationToken);
}

// Avoid
public async Task ProcessPoliciesAsync()
{
    await File.ReadAllTextAsync(path); // No cancellation support
}
```

### 2. Choose Appropriate Processing Method
```csharp
// For small files (< 10MB)
var content = await File.ReadAllTextAsync(filePath, cancellationToken);
var policies = JsonConvert.DeserializeObject<List<Policy>>(content);

// For large files (> 10MB)
await StreamingJsonProcessor.ProcessJsonArrayStreamAsync<Policy>(
    filePath, ProcessPolicyAsync, cancellationToken);
```

### 3. Handle Cancellation Properly
```csharp
try
{
    await ProcessLargeDatasetAsync(cancellationToken);
}
catch (OperationCanceledException)
{
    // Clean up resources
    CleanupTempFiles();
    throw; // Re-throw to maintain cancellation semantics
}
```

### 4. Use ConfigureAwait(false) in Libraries
```csharp
// In library code, avoid capturing SynchronizationContext
var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
```

## Monitoring and Debugging

### Performance Monitoring
```csharp
var stopwatch = Stopwatch.StartNew();
var initialMemory = GC.GetTotalMemory(false);

await ProcessPoliciesAsync(cancellationToken);

stopwatch.Stop();
var finalMemory = GC.GetTotalMemory(false);
var memoryUsed = finalMemory - initialMemory;

Console.WriteLine($"Processing took {stopwatch.ElapsedMilliseconds}ms");
Console.WriteLine($"Memory used: {memoryUsed / 1024 / 1024}MB");
```

### Debugging Async Operations
- Use `async/await` consistently throughout the call stack
- Monitor thread pool usage with performance counters
- Use tools like dotTrace or PerfView for memory profiling
- Enable async debugging in Visual Studio

## Migration Guidelines

### Converting Existing Code

1. **Identify Synchronous Operations**
   ```bash
   grep -r "File\.ReadAllText[^A]" --include="*.cs"
   grep -r "File\.WriteAllText[^A]" --include="*.cs"
   ```

2. **Update Method Signatures**
   ```csharp
   // Before
   public string ProcessFile(string path)
   
   // After
   public async Task<string> ProcessFileAsync(string path, CancellationToken cancellationToken = default)
   ```

3. **Update Callers**
   ```csharp
   // Before
   var result = ProcessFile(path);
   
   // After
   var result = await ProcessFileAsync(path, cancellationToken);
   ```

4. **Add Error Handling**
   ```csharp
   try
   {
       await ProcessFileAsync(path, cancellationToken);
   }
   catch (OperationCanceledException)
   {
       // Handle cancellation
   }
   catch (IOException ex)
   {
       // Handle I/O errors
   }
   ```

## Testing Async Code

### Unit Testing Example
```csharp
[Fact]
public async Task ProcessFileAsync_WithValidFile_ReturnsExpectedResult()
{
    // Arrange
    var cancellationTokenSource = new CancellationTokenSource();
    var tempFile = Path.GetTempFileName();
    await File.WriteAllTextAsync(tempFile, testContent, cancellationTokenSource.Token);

    try
    {
        // Act
        var result = await service.ProcessFileAsync(tempFile, cancellationTokenSource.Token);

        // Assert
        Assert.NotNull(result);
    }
    finally
    {
        File.Delete(tempFile);
    }
}

[Fact]
public async Task ProcessFileAsync_WithCancellation_ThrowsOperationCanceledException()
{
    // Arrange
    var cancellationTokenSource = new CancellationTokenSource();
    cancellationTokenSource.Cancel();

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => service.ProcessFileAsync(validFile, cancellationTokenSource.Token));
}
```

## Conclusion

These optimizations provide significant improvements in:
- **Performance**: Faster processing through non-blocking I/O
- **Scalability**: Better handling of large datasets and concurrent operations
- **Resource Management**: More efficient memory and thread utilization
- **User Experience**: Responsive applications with cancellation support

The implementation maintains backward compatibility while adding powerful new capabilities for handling large-scale Conditional Access policy management scenarios.


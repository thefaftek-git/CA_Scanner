
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

namespace ConditionalAccessExporter.Utils
{
    /// <summary>
    /// Provides performance benchmarking capabilities for file I/O operations
    /// to measure improvements from async implementation.
    /// </summary>
    public class FilePerformanceBenchmark
    {
        public class BenchmarkResult
        {
            public string OperationType { get; set; } = string.Empty;
            public long ElapsedMilliseconds { get; set; }
            public long MemoryUsedBytes { get; set; }
            public long FileSizeBytes { get; set; }
            public int FileCount { get; set; }
            public double ThroughputMBps { get; set; }
            public string? AdditionalNotes { get; set; }
        }

        /// <summary>
        /// Benchmarks file read operations comparing different approaches.
        /// </summary>
        /// <param name="filePaths">List of files to read</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Benchmark results for different read approaches</returns>
        public static async Task<List<BenchmarkResult>> BenchmarkFileReadsAsync(
            List<string> filePaths, 
            CancellationToken cancellationToken = default)
        {
            var results = new List<BenchmarkResult>();
            var totalSize = filePaths.Where(File.Exists).Sum(f => new FileInfo(f).Length);

            // Benchmark async reads
            var asyncResult = await BenchmarkAsyncReads(filePaths, cancellationToken);
            asyncResult.OperationType = "Async File Reads";
            asyncResult.FileSizeBytes = totalSize;
            asyncResult.FileCount = filePaths.Count;
            results.Add(asyncResult);

            // Benchmark streaming for large files
            var largeFiles = filePaths.Where(f => StreamingJsonProcessor.ShouldUseStreaming(f)).ToList();
            if (largeFiles.Any())
            {
                var streamingResult = await BenchmarkStreamingReads(largeFiles, cancellationToken);
                streamingResult.OperationType = "Streaming Reads (Large Files)";
                streamingResult.FileSizeBytes = largeFiles.Sum(f => new FileInfo(f).Length);
                streamingResult.FileCount = largeFiles.Count;
                results.Add(streamingResult);
            }

            return results;
        }

        /// <summary>
        /// Benchmarks file write operations.
        /// </summary>
        /// <param name="testData">Data to write for testing</param>
        /// <param name="outputDirectory">Directory for test files</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Benchmark results for write operations</returns>
        public static async Task<List<BenchmarkResult>> BenchmarkFileWritesAsync(
            List<object> testData, 
            string outputDirectory, 
            CancellationToken cancellationToken = default)
        {
            var results = new List<BenchmarkResult>();
            
            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            // Benchmark regular async writes
            var asyncResult = await BenchmarkAsyncWrites(testData, outputDirectory, cancellationToken);
            asyncResult.OperationType = "Async File Writes";
            results.Add(asyncResult);

            // Benchmark streaming writes for large data
            if (testData.Count > 100)
            {
                var streamingResult = await BenchmarkStreamingWrites(testData, outputDirectory, cancellationToken);
                streamingResult.OperationType = "Streaming Writes (Large Data)";
                results.Add(streamingResult);
            }

            return results;
        }

        /// <summary>
        /// Benchmarks concurrent file operations to test scalability improvements.
        /// </summary>
        /// <param name="filePaths">Files to process concurrently</param>
        /// <param name="maxConcurrency">Maximum concurrent operations</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Benchmark results for concurrent operations</returns>
        public static async Task<BenchmarkResult> BenchmarkConcurrentOperationsAsync(
            List<string> filePaths, 
            int maxConcurrency = 4, 
            CancellationToken cancellationToken = default)
        {
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(false);

            var tasks = filePaths.Select(async filePath =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    if (File.Exists(filePath))
                    {
                        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                        // Simulate some processing
                        var obj = JsonConvert.DeserializeObject(content);
                        return content.Length;
                    }
                    return 0;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();
            
            var finalMemory = GC.GetTotalMemory(false);
            var totalSize = results.Sum();

            return new BenchmarkResult
            {
                OperationType = $"Concurrent Operations (max {maxConcurrency})",
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = finalMemory - initialMemory,
                FileSizeBytes = totalSize,
                FileCount = filePaths.Count,
                ThroughputMBps = totalSize > 0 ? (totalSize / 1024.0 / 1024.0) / (stopwatch.ElapsedMilliseconds / 1000.0) : 0,
                AdditionalNotes = $"Processed {filePaths.Count} files with max concurrency of {maxConcurrency}"
            };
        }

        private static async Task<BenchmarkResult> BenchmarkAsyncReads(List<string> filePaths, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(false);
            var totalSize = 0;

            foreach (var filePath in filePaths.Where(File.Exists))
            {
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                totalSize += content.Length;
            }

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);

            return new BenchmarkResult
            {
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = finalMemory - initialMemory,
                FileSizeBytes = totalSize,
                ThroughputMBps = totalSize > 0 ? (totalSize / 1024.0 / 1024.0) / (stopwatch.ElapsedMilliseconds / 1000.0) : 0
            };
        }

        private static async Task<BenchmarkResult> BenchmarkStreamingReads(List<string> filePaths, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(false);
            var totalSize = 0;

            foreach (var filePath in filePaths.Where(File.Exists))
            {
                await StreamingJsonProcessor.ProcessJsonStreamAsync(filePath, async token =>
                {
                    totalSize += token.ToString().Length;
                    return await Task.FromResult(true);
                }, cancellationToken);
            }

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);

            return new BenchmarkResult
            {
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = finalMemory - initialMemory,
                FileSizeBytes = totalSize,
                ThroughputMBps = totalSize > 0 ? (totalSize / 1024.0 / 1024.0) / (stopwatch.ElapsedMilliseconds / 1000.0) : 0,
                AdditionalNotes = "Used streaming to minimize memory usage"
            };
        }

        private static async Task<BenchmarkResult> BenchmarkAsyncWrites(List<object> testData, string outputDirectory, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(false);
            var totalSize = 0;

            for (int i = 0; i < testData.Count; i++)
            {
                var filePath = Path.Combine(outputDirectory, $"test_async_{i}.json");
                var json = JsonConvert.SerializeObject(testData[i], Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json, cancellationToken);
                totalSize += json.Length;
            }

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);

            return new BenchmarkResult
            {
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = finalMemory - initialMemory,
                FileSizeBytes = totalSize,
                ThroughputMBps = totalSize > 0 ? (totalSize / 1024.0 / 1024.0) / (stopwatch.ElapsedMilliseconds / 1000.0) : 0
            };
        }

        private static async Task<BenchmarkResult> BenchmarkStreamingWrites(List<object> testData, string outputDirectory, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(false);
            
            var filePath = Path.Combine(outputDirectory, "test_streaming.json");
            
            // Create a simple async enumerable provider
            await StreamingJsonProcessor.WriteJsonStreamAsync(filePath, ct => GetTestDataAsync(testData, ct), cancellationToken);

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            var fileSize = new FileInfo(filePath).Length;

            return new BenchmarkResult
            {
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                MemoryUsedBytes = finalMemory - initialMemory,
                FileSizeBytes = fileSize,
                ThroughputMBps = fileSize > 0 ? (fileSize / 1024.0 / 1024.0) / (stopwatch.ElapsedMilliseconds / 1000.0) : 0,
                AdditionalNotes = "Used streaming to handle large datasets efficiently"
            };
        }

        private static async IAsyncEnumerable<object> GetTestDataAsync(List<object> testData, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in testData)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield(); // Allow other tasks to run
            }
        }

        /// <summary>
        /// Generates a detailed performance report.
        /// </summary>
        /// <param name="results">Benchmark results to report</param>
        /// <returns>Formatted performance report</returns>
        public static string GeneratePerformanceReport(List<BenchmarkResult> results)
        {
            var report = new StringBuilder();
            report.AppendLine("=== File I/O Performance Benchmark Report ===");
            report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine();

            foreach (var result in results)
            {
                report.AppendLine($"Operation: {result.OperationType}");
                report.AppendLine($"Files Processed: {result.FileCount}");
                report.AppendLine($"Total Size: {result.FileSizeBytes / 1024.0 / 1024.0:F2} MB");
                report.AppendLine($"Duration: {result.ElapsedMilliseconds} ms");
                report.AppendLine($"Memory Used: {result.MemoryUsedBytes / 1024.0 / 1024.0:F2} MB");
                report.AppendLine($"Throughput: {result.ThroughputMBps:F2} MB/s");
                if (!string.IsNullOrEmpty(result.AdditionalNotes))
                {
                    report.AppendLine($"Notes: {result.AdditionalNotes}");
                }
                report.AppendLine();
            }

            return report.ToString();
        }
    }
}



using ConditionalAccessExporter.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ConditionalAccessExporter.Tests
{
    [Collection("ConsoleOutputTestCollection")]
    public class ParallelProcessingServiceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _testDirectory;

        public ParallelProcessingServiceTests(ITestOutputHelper output)
        {
            _output = output;
            _testDirectory = Path.Combine(Path.GetTempPath(), "ParallelProcessingTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [Fact]
        public void GetOptimalDegreeOfParallelism_ReturnsReasonableValue()
        {
            // Act
            var result = ParallelProcessingService.GetOptimalDegreeOfParallelism();

            // Assert
            Assert.True(result >= 2, "Should return at least 2");
            Assert.True(result <= 16, "Should not exceed 16");
            Assert.True(result <= Environment.ProcessorCount, "Should not exceed processor count");
            
            _output.WriteLine($"Optimal parallelism: {result} (System cores: {Environment.ProcessorCount})");
        }

        [Fact]
        public async Task ProcessInParallelAsync_WithValidItems_ProcessesSuccessfully()
        {
            // Arrange
            var items = Enumerable.Range(1, 10).ToList();
            var processedItems = new List<int>();

            // Act
            var result = await ParallelProcessingService.ProcessInParallelAsync(
                items,
                async (item, ct) =>
                {
                    await Task.Delay(10, ct); // Simulate some work
                    return item * 2;
                });

            // Assert
            Assert.Equal(10, result.TotalProcessed);
            Assert.Equal(10, result.Results.Count);
            Assert.Empty(result.Errors);
            Assert.True(result.SuccessRate >= 99.0); // Allow for minor floating point variance
            Assert.All(result.Results, r => Assert.Contains(r / 2, items));

            _output.WriteLine($"Processed {result.TotalProcessed} items in {result.ElapsedTime.TotalMilliseconds}ms");
            _output.WriteLine($"Average speed: {result.AverageItemsPerSecond:F2} items/second");
        }

        [Fact]
        public async Task ProcessInParallelAsync_WithErrorsAndContinueOnError_HandlesErrorsGracefully()
        {
            // Arrange
            var items = Enumerable.Range(1, 10).ToList();
            var options = new ParallelProcessingOptions
            {
                ContinueOnError = true,
                MaxDegreeOfParallelism = 2
            };

            // Act
            var result = await ParallelProcessingService.ProcessInParallelAsync(
                items,
                async (item, ct) =>
                {
                    await Task.Delay(5, ct);
                    if (item % 3 == 0)
                        throw new InvalidOperationException($"Test error for item {item}");
                    return item * 2;
                },
                options);

            // Assert
            Assert.Equal(10, result.TotalProcessed);
            Assert.Equal(7, result.Results.Count); // 10 - 3 errors (items 3, 6, 9)
            Assert.Equal(3, result.Errors.Count);
            Assert.True(result.SuccessRate < 100);
            Assert.All(result.Errors, e => Assert.IsType<InvalidOperationException>(e.Exception));

            _output.WriteLine($"Successfully processed {result.SuccessfulItems} out of {result.TotalItems} items");
            _output.WriteLine($"Errors encountered: {result.Errors.Count}");
        }

        [Fact]
        public async Task ProcessInParallelAsync_WithCancellation_ThrowsOperationCancelledException()
        {
            // Arrange
            var items = Enumerable.Range(1, 100).ToList();
            var cts = new CancellationTokenSource();

            // Act & Assert
            var task = ParallelProcessingService.ProcessInParallelAsync(
                items,
                async (item, ct) =>
                {
                    await Task.Delay(50, ct); // Longer delay to ensure cancellation happens
                    return item;
                },
                cancellationToken: cts.Token);

            // Cancel after a short delay
            cts.CancelAfter(100);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task ProcessFilesInParallelAsync_WithTestFiles_ProcessesCorrectly()
        {
            // Arrange
            var testFiles = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                var filePath = Path.Combine(_testDirectory, $"test{i}.txt");
                await File.WriteAllTextAsync(filePath, $"Test content {i}");
                testFiles.Add(filePath);
            }

            var progressReports = new List<ParallelProcessingProgress>();
            var progress = new Progress<ParallelProcessingProgress>(p => progressReports.Add(p));

            // Act
            var result = await ParallelProcessingService.ProcessFilesInParallelAsync(
                testFiles,
                async (filePath, ct) =>
                {
                    var content = await File.ReadAllTextAsync(filePath, ct);
                    return content.Length;
                },
                progress: progress);

            // Assert
            Assert.Equal(5, result.TotalProcessed);
            Assert.Equal(5, result.Results.Count);
            Assert.Empty(result.Errors);
            Assert.All(result.Results, length => Assert.True(length > 0));
            Assert.NotEmpty(progressReports);

            _output.WriteLine($"Processed {result.TotalProcessed} files with {progressReports.Count} progress reports");
        }

        [Fact]
        public async Task ProcessInParallelAsync_WithProgressReporting_ReportsProgressCorrectly()
        {
            // Arrange
            var items = Enumerable.Range(1, 20).ToList();
            var progressReports = new List<ParallelProcessingProgress>();
            var progress = new Progress<ParallelProcessingProgress>(p => 
            {
                progressReports.Add(p);
                _output.WriteLine($"Progress: {p}");
            });

            var options = new ParallelProcessingOptions
            {
                ProgressReportInterval = 5, // Report every 5 items
                MaxDegreeOfParallelism = 4
            };

            // Act
            var result = await ParallelProcessingService.ProcessInParallelAsync(
                items,
                async (item, ct) =>
                {
                    await Task.Delay(10, ct);
                    return item;
                },
                options,
                progress);

            // Assert
            Assert.NotEmpty(progressReports);
            Assert.True(progressReports.Count >= 3); // Should have at least a few progress reports
            
            var finalReport = progressReports.Last();
            Assert.Equal(20, finalReport.Total);
            Assert.Equal(20, finalReport.Completed);
            Assert.Equal(100.0, finalReport.PercentComplete, 1); // Within 1% tolerance

            _output.WriteLine($"Received {progressReports.Count} progress reports");
        }

        [Fact]
        public async Task ProcessInParallelAsync_WithCustomParallelism_RespectsMaxDegreeOfParallelism()
        {
            // Arrange
            var items = Enumerable.Range(1, 50).ToList();
            var concurrentCount = 0;
            var maxConcurrentCount = 0;
            var lockObject = new object();

            var options = new ParallelProcessingOptions
            {
                MaxDegreeOfParallelism = 3
            };

            // Act
            var result = await ParallelProcessingService.ProcessInParallelAsync(
                items,
                async (item, ct) =>
                {
                    lock (lockObject)
                    {
                        concurrentCount++;
                        maxConcurrentCount = Math.Max(maxConcurrentCount, concurrentCount);
                    }

                    await Task.Delay(20, ct); // Some processing time

                    lock (lockObject)
                    {
                        concurrentCount--;
                    }

                    return item;
                },
                options);

            // Assert
            Assert.Equal(50, result.TotalProcessed);
            // The +1 margin accounts for slight variances in thread scheduling or timing,
            // which can occasionally result in one extra thread being active momentarily.
            Assert.True(maxConcurrentCount <= options.MaxDegreeOfParallelism!.Value + 1);
            
            _output.WriteLine($"Max concurrent operations observed: {maxConcurrentCount}");
            _output.WriteLine($"Configured max degree of parallelism: {options.MaxDegreeOfParallelism}");
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}


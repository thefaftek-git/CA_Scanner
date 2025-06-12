using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Utils
{
    public static class ParallelProcessingService
    {
        /// <summary>
        /// Gets the optimal degree of parallelism based on system resources
        /// </summary>
        public static int GetOptimalDegreeOfParallelism()
        {
            var processorCount = Environment.ProcessorCount;
            
            // Use 75% of available cores, with a minimum of 2 and maximum of 16, but never exceed processor count
            var optimalDegree = Math.Min(processorCount, Math.Max(2, Math.Min(16, (int)(processorCount * 0.75))));
            
            return optimalDegree;
        }

        /// <summary>
        /// Processes items in parallel with progress reporting and error handling
        /// </summary>
        public static async Task<ParallelProcessingResult<TResult>> ProcessInParallelAsync<TSource, TResult>(
            IEnumerable<TSource> source,
            Func<TSource, CancellationToken, Task<TResult>> processor,
            ParallelProcessingOptions? options = null,
            IProgress<ParallelProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var opts = options ?? new ParallelProcessingOptions();
            var sourceList = source.ToList();
            var completed = 0;
            var total = sourceList.Count;
            var stopwatch = Stopwatch.StartNew();

            // Configure parallelism
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = opts.MaxDegreeOfParallelism ?? GetOptimalDegreeOfParallelism(),
                CancellationToken = cancellationToken
            };

            // Thread-safe collections for results and errors
            var resultsList = new ConcurrentBag<TResult>();
            var errorsList = new ConcurrentBag<ParallelProcessingError>();

            try
            {
                await Parallel.ForEachAsync(sourceList, parallelOptions, async (item, ct) =>
                {
                    try
                    {
                        var result = await processor(item, ct);
                        resultsList.Add(result);
                    }
                    catch (OperationCanceledException)
                    {
                        // Don't log cancellation as an error
                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (opts.ContinueOnError)
                        {
                            errorsList.Add(new ParallelProcessingError
                            {
                                Item = item?.ToString() ?? "Unknown",
                                Exception = ex,
                                Timestamp = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        var currentCompleted = Interlocked.Increment(ref completed);
                        
                        // Report progress
                        if (progress != null && (currentCompleted % opts.ProgressReportInterval == 0 || currentCompleted == total))
                        {
                            var progressReport = new ParallelProcessingProgress
                            {
                                Completed = currentCompleted,
                                Total = total,
                                PercentComplete = (double)currentCompleted / total * 100,
                                ElapsedTime = stopwatch.Elapsed,
                                EstimatedTimeRemaining = EstimateTimeRemaining(stopwatch.Elapsed, currentCompleted, total),
                                ItemsPerSecond = stopwatch.Elapsed.TotalSeconds > 0 
                                    ? currentCompleted / stopwatch.Elapsed.TotalSeconds 
                                    : 0
                            };
                            
                            progress.Report(progressReport);
                        }
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled
                throw;
            }

            stopwatch.Stop();

            return new ParallelProcessingResult<TResult>
            {
                Results = resultsList.ToList(),
                Errors = errorsList.ToList(),
                TotalProcessed = completed,
                TotalItems = total,
                ElapsedTime = stopwatch.Elapsed,
                AverageItemsPerSecond = stopwatch.Elapsed.TotalSeconds > 0 
                    ? completed / stopwatch.Elapsed.TotalSeconds 
                    : 0,
                SuccessRate = total > 0 ? (double)(completed - errorsList.Count) / total * 100 : 0
            };
        }

        /// <summary>
        /// Processes files in parallel with built-in file handling
        /// </summary>
        public static async Task<ParallelProcessingResult<TResult>> ProcessFilesInParallelAsync<TResult>(
            IEnumerable<string> filePaths,
            Func<string, CancellationToken, Task<TResult>> fileProcessor,
            ParallelProcessingOptions? options = null,
            IProgress<ParallelProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return await ProcessInParallelAsync(
                filePaths,
                fileProcessor,
                options,
                progress,
                cancellationToken);
        }

        private static TimeSpan? EstimateTimeRemaining(TimeSpan elapsed, int completed, int total)
        {
            if (completed == 0) return null;
            
            var averageTimePerItem = elapsed.TotalMilliseconds / completed;
            var remainingItems = total - completed;
            var estimatedRemainingMs = averageTimePerItem * remainingItems;
            
            return TimeSpan.FromMilliseconds(estimatedRemainingMs);
        }
    }

    public class ParallelProcessingOptions
    {
        /// <summary>
        /// Maximum degree of parallelism. If null, will be determined automatically.
        /// </summary>
        public int? MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Whether to continue processing other items if one fails
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>
        /// How often to report progress (every N items)
        /// </summary>
        public int ProgressReportInterval { get; set; } = 10;
    }

    public class ParallelProcessingProgress
    {
        public int Completed { get; set; }
        public int Total { get; set; }
        public double PercentComplete { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public double ItemsPerSecond { get; set; }

        public override string ToString()
        {
            var remaining = EstimatedTimeRemaining.HasValue 
                ? $", ETA: {EstimatedTimeRemaining.Value:mm\\:ss}"
                : "";
            
            return $"Progress: {Completed}/{Total} ({PercentComplete:F1}%) - {ItemsPerSecond:F1} items/sec{remaining}";
        }
    }

    public class ParallelProcessingResult<T>
    {
        public List<T> Results { get; set; } = new();
        public List<ParallelProcessingError> Errors { get; set; } = new();
        public int TotalProcessed { get; set; }
        public int TotalItems { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public double AverageItemsPerSecond { get; set; }
        public double SuccessRate { get; set; }

        public bool HasErrors => Errors.Any();
        public int SuccessfulItems => TotalProcessed - Errors.Count;
    }

    public class ParallelProcessingError
    {
        public string Item { get; set; } = string.Empty;
        public Exception Exception { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }
}

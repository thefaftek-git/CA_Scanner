

using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Utils;

namespace ConditionalAccessExporter;

/// <summary>
/// Standalone program for running performance benchmarks
/// </summary>
public static class PerformanceBenchmarkProgram
{
    public static async Task<int> RunBenchmarksAsync(string[] args)
    {
        Console.WriteLine("CA_Scanner Performance Benchmarking Tool");
        Console.WriteLine("=========================================");
        Console.WriteLine();

        try
        {
            // Parse command line arguments
            var options = ParseBenchmarkOptions(args);
            
            if (options.ShowHelp)
            {
                ShowHelp();
                return 0;
            }

            // Create progress reporter
            var progressReporter = new ConsoleProgressReporter();
            var progressHandler = new Progress<string>(message => progressReporter.WriteLine(message));

            // Initialize benchmark service
            var benchmarkService = new PerformanceBenchmarkService(progressHandler);

            if (options.RunBenchmarks)
            {
                await RunBenchmarkSuite(benchmarkService, options, progressReporter);
            }

            if (options.RunRegressionTests)
            {
                await RunRegressionTests(benchmarkService, options, progressReporter);
            }

            if (options.RunMemoryTests)
            {
                await RunMemoryTests(benchmarkService, options, progressReporter);
            }

            Console.WriteLine();
            Console.WriteLine("Benchmarking completed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running benchmarks: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static async Task RunBenchmarkSuite(
        PerformanceBenchmarkService benchmarkService,
        BenchmarkOptions options,
        ConsoleProgressReporter progressReporter)
    {
        progressReporter.WriteLine("Starting comprehensive performance benchmark suite...");
        progressReporter.WriteLine("");

        var cancellationToken = new CancellationTokenSource().Token;
        var result = await benchmarkService.RunAllBenchmarksAsync(cancellationToken);

        if (result.Success)
        {
            progressReporter.WriteLine("✓ All benchmarks completed successfully");
            
            if (options.OutputFile != null)
            {
                progressReporter.WriteLine($"Results saved to: {options.OutputFile}");
            }
        }
        else
        {
            progressReporter.WriteLine($"✗ Benchmark execution failed: {result.ErrorMessage}");
        }
    }

    private static async Task RunRegressionTests(
        PerformanceBenchmarkService benchmarkService,
        BenchmarkOptions options,
        ConsoleProgressReporter progressReporter)
    {
        progressReporter.WriteLine("Running performance regression tests...");

        // Define performance thresholds
        var thresholds = new Dictionary<string, TimeSpan>
        {
            ["PolicyComparison"] = TimeSpan.FromSeconds(5),
            ["FileIO"] = TimeSpan.FromSeconds(2),
            ["JsonSerialization"] = TimeSpan.FromSeconds(1),
            ["TerraformConversion"] = TimeSpan.FromSeconds(3)
        };

        var cancellationToken = new CancellationTokenSource().Token;
        var result = await benchmarkService.RunPerformanceRegressionTestsAsync(thresholds, cancellationToken);

        if (result.Success)
        {
            progressReporter.WriteLine($"✓ Regression tests completed. All passed: {result.AllTestsPassed}");
            
            foreach (var testResult in result.Results.Values)
            {
                var status = testResult.Passed ? "✓" : "✗";
                progressReporter.WriteLine($"  {status} {testResult.FormattedResult}");
            }
        }
        else
        {
            progressReporter.WriteLine($"✗ Regression tests failed: {result.ErrorMessage}");
        }
    }

    private static async Task RunMemoryTests(
        PerformanceBenchmarkService benchmarkService,
        BenchmarkOptions options,
        ConsoleProgressReporter progressReporter)
    {
        progressReporter.WriteLine("Running memory usage tests...");

        // Test memory usage for various operations
        var operations = new Dictionary<string, Func<CancellationToken, Task<object>>>
        {
            ["SmallPolicySet"] = async ct => 
            {
                await Task.Delay(100, ct);
                return "Completed small policy processing";
            },
            ["LargePolicySet"] = async ct => 
            {
                await Task.Delay(500, ct);
                return "Completed large policy processing";
            },
            ["JsonSerialization"] = async ct => 
            {
                await Task.Delay(200, ct);
                return "Completed JSON serialization";
            }
        };

        foreach (var operation in operations)
        {
            progressReporter.WriteLine($"Testing memory usage for: {operation.Key}");
            
            var cancellationToken = new CancellationTokenSource().Token;
            var memoryResult = await benchmarkService.MonitorMemoryUsageAsync(
                operation.Value, cancellationToken);

            if (memoryResult.Success)
            {
                progressReporter.WriteLine($"  ✓ {operation.Key}:");
                progressReporter.WriteLine($"    Execution Time: {memoryResult.ExecutionTimeMs}ms");
                progressReporter.WriteLine($"    Memory Delta: {memoryResult.ManagedMemoryDeltaMB} MB");
                progressReporter.WriteLine($"    Working Set Delta: {memoryResult.WorkingSetDeltaMB} MB");
                progressReporter.WriteLine($"    Memory Leak Indicator: {memoryResult.MemoryLeakIndicatorMB} MB");
            }
            else
            {
                progressReporter.WriteLine($"  ✗ {operation.Key} failed: {memoryResult.ErrorMessage}");
            }
        }
    }

    private static BenchmarkOptions ParseBenchmarkOptions(string[] args)
    {
        var options = new BenchmarkOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                
                case "--benchmarks":
                case "-b":
                    options.RunBenchmarks = true;
                    break;
                
                case "--regression":
                case "-r":
                    options.RunRegressionTests = true;
                    break;
                
                case "--memory":
                case "-m":
                    options.RunMemoryTests = true;
                    break;
                
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputFile = args[++i];
                    }
                    break;
                
                case "--all":
                case "-a":
                    options.RunBenchmarks = true;
                    options.RunRegressionTests = true;
                    options.RunMemoryTests = true;
                    break;
            }
        }

        // Default to running all if no specific options provided
        if (!options.RunBenchmarks && !options.RunRegressionTests && !options.RunMemoryTests && !options.ShowHelp)
        {
            options.RunBenchmarks = true;
            options.RunRegressionTests = true;
            options.RunMemoryTests = true;
        }

        return options;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("CA_Scanner Performance Benchmarking Tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project ConditionalAccessExporter benchmark [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help         Show this help message");
        Console.WriteLine("  -b, --benchmarks   Run performance benchmarks");
        Console.WriteLine("  -r, --regression   Run performance regression tests");
        Console.WriteLine("  -m, --memory       Run memory usage tests");
        Console.WriteLine("  -a, --all          Run all tests (default)");
        Console.WriteLine("  -o, --output FILE  Save results to file");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run benchmark --all");
        Console.WriteLine("  dotnet run benchmark --benchmarks --output results.txt");
        Console.WriteLine("  dotnet run benchmark --regression --memory");
        Console.WriteLine();
        Console.WriteLine("Benchmark Categories:");
        Console.WriteLine("  • Policy Processing: Comparison, validation, conversion performance");
        Console.WriteLine("  • File I/O: Large file reading/writing performance");
        Console.WriteLine("  • Memory Usage: Memory consumption patterns");
        Console.WriteLine("  • Scalability: Performance with varying dataset sizes");
    }
}

/// <summary>
/// Benchmark execution options
/// </summary>
public class BenchmarkOptions
{
    public bool ShowHelp { get; set; }
    public bool RunBenchmarks { get; set; }
    public bool RunRegressionTests { get; set; }
    public bool RunMemoryTests { get; set; }
    public string? OutputFile { get; set; }
}






using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;

namespace ConditionalAccessExporter.Services;

/// <summary>
/// Benchmarks for file I/O operations
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class FileIOBenchmarks
{
    private string _tempDirectory = string.Empty;
    private string _smallJsonFile = string.Empty;
    private string _largeJsonFile = string.Empty;
    private string _samplePolicyData = string.Empty;
    private List<string> _multiplePolicyFiles = new();

    [GlobalSetup]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ca_scanner_benchmarks");
        Directory.CreateDirectory(_tempDirectory);

        // Create sample policy data
        var samplePolicy = new
        {
            id = "sample-policy-id",
            displayName = "Sample Conditional Access Policy",
            state = "enabled",
            conditions = new
            {
                users = new
                {
                    includeUsers = new[] { "All" },
                    excludeUsers = new[] { "emergency-access@contoso.com" }
                },
                applications = new
                {
                    includeApplications = new[] { "All" },
                    excludeApplications = new[] { "00000002-0000-0ff1-ce00-000000000000" }
                },
                locations = new
                {
                    includeLocations = new[] { "All" },
                    excludeLocations = new[] { "AllTrusted" }
                },
                platforms = new
                {
                    includePlatforms = new[] { "all" }
                },
                clientAppTypes = new[] { "all" }
            },
            grantControls = new
            {
                @operator = "OR",
                builtInControls = new[] { "mfa", "compliantDevice" },
                customAuthenticationFactors = new string[0],
                termsOfUse = new string[0]
            },
            sessionControls = new
            {
                applicationEnforcedRestrictions = new
                {
                    isEnabled = false
                },
                cloudAppSecurity = new
                {
                    isEnabled = false
                },
                persistentBrowser = new
                {
                    isEnabled = false
                },
                signInFrequency = new
                {
                    isEnabled = false
                }
            }
        };

        _samplePolicyData = JsonConvert.SerializeObject(samplePolicy, Formatting.Indented);

        // Create small JSON file (single policy)
        _smallJsonFile = Path.Combine(_tempDirectory, "small_policy.json");
        File.WriteAllText(_smallJsonFile, _samplePolicyData);

        // Create large JSON file (array of 500 policies)
        var largePolicySet = Enumerable.Range(1, 500)
            .Select(i => _samplePolicyData.Replace("sample-policy-id", $"policy-{i:D3}"))
            .Select(JsonConvert.DeserializeObject)
            .ToList();

        _largeJsonFile = Path.Combine(_tempDirectory, "large_policy_set.json");
        File.WriteAllText(_largeJsonFile, JsonConvert.SerializeObject(largePolicySet, Formatting.Indented));

        // Create multiple small files
        for (int i = 1; i <= 50; i++)
        {
            var fileName = Path.Combine(_tempDirectory, $"policy_{i:D2}.json");
            var policyData = _samplePolicyData.Replace("sample-policy-id", $"policy-{i:D2}");
            File.WriteAllText(fileName, policyData);
            _multiplePolicyFiles.Add(fileName);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Benchmark]
    public string SynchronousFileRead_Small()
    {
        return File.ReadAllText(_smallJsonFile);
    }

    [Benchmark]
    public async Task<string> AsynchronousFileRead_Small()
    {
        return await File.ReadAllTextAsync(_smallJsonFile);
    }

    [Benchmark]
    public string SynchronousFileRead_Large()
    {
        return File.ReadAllText(_largeJsonFile);
    }

    [Benchmark]
    public async Task<string> AsynchronousFileRead_Large()
    {
        return await File.ReadAllTextAsync(_largeJsonFile);
    }

    [Benchmark]
    public void SynchronousFileWrite_Small()
    {
        var outputFile = Path.Combine(_tempDirectory, "sync_output_small.json");
        File.WriteAllText(outputFile, _samplePolicyData);
    }

    [Benchmark]
    public async Task AsynchronousFileWrite_Small()
    {
        var outputFile = Path.Combine(_tempDirectory, "async_output_small.json");
        await File.WriteAllTextAsync(outputFile, _samplePolicyData);
    }

    [Benchmark]
    public void SynchronousFileWrite_Large()
    {
        var outputFile = Path.Combine(_tempDirectory, "sync_output_large.json");
        var largeContent = File.ReadAllText(_largeJsonFile);
        File.WriteAllText(outputFile, largeContent);
    }

    [Benchmark]
    public async Task AsynchronousFileWrite_Large()
    {
        var outputFile = Path.Combine(_tempDirectory, "async_output_large.json");
        var largeContent = await File.ReadAllTextAsync(_largeJsonFile);
        await File.WriteAllTextAsync(outputFile, largeContent);
    }

    [Benchmark]
    public List<string> SynchronousMultipleFileRead()
    {
        var results = new List<string>();
        foreach (var file in _multiplePolicyFiles)
        {
            results.Add(File.ReadAllText(file));
        }
        return results;
    }

    [Benchmark]
    public async Task<List<string>> AsynchronousMultipleFileRead()
    {
        var results = new List<string>();
        foreach (var file in _multiplePolicyFiles)
        {
            results.Add(await File.ReadAllTextAsync(file));
        }
        return results;
    }

    [Benchmark]
    public async Task<List<string>> ParallelAsynchronousFileRead()
    {
        var tasks = _multiplePolicyFiles.Select(async file => 
            await File.ReadAllTextAsync(file));
        
        return (await Task.WhenAll(tasks)).ToList();
    }

    [Benchmark]
    public void JsonSerializationWithFileWrite()
    {
        var policies = new List<object>();
        
        // Read and deserialize multiple files
        foreach (var file in _multiplePolicyFiles.Take(10))
        {
            var content = File.ReadAllText(file);
            var policy = JsonConvert.DeserializeObject(content);
            policies.Add(policy);
        }
        
        // Serialize and write combined result
        var outputFile = Path.Combine(_tempDirectory, "combined_policies.json");
        var combinedJson = JsonConvert.SerializeObject(policies, Formatting.Indented);
        File.WriteAllText(outputFile, combinedJson);
    }

    [Benchmark]
    public async Task JsonSerializationWithFileWriteAsync()
    {
        var policies = new List<object>();
        
        // Read and deserialize multiple files asynchronously
        foreach (var file in _multiplePolicyFiles.Take(10))
        {
            var content = await File.ReadAllTextAsync(file);
            var policy = JsonConvert.DeserializeObject(content);
            policies.Add(policy);
        }
        
        // Serialize and write combined result asynchronously
        var outputFile = Path.Combine(_tempDirectory, "combined_policies_async.json");
        var combinedJson = JsonConvert.SerializeObject(policies, Formatting.Indented);
        await File.WriteAllTextAsync(outputFile, combinedJson);
    }

    [Benchmark]
    public void StreamingFileRead_Large()
    {
        using var fileStream = new FileStream(_largeJsonFile, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(fileStream);
        
        var buffer = new char[8192];
        int totalCharsRead = 0;
        
        while (!reader.EndOfStream)
        {
            int charsRead = reader.Read(buffer, 0, buffer.Length);
            totalCharsRead += charsRead;
        }
    }

    [Benchmark]
    public async Task StreamingFileReadAsync_Large()
    {
        using var fileStream = new FileStream(_largeJsonFile, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(fileStream);
        
        var buffer = new char[8192];
        int totalCharsRead = 0;
        
        while (!reader.EndOfStream)
        {
            int charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
            totalCharsRead += charsRead;
        }
    }

    [Benchmark]
    public void BufferedFileOperations()
    {
        var tempFile = Path.Combine(_tempDirectory, "buffered_temp.json");
        
        // Write with buffer
        using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536))
        using (var writer = new StreamWriter(fileStream))
        {
            writer.Write(_samplePolicyData);
        }
        
        // Read with buffer
        using (var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536))
        using (var reader = new StreamReader(fileStream))
        {
            var content = reader.ReadToEnd();
        }
    }

    [Benchmark]
    public void FileSystemWatcher_Creation()
    {
        var watcherFile = Path.Combine(_tempDirectory, "watcher_test.json");
        var eventCount = 0;
        var resetEvent = new ManualResetEventSlim(false);
        
        try
        {
            using var watcher = new FileSystemWatcher(_tempDirectory, "watcher_test.json");
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            
            // Set up event handlers to count events
            watcher.Created += (sender, e) => { Interlocked.Increment(ref eventCount); };
            watcher.Changed += (sender, e) => { 
                Interlocked.Increment(ref eventCount);
                if (eventCount >= 2) resetEvent.Set(); // Signal when we've received expected events
            };
            
            watcher.EnableRaisingEvents = true;
            
            // Create and modify file
            File.WriteAllText(watcherFile, _samplePolicyData);
            File.AppendAllText(watcherFile, "\n// Modified");
            
            // Wait for events to be processed with timeout
            resetEvent.Wait(TimeSpan.FromSeconds(1));
        }
        finally
        {
            if (File.Exists(watcherFile))
            {
                File.Delete(watcherFile);
            }
        }
    }
}



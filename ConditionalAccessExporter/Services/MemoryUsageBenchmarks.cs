


using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using System.Text;

namespace ConditionalAccessExporter.Services;

/// <summary>
/// Benchmarks for memory usage patterns and optimization
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MemoryUsageBenchmarks
{
    private List<string> _smallDataSet = new();
    private List<string> _largeDataSet = new();
    private string _largePolicyJson = string.Empty;
    private byte[] _largeBinaryData = Array.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        // Create a complex policy object for memory testing
        var complexPolicy = new
        {
            id = "memory-test-policy",
            displayName = "Memory Usage Test Policy with Very Long Name and Detailed Description",
            description = string.Join(" ", Enumerable.Range(1, 100).Select(i => $"Description word {i}")),
            state = "enabled",
            conditions = new
            {
                users = new
                {
                    includeUsers = Enumerable.Range(1, 50).Select(i => $"user-{i}@contoso.com").ToArray(),
                    excludeUsers = Enumerable.Range(1, 25).Select(i => $"exclude-user-{i}@contoso.com").ToArray(),
                    includeGroups = Enumerable.Range(1, 20).Select(i => $"group-{i}").ToArray(),
                    excludeGroups = Enumerable.Range(1, 10).Select(i => $"exclude-group-{i}").ToArray()
                },
                applications = new
                {
                    includeApplications = Enumerable.Range(1, 30).Select(i => $"app-{i}").ToArray(),
                    excludeApplications = Enumerable.Range(1, 15).Select(i => $"exclude-app-{i}").ToArray()
                },
                locations = new
                {
                    includeLocations = Enumerable.Range(1, 20).Select(i => $"location-{i}").ToArray(),
                    excludeLocations = Enumerable.Range(1, 10).Select(i => $"exclude-location-{i}").ToArray()
                },
                platforms = new
                {
                    includePlatforms = new[] { "android", "iOS", "windows", "macOS", "linux" },
                    excludePlatforms = new[] { "windowsPhone" }
                },
                clientAppTypes = new[] { "browser", "mobileAppsAndDesktopClients", "exchangeActiveSync", "other" },
                deviceStates = new
                {
                    includeStates = new[] { "All" },
                    excludeStates = new[] { "compliant", "domainJoined" }
                }
            },
            grantControls = new
            {
                @operator = "OR",
                builtInControls = new[] { "mfa", "compliantDevice", "domainJoinedDevice", "approvedApplication", "compliantApplication" },
                customAuthenticationFactors = Enumerable.Range(1, 5).Select(i => $"custom-factor-{i}").ToArray(),
                termsOfUse = Enumerable.Range(1, 3).Select(i => $"terms-of-use-{i}").ToArray()
            },
            sessionControls = new
            {
                applicationEnforcedRestrictions = new
                {
                    isEnabled = true
                },
                cloudAppSecurity = new
                {
                    isEnabled = true,
                    cloudAppSecurityType = "monitorOnly"
                },
                persistentBrowser = new
                {
                    isEnabled = true,
                    mode = "always"
                },
                signInFrequency = new
                {
                    isEnabled = true,
                    type = "hours",
                    value = 8
                }
            },
            metadata = new
            {
                createdDateTime = DateTime.UtcNow,
                modifiedDateTime = DateTime.UtcNow,
                version = "1.0.0",
                tags = Enumerable.Range(1, 20).Select(i => $"tag-{i}").ToArray(),
                customData = Enumerable.Range(1, 50).ToDictionary(i => $"key-{i}", i => $"value-{i}")
            }
        };

        _largePolicyJson = JsonConvert.SerializeObject(complexPolicy, Formatting.Indented);

        // Create small dataset (10 policies)
        _smallDataSet = Enumerable.Range(1, 10)
            .Select(i => _largePolicyJson.Replace("memory-test-policy", $"policy-{i}"))
            .ToList();

        // Create large dataset (1000 policies)
        _largeDataSet = Enumerable.Range(1, 1000)
            .Select(i => _largePolicyJson.Replace("memory-test-policy", $"policy-{i}"))
            .ToList();

        // Create large binary data (10MB)
        _largeBinaryData = new byte[10 * 1024 * 1024];
        new Random(42).NextBytes(_largeBinaryData);
    }

    [Benchmark]
    public List<object> StringToObject_SmallDataSet()
    {
        var results = new List<object>();
        foreach (var json in _smallDataSet)
        {
            var obj = JsonConvert.DeserializeObject(json);
            results.Add(obj);
        }
        return results;
    }

    [Benchmark]
    public List<object> StringToObject_LargeDataSet()
    {
        var results = new List<object>();
        foreach (var json in _largeDataSet)
        {
            var obj = JsonConvert.DeserializeObject(json);
            results.Add(obj);
        }
        return results;
    }

    [Benchmark]
    public void StringConcatenation_Large()
    {
        var result = string.Empty;
        foreach (var json in _smallDataSet)
        {
            result += json + Environment.NewLine;
        }
    }

    [Benchmark]
    public string StringBuilder_Large()
    {
        var sb = new StringBuilder();
        foreach (var json in _smallDataSet)
        {
            sb.AppendLine(json);
        }
        return sb.ToString();
    }

    [Benchmark]
    public void ArrayCreation_Large()
    {
        var arrays = new List<byte[]>();
        for (int i = 0; i < 100; i++)
        {
            var array = new byte[1024 * 1024]; // 1MB each
            arrays.Add(array);
        }
    }

    [Benchmark]
    public void ArrayPoolUsage_Large()
    {
        var pool = System.Buffers.ArrayPool<byte>.Shared;
        var arrays = new List<byte[]>();
        
        try
        {
            for (int i = 0; i < 100; i++)
            {
                var array = pool.Rent(1024 * 1024); // 1MB each
                arrays.Add(array);
            }
        }
        finally
        {
            foreach (var array in arrays)
            {
                pool.Return(array);
            }
        }
    }

    [Benchmark]
    public void MemoryStreamUsage_Large()
    {
        using var memoryStream = new MemoryStream();
        for (int i = 0; i < 1000; i++)
        {
            var data = Encoding.UTF8.GetBytes(_largePolicyJson);
            memoryStream.Write(data, 0, data.Length);
        }
    }

    [Benchmark]
    public void ListGrowth_Incremental()
    {
        var list = new List<string>();
        for (int i = 0; i < 10000; i++)
        {
            list.Add($"Item {i}");
        }
    }

    [Benchmark]
    public void ListGrowth_PreSized()
    {
        var list = new List<string>(10000);
        for (int i = 0; i < 10000; i++)
        {
            list.Add($"Item {i}");
        }
    }

    [Benchmark]
    public void DictionaryUsage_Large()
    {
        var dictionary = new Dictionary<string, object>();
        for (int i = 0; i < 1000; i++)
        {
            var policy = JsonConvert.DeserializeObject(_largePolicyJson);
            dictionary[$"policy-{i}"] = policy;
        }
    }

    [Benchmark]
    public void DictionaryUsage_PreSized()
    {
        var dictionary = new Dictionary<string, object>(1000);
        for (int i = 0; i < 1000; i++)
        {
            var policy = JsonConvert.DeserializeObject(_largePolicyJson);
            dictionary[$"policy-{i}"] = policy;
        }
    }

    [Benchmark]
    public void GarbageCollection_Frequent()
    {
        var objects = new List<object>();
        
        for (int i = 0; i < 1000; i++)
        {
            // Create temporary objects that will become garbage
            var tempData = new
            {
                Id = i,
                Data = new byte[1024], // 1KB
                Text = string.Join("", Enumerable.Range(1, 100).Select(x => $"Text {x}"))
            };
            
            // Only keep every 10th object
            if (i % 10 == 0)
            {
                objects.Add(tempData);
            }
        }
    }

    [Benchmark]
    public void WeakReferenceUsage()
    {
        var weakReferences = new List<WeakReference>();
        
        for (int i = 0; i < 1000; i++)
        {
            var largeObject = JsonConvert.DeserializeObject(_largePolicyJson);
            weakReferences.Add(new WeakReference(largeObject));
        }
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Check how many objects are still alive
        var aliveCount = weakReferences.Count(wr => wr.IsAlive);
    }

    [Benchmark]
    public void StreamingJsonProcessing()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var jsonWriter = new JsonTextWriter(writer);
        
        jsonWriter.WriteStartArray();
        
        for (int i = 0; i < 100; i++)
        {
            // Write JSON directly to stream without creating intermediate objects
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("id");
            jsonWriter.WriteValue($"streaming-policy-{i}");
            jsonWriter.WritePropertyName("displayName");
            jsonWriter.WriteValue($"Streaming Policy {i}");
            jsonWriter.WritePropertyName("state");
            jsonWriter.WriteValue("enabled");
            jsonWriter.WriteEndObject();
        }
        
        jsonWriter.WriteEndArray();
        jsonWriter.Flush();
    }

    [Benchmark]
    public void NonStreamingJsonProcessing()
    {
        var policies = new List<object>();
        
        for (int i = 0; i < 100; i++)
        {
            var policy = new
            {
                id = $"non-streaming-policy-{i}",
                displayName = $"Non-Streaming Policy {i}",
                state = "enabled"
            };
            policies.Add(policy);
        }
        
        var json = JsonConvert.SerializeObject(policies, Formatting.Indented);
    }

    [Benchmark]
    public void MemoryPressureSimulation()
    {
        var largeObjects = new List<byte[]>();
        
        try
        {
            // Allocate increasingly larger objects until we hit memory pressure
            for (int i = 1; i <= 100; i++)
            {
                var size = i * 1024 * 1024; // i MB
                var largeObject = new byte[size];
                largeObjects.Add(largeObject);
                
                // Simulate some work with the object
                largeObject[0] = (byte)(i % 256);
                largeObject[size - 1] = (byte)(i % 256);
            }
        }
        catch (OutOfMemoryException)
        {
            // Handle memory pressure gracefully
        }
        finally
        {
            largeObjects.Clear();
            GC.Collect();
        }
    }

    [Benchmark]
    public void DisposablePatternUsage()
    {
        for (int i = 0; i < 1000; i++)
        {
            using var memoryStream = new MemoryStream(_largeBinaryData);
            using var reader = new BinaryReader(memoryStream);
            
            // Read some data
            if (memoryStream.Length >= 4)
            {
                var value = reader.ReadInt32();
            }
        }
    }

    [Benchmark]
    public void UsingDeclarationPattern()
    {
        for (int i = 0; i < 1000; i++)
        {
            using var memoryStream = new MemoryStream(_largeBinaryData);
            using var reader = new BinaryReader(memoryStream);
            
            // Read some data
            if (memoryStream.Length >= 4)
            {
                var value = reader.ReadInt32();
            }
            
            // Objects are automatically disposed at end of scope
        }
    }
}




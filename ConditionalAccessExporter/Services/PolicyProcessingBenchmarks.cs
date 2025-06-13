
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;

namespace ConditionalAccessExporter.Services;

/// <summary>
/// Benchmarks for policy processing operations
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class PolicyProcessingBenchmarks
{
    private List<string> _smallPolicySet = new();
    private List<string> _largePolicySet = new();
    private string _samplePolicyJson = string.Empty;
    private string _sampleTerraformConfig = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        // Create sample policy JSON
        _samplePolicyJson = JsonConvert.SerializeObject(new
        {
            id = "test-policy-id",
            displayName = "Test Conditional Access Policy",
            state = "enabled",
            conditions = new
            {
                users = new
                {
                    includeUsers = new[] { "All" }
                },
                applications = new
                {
                    includeApplications = new[] { "All" }
                }
            },
            grantControls = new
            {
                @operator = "OR",
                builtInControls = new[] { "mfa" }
            }
        }, Formatting.Indented);

        // Create sample Terraform configuration
        _sampleTerraformConfig = @"
resource ""azuread_conditional_access_policy"" ""test_policy"" {
  display_name = ""Test Conditional Access Policy""
  state        = ""enabled""
  
  conditions {
    users {
      included_users = [""All""]
    }
    
    applications {
      included_applications = [""All""]
    }
  }
  
  grant_controls {
    operator          = ""OR""
    built_in_controls = [""mfa""]
  }
}";

        // Create policy sets for scalability testing
        _smallPolicySet = Enumerable.Range(1, 10)
            .Select(i => _samplePolicyJson.Replace("test-policy-id", $"policy-{i}"))
            .ToList();

        _largePolicySet = Enumerable.Range(1, 100)
            .Select(i => _samplePolicyJson.Replace("test-policy-id", $"policy-{i}"))
            .ToList();
    }

    [Benchmark]
    public void JsonDeserialization_SinglePolicy()
    {
        JsonConvert.DeserializeObject(_samplePolicyJson);
    }

    [Benchmark]
    public void JsonSerialization_SinglePolicy()
    {
        var policy = JsonConvert.DeserializeObject(_samplePolicyJson);
        JsonConvert.SerializeObject(policy, Formatting.Indented);
    }

    [Benchmark]
    public void JsonDeserialization_SmallPolicySet()
    {
        foreach (var policyJson in _smallPolicySet)
        {
            JsonConvert.DeserializeObject(policyJson);
        }
    }

    [Benchmark]
    public void JsonDeserialization_LargePolicySet()
    {
        foreach (var policyJson in _largePolicySet)
        {
            JsonConvert.DeserializeObject(policyJson);
        }
    }

    [Benchmark]
    public void PolicyComparison_SmallSet()
    {
        // Simulate policy comparison logic
        for (int i = 0; i < _smallPolicySet.Count - 1; i++)
        {
            var policy1 = JsonConvert.DeserializeObject(_smallPolicySet[i]);
            var policy2 = JsonConvert.DeserializeObject(_smallPolicySet[i + 1]);
            
            // Simple comparison simulation
            var json1 = JsonConvert.SerializeObject(policy1);
            var json2 = JsonConvert.SerializeObject(policy2);
            var areEqual = json1.Equals(json2, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Benchmark]
    public void PolicyComparison_LargeSet()
    {
        // Simulate policy comparison logic for larger set
        var referencePolicy = JsonConvert.DeserializeObject(_largePolicySet[0]);
        var referenceJson = JsonConvert.SerializeObject(referencePolicy);
        
        foreach (var policyJson in _largePolicySet.Skip(1))
        {
            var policy = JsonConvert.DeserializeObject(policyJson);
            var compareJson = JsonConvert.SerializeObject(policy);
            var areEqual = referenceJson.Equals(compareJson, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Benchmark]
    public void TerraformConversion_SinglePolicy()
    {
        // Simulate JSON to Terraform conversion
        var policy = JsonConvert.DeserializeObject(_samplePolicyJson);
        var policyJson = JsonConvert.SerializeObject(policy);
        
        // Simple conversion simulation - in real implementation this would use TerraformConversionService
        var terraformConfig = ConvertJsonToTerraformSimulation(policyJson);
    }

    [Benchmark]
    public void TerraformConversion_SmallPolicySet()
    {
        foreach (var policyJson in _smallPolicySet)
        {
            var policy = JsonConvert.DeserializeObject(policyJson);
            var json = JsonConvert.SerializeObject(policy);
            var terraformConfig = ConvertJsonToTerraformSimulation(json);
        }
    }

    [Benchmark]
    public void PolicyValidation_SmallSet()
    {
        foreach (var policyJson in _smallPolicySet)
        {
            // Simulate policy validation
            var isValid = ValidatePolicySimulation(policyJson);
        }
    }

    [Benchmark]
    public void PolicyValidation_LargeSet()
    {
        foreach (var policyJson in _largePolicySet)
        {
            // Simulate policy validation
            var isValid = ValidatePolicySimulation(policyJson);
        }
    }

    [Benchmark]
    public void ParallelPolicyProcessing_LargeSet()
    {
        // Simulate parallel processing
        Parallel.ForEach(_largePolicySet, policyJson =>
        {
            var policy = JsonConvert.DeserializeObject(policyJson);
            var validated = ValidatePolicySimulation(policyJson);
            var terraform = ConvertJsonToTerraformSimulation(policyJson);
        });
    }

    // Helper methods for simulation
    private string ConvertJsonToTerraformSimulation(string policyJson)
    {
        // Simplified simulation of Terraform conversion
        var policy = JsonConvert.DeserializeObject<dynamic>(policyJson);
        return _sampleTerraformConfig.Replace("Test Conditional Access Policy", 
            policy?.displayName?.ToString() ?? "Generated Policy");
    }

    private bool ValidatePolicySimulation(string policyJson)
    {
        try
        {
            var policy = JsonConvert.DeserializeObject<dynamic>(policyJson);
            
            // Basic validation simulation
            return policy?.id != null && 
                   policy?.displayName != null && 
                   policy?.conditions != null;
        }
        catch
        {
            return false;
        }
    }
}


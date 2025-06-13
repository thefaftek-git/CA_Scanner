
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
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
        // Create varied sample policy configurations to better represent real-world scenarios
        var policyTemplates = new object[]
        {
            // Basic MFA policy
            new
            {
                id = "policy-{0}",
                displayName = "Require MFA for All Users - {0}",
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
                    builtInControls = new[] { "mfa" }
                }
            },
            // Device compliance policy
            new
            {
                id = "policy-{0}",
                displayName = "Require Compliant Device - {0}",
                state = "enabled",
                conditions = new
                {
                    users = new
                    {
                        includeGroups = new[] { "Engineers", "Administrators" }
                    },
                    applications = new
                    {
                        includeApplications = new[] { "Office365", "SharePoint" }
                    },
                    platforms = new
                    {
                        includePlatforms = new[] { "windows", "macOS" }
                    },
                    clientAppTypes = new[] { "browser", "mobileAppsAndDesktopClients" }
                },
                grantControls = new
                {
                    @operator = "AND",
                    builtInControls = new[] { "compliantDevice", "mfa" }
                }
            },
            // Risk-based policy
            new
            {
                id = "policy-{0}",
                displayName = "Block High Risk Sign-ins - {0}",
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
                    },
                    signInRiskLevels = new[] { "high", "medium" },
                    userRiskLevels = new[] { "high" }
                },
                grantControls = new
                {
                    @operator = "OR",
                    builtInControls = new[] { "block" }
                }
            },
            // Session control policy
            new
            {
                id = "policy-{0}",
                displayName = "Limit Browser Session - {0}",
                state = "enabledForReportingButNotEnforced",
                conditions = new
                {
                    users = new
                    {
                        includeUsers = new[] { "All" }
                    },
                    applications = new
                    {
                        includeApplications = new[] { "Office365" }
                    },
                    clientAppTypes = new[] { "browser" }
                },
                sessionControls = new
                {
                    signInFrequency = new
                    {
                        isEnabled = true,
                        type = "hours",
                        value = 4
                    },
                    persistentBrowser = new
                    {
                        isEnabled = true,
                        mode = "never"
                    }
                }
            }
        };

        // Create the first sample policy using the first template
        _samplePolicyJson = JsonConvert.SerializeObject(policyTemplates[0], Formatting.Indented);

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

        // Create varied policy sets for scalability testing using different templates
        _smallPolicySet = Enumerable.Range(1, 10)
            .Select(i => 
            {
                var template = policyTemplates[i % policyTemplates.Length];
                var policyJson = JsonConvert.SerializeObject(template, Formatting.Indented);
                return policyJson.Replace("policy-{0}", $"policy-{i}")
                                .Replace("- {0}", $"- {i}");
            })
            .ToList();

        _largePolicySet = Enumerable.Range(1, 100)
            .Select(i => 
            {
                var template = policyTemplates[i % policyTemplates.Length];
                var policyJson = JsonConvert.SerializeObject(template, Formatting.Indented);
                return policyJson.Replace("policy-{0}", $"policy-{i}")
                                .Replace("- {0}", $"- {i}");
            })
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
    public int PolicyComparison_SmallSet()
    {
        int equalCount = 0;
        // Simulate policy comparison logic
        for (int i = 0; i < _smallPolicySet.Count - 1; i++)
        {
            var policy1 = JsonConvert.DeserializeObject(_smallPolicySet[i]);
            var policy2 = JsonConvert.DeserializeObject(_smallPolicySet[i + 1]);
            
            // Simple comparison simulation
            var json1 = JsonConvert.SerializeObject(policy1);
            var json2 = JsonConvert.SerializeObject(policy2);
            var areEqual = json1.Equals(json2, StringComparison.OrdinalIgnoreCase);
            if (areEqual) equalCount++;
        }
        return equalCount;
    }

    [Benchmark]
    public int PolicyComparison_LargeSet()
    {
        int equalCount = 0;
        // Simulate policy comparison logic for larger set
        var referencePolicy = JsonConvert.DeserializeObject(_largePolicySet[0]);
        var referenceJson = JsonConvert.SerializeObject(referencePolicy);
        
        foreach (var policyJson in _largePolicySet.Skip(1))
        {
            var policy = JsonConvert.DeserializeObject(policyJson);
            var compareJson = JsonConvert.SerializeObject(policy);
            var areEqual = referenceJson.Equals(compareJson, StringComparison.OrdinalIgnoreCase);
            if (areEqual) equalCount++;
        }
        return equalCount;
    }

    [Benchmark]
    public string TerraformConversion_SinglePolicy()
    {
        // Simulate JSON to Terraform conversion
        var policy = JsonConvert.DeserializeObject(_samplePolicyJson);
        var policyJson = JsonConvert.SerializeObject(policy);
        
        // Simple conversion simulation - in real implementation this would use TerraformConversionService
        var terraformConfig = ConvertJsonToTerraformSimulation(policyJson);
        return terraformConfig;
    }

    [Benchmark]
    public int TerraformConversion_SmallPolicySet()
    {
        int conversionCount = 0;
        foreach (var policyJson in _smallPolicySet)
        {
            var policy = JsonConvert.DeserializeObject(policyJson);
            var json = JsonConvert.SerializeObject(policy);
            var terraformConfig = ConvertJsonToTerraformSimulation(json);
            if (!string.IsNullOrEmpty(terraformConfig)) conversionCount++;
        }
        return conversionCount;
    }

    [Benchmark]
    public int PolicyValidation_SmallSet()
    {
        int validCount = 0;
        foreach (var policyJson in _smallPolicySet)
        {
            // Simulate policy validation
            var isValid = ValidatePolicySimulation(policyJson);
            if (isValid) validCount++;
        }
        return validCount;
    }

    [Benchmark]
    public int PolicyValidation_LargeSet()
    {
        int validCount = 0;
        foreach (var policyJson in _largePolicySet)
        {
            // Simulate policy validation
            var isValid = ValidatePolicySimulation(policyJson);
            if (isValid) validCount++;
        }
        return validCount;
    }

    [Benchmark]
    public int ParallelPolicyProcessing_LargeSet()
    {
        var validationResults = new bool[_largePolicySet.Count];
        var conversionResults = new string[_largePolicySet.Count];
        
        // Simulate parallel processing
        Parallel.ForEach(_largePolicySet.Select((json, index) => new { json, index }), item =>
        {
            var policy = JsonConvert.DeserializeObject(item.json);
            var validated = ValidatePolicySimulation(item.json);
            var terraform = ConvertJsonToTerraformSimulation(item.json);
            
            validationResults[item.index] = validated;
            conversionResults[item.index] = terraform;
        });
        
        return validationResults.Count(v => v);
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


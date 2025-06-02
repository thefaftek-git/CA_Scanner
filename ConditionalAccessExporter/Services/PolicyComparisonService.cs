using ConditionalAccessExporter.Models;
using JsonDiffPatchDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConditionalAccessExporter.Services
{
    public class PolicyComparisonService
    {
        private readonly JsonDiffPatch _jsonDiffPatch;

        public PolicyComparisonService()
        {
            _jsonDiffPatch = new JsonDiffPatch();
        }

        public async Task<ComparisonResult> CompareAsync(
            object entraExport, 
            string referenceDirectory, 
            MatchingOptions matchingOptions)
        {
            Console.WriteLine($"Starting comparison with reference directory: {referenceDirectory}");
            
            var result = new ComparisonResult
            {
                ComparedAt = DateTime.UtcNow,
                ReferenceDirectory = referenceDirectory
            };

            // Parse Entra export
            var entraData = ParseEntraExport(entraExport);
            result.TenantId = entraData.TenantId;
            result.Summary.TotalEntraPolicies = entraData.Policies.Count;

            Console.WriteLine($"Found {entraData.Policies.Count} policies in Entra export");

            // Load reference files
            var referencePolicies = await LoadReferencePoliciesAsync(referenceDirectory);
            result.Summary.TotalReferencePolicies = referencePolicies.Count;

            Console.WriteLine($"Found {referencePolicies.Count} reference policy files");

            // Perform comparison
            await PerformComparisonAsync(result, entraData.Policies, referencePolicies, matchingOptions);

            return result;
        }

        private EntraExportData ParseEntraExport(object entraExport)
        {
            JObject jObject;
            
            // Handle different input types: JToken, JSON string, or arbitrary object
            if (entraExport is JToken token)
            {
                if (token.Type == JTokenType.Object)
                {
                    jObject = (JObject)token;
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported JToken type: {token.Type}. Expected a JObject.");
                }
            }
            else if (entraExport is string jsonString)
            {
                jObject = JsonConvert.DeserializeObject<JObject>(jsonString);
            }
            else
            {
                var json = JsonConvert.SerializeObject(entraExport);
                jObject = JsonConvert.DeserializeObject<JObject>(json);
            }
            
            if (jObject == null)
                throw new InvalidOperationException("Failed to parse Entra export data");

            var data = new EntraExportData
            {
                TenantId = jObject["TenantId"]?.ToString() ?? string.Empty,
                Policies = new List<JObject>()
            };

            var policiesArray = jObject["Policies"] as JArray;
            if (policiesArray != null)
            {
                foreach (var policy in policiesArray)
                {
                    if (policy is JObject policyObj)
                    {
                        data.Policies.Add(policyObj);
                    }
                }
            }

            return data;
        }

        private async Task<Dictionary<string, ReferencePolicy>> LoadReferencePoliciesAsync(string directory)
        {
            var policies = new Dictionary<string, ReferencePolicy>();

            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Warning: Reference directory '{directory}' does not exist");
                return policies;
            }

            var jsonFiles = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            
            foreach (var file in jsonFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var policy = JsonConvert.DeserializeObject<JObject>(content);
                    
                    if (policy != null)
                    {
                        var fileName = Path.GetFileName(file);
                        policies[fileName] = new ReferencePolicy
                        {
                            FileName = fileName,
                            FilePath = file,
                            Policy = policy
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load reference file '{file}': {ex.Message}");
                }
            }

            return policies;
        }

        private async Task PerformComparisonAsync(
            ComparisonResult result,
            List<JObject> entraPolicies,
            Dictionary<string, ReferencePolicy> referencePolicies,
            MatchingOptions matchingOptions)
        {
            var matchedReferences = new HashSet<string>();

            // Compare each Entra policy
            foreach (var entraPolicy in entraPolicies)
            {
                var comparison = await CompareEntraPolicyAsync(entraPolicy, referencePolicies, matchingOptions);
                result.PolicyComparisons.Add(comparison);

                if (comparison.ReferenceFileName != null)
                {
                    matchedReferences.Add(comparison.ReferenceFileName);
                }

                // Update summary
                switch (comparison.Status)
                {
                    case ComparisonStatus.EntraOnly:
                        result.Summary.EntraOnlyPolicies++;
                        break;
                    case ComparisonStatus.Identical:
                        result.Summary.MatchingPolicies++;
                        break;
                    case ComparisonStatus.Different:
                        result.Summary.PoliciesWithDifferences++;
                        break;
                }
            }

            // Add unmatched reference policies
            foreach (var kvp in referencePolicies)
            {
                if (!matchedReferences.Contains(kvp.Key))
                {
                    var comparison = new PolicyComparison
                    {
                        PolicyName = kvp.Value.Policy["DisplayName"]?.ToString() ?? "Unknown",
                        Status = ComparisonStatus.ReferenceOnly,
                        ReferenceFileName = kvp.Key,
                        ReferencePolicy = kvp.Value.Policy
                    };
                    result.PolicyComparisons.Add(comparison);
                    result.Summary.ReferenceOnlyPolicies++;
                }
            }
        }

        private async Task<PolicyComparison> CompareEntraPolicyAsync(
            JObject entraPolicy,
            Dictionary<string, ReferencePolicy> referencePolicies,
            MatchingOptions matchingOptions)
        {
            var comparison = new PolicyComparison
            {
                PolicyId = entraPolicy["Id"]?.ToString() ?? string.Empty,
                PolicyName = entraPolicy["DisplayName"]?.ToString() ?? "Unknown",
                EntraPolicy = entraPolicy,
                Status = ComparisonStatus.EntraOnly
            };

            var matchingReference = FindMatchingReference(entraPolicy, referencePolicies, matchingOptions);

            if (matchingReference != null)
            {
                comparison.ReferenceFileName = matchingReference.FileName;
                comparison.ReferencePolicy = matchingReference.Policy;

                // Compare the policies
                var diff = _jsonDiffPatch.Diff(matchingReference.Policy, entraPolicy);
                
                if (diff == null)
                {
                    comparison.Status = ComparisonStatus.Identical;
                }
                else
                {
                    comparison.Status = ComparisonStatus.Different;
                    comparison.Differences = diff;
                }
            }

            return comparison;
        }

        private ReferencePolicy? FindMatchingReference(
            JObject entraPolicy,
            Dictionary<string, ReferencePolicy> referencePolicies,
            MatchingOptions matchingOptions)
        {
            switch (matchingOptions.Strategy)
            {
                case MatchingStrategy.ByName:
                    return FindByName(entraPolicy, referencePolicies, matchingOptions.CaseSensitive);
                
                case MatchingStrategy.ById:
                    return FindById(entraPolicy, referencePolicies);
                
                case MatchingStrategy.CustomMapping:
                    return FindByCustomMapping(entraPolicy, referencePolicies, matchingOptions.CustomMappings);
                
                default:
                    return FindByName(entraPolicy, referencePolicies, matchingOptions.CaseSensitive);
            }
        }

        private ReferencePolicy? FindByName(JObject entraPolicy, Dictionary<string, ReferencePolicy> referencePolicies, bool caseSensitive)
        {
            var entraPolicyName = entraPolicy["DisplayName"]?.ToString();
            if (string.IsNullOrEmpty(entraPolicyName))
                return null;

            foreach (var refPolicy in referencePolicies.Values)
            {
                var refPolicyName = refPolicy.Policy["DisplayName"]?.ToString();
                if (string.IsNullOrEmpty(refPolicyName))
                    continue;

                var comparison = caseSensitive ? 
                    StringComparison.Ordinal : 
                    StringComparison.OrdinalIgnoreCase;

                if (string.Equals(entraPolicyName, refPolicyName, comparison))
                {
                    return refPolicy;
                }
            }

            return null;
        }

        private ReferencePolicy? FindById(JObject entraPolicy, Dictionary<string, ReferencePolicy> referencePolicies)
        {
            var entraPolicyId = entraPolicy["Id"]?.ToString();
            if (string.IsNullOrEmpty(entraPolicyId))
                return null;

            foreach (var refPolicy in referencePolicies.Values)
            {
                var refPolicyId = refPolicy.Policy["Id"]?.ToString();
                if (string.Equals(entraPolicyId, refPolicyId, StringComparison.OrdinalIgnoreCase))
                {
                    return refPolicy;
                }
            }

            return null;
        }

        private ReferencePolicy? FindByCustomMapping(
            JObject entraPolicy, 
            Dictionary<string, ReferencePolicy> referencePolicies, 
            Dictionary<string, string> customMappings)
        {
            var entraPolicyId = entraPolicy["Id"]?.ToString();
            var entraPolicyName = entraPolicy["DisplayName"]?.ToString();

            if (string.IsNullOrEmpty(entraPolicyId) && string.IsNullOrEmpty(entraPolicyName))
                return null;

            // Try mapping by ID first, then by name
            var key = entraPolicyId ?? entraPolicyName;
            if (key != null && customMappings.TryGetValue(key, out var referenceFileName))
            {
                if (referencePolicies.TryGetValue(referenceFileName, out var referencePolicy))
                {
                    return referencePolicy;
                }
            }

            return null;
        }
    }

    internal class EntraExportData
    {
        public string TenantId { get; set; } = string.Empty;
        public List<JObject> Policies { get; set; } = new();
    }

    internal class ReferencePolicy
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public JObject Policy { get; set; } = new();
    }
}
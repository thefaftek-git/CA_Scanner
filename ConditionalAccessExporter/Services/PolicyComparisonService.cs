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

        /// <summary>
        /// Deserializes a JSON string into a JObject, with proper error handling for various input formats.
        /// </summary>
        /// <param name="json">The JSON string to deserialize</param>
        /// <returns>A JObject representing the parsed JSON</returns>
        /// <exception cref="ArgumentException">Thrown when the input is null, empty, or represents a non-object JSON structure</exception>
        private static JObject DeserializeToJObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Input JSON string cannot be null or empty.", nameof(json));
            }

            JToken parsedToken;
            try
            {
                parsedToken = JsonConvert.DeserializeObject<JToken>(json);
                
                if (parsedToken == null)
                {
                    throw new ArgumentException("Failed to parse JSON string.", nameof(json));
                }
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Failed to parse JSON string due to JSON processing error.", nameof(json), ex);
            }
            
            if (parsedToken is JObject obj)
            {
                return obj;
            }
            else if (parsedToken is JArray)
            {
                throw new ArgumentException("JSON string represents an array, but an object was expected.", nameof(json));
            }
            else
            {
                throw new ArgumentException($"Unsupported JSON type: {parsedToken.Type}. Expected a JSON object.", nameof(json));
            }
        }

        /// <summary>
        /// Parses various input formats into an EntraExportData object.
        /// </summary>
        /// <param name="entraExport">The export data, which can be a JObject, a JSON string, or an arbitrary object</param>
        /// <returns>A structured EntraExportData object containing tenant ID and policies</returns>
        /// <exception cref="ArgumentException">Thrown when the input is invalid or in an unsupported format</exception>
        /// <exception cref="ArgumentNullException">Thrown when the input is null</exception>
        private EntraExportData ParseEntraExport(object entraExport)
        {
            if (entraExport == null)
            {
                throw new ArgumentNullException(nameof(entraExport), "The Entra export data cannot be null.");
            }
            
            JObject jObject;
            
            // Parse the input based on its type (JObject, JToken, string, or arbitrary object)
            if (entraExport is JObject existingJObject)
            {
                jObject = existingJObject;
            }
            else if (entraExport is JToken token)
            {
                if (token.Type == JTokenType.Object)
                {
                    jObject = (JObject)token;
                }
                else
                {
                    throw new ArgumentException($"Unsupported JToken type: {token.Type}. Expected a JObject.", nameof(entraExport));
                }
            }
            else if (entraExport is string jsonString)
            {
                // Delegate validation to DeserializeToJObject
                jObject = DeserializeToJObject(jsonString);
            }
            else
            {
                // Use JToken.FromObject for arbitrary objects
                // Note: While this uses serialization under the hood, it's the most reliable approach
                // for converting arbitrary objects to JObject without custom mapping logic
                try
                {
                    var convertedToken = JToken.FromObject(entraExport);
                    if (convertedToken.Type == JTokenType.Object)
                    {
                        jObject = (JObject)convertedToken;
                    }
                    else
                    {
                        throw new ArgumentException($"Unsupported object type: {convertedToken.Type}. Expected a JSON object.", nameof(entraExport));
                    }
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException("Failed to convert object to JSON format.", nameof(entraExport), ex);
                }
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
                
                if (diff == null || IsEmptyDiff(diff))
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

        private bool IsEmptyDiff(JToken diff)
        {
            if (diff == null)
                return true;

            if (diff.Type != JTokenType.Object)
                return false;

            var diffObj = (JObject)diff;
            if (!diffObj.HasValues)
                return true;

            // Check if all properties represent no actual changes
            // In JsonDiffPatch, identical values are represented as [value, value]
            foreach (var property in diffObj.Properties())
            {
                var value = property.Value;
                
                // If it's an array with 2 identical elements, it means no change
                if (value.Type == JTokenType.Array)
                {
                    var array = (JArray)value;
                    
                    if (array.Count == 2)
                    {
                        bool areEqual = false;
                        
                        // First try direct comparison
                        if (JToken.DeepEquals(array[0], array[1]))
                        {
                            areEqual = true;
                        }
                        else
                        {
                            // If direct comparison fails, try string comparison for cases where
                            // one might be parsed as Date and other as String
                            var str0 = array[0].ToString();
                            var str1 = array[1].ToString();
                            
                            if (str0 == str1)
                            {
                                areEqual = true;
                            }
                            else
                            {
                                // Try parsing both as DateTime if they look like dates
                                if (DateTime.TryParse(str0, out var date0) && DateTime.TryParse(str1, out var date1))
                                {
                                    areEqual = date0 == date1;
                                }
                            }
                        }
                        
                        if (areEqual)
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (value.Type == JTokenType.Object)
                {
                    // Recursively check nested objects
                    if (!IsEmptyDiff(value))
                        return false;
                }
                else
                {
                    // Any other type represents a change
                    return false;
                }
            }

            return true;
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
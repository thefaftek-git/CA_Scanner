using ConditionalAccessExporter.Models;
using ConditionalAccessExporter.Utils;
using JsonDiffPatchDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace ConditionalAccessExporter.Services
{
    public class PolicyComparisonService
    {
        private readonly JsonDiffPatch _jsonDiffPatch;
        private readonly PolicyValidationService _validationService;
        private readonly ILogger<PolicyComparisonService> _logger;
        
        private static readonly string[] DateFormats = 
        {
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "MM/dd/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss"
        };

        // Constants for date string length validation heuristics
        private const int MinDateStringLength = 8;   // Minimum reasonable date string length (e.g., "01/01/24")
        private const int MaxDateStringLength = 30;  // Maximum reasonable date string length (e.g., "2024-01-01T12:00:00.000Z")

        public PolicyComparisonService(ILogger<PolicyComparisonService>? logger = null, PolicyValidationService? validationService = null)
        {
            _jsonDiffPatch = new JsonDiffPatch();
            _validationService = validationService ?? new PolicyValidationService();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PolicyComparisonService>.Instance;
        }

        public async Task<DirectoryValidationResult> ValidateReferenceDirectoryAsync(string referenceDirectory)
        {
            _logger.LogInformation("Validating reference directory: {ReferenceDirectory}", referenceDirectory);
            return await _validationService.ValidateDirectoryAsync(referenceDirectory);
        }

        public async Task<ComparisonResult> CompareAsync(
            object entraExport, 
            string referenceDirectory, 
            MatchingOptions matchingOptions,
            bool skipValidation = false)
        {
            _logger.LogInformation("Starting comparison with reference directory: {ReferenceDirectory}", referenceDirectory);
            
            var result = new ComparisonResult
            {
                ComparedAt = DateTime.UtcNow,
                ReferenceDirectory = referenceDirectory
            };

            // Parse Entra export
            var entraData = ParseEntraExport(entraExport);
            result.TenantId = entraData.TenantId;
            result.Summary.TotalEntraPolicies = entraData.Policies.Count;

            _logger.LogInformation("Found {PolicyCount} policies in Entra export", entraData.Policies.Count);

            // Validate reference files before loading (unless skipped)
            if (!skipValidation)
            {
                _logger.LogInformation("Validating reference files...");
                var validationResult = await ValidateReferenceDirectoryAsync(referenceDirectory);
                if (!validationResult.IsValid)
                {
                    Logger.WriteError($"Reference file validation failed. Found {validationResult.InvalidFiles} invalid files out of {validationResult.TotalFiles}.");
                    // You could throw an exception here or continue with warnings
                    foreach (var fileResult in validationResult.FileResults.Where(f => !f.IsValid))
                    {
                        Logger.WriteError($"  ✗ {fileResult.FileName}: {fileResult.Errors.Count} errors");
                        foreach (var error in fileResult.Errors)
                        {
                            var location = error.LineNumber.HasValue ? $" (line {error.LineNumber})" : "";
                            Logger.WriteError($"    - {error.Message}{location}");
                            if (!string.IsNullOrEmpty(error.Suggestion))
                            {
                                Logger.WriteInfo($"      Suggestion: {error.Suggestion}");
                            }
                        }
                    }
                }
                else
                {
                    Logger.WriteInfo($"✓ All {validationResult.ValidFiles} reference files are valid");
                }
            }

            // Load reference files
            var referencePolicies = await LoadReferencePoliciesAsync(referenceDirectory);
            result.Summary.TotalReferencePolicies = referencePolicies.Count;

            _logger.LogInformation("Found {ReferenceFileCount} reference policy files", referencePolicies.Count);

            // Perform comparison
            PerformComparison(result, entraData.Policies, referencePolicies, matchingOptions);

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

            JToken? parsedToken;
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

        private async Task<Dictionary<string, ReferencePolicy>> LoadReferencePoliciesAsync(string directory, CancellationToken cancellationToken = default)
        {
            var policies = new Dictionary<string, ReferencePolicy>();

            if (!Directory.Exists(directory))
            {
                Logger.WriteError($"Reference directory '{directory}' does not exist");
                return policies;
            }

            var jsonFiles = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            
            if (jsonFiles.Length == 0)
            {
                Logger.WriteInfo($"No JSON files found in reference directory '{directory}'");
                return policies;
            }

            Logger.WriteInfo($"Loading {jsonFiles.Length} reference policy files using parallel processing...");

            // Use parallel processing for loading reference files
            var parallelOptions = new ParallelProcessingOptions
            {
                ContinueOnError = true, // Continue loading other files even if some fail
                ProgressReportInterval = Math.Max(1, jsonFiles.Length / 10) // Report progress every 10%
            };

            var progress = new Progress<ParallelProcessingProgress>(p => 
            {
                if (p.Completed % parallelOptions.ProgressReportInterval == 0 || p.Completed == p.Total)
                {
                    Logger.WriteInfo($"Loading reference files: {p}");
                }
            });

            var parallelResult = await ParallelProcessingService.ProcessFilesInParallelAsync(
                jsonFiles,
                async (file, ct) => await LoadSingleReferencePolicyAsync(file, ct),
                parallelOptions,
                progress,
                cancellationToken);

            // Aggregate successful results
            foreach (var refPolicy in parallelResult.Results.Where(p => p != null))
            {
                policies[refPolicy!.FileName] = refPolicy;
            }

            // Log errors for failed files
            foreach (var error in parallelResult.Errors)
            {
                Logger.WriteError($"Failed to load reference file '{Path.GetFileName(error.Item)}': {error.Exception.Message}");
                
                if (error.Exception is JsonReaderException jsonEx)
                {
                    Logger.WriteError($"  Invalid JSON syntax at line {jsonEx.LineNumber}, position {jsonEx.LinePosition}");
                    Logger.WriteInfo($"  Suggestion: Check the JSON structure for missing commas, brackets, or quotes.");
                }
                else if (error.Exception is IOException)
                {
                    Logger.WriteInfo($"  Suggestion: Ensure the file exists and the application has read permissions.");
                }
                else
                {
                    Logger.WriteInfo($"  Suggestion: Review the file for any obvious issues.");
                }
            }

            Logger.WriteInfo($"Reference file loading completed: {policies.Count} files loaded successfully");
            Logger.WriteInfo($"Processing time: {parallelResult.ElapsedTime.TotalMilliseconds:F0}ms");
            Logger.WriteInfo($"Average speed: {parallelResult.AverageItemsPerSecond:F1} files/second");

            return policies;
        }

        private async Task<ReferencePolicy?> LoadSingleReferencePolicyAsync(string file, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            var policy = JsonConvert.DeserializeObject<JObject>(content);
            
            if (policy != null)
            {
                var fileName = Path.GetFileName(file);
                return new ReferencePolicy
                {
                    FileName = fileName,
                    FilePath = file,
                    Policy = policy
                };
            }

            return null;
        }

        private void PerformComparison(
            ComparisonResult result,
            List<JObject> entraPolicies,
            Dictionary<string, ReferencePolicy> referencePolicies,
            MatchingOptions matchingOptions)
        {
            var matchedReferences = new HashSet<string>();

            // Compare each Entra policy
            foreach (var entraPolicy in entraPolicies)
            {
                var comparison = CompareEntraPolicy(entraPolicy, referencePolicies, matchingOptions);
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

        private PolicyComparison CompareEntraPolicy(
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

        private static bool IsEmptyDiff(JToken diff)
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
                        // First try direct comparison (most efficient)
                        if (JToken.DeepEquals(array[0], array[1]))
                        {
                            continue;
                        }
                        
                        // If direct comparison fails, try semantic comparison
                        if (!AreValuesSemanticallySame(array[0], array[1]))
                        {
                            return false; // Short-circuit on first difference
                        }
                    }
                    else
                    {
                        return false; // Short-circuit on non-pair arrays
                    }
                }
                else if (value.Type == JTokenType.Object)
                {
                    // Recursively check nested objects
                    if (!IsEmptyDiff(value))
                        return false; // Short-circuit on nested differences
                }
                else
                {
                    // Any other type represents a change
                    return false; // Short-circuit on direct changes
                }
            }

            return true;
        }



        private static bool AreValuesSemanticallySame(JToken token1, JToken token2)
        {
            // Fast path: Check token types first to avoid unnecessary string conversions
            if (token1.Type != token2.Type)
            {
                // Check if either token could represent a date
                if (CouldBeDateTime(token1) || CouldBeDateTime(token2))
                {
                    // Proceed to string conversion for potential date comparison
                    var str1 = token1.ToString();
                    var str2 = token2.ToString();
                    
                    // Only attempt date parsing if strings differ and could be dates
                    if (CouldBeDateTime(str1) && CouldBeDateTime(str2))
                        return AreEquivalentDates(str1, str2);
                }
                return false; // Short-circuit if neither token could be a date
            }
            
            // Convert to strings only once for efficiency
            var str1Direct = token1.ToString();
            var str2Direct = token2.ToString();
            
            // Direct string comparison (handles most cases efficiently)
            if (str1Direct == str2Direct)
                return true;
            
            // Only attempt date parsing if strings differ and could be dates
            if (CouldBeDateTime(str1Direct) && CouldBeDateTime(str2Direct))
                return AreEquivalentDates(str1Direct, str2Direct);
                
            return false;
        }

        private static bool CouldBeDateTime(JToken token)
        {
            return token.Type == JTokenType.Date || token.Type == JTokenType.String;
        }
        
        private static bool CouldBeDateTime(string str)
        {
            // Quick heuristic: dates usually contain digits and common separators
            // This avoids expensive parsing for obviously non-date strings
            if (str.Length < MinDateStringLength || str.Length > MaxDateStringLength)
                return false;
                
            bool hasDigit = false;
            bool hasDateSeparator = false;
            
            foreach (char c in str)
            {
                if (char.IsDigit(c))
                    hasDigit = true;
                else if (c == '-' || c == '/' || c == ':' || c == 'T' || c == 'Z')
                    hasDateSeparator = true;
                    
                if (hasDigit && hasDateSeparator)
                    return true;
            }
            
            return false;
        }

        public static bool AreEquivalentDates(string str1, string str2)
        {
            // Use culture-invariant parsing with predefined date formats
            bool date1Parsed = DateTime.TryParseExact(str1, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date1) ||
                               DateTime.TryParse(str1, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date1);
            
            // Short-circuit if first string isn't a valid date
            if (!date1Parsed)
                return false;
            
            bool date2Parsed = DateTime.TryParseExact(str2, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date2) ||
                               DateTime.TryParse(str2, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date2);
            
            // Compare dates with time component ignored for better comparison accuracy
            if (date1Parsed && date2Parsed)
            {
                return date1.Date == date2.Date; // Compare only the date part
            }

            return false;
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
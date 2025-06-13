using ConditionalAccessExporter.Models;
using JsonDiffPatchDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConditionalAccessExporter.Services
{
    public class CrossFormatPolicyComparisonService
    {
        private readonly PolicyComparisonService _jsonComparisonService;
        private readonly TerraformParsingService _terraformParsingService;
        private readonly TerraformConversionService _terraformConversionService;
        private readonly JsonDiffPatch _jsonDiffPatch;

        public CrossFormatPolicyComparisonService(
            PolicyComparisonService jsonComparisonService,
            TerraformParsingService terraformParsingService,
            TerraformConversionService terraformConversionService)
        {
            _jsonComparisonService = jsonComparisonService;
            _terraformParsingService = terraformParsingService;
            _terraformConversionService = terraformConversionService;
            _jsonDiffPatch = new JsonDiffPatch();
        }

        public async Task<CrossFormatComparisonResult> CompareAsync(
            string sourceDirectory,
            string referenceDirectory,
            CrossFormatMatchingOptions matchingOptions)
        {
            Console.WriteLine($"Starting cross-format comparison between '{sourceDirectory}' and '{referenceDirectory}'");
            
            var result = new CrossFormatComparisonResult
            {
                ComparedAt = DateTime.UtcNow,
                SourceDirectory = sourceDirectory,
                ReferenceDirectory = referenceDirectory,
                SourceFormat = DetectFormat(sourceDirectory),
                ReferenceFormat = DetectFormat(referenceDirectory)
            };

            // Load and normalize source policies
            var sourcePolicies = await LoadAndNormalizePoliciesAsync(sourceDirectory, result.SourceFormat);
            result.Summary.TotalSourcePolicies = sourcePolicies.Count;

            // Load and normalize reference policies
            var referencePolicies = await LoadAndNormalizePoliciesAsync(referenceDirectory, result.ReferenceFormat);
            result.Summary.TotalReferencePolicies = referencePolicies.Count;

            Console.WriteLine($"Found {sourcePolicies.Count} source policies ({result.SourceFormat}) and {referencePolicies.Count} reference policies ({result.ReferenceFormat})");

            // Perform cross-format comparison
            PerformCrossFormatComparison(result, sourcePolicies, referencePolicies, matchingOptions);

            return result;
        }

        public async Task<CrossFormatComparisonResult> CompareJsonToTerraformAsync(
            object entraExport,
            string terraformDirectory,
            CrossFormatMatchingOptions matchingOptions)
        {
            var result = new CrossFormatComparisonResult
            {
                ComparedAt = DateTime.UtcNow,
                SourceDirectory = "Entra Export",
                ReferenceDirectory = terraformDirectory,
                SourceFormat = PolicyFormat.Json,
                ReferenceFormat = PolicyFormat.Terraform
            };

            // Parse Entra export to normalized format
            var sourcePolicies = ParseEntraExportToNormalized(entraExport);
            result.Summary.TotalSourcePolicies = sourcePolicies.Count;

            // Load and normalize Terraform policies
            var referencePolicies = await LoadAndNormalizePoliciesAsync(terraformDirectory, PolicyFormat.Terraform);
            result.Summary.TotalReferencePolicies = referencePolicies.Count;

            // Perform comparison
            PerformCrossFormatComparison(result, sourcePolicies, referencePolicies, matchingOptions);

            return result;
        }

        private PolicyFormat DetectFormat(string directory)
        {
            if (!Directory.Exists(directory))
                return PolicyFormat.Unknown;

            var jsonFiles = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly).Length;
            var terraformFiles = Directory.GetFiles(directory, "*.tf", SearchOption.TopDirectoryOnly).Length;
            var terraformStateFiles = Directory.GetFiles(directory, "*.tfstate*", SearchOption.TopDirectoryOnly).Length;

            if (terraformFiles > 0 || terraformStateFiles > 0)
                return PolicyFormat.Terraform;
            
            if (jsonFiles > 0)
                return PolicyFormat.Json;

            return PolicyFormat.Mixed;
        }

        private async Task<List<NormalizedPolicy>> LoadAndNormalizePoliciesAsync(string directory, PolicyFormat format)
        {
            var policies = new List<NormalizedPolicy>();

            switch (format)
            {
                case PolicyFormat.Json:
                    policies = await LoadJsonPoliciesAsync(directory);
                    break;
                case PolicyFormat.Terraform:
                    policies = await LoadTerraformPoliciesAsync(directory);
                    break;
                case PolicyFormat.Mixed:
                    var jsonPolicies = await LoadJsonPoliciesAsync(directory);
                    var terraformPolicies = await LoadTerraformPoliciesAsync(directory);
                    policies.AddRange(jsonPolicies);
                    policies.AddRange(terraformPolicies);
                    break;
            }

            return policies;
        }

        private async Task<List<NormalizedPolicy>> LoadJsonPoliciesAsync(string directory)
        {
            var policies = new List<NormalizedPolicy>();

            if (!Directory.Exists(directory))
                return policies;

            var jsonFiles = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
            
            foreach (var file in jsonFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var policy = JsonConvert.DeserializeObject<JObject>(content);
                    
                    if (policy != null)
                    {
                        var normalized = NormalizeJsonPolicy(policy, Path.GetFileName(file));
                        policies.Add(normalized);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load JSON file '{file}': {ex.Message}");
                }
            }

            return policies;
        }

        private async Task<List<NormalizedPolicy>> LoadTerraformPoliciesAsync(string directory)
        {
            var policies = new List<NormalizedPolicy>();

            try
            {
                var parseResult = await _terraformParsingService.ParseTerraformDirectoryAsync(directory);
                
                foreach (var terraformPolicy in parseResult.Policies)
                {
                    var normalized = NormalizeTerraformPolicy(terraformPolicy);
                    policies.Add(normalized);
                }

                // Log any errors or warnings
                foreach (var error in parseResult.Errors)
                {
                    Console.WriteLine($"Terraform parsing error: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Terraform policies from '{directory}': {ex.Message}");
            }

            return policies;
        }

        private List<NormalizedPolicy> ParseEntraExportToNormalized(object entraExport)
        {
            var policies = new List<NormalizedPolicy>();

            try
            {
                var json = JsonConvert.SerializeObject(entraExport);
                var jObject = JsonConvert.DeserializeObject<JObject>(json);
                
                if (jObject?["Policies"] is JArray policiesArray)
                {
                    foreach (var policy in policiesArray)
                    {
                        if (policy is JObject policyObj)
                        {
                            var normalized = NormalizeJsonPolicy(policyObj, policyObj["Id"]?.ToString() ?? "Unknown");
                            policies.Add(normalized);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Entra export: {ex.Message}");
            }

            return policies;
        }

        private NormalizedPolicy NormalizeJsonPolicy(JObject jsonPolicy, string fileName)
        {
            return new NormalizedPolicy
            {
                Id = jsonPolicy["Id"]?.ToString() ?? string.Empty,
                DisplayName = jsonPolicy["DisplayName"]?.ToString() ?? "Unknown",
                State = NormalizeState(jsonPolicy["State"]?.ToString()),
                SourceFormat = PolicyFormat.Json,
                SourceFile = fileName,
                OriginalPolicy = jsonPolicy,
                NormalizedConditions = NormalizeJsonConditions(jsonPolicy["Conditions"] as JObject),
                NormalizedGrantControls = NormalizeJsonGrantControls(jsonPolicy["GrantControls"] as JObject),
                NormalizedSessionControls = NormalizeJsonSessionControls(jsonPolicy["SessionControls"] as JObject)
            };
        }

        private NormalizedPolicy NormalizeTerraformPolicy(TerraformConditionalAccessPolicy terraformPolicy)
        {
            return new NormalizedPolicy
            {
                Id = GenerateConsistentId(terraformPolicy.ResourceName),
                DisplayName = terraformPolicy.DisplayName,
                State = NormalizeState(terraformPolicy.State),
                SourceFormat = PolicyFormat.Terraform,
                SourceFile = terraformPolicy.ResourceName,
                OriginalPolicy = terraformPolicy,
                NormalizedConditions = NormalizeTerraformConditions(terraformPolicy.Conditions),
                NormalizedGrantControls = NormalizeTerraformGrantControls(terraformPolicy.GrantControls),
                NormalizedSessionControls = NormalizeTerraformSessionControls(terraformPolicy.SessionControls)
            };
        }

        private string NormalizeState(string? state)
        {
            if (string.IsNullOrEmpty(state))
                return "disabled";

            return state.ToLowerInvariant() switch
            {
                "enabled" => "enabled",
                "disabled" => "disabled",
                "enabledforcontrolvalidationonly" => "enabledForReportingButNotEnforced",
                "enabledforreportingbutnotenforced" => "enabledForReportingButNotEnforced",
                _ => state.ToLowerInvariant()
            };
        }

        private string GenerateConsistentId(string resourceName)
        {
            // Generate a consistent GUID based on resource name for comparison purposes
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(resourceName));
            var guid = new Guid(hash.Take(16).ToArray());
            return guid.ToString();
        }

        private NormalizedConditions? NormalizeJsonConditions(JObject? conditions)
        {
            if (conditions == null) return null;

            return new NormalizedConditions
            {
                Applications = NormalizeApplications(conditions["Applications"] as JObject),
                Users = NormalizeUsers(conditions["Users"] as JObject),
                ClientAppTypes = NormalizeStringArray(conditions["ClientAppTypes"] as JArray),
                Platforms = NormalizePlatforms(conditions["Platforms"] as JObject),
                Locations = NormalizeLocations(conditions["Locations"] as JObject),
                SignInRiskLevels = NormalizeStringArray(conditions["SignInRiskLevels"] as JArray),
                UserRiskLevels = NormalizeStringArray(conditions["UserRiskLevels"] as JArray)
            };
        }

        private NormalizedConditions? NormalizeTerraformConditions(TerraformConditions? conditions)
        {
            if (conditions == null) return null;

            return new NormalizedConditions
            {
                Applications = NormalizeApplications(conditions.Applications),
                Users = NormalizeUsers(conditions.Users),
                ClientAppTypes = conditions.ClientAppTypes,
                Platforms = NormalizePlatforms(conditions.Platforms),
                Locations = NormalizeLocations(conditions.Locations),
                SignInRiskLevels = conditions.SignInRiskLevels,
                UserRiskLevels = conditions.UserRiskLevels
            };
        }

        private NormalizedApplications? NormalizeApplications(object? applications)
        {
            if (applications == null) return null;

            if (applications is JObject jsonApps)
            {
                return new NormalizedApplications
                {
                    IncludeApplications = NormalizeStringArray(jsonApps["IncludeApplications"] as JArray),
                    ExcludeApplications = NormalizeStringArray(jsonApps["ExcludeApplications"] as JArray),
                    IncludeUserActions = NormalizeStringArray(jsonApps["IncludeUserActions"] as JArray)
                };
            }

            if (applications is TerraformApplications tfApps)
            {
                return new NormalizedApplications
                {
                    IncludeApplications = tfApps.IncludeApplications,
                    ExcludeApplications = tfApps.ExcludeApplications,
                    IncludeUserActions = tfApps.IncludeUserActions
                };
            }

            return null;
        }

        private NormalizedUsers? NormalizeUsers(object? users)
        {
            if (users == null) return null;

            if (users is JObject jsonUsers)
            {
                return new NormalizedUsers
                {
                    IncludeUsers = NormalizeStringArray(jsonUsers["IncludeUsers"] as JArray),
                    ExcludeUsers = NormalizeStringArray(jsonUsers["ExcludeUsers"] as JArray),
                    IncludeGroups = NormalizeStringArray(jsonUsers["IncludeGroups"] as JArray),
                    ExcludeGroups = NormalizeStringArray(jsonUsers["ExcludeGroups"] as JArray),
                    IncludeRoles = NormalizeStringArray(jsonUsers["IncludeRoles"] as JArray),
                    ExcludeRoles = NormalizeStringArray(jsonUsers["ExcludeRoles"] as JArray)
                };
            }

            if (users is TerraformUsers tfUsers)
            {
                return new NormalizedUsers
                {
                    IncludeUsers = tfUsers.IncludeUsers,
                    ExcludeUsers = tfUsers.ExcludeUsers,
                    IncludeGroups = tfUsers.IncludeGroups,
                    ExcludeGroups = tfUsers.ExcludeGroups,
                    IncludeRoles = tfUsers.IncludeRoles,
                    ExcludeRoles = tfUsers.ExcludeRoles
                };
            }

            return null;
        }

        private NormalizedPlatforms? NormalizePlatforms(object? platforms)
        {
            if (platforms == null) return null;

            if (platforms is JObject jsonPlatforms)
            {
                return new NormalizedPlatforms
                {
                    IncludePlatforms = NormalizeStringArray(jsonPlatforms["IncludePlatforms"] as JArray),
                    ExcludePlatforms = NormalizeStringArray(jsonPlatforms["ExcludePlatforms"] as JArray)
                };
            }

            if (platforms is TerraformPlatforms tfPlatforms)
            {
                return new NormalizedPlatforms
                {
                    IncludePlatforms = tfPlatforms.IncludePlatforms,
                    ExcludePlatforms = tfPlatforms.ExcludePlatforms
                };
            }

            return null;
        }

        private NormalizedLocations? NormalizeLocations(object? locations)
        {
            if (locations == null) return null;

            if (locations is JObject jsonLocations)
            {
                return new NormalizedLocations
                {
                    IncludeLocations = NormalizeStringArray(jsonLocations["IncludeLocations"] as JArray),
                    ExcludeLocations = NormalizeStringArray(jsonLocations["ExcludeLocations"] as JArray)
                };
            }

            if (locations is TerraformLocations tfLocations)
            {
                return new NormalizedLocations
                {
                    IncludeLocations = tfLocations.IncludeLocations,
                    ExcludeLocations = tfLocations.ExcludeLocations
                };
            }

            return null;
        }

        private NormalizedGrantControls? NormalizeJsonGrantControls(JObject? grantControls)
        {
            if (grantControls == null) return null;

            return new NormalizedGrantControls
            {
                Operator = grantControls["Operator"]?.ToString(),
                BuiltInControls = NormalizeStringArray(grantControls["BuiltInControls"] as JArray),
                CustomAuthenticationFactors = NormalizeStringArray(grantControls["CustomAuthenticationFactors"] as JArray),
                TermsOfUse = NormalizeStringArray(grantControls["TermsOfUse"] as JArray)
            };
        }

        private NormalizedGrantControls? NormalizeTerraformGrantControls(TerraformGrantControls? grantControls)
        {
            if (grantControls == null) return null;

            return new NormalizedGrantControls
            {
                Operator = grantControls.Operator,
                BuiltInControls = grantControls.BuiltInControls,
                CustomAuthenticationFactors = grantControls.CustomAuthenticationFactors,
                TermsOfUse = grantControls.TermsOfUse
            };
        }

        private NormalizedSessionControls? NormalizeJsonSessionControls(JObject? sessionControls)
        {
            if (sessionControls == null) return null;

            return new NormalizedSessionControls
            {
                // Implement session controls normalization as needed
            };
        }

        private NormalizedSessionControls? NormalizeTerraformSessionControls(TerraformSessionControls? sessionControls)
        {
            if (sessionControls == null) return null;

            return new NormalizedSessionControls
            {
                // Implement session controls normalization as needed
            };
        }

        private List<string>? NormalizeStringArray(JArray? jsonArray)
        {
            if (jsonArray == null) return null;

            return jsonArray.Select(token => token.ToString()).ToList();
        }

        private void PerformCrossFormatComparison(
            CrossFormatComparisonResult result,
            List<NormalizedPolicy> sourcePolicies,
            List<NormalizedPolicy> referencePolicies,
            CrossFormatMatchingOptions matchingOptions)
        {
            var matchedReferences = new HashSet<string>();

            // Compare each source policy
            foreach (var sourcePolicy in sourcePolicies)
            {
                var comparison = ComparePolicy(sourcePolicy, referencePolicies, matchingOptions);
                result.PolicyComparisons.Add(comparison);

                if (comparison.ReferencePolicy != null)
                {
                    matchedReferences.Add(comparison.ReferencePolicy.SourceFile);
                }

                // Update summary
                switch (comparison.Status)
                {
                    case CrossFormatComparisonStatus.SourceOnly:
                        result.Summary.SourceOnlyPolicies++;
                        break;
                    case CrossFormatComparisonStatus.Identical:
                        result.Summary.MatchingPolicies++;
                        break;
                    case CrossFormatComparisonStatus.SemanticallyEquivalent:
                        result.Summary.SemanticallyEquivalentPolicies++;
                        break;
                    case CrossFormatComparisonStatus.Different:
                        result.Summary.PoliciesWithDifferences++;
                        break;
                }
            }

            // Add unmatched reference policies
            foreach (var referencePolicy in referencePolicies)
            {
                if (!matchedReferences.Contains(referencePolicy.SourceFile))
                {
                    var comparison = new CrossFormatPolicyComparison
                    {
                        PolicyName = referencePolicy.DisplayName,
                        Status = CrossFormatComparisonStatus.ReferenceOnly,
                        ReferencePolicy = referencePolicy
                    };
                    result.PolicyComparisons.Add(comparison);
                    result.Summary.ReferenceOnlyPolicies++;
                }
            }
        }

        private CrossFormatPolicyComparison ComparePolicy(
            NormalizedPolicy sourcePolicy,
            List<NormalizedPolicy> referencePolicies,
            CrossFormatMatchingOptions matchingOptions)
        {
            var comparison = new CrossFormatPolicyComparison
            {
                PolicyId = sourcePolicy.Id,
                PolicyName = sourcePolicy.DisplayName,
                SourcePolicy = sourcePolicy,
                Status = CrossFormatComparisonStatus.SourceOnly
            };

            var matchingReference = FindMatchingPolicy(sourcePolicy, referencePolicies, matchingOptions);

            if (matchingReference != null)
            {
                comparison.ReferencePolicy = matchingReference;

                // Perform semantic comparison
                var semanticComparison = CompareSemanticEquivalence(sourcePolicy, matchingReference);
                comparison.SemanticAnalysis = semanticComparison;

                
                if (semanticComparison.IsIdentical)
                {
                    comparison.Status = CrossFormatComparisonStatus.Identical;
                }
                else if (semanticComparison.IsSemanticallyEquivalent)
                {
                    comparison.Status = CrossFormatComparisonStatus.SemanticallyEquivalent;
                }
                else
                {
                    comparison.Status = CrossFormatComparisonStatus.Different;
                }
                

                comparison.Differences = semanticComparison.Differences;
                comparison.ConversionSuggestions = GenerateConversionSuggestions(sourcePolicy, matchingReference);
            }

            return comparison;
        }

        private NormalizedPolicy? FindMatchingPolicy(
            NormalizedPolicy sourcePolicy,
            List<NormalizedPolicy> referencePolicies,
            CrossFormatMatchingOptions matchingOptions)
        {
            // Provide default options if null
            matchingOptions ??= new CrossFormatMatchingOptions();
            
            switch (matchingOptions.Strategy)
            {
                case CrossFormatMatchingStrategy.ByName:
                    return FindByName(sourcePolicy, referencePolicies, matchingOptions.CaseSensitive);
                
                case CrossFormatMatchingStrategy.ById:
                    return FindById(sourcePolicy, referencePolicies);
                
                case CrossFormatMatchingStrategy.SemanticSimilarity:
                    return FindBySemanticSimilarity(sourcePolicy, referencePolicies);
                
                case CrossFormatMatchingStrategy.CustomMapping:
                    return FindByCustomMapping(sourcePolicy, referencePolicies, matchingOptions.CustomMappings);
                
                default:
                    return FindByName(sourcePolicy, referencePolicies, matchingOptions.CaseSensitive);
            }
        }

        private NormalizedPolicy? FindByName(NormalizedPolicy sourcePolicy, List<NormalizedPolicy> referencePolicies, bool caseSensitive)
        {
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            
            return referencePolicies.FirstOrDefault(refPolicy => 
                string.Equals(sourcePolicy.DisplayName, refPolicy.DisplayName, comparison));
        }

        private NormalizedPolicy? FindById(NormalizedPolicy sourcePolicy, List<NormalizedPolicy> referencePolicies)
        {
            return referencePolicies.FirstOrDefault(refPolicy => 
                string.Equals(sourcePolicy.Id, refPolicy.Id, StringComparison.OrdinalIgnoreCase));
        }

        private NormalizedPolicy? FindBySemanticSimilarity(NormalizedPolicy sourcePolicy, List<NormalizedPolicy> referencePolicies)
        {
            // Find the most semantically similar policy
            var bestMatch = referencePolicies
                .Select(refPolicy => new { 
                    Policy = refPolicy, 
                    Similarity = CalculateSemanticSimilarity(sourcePolicy, refPolicy) 
                })
                .Where(x => x.Similarity > 0.8) // 80% similarity threshold
                .OrderByDescending(x => x.Similarity)
                .FirstOrDefault();

            return bestMatch?.Policy;
        }

        private double CalculateSemanticSimilarity(NormalizedPolicy policy1, NormalizedPolicy policy2)
        {
            // Simple similarity calculation based on key attributes
            double score = 0.0;
            int comparisons = 0;

            // Compare state
            if (policy1.State == policy2.State) score += 1.0;
            comparisons++;

            // Compare conditions (simplified)
            if (policy1.NormalizedConditions != null && policy2.NormalizedConditions != null)
            {
                // Compare applications
                if (ListsEqual(policy1.NormalizedConditions.Applications?.IncludeApplications, 
                             policy2.NormalizedConditions.Applications?.IncludeApplications))
                    score += 1.0;
                comparisons++;

                // Compare users
                if (ListsEqual(policy1.NormalizedConditions.Users?.IncludeUsers, 
                             policy2.NormalizedConditions.Users?.IncludeUsers))
                    score += 1.0;
                comparisons++;

                // Compare client app types
                if (ListsEqual(policy1.NormalizedConditions.ClientAppTypes, 
                             policy2.NormalizedConditions.ClientAppTypes))
                    score += 1.0;
                comparisons++;
            }

            return comparisons > 0 ? score / comparisons : 0.0;
        }

        private bool ListsEqual(List<string>? list1, List<string>? list2)
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            
            return list1.OrderBy(x => x).SequenceEqual(list2.OrderBy(x => x));
        }

        private NormalizedPolicy? FindByCustomMapping(
            NormalizedPolicy sourcePolicy, 
            List<NormalizedPolicy> referencePolicies, 
            Dictionary<string, string> customMappings)
        {
            var key = sourcePolicy.Id ?? sourcePolicy.DisplayName;
            
            if (key != null && customMappings.TryGetValue(key, out var referenceFileName))
            {
                // Look for a match on the SourceFile (which contains the ResourceName for Terraform policies)
                // The custom mapping value could be either:
                // 1. The exact SourceFile (ResourceName)
                // 2. A filename ending with .tf that contains the ResourceName
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(referenceFileName);
                var matchedPolicy = referencePolicies.FirstOrDefault(refPolicy => 
                    // Exact matches (highest priority)
                    refPolicy.SourceFile == referenceFileName ||
                    refPolicy.SourceFile == fileNameWithoutExtension ||
                    // Substring matching with constraints to prevent false positives
                    (referenceFileName.EndsWith(".tf") && referenceFileName.Contains(refPolicy.SourceFile) && !string.IsNullOrWhiteSpace(refPolicy.SourceFile)) ||
                    (refPolicy.SourceFile.EndsWith(".tf") && refPolicy.SourceFile.Contains(fileNameWithoutExtension) && !string.IsNullOrWhiteSpace(fileNameWithoutExtension)) ||
                    // Fallback: careful substring matching for custom mappings (only if strings are reasonable length)
                    (referenceFileName.Length >= 3 && refPolicy.SourceFile.Length >= 3 && 
                     (referenceFileName.Contains(refPolicy.SourceFile) || refPolicy.SourceFile.Contains(fileNameWithoutExtension))));
                
                return matchedPolicy;
            }

            return null;
        }

        private SemanticAnalysisResult CompareSemanticEquivalence(
            NormalizedPolicy sourcePolicy, 
            NormalizedPolicy referencePolicy)
        {
            var result = new SemanticAnalysisResult();
            var differences = new List<string>();

            // Compare basic properties
            if (sourcePolicy.State != referencePolicy.State)
            {
                differences.Add($"State: {sourcePolicy.State} vs {referencePolicy.State}");
            }

            // Compare conditions
            if (sourcePolicy.NormalizedConditions != null && referencePolicy.NormalizedConditions != null)
            {
                CompareConditions(sourcePolicy.NormalizedConditions, referencePolicy.NormalizedConditions, differences);
            }
            else if (sourcePolicy.NormalizedConditions != referencePolicy.NormalizedConditions)
            {
                differences.Add("One policy has conditions while the other doesn't");
            }

            // Compare grant controls
            if (sourcePolicy.NormalizedGrantControls != null && referencePolicy.NormalizedGrantControls != null)
            {
                CompareGrantControls(sourcePolicy.NormalizedGrantControls, referencePolicy.NormalizedGrantControls, differences);
            }
            else if (sourcePolicy.NormalizedGrantControls != referencePolicy.NormalizedGrantControls)
            {
                differences.Add("One policy has grant controls while the other doesn't");
            }

            result.Differences = differences;
            result.IsIdentical = differences.Count == 0;
            
            // Modified logic: Only consider semantically equivalent if there are NO differences
            result.IsSemanticallyEquivalent = differences.Count == 0;

            return result;
        }

        private void CompareConditions(NormalizedConditions source, NormalizedConditions reference, List<string> differences)
        {
            // Compare applications
            if (!ApplicationsEqual(source.Applications, reference.Applications))
            {
                differences.Add("Application conditions differ");
            }

            // Compare users
            if (!UsersEqual(source.Users, reference.Users))
            {
                differences.Add("User conditions differ");
            }

            // Compare client app types
            if (!ListsEqual(source.ClientAppTypes, reference.ClientAppTypes))
            {
                differences.Add("Client app types differ");
            }

            // Compare platforms
            if (!PlatformsEqual(source.Platforms, reference.Platforms))
            {
                differences.Add("Platform conditions differ");
            }

            // Compare locations
            if (!LocationsEqual(source.Locations, reference.Locations))
            {
                differences.Add("Location conditions differ");
            }

            // Compare risk levels
            if (!ListsEqual(source.SignInRiskLevels, reference.SignInRiskLevels))
            {
                differences.Add("Sign-in risk levels differ");
            }

            if (!ListsEqual(source.UserRiskLevels, reference.UserRiskLevels))
            {
                differences.Add("User risk levels differ");
            }
        }

        private bool ApplicationsEqual(NormalizedApplications? apps1, NormalizedApplications? apps2)
        {
            if (apps1 == null && apps2 == null) return true;
            if (apps1 == null || apps2 == null) return false;

            return ListsEqual(apps1.IncludeApplications, apps2.IncludeApplications) &&
                   ListsEqual(apps1.ExcludeApplications, apps2.ExcludeApplications) &&
                   ListsEqual(apps1.IncludeUserActions, apps2.IncludeUserActions);
        }

        private bool UsersEqual(NormalizedUsers? users1, NormalizedUsers? users2)
        {
            if (users1 == null && users2 == null) return true;
            if (users1 == null || users2 == null) return false;

            return ListsEqual(users1.IncludeUsers, users2.IncludeUsers) &&
                   ListsEqual(users1.ExcludeUsers, users2.ExcludeUsers) &&
                   ListsEqual(users1.IncludeGroups, users2.IncludeGroups) &&
                   ListsEqual(users1.ExcludeGroups, users2.ExcludeGroups) &&
                   ListsEqual(users1.IncludeRoles, users2.IncludeRoles) &&
                   ListsEqual(users1.ExcludeRoles, users2.ExcludeRoles);
        }

        private bool PlatformsEqual(NormalizedPlatforms? platforms1, NormalizedPlatforms? platforms2)
        {
            if (platforms1 == null && platforms2 == null) return true;
            if (platforms1 == null || platforms2 == null) return false;

            return ListsEqual(platforms1.IncludePlatforms, platforms2.IncludePlatforms) &&
                   ListsEqual(platforms1.ExcludePlatforms, platforms2.ExcludePlatforms);
        }

        private bool LocationsEqual(NormalizedLocations? locations1, NormalizedLocations? locations2)
        {
            if (locations1 == null && locations2 == null) return true;
            if (locations1 == null || locations2 == null) return false;

            return ListsEqual(locations1.IncludeLocations, locations2.IncludeLocations) &&
                   ListsEqual(locations1.ExcludeLocations, locations2.ExcludeLocations);
        }

        private void CompareGrantControls(NormalizedGrantControls source, NormalizedGrantControls reference, List<string> differences)
        {
            if (source.Operator != reference.Operator)
            {
                differences.Add($"Grant controls operator: {source.Operator} vs {reference.Operator}");
            }

            if (!ListsEqual(source.BuiltInControls, reference.BuiltInControls))
            {
                differences.Add("Built-in controls differ");
            }

            if (!ListsEqual(source.CustomAuthenticationFactors, reference.CustomAuthenticationFactors))
            {
                differences.Add("Custom authentication factors differ");
            }

            if (!ListsEqual(source.TermsOfUse, reference.TermsOfUse))
            {
                differences.Add("Terms of use differ");
            }
        }

        private List<string> GenerateConversionSuggestions(
            NormalizedPolicy sourcePolicy, 
            NormalizedPolicy referencePolicy)
        {
            var suggestions = new List<string>();

            if (sourcePolicy.SourceFormat != referencePolicy.SourceFormat)
            {
                if (sourcePolicy.SourceFormat == PolicyFormat.Json && referencePolicy.SourceFormat == PolicyFormat.Terraform)
                {
                    suggestions.Add("Consider converting JSON policy to Terraform format for consistency");
                    suggestions.Add($"Use terraform import to bring existing policy '{sourcePolicy.DisplayName}' under Terraform management");
                }
                else if (sourcePolicy.SourceFormat == PolicyFormat.Terraform && referencePolicy.SourceFormat == PolicyFormat.Json)
                {
                    suggestions.Add("Consider applying Terraform configuration to create/update the policy in Azure AD");
                    suggestions.Add($"Run 'terraform plan' to see changes for policy '{sourcePolicy.DisplayName}'");
                }
            }

            // Add specific conversion suggestions based on differences
            if (sourcePolicy.State != referencePolicy.State)
            {
                suggestions.Add($"Update policy state from '{sourcePolicy.State}' to '{referencePolicy.State}'");
            }

            return suggestions;
        }
    }
}
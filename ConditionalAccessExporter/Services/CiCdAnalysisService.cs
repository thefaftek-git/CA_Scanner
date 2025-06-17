using ConditionalAccessExporter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConditionalAccessExporter.Services
{
    public class CiCdAnalysisService
    {
        // Status constants to ensure consistency
        public static class StatusConstants
        {
            public const string CriticalDriftDetected = "critical_drift_detected";
            public const string DriftDetected = "drift_detected";
            public const string NoDrift = "no_drift";
            public const string MissingPolicy = "MissingPolicy";
        }
        // Critical change types that typically indicate security policy violations
        private static readonly HashSet<string> CriticalChangeTypes = new()
        {
            "GrantControls",
            "SessionControls", 
            "Conditions.SignInRiskLevels",
            "Conditions.UserRiskLevels",
            "Conditions.Applications.IncludeApplications",
            "Conditions.Applications.ExcludeApplications",
            "Conditions.Users.IncludeUsers",
            "Conditions.Users.ExcludeUsers",
            "Conditions.Users.IncludeGroups",
            "Conditions.Users.ExcludeGroups",
            "Conditions.Users.IncludeRoles",
            "Conditions.Users.ExcludeRoles",
            "State"
        };

        // Non-critical change types that are typically metadata or informational
        private static readonly HashSet<string> NonCriticalChangeTypes = new()
        {
            "CreatedDateTime",
            "ModifiedDateTime", 
            "Id",
            "Description",
            "DisplayName"
        };

        public CiCdAnalysisResult AnalyzeComparison(ComparisonResult result, CiCdOptions options)
        {
            var analysis = new CiCdAnalysisResult
            {
                Options = options
            };

            // Precompute normalized change type sets for better performance with large diffs
            var normalizedIgnoreSet = new HashSet<string>(options.IgnoreChangeTypes.Select(ignore => ignore.ToLowerInvariant()));
            var normalizedFailOnSet = new HashSet<string>(options.FailOnChangeTypes.Select(critical => critical.ToLowerInvariant()));
            var normalizedCriticalSet = new HashSet<string>(CriticalChangeTypes.Select(critical => critical.ToLowerInvariant()));
            var normalizedNonCriticalSet = new HashSet<string>(NonCriticalChangeTypes.Select(nonCritical => nonCritical.ToLowerInvariant()));

            // Analyze each policy comparison
            foreach (var comparison in result.PolicyComparisons)
            {
                if (comparison.Status == ComparisonStatus.Different && comparison.Differences != null)
                {
                    AnalyzePolicyDifferences(comparison, options, normalizedIgnoreSet, normalizedFailOnSet, normalizedCriticalSet, normalizedNonCriticalSet);
                    
                    if (comparison.HasCriticalDifferences)
                    {
                        analysis.CriticalPolicies.Add(comparison.PolicyName);
                        analysis.CriticalDifferences += comparison.CriticalDifferenceTypes.Count;
                        
                        // Collect all critical change types
                        foreach (var changeType in comparison.CriticalDifferenceTypes)
                        {
                            analysis.AllCriticalChangeTypes.Add(changeType);
                        }
                    }
                    
                    analysis.NonCriticalDifferences += comparison.NonCriticalDifferenceTypes.Count;
                    
                    // Collect all non-critical change types
                    foreach (var changeType in comparison.NonCriticalDifferenceTypes)
                    {
                        analysis.AllNonCriticalChangeTypes.Add(changeType);
                    }
                }
                else if (comparison.Status == ComparisonStatus.EntraOnly || comparison.Status == ComparisonStatus.ReferenceOnly)
                {
                    // Consider missing policies as critical
                    comparison.HasCriticalDifferences = true;
                    comparison.CriticalDifferenceTypes.Add(StatusConstants.MissingPolicy);
                    analysis.CriticalPolicies.Add(comparison.PolicyName);
                    analysis.CriticalDifferences++;
                    
                    // Collect the missing policy change type
                    analysis.AllCriticalChangeTypes.Add(StatusConstants.MissingPolicy);
                }
            }

            // Calculate total differences as sum of critical and non-critical differences
            analysis.TotalDifferences = analysis.CriticalDifferences + analysis.NonCriticalDifferences;

            // Update summary statistics
            result.Summary.CriticalDifferences = analysis.CriticalDifferences;
            result.Summary.NonCriticalDifferences = analysis.NonCriticalDifferences;
            result.Summary.CriticalChangeTypes.Clear();
            result.Summary.CriticalChangeTypes.AddRange(analysis.GetAllCriticalChangeTypes());
            result.Summary.NonCriticalChangeTypes.Clear();
            result.Summary.NonCriticalChangeTypes.AddRange(analysis.GetAllNonCriticalChangeTypes());

            // Determine exit code and status
            analysis.ExitCode = DetermineExitCode(analysis, options);
            analysis.Status = DetermineStatus(analysis);

            return analysis;
        }

        private void AnalyzePolicyDifferences(PolicyComparison comparison, CiCdOptions options, 
            HashSet<string> normalizedIgnoreSet, HashSet<string> normalizedFailOnSet, 
            HashSet<string> normalizedCriticalSet, HashSet<string> normalizedNonCriticalSet)
        {
            if (comparison.Differences == null) return;

            // Improved error handling for the Differences object casting
            var differences = comparison.Differences as JToken;
            if (differences == null)
            {
                // Fallback: try to handle if Differences is a different type
                try
                {
                    var json = JsonConvert.SerializeObject(comparison.Differences);
                    differences = JToken.Parse(json);
                }
                catch (Exception ex)
                {
                    // Log the exception details to aid debugging
                    if (!options.QuietMode)
                    {
                        Logger.WriteError($"Error parsing Differences object for policy '{comparison.PolicyName}': {ex.Message}");
                        Logger.WriteVerbose($"Stack trace: {ex.StackTrace}");
                    }
                    // If all else fails, skip this comparison gracefully
                    return;
                }
            }

            CategorizeDifferences(differences, "", comparison, options, normalizedIgnoreSet, normalizedFailOnSet, normalizedCriticalSet, normalizedNonCriticalSet);
        }

        private void CategorizeDifferences(JToken diff, string path, PolicyComparison comparison, CiCdOptions options,
            HashSet<string> normalizedIgnoreSet, HashSet<string> normalizedFailOnSet, 
            HashSet<string> normalizedCriticalSet, HashSet<string> normalizedNonCriticalSet)
        {
            if (diff == null) return;

            if (diff.Type == JTokenType.Object)
            {
                var diffObj = (JObject)diff;
                foreach (var property in diffObj.Properties())
                {
                    var currentPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                    CategorizeDifferences(property.Value, currentPath, comparison, options, normalizedIgnoreSet, normalizedFailOnSet, normalizedCriticalSet, normalizedNonCriticalSet);
                }
            }
            else if (diff.Type == JTokenType.Array)
            {
                var diffArray = (JArray)diff;
                // For arrays in JsonDiffPatch, changes are typically [oldValue, newValue]
                // Arrays with fewer than 2 elements represent edge cases (e.g., deletions, additions)
                // and should still be categorized as changes
                if (diffArray.Count >= 2)
                {
                    CategorizeChangePath(path, comparison, options, normalizedIgnoreSet, normalizedFailOnSet, normalizedCriticalSet, normalizedNonCriticalSet);
                }
                else if (diffArray.Count == 1)
                {
                    // Single element arrays typically represent additions or special diff markers
                    CategorizeChangePath(path, comparison, options, normalizedIgnoreSet, normalizedFailOnSet, normalizedCriticalSet, normalizedNonCriticalSet);
                }
                // Empty arrays are ignored as they represent no change
            }
            else
            {
                // Direct value change
                CategorizeChangePath(path, comparison, options, normalizedIgnoreSet, normalizedFailOnSet, normalizedCriticalSet, normalizedNonCriticalSet);
            }
        }

        private void CategorizeChangePath(string path, PolicyComparison comparison, CiCdOptions options,
            HashSet<string> normalizedIgnoreSet, HashSet<string> normalizedFailOnSet, 
            HashSet<string> normalizedCriticalSet, HashSet<string> normalizedNonCriticalSet)
        {
            // Check if this change type should be ignored using precomputed sets
            var normalizedPath = path.ToLowerInvariant();
            bool shouldIgnore = normalizedIgnoreSet.Any(ignore => IsPathMatch(normalizedPath, ignore));
            
            if (shouldIgnore)
            {
                comparison.IgnoredDifferenceTypes.Add(path);
                // Debug logging for ignored changes
                if (options.ExplainValues && !options.QuietMode)
                {
                    Logger.WriteVerbose($"[DEBUG] Ignored change path: {path} (policy: {comparison.PolicyName})");
                }
                return;
            }

            // Check if this is a critical change type using precomputed sets
            bool isCritical = IsCriticalChange(path, normalizedPath, normalizedFailOnSet, normalizedCriticalSet, normalizedNonCriticalSet);
            
            if (isCritical)
            {
                comparison.HasCriticalDifferences = true;
                if (!comparison.CriticalDifferenceTypes.Contains(path))
                {
                    comparison.CriticalDifferenceTypes.Add(path);
                    // Debug logging for critical changes
                    if (options.ExplainValues && !options.QuietMode)
                    {
                        Logger.WriteVerbose($"[DEBUG] Critical change path: {path} (policy: {comparison.PolicyName})");
                    }
                }
            }
            else
            {
                if (!comparison.NonCriticalDifferenceTypes.Contains(path))
                {
                    comparison.NonCriticalDifferenceTypes.Add(path);
                    // Debug logging for non-critical changes
                    if (options.ExplainValues && !options.QuietMode)
                    {
                        Logger.WriteVerbose($"[DEBUG] Non-critical change path: {path} (policy: {comparison.PolicyName})");
                    }
                }
            }
        }

        private bool IsCriticalChange(string path, string normalizedPath, 
            HashSet<string> normalizedFailOnSet, HashSet<string> normalizedCriticalSet, HashSet<string> normalizedNonCriticalSet)
        {
            // Check user-defined critical types first using precomputed sets
            // Use more precise matching to avoid unintended matches with similar names
            if (normalizedFailOnSet.Any(critical => IsPathMatch(normalizedPath, critical)))
            {
                return true;
            }

            // Check built-in critical types using precomputed sets
            if (normalizedCriticalSet.Any(critical => IsPathMatch(normalizedPath, critical)))
            {
                return true;
            }

            // Check if it's a known non-critical type using precomputed sets
            if (normalizedNonCriticalSet.Any(nonCritical => IsPathMatch(normalizedPath, nonCritical)))
            {
                return false;
            }

            // Default: treat unknown changes as critical for security
            return true;
        }

        /// <summary>
        /// More precise path matching to avoid unintended matches with similar names
        /// </summary>
        private bool IsPathMatch(string path, string pattern)
        {
            // Exact match
            if (path.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Pattern is a substring that matches a complete path segment
            // This prevents partial matches like "Application" matching "Applications"
            if (path.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                // Check if the pattern appears as a complete segment (word boundary)
                int index = path.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    bool validStart = index == 0 || path[index - 1] == '.';
                    bool validEnd = index + pattern.Length == path.Length || path[index + pattern.Length] == '.';
                    return validStart && validEnd;
                }
            }

            return false;
        }

        private int DetermineExitCode(CiCdAnalysisResult analysis, CiCdOptions options)
        {
            if (!options.ExitOnDifferences)
            {
                return (int)ExitCode.Success;
            }

            // Check max differences threshold
            if (options.MaxDifferences.HasValue && 
                analysis.TotalDifferences > options.MaxDifferences.Value)
            {
                return (int)ExitCode.CriticalDifferences;
            }

            // Check for critical differences
            if (analysis.CriticalDifferences > 0)
            {
                return (int)ExitCode.CriticalDifferences;
            }

            // Check for any differences
            if (analysis.TotalDifferences > 0)
            {
                return (int)ExitCode.DifferencesFound;
            }

            return (int)ExitCode.Success;
        }

        private string DetermineStatus(CiCdAnalysisResult analysis)
        {
            if (analysis.CriticalDifferences > 0)
            {
                return StatusConstants.CriticalDriftDetected;
            }
            
            if (analysis.TotalDifferences > 0)
            {
                return StatusConstants.DriftDetected;
            }
            
            return StatusConstants.NoDrift;
        }

        public PipelineOutput GeneratePipelineOutput(CiCdAnalysisResult analysis, ComparisonResult result)
        {
            return new PipelineOutput
            {
                Status = analysis.Status,
                ExitCode = analysis.ExitCode,
                DifferencesCount = analysis.TotalDifferences,
                CriticalChanges = analysis.CriticalDifferences,
                NonCriticalChanges = analysis.NonCriticalDifferences,
                ComparedAt = result.ComparedAt,
                TenantId = result.TenantId,
                CriticalChangeTypes = analysis.GetAllCriticalChangeTypes(),
                PolicyNames = analysis.CriticalPolicies,
                Message = GenerateMessage(analysis)
            };
        }

        private string GenerateMessage(CiCdAnalysisResult analysis)
        {
            if (analysis.CriticalDifferences > 0)
            {
                return $"Critical policy drift detected: {analysis.CriticalDifferences} critical differences found in {analysis.CriticalPolicies.Count} policies";
            }
            
            if (analysis.TotalDifferences > 0)
            {
                return $"Policy drift detected: {analysis.TotalDifferences} differences found";
            }
            
            return "No policy drift detected";
        }
    }

    public class CiCdAnalysisResult
    {
        public int TotalDifferences { get; set; }
        public int CriticalDifferences { get; set; }
        public int NonCriticalDifferences { get; set; }
        public List<string> CriticalPolicies { get; set; } = new();
        public int ExitCode { get; set; }
        public string Status { get; set; } = string.Empty;
        public CiCdOptions Options { get; set; } = new();
        public HashSet<string> AllCriticalChangeTypes { get; set; } = new();
        public HashSet<string> AllNonCriticalChangeTypes { get; set; } = new();

        public List<string> GetAllCriticalChangeTypes()
        {
            return AllCriticalChangeTypes.ToList();
        }

        public List<string> GetAllNonCriticalChangeTypes()
        {
            return AllNonCriticalChangeTypes.ToList();
        }
    }
}

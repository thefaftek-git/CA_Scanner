using ConditionalAccessExporter.Models;
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

            // Analyze each policy comparison
            foreach (var comparison in result.PolicyComparisons)
            {
                if (comparison.Status == ComparisonStatus.Different && comparison.Differences != null)
                {
                    AnalyzePolicyDifferences(comparison, options);
                    
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
                    comparison.CriticalDifferenceTypes.Add("MissingPolicy");
                    analysis.CriticalPolicies.Add(comparison.PolicyName);
                    analysis.CriticalDifferences++;
                    
                    // Collect the missing policy change type
                    analysis.AllCriticalChangeTypes.Add("MissingPolicy");
                }
            }

            // Calculate total differences as sum of critical and non-critical differences
            analysis.TotalDifferences = analysis.CriticalDifferences + analysis.NonCriticalDifferences;

            // Update summary statistics
            result.Summary.CriticalDifferences = analysis.CriticalDifferences;
            result.Summary.NonCriticalDifferences = analysis.NonCriticalDifferences;
            result.Summary.CriticalChangeTypes = analysis.GetAllCriticalChangeTypes();
            result.Summary.NonCriticalChangeTypes = analysis.GetAllNonCriticalChangeTypes();

            // Determine exit code and status
            analysis.ExitCode = DetermineExitCode(analysis, options);
            analysis.Status = DetermineStatus(analysis);

            return analysis;
        }

        private void AnalyzePolicyDifferences(PolicyComparison comparison, CiCdOptions options)
        {
            if (comparison.Differences == null) return;

            var differences = comparison.Differences as JToken;
            if (differences == null) return;

            CategorizeDifferences(differences, "", comparison, options);
        }

        private void CategorizeDifferences(JToken diff, string path, PolicyComparison comparison, CiCdOptions options)
        {
            if (diff == null) return;

            if (diff.Type == JTokenType.Object)
            {
                var diffObj = (JObject)diff;
                foreach (var property in diffObj.Properties())
                {
                    var currentPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                    CategorizeDifferences(property.Value, currentPath, comparison, options);
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
                    CategorizeChangePath(path, comparison, options);
                }
                else if (diffArray.Count == 1)
                {
                    // Single element arrays typically represent additions or special diff markers
                    CategorizeChangePath(path, comparison, options);
                }
                // Empty arrays are ignored as they represent no change
            }
            else
            {
                // Direct value change
                CategorizeChangePath(path, comparison, options);
            }
        }

        private void CategorizeChangePath(string path, PolicyComparison comparison, CiCdOptions options)
        {
            // Check if this change type should be ignored
            // Optimized: avoid Any() + Contains() for better performance with large diffs
            bool shouldIgnore = false;
            foreach (var ignore in options.IgnoreChangeTypes)
            {
                if (path.Contains(ignore, StringComparison.OrdinalIgnoreCase))
                {
                    shouldIgnore = true;
                    break;
                }
            }
            
            if (shouldIgnore)
            {
                comparison.IgnoredDifferenceTypes.Add(path);
                return;
            }

            // Check if this is a critical change type
            bool isCritical = IsCriticalChange(path, options);
            
            if (isCritical)
            {
                comparison.HasCriticalDifferences = true;
                if (!comparison.CriticalDifferenceTypes.Contains(path))
                {
                    comparison.CriticalDifferenceTypes.Add(path);
                }
            }
            else
            {
                if (!comparison.NonCriticalDifferenceTypes.Contains(path))
                {
                    comparison.NonCriticalDifferenceTypes.Add(path);
                }
            }
        }

        private bool IsCriticalChange(string path, CiCdOptions options)
        {
            // Check user-defined critical types first
            // Optimized: avoid Any() + Contains() for better performance with large diffs
            foreach (var critical in options.FailOnChangeTypes)
            {
                if (path.Contains(critical, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Check built-in critical types
            foreach (var critical in CriticalChangeTypes)
            {
                if (path.Contains(critical, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Check if it's a known non-critical type
            foreach (var nonCritical in NonCriticalChangeTypes)
            {
                if (path.Contains(nonCritical, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Default: treat unknown changes as critical for security
            return true;
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

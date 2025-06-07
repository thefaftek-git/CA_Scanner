using Newtonsoft.Json;

namespace ConditionalAccessExporter.Models
{
    public class ComparisonResult
    {
        public DateTime ComparedAt { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string ReferenceDirectory { get; set; } = string.Empty;
        public ComparisonSummary Summary { get; set; } = new();
        public List<PolicyComparison> PolicyComparisons { get; set; } = new();
    }

    public class ComparisonSummary
    {
        public int EntraOnlyPolicies { get; set; }
        public int ReferenceOnlyPolicies { get; set; }
        public int MatchingPolicies { get; set; }
        public int PoliciesWithDifferences { get; set; }
        public int TotalEntraPolicies { get; set; }
        public int TotalReferencePolicies { get; set; }
        public int CriticalDifferences { get; set; }
        public int NonCriticalDifferences { get; set; }
        public List<string> CriticalChangeTypes { get; set; } = new();
        public List<string> NonCriticalChangeTypes { get; set; } = new();
    }

    public class PolicyComparison
    {
        public string PolicyId { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public ComparisonStatus Status { get; set; }
        public string? ReferenceFileName { get; set; }
        public object? EntraPolicy { get; set; }
        public object? ReferencePolicy { get; set; }
        public object? Differences { get; set; }
        public bool HasCriticalDifferences { get; set; }
        public List<string> CriticalDifferenceTypes { get; set; } = new();
        public List<string> NonCriticalDifferenceTypes { get; set; } = new();
        public List<string> IgnoredDifferenceTypes { get; set; } = new();
    }

    public enum ComparisonStatus
    {
        EntraOnly,
        ReferenceOnly,
        Identical,
        Different
    }

    public class MatchingOptions
    {
        public MatchingStrategy Strategy { get; set; } = MatchingStrategy.ByName;
        public bool CaseSensitive { get; set; } = false;
        public Dictionary<string, string> CustomMappings { get; set; } = new();
    }

    public enum MatchingStrategy
    {
        ByName,
        ById,
        CustomMapping
    }

    // Cross-format comparison models
    public class CrossFormatComparisonResult
    {
        public DateTime ComparedAt { get; set; }
        public string SourceDirectory { get; set; } = string.Empty;
        public string ReferenceDirectory { get; set; } = string.Empty;
        public PolicyFormat SourceFormat { get; set; }
        public PolicyFormat ReferenceFormat { get; set; }
        public CrossFormatComparisonSummary Summary { get; set; } = new();
        public List<CrossFormatPolicyComparison> PolicyComparisons { get; set; } = new();
    }

    public class CrossFormatComparisonSummary
    {
        public int SourceOnlyPolicies { get; set; }
        public int ReferenceOnlyPolicies { get; set; }
        public int MatchingPolicies { get; set; }
        public int SemanticallyEquivalentPolicies { get; set; }
        public int PoliciesWithDifferences { get; set; }
        public int TotalSourcePolicies { get; set; }
        public int TotalReferencePolicies { get; set; }
    }

    public class CrossFormatPolicyComparison
    {
        public string PolicyId { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public CrossFormatComparisonStatus Status { get; set; }
        public NormalizedPolicy? SourcePolicy { get; set; }
        public NormalizedPolicy? ReferencePolicy { get; set; }
        public SemanticAnalysisResult? SemanticAnalysis { get; set; }
        public List<string>? Differences { get; set; }
        public List<string>? ConversionSuggestions { get; set; }
    }

    public enum CrossFormatComparisonStatus
    {
        SourceOnly,
        ReferenceOnly,
        Identical,
        SemanticallyEquivalent,
        Different
    }

    public enum PolicyFormat
    {
        Json,
        Terraform,
        Mixed,
        Unknown
    }

    public class CrossFormatMatchingOptions
    {
        public CrossFormatMatchingStrategy Strategy { get; set; } = CrossFormatMatchingStrategy.ByName;
        public bool CaseSensitive { get; set; } = false;
        public bool EnableSemanticComparison { get; set; } = true;
        public double SemanticSimilarityThreshold { get; set; } = 0.8;
        public Dictionary<string, string> CustomMappings { get; set; } = new();
    }

    public enum CrossFormatMatchingStrategy
    {
        ByName,
        ById,
        SemanticSimilarity,
        CustomMapping
    }

    // Normalized policy representation for cross-format comparison
    public class NormalizedPolicy
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public PolicyFormat SourceFormat { get; set; }
        public string SourceFile { get; set; } = string.Empty;
        public object? OriginalPolicy { get; set; }
        public NormalizedConditions? NormalizedConditions { get; set; }
        public NormalizedGrantControls? NormalizedGrantControls { get; set; }
        public NormalizedSessionControls? NormalizedSessionControls { get; set; }
    }

    public class NormalizedConditions
    {
        public NormalizedApplications? Applications { get; set; }
        public NormalizedUsers? Users { get; set; }
        public List<string>? ClientAppTypes { get; set; }
        public NormalizedPlatforms? Platforms { get; set; }
        public NormalizedLocations? Locations { get; set; }
        public List<string>? SignInRiskLevels { get; set; }
        public List<string>? UserRiskLevels { get; set; }
    }

    public class NormalizedApplications
    {
        public List<string>? IncludeApplications { get; set; }
        public List<string>? ExcludeApplications { get; set; }
        public List<string>? IncludeUserActions { get; set; }
    }

    public class NormalizedUsers
    {
        public List<string>? IncludeUsers { get; set; }
        public List<string>? ExcludeUsers { get; set; }
        public List<string>? IncludeGroups { get; set; }
        public List<string>? ExcludeGroups { get; set; }
        public List<string>? IncludeRoles { get; set; }
        public List<string>? ExcludeRoles { get; set; }
    }

    public class NormalizedPlatforms
    {
        public List<string>? IncludePlatforms { get; set; }
        public List<string>? ExcludePlatforms { get; set; }
    }

    public class NormalizedLocations
    {
        public List<string>? IncludeLocations { get; set; }
        public List<string>? ExcludeLocations { get; set; }
    }

    public class NormalizedGrantControls
    {
        public string? Operator { get; set; }
        public List<string>? BuiltInControls { get; set; }
        public List<string>? CustomAuthenticationFactors { get; set; }
        public List<string>? TermsOfUse { get; set; }
    }

    public class NormalizedSessionControls
    {
        // Add session control properties as needed
    }

    public class SemanticAnalysisResult
    {
        public bool IsIdentical { get; set; }
        public bool IsSemanticallyEquivalent { get; set; }
        public List<string> Differences { get; set; } = new();
        public double SimilarityScore { get; set; }
        public List<string> SemanticInsights { get; set; } = new();
    }

    // CI/CD Integration Models
    public class CiCdOptions
    {
        public bool ExitOnDifferences { get; set; }
        public int? MaxDifferences { get; set; }
        public List<string> FailOnChangeTypes { get; set; } = new();
        public List<string> IgnoreChangeTypes { get; set; } = new();
        public bool QuietMode { get; set; }
    }

    public class PipelineOutput
    {
        public string Status { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public int DifferencesCount { get; set; }
        public int CriticalChanges { get; set; }
        public int NonCriticalChanges { get; set; }
        public DateTime ComparedAt { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public List<string> CriticalChangeTypes { get; set; } = new();
        public List<string> PolicyNames { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public enum ExitCode
    {
        Success = 0,              // No differences found
        DifferencesFound = 1,     // Differences found (policy drift detected)
        CriticalDifferences = 2,  // Critical differences (security policy violations)
        Error = 3                 // Error (authentication, file not found, etc.)
    }
}
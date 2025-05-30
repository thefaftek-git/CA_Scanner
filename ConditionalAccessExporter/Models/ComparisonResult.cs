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
}
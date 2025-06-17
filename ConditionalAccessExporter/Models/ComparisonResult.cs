using Newtonsoft.Json;

namespace ConditionalAccessExporter.Models
{
    public class ComparisonResult
    {
        public DateTime ComparedAt { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string ReferenceDirectory { get; set; } = string.Empty;
        public ComparisonSummary Summary { get; set; } = new();
        public List<PolicyComparison> PolicyComparisons { get; } = new();
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
        public List<string> CriticalChangeTypes { get; } = new();
        public List<string> NonCriticalChangeTypes { get; } = new();
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
        public List<string> CriticalDifferenceTypes { get; } = new();
        public List<string> NonCriticalDifferenceTypes { get; } = new();
        public List<string> IgnoredDifferenceTypes { get; } = new();
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
        public bool CaseSensitive { get; set; }
        public Dictionary<string, string> CustomMappings { get; } = new();
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
        public List<CrossFormatPolicyComparison> PolicyComparisons { get; } = new();
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
        public bool CaseSensitive { get; set; }
        public bool EnableSemanticComparison { get; set; } = true;
        public double SemanticSimilarityThreshold { get; set; } = 0.8;
        public Dictionary<string, string> CustomMappings { get; } = new();
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
        public bool QuietMode { get; set; } // Suppresses output when enabled
        public bool ExplainValues { get; set; }
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

    // Policy Validation Models
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
        public List<ValidationRecommendation> Recommendations { get; set; } = new();
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string PolicyId { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public double SecurityScore { get; set; }
        public double ComplianceScore { get; set; }
        public ValidationSeverity HighestSeverity => GetHighestSeverity();
        public Dictionary<string, object> Metadata { get; set; } = new();

        private ValidationSeverity GetHighestSeverity()
        {
            var maxSeverity = ValidationSeverity.Info;
            
            foreach (var error in Errors)
            {
                if (error.Type == ValidationErrorType.SecurityIssue)
                    maxSeverity = ValidationSeverity.Critical;
                else if (maxSeverity < ValidationSeverity.Error)
                    maxSeverity = ValidationSeverity.Error;
            }
            
            if (maxSeverity < ValidationSeverity.Error && Warnings.Any())
                maxSeverity = ValidationSeverity.Warning;
                
            return maxSeverity;
        }
    }

    public class ValidationError
    {
        public ValidationErrorType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public int? LineNumber { get; set; }
        public int? ColumnNumber { get; set; }
        public string Suggestion { get; set; } = string.Empty;
    }

    public class ValidationWarning
    {
        public ValidationWarningType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public int? LineNumber { get; set; }
        public string Suggestion { get; set; } = string.Empty;
    }

    public enum ValidationErrorType
    {
        JsonSyntaxError,
        RequiredFieldMissing,
        InvalidFieldType,
        InvalidFieldValue,
        InvalidGuid,
        InvalidEnum,
        JsonSchemaViolation,
        FileAccessError,
        SecurityIssue,
        UnexpectedError
    }

    public enum ValidationWarningType
    {
        BestPracticeViolation,
        SecurityRecommendation,
        PerformanceWarning,
        DeprecatedField,
        UnknownField
    }

    public enum ValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Critical = 3
    }

    public class ValidationRecommendation
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string CurrentValue { get; set; } = string.Empty;
        public string RecommendedValue { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public List<string> References { get; set; } = new();
        public double ImpactScore { get; set; }
        public string AutomationScript { get; set; } = string.Empty;
    }

    public class PolicyValidationReport
    {
        public string ReportId { get; set; } = Guid.NewGuid().ToString();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string GeneratedBy { get; set; } = "PolicyValidationEngine";
        public int TotalPolicies { get; set; }
        public int ValidPolicies { get; set; }
        public int InvalidPolicies { get; set; }
        public int PoliciesWithWarnings { get; set; }
        public double OverallComplianceScore { get; set; }
        public double SecurityPostureScore { get; set; }
        public List<ValidationResult> PolicyResults { get; set; } = new();
        public List<ValidationRecommendation> Recommendations { get; set; } = new();
        public Dictionary<string, int> ErrorsByType { get; set; } = new();
        public Dictionary<string, int> WarningsByType { get; set; } = new();
        public SecurityAssessment SecurityAssessment { get; set; } = new();
        public ComplianceAssessment ComplianceAssessment { get; set; } = new();
    }

    public class SecurityAssessment
    {
        public double OverallScore { get; set; }
        public Dictionary<string, double> CategoryScores { get; set; } = new();
        public List<SecurityRisk> IdentifiedRisks { get; set; } = new();
        public List<SecurityStrength> Strengths { get; set; } = new();
        public int CriticalFindings { get; set; }
        public int HighRiskFindings { get; set; }
        public int MediumRiskFindings { get; set; }
        public int LowRiskFindings { get; set; }
    }

    public class ComplianceAssessment
    {
        public double OverallScore { get; set; }
        public Dictionary<string, ComplianceFrameworkScore> FrameworkScores { get; set; } = new();
        public List<ComplianceGap> Gaps { get; set; } = new();
        public DateTime LastAssessment { get; set; } = DateTime.UtcNow;
        public DateTime NextRecommendedAssessment { get; set; }
    }

    public class ComplianceFrameworkScore
    {
        public string Framework { get; set; } = string.Empty;
        public double Score { get; set; }
        public int TotalControls { get; set; }
        public int PassingControls { get; set; }
        public int FailingControls { get; set; }
        public List<string> NonCompliantControls { get; set; } = new();
    }

    public class SecurityRisk
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; }
        public string Category { get; set; } = string.Empty;
        public List<string> AffectedPolicies { get; set; } = new();
        public string Mitigation { get; set; } = string.Empty;
        public double RiskScore { get; set; }
    }

    public class SecurityStrength
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> EvidencePolicies { get; set; } = new();
    }



    public class DirectoryValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationResult> FileResults { get; set; } = new();
        public string DirectoryPath { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int ValidFiles { get; set; }
        public int InvalidFiles { get; set; }
        public int FilesWithWarnings => FileResults.Count(f => f.Warnings.Any());
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public List<string> PreflightErrors { get; set; } = new();
    }

    public class PreflightCheck
    {
        public string Name { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
    }
}
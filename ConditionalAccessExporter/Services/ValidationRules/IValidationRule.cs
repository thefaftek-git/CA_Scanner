
using ConditionalAccessExporter.Models;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Services.ValidationRules
{
    /// <summary>
    /// Interface for all validation rules
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// Unique identifier for the rule
        /// </summary>
        string RuleId { get; }
        
        /// <summary>
        /// Human-readable name for the rule
        /// </summary>
        string RuleName { get; }
        
        /// <summary>
        /// Description of what the rule validates
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Category of the validation rule
        /// </summary>
        ValidationRuleCategory Category { get; }
        
        /// <summary>
        /// Severity level of violations found by this rule
        /// </summary>
        ValidationSeverity DefaultSeverity { get; }
        
        /// <summary>
        /// Whether this rule is enabled by default
        /// </summary>
        bool IsEnabledByDefault { get; }
        
        /// <summary>
        /// Validates a policy and returns any issues found
        /// </summary>
        /// <param name="policy">The policy to validate</param>
        /// <param name="context">Validation context with additional information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result for this rule</returns>
        Task<RuleValidationResult> ValidateAsync(JObject policy, ValidationContext context, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Categories of validation rules
    /// </summary>
    public enum ValidationRuleCategory
    {
        Security,
        Compliance,
        BestPractices,
        Performance,
        Governance,
        Custom
    }

    /// <summary>
    /// Result of a single validation rule execution
    /// </summary>
    public class RuleValidationResult
    {
        public string RuleId { get; set; } = string.Empty;
        public bool Passed { get; set; } = true;
        public List<ValidationIssue> Issues { get; set; } = new();
        public List<ValidationRecommendation> Recommendations { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a validation issue found by a rule
    /// </summary>
    public class ValidationIssue
    {
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string CurrentValue { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public List<string> References { get; set; } = new();
    }

    /// <summary>
    /// Context provided to validation rules
    /// </summary>
    public class ValidationContext
    {
        public string? TenantId { get; set; }
        public string? FilePath { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<JObject> AllPolicies { get; set; } = new();
        public Dictionary<string, string> NamedValues { get; set; } = new();
        public ValidationOptions Options { get; set; } = new();
    }

    /// <summary>
    /// Options for validation behavior
    /// </summary>
    public class ValidationOptions
    {
        public bool StrictMode { get; set; } = false;
        public bool IncludeRecommendations { get; set; } = true;
        public bool SkipWarnings { get; set; } = false;
        public List<string> DisabledRules { get; set; } = new();
        public Dictionary<string, ValidationSeverity> CustomSeverities { get; set; } = new();
        public bool GenerateRemediationScripts { get; set; } = false;
        public Dictionary<string, object> Configuration { get; set; } = new();
    }
}


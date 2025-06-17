

using ConditionalAccessExporter.Models;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Services.ValidationRules
{
    /// <summary>
    /// Base class for validation rules providing common functionality
    /// </summary>
    public abstract class BaseValidationRule : IValidationRule
    {
        public abstract string RuleId { get; }
        public abstract string RuleName { get; }
        public abstract string Description { get; }
        public abstract ValidationRuleCategory Category { get; }
        public abstract ValidationSeverity DefaultSeverity { get; }
        public virtual bool IsEnabledByDefault => true;

        public abstract Task<RuleValidationResult> ValidateAsync(JObject policy, ValidationContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Helper method to create a validation issue
        /// </summary>
        protected ValidationIssue CreateIssue(
            ValidationSeverity severity,
            string message,
            string field = "",
            string currentValue = "",
            string expectedValue = "",
            string suggestion = "",
            params string[] references)
        {
            return new ValidationIssue
            {
                Severity = severity,
                Message = message,
                Field = field,
                CurrentValue = currentValue,
                ExpectedValue = expectedValue,
                Suggestion = suggestion,
                References = references.ToList()
            };
        }

        /// <summary>
        /// Helper method to create a validation recommendation
        /// </summary>
        protected ValidationRecommendation CreateRecommendation(
            string title,
            string description,
            ValidationSeverity severity,
            string category,
            string field = "",
            string currentValue = "",
            string recommendedValue = "",
            string rationale = "",
            double impactScore = 0.0,
            params string[] references)
        {
            return new ValidationRecommendation
            {
                Id = $"{RuleId}_{Guid.NewGuid():N}",
                Title = title,
                Description = description,
                Severity = severity,
                Category = category,
                Field = field,
                CurrentValue = currentValue,
                RecommendedValue = recommendedValue,
                Rationale = rationale,
                References = references.ToList(),
                ImpactScore = impactScore
            };
        }

        /// <summary>
        /// Helper method to create a successful validation result
        /// </summary>
        protected RuleValidationResult CreateSuccessResult()
        {
            return new RuleValidationResult
            {
                RuleId = RuleId,
                Passed = true
            };
        }

        /// <summary>
        /// Helper method to create a failed validation result
        /// </summary>
        protected RuleValidationResult CreateFailureResult(params ValidationIssue[] issues)
        {
            return new RuleValidationResult
            {
                RuleId = RuleId,
                Passed = false,
                Issues = issues.ToList()
            };
        }

        /// <summary>
        /// Helper method to safely get a string value from a JObject
        /// </summary>
        protected string GetStringValue(JObject policy, string path, string defaultValue = "")
        {
            try
            {
                var token = policy.SelectToken(path);
                return token?.ToString() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Helper method to safely get an array from a JObject
        /// </summary>
        protected JArray? GetArrayValue(JObject policy, string path)
        {
            try
            {
                var token = policy.SelectToken(path);
                return token as JArray;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Helper method to safely get a boolean value from a JObject
        /// </summary>
        protected bool GetBooleanValue(JObject policy, string path, bool defaultValue = false)
        {
            try
            {
                var token = policy.SelectToken(path);
                if (token?.Type == JTokenType.Boolean)
                    return token.Value<bool>();
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Helper method to check if a policy contains any of the specified values in an array field
        /// </summary>
        protected bool ContainsAnyValue(JObject policy, string arrayPath, params string[] values)
        {
            var array = GetArrayValue(policy, arrayPath);
            if (array == null) return false;

            var arrayValues = array.Select(t => t.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return values.Any(v => arrayValues.Contains(v));
        }

        /// <summary>
        /// Helper method to check if a policy contains all of the specified values in an array field
        /// </summary>
        protected bool ContainsAllValues(JObject policy, string arrayPath, params string[] values)
        {
            var array = GetArrayValue(policy, arrayPath);
            if (array == null) return false;

            var arrayValues = array.Select(t => t.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return values.All(v => arrayValues.Contains(v));
        }
    }
}



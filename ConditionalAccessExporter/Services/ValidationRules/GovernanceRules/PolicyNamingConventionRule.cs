









using ConditionalAccessExporter.Models;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Services.ValidationRules.GovernanceRules
{
    /// <summary>
    /// Validates that policies follow organizational naming conventions
    /// </summary>
    public class PolicyNamingConventionRule : BaseValidationRule
    {
        public override string RuleId => "GOV001";
        public override string RuleName => "Policy Naming Convention";
        public override string Description => "Ensures policies follow organizational naming conventions for consistency and management";
        public override ValidationRuleCategory Category => ValidationRuleCategory.Governance;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Warning;

        // Default naming patterns - can be customized through configuration
        private readonly string[] _defaultNamingPatterns = 
        {
            @"^CA-\d{3}-.*",           // CA-001-Description
            @"^[A-Z]{2,4}-.*",         // PREFIX-Description
            @"^\d{3}-.*",              // 001-Description
            @"^(Block|Allow|Require).*" // Action-based naming
        };

        public override async Task<RuleValidationResult> ValidateAsync(JObject policy, ValidationContext context, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            
            var result = new RuleValidationResult { RuleId = RuleId, Passed = true };
            
            var displayName = GetStringValue(policy, "displayName");
            if (string.IsNullOrWhiteSpace(displayName))
            {
                result.Passed = false;
                result.Issues.Add(CreateIssue(
                    ValidationSeverity.Error,
                    "Policy must have a display name",
                    "displayName",
                    "null or empty",
                    "Descriptive policy name",
                    "Provide a clear, descriptive name for the policy"
                ));
                return result;
            }

            // Get custom naming patterns from configuration
            var customPatterns = GetCustomNamingPatterns(context);
            var patterns = customPatterns.Any() ? customPatterns : _defaultNamingPatterns;

            // Check if the policy name matches any pattern
            var matchesPattern = patterns.Any(pattern => Regex.IsMatch(displayName, pattern, RegexOptions.IgnoreCase));

            if (!matchesPattern)
            {
                result.Passed = false;
                result.Issues.Add(CreateIssue(
                    ValidationSeverity.Warning,
                    $"Policy name '{displayName}' does not follow organizational naming conventions",
                    "displayName",
                    displayName,
                    string.Join(" OR ", patterns),
                    "Follow the organizational naming convention for consistency and management",
                    "https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/plan-conditional-access"
                ));

                result.Recommendations.Add(CreateRecommendation(
                    "Follow Naming Convention",
                    "Consistent naming conventions improve policy management and understanding",
                    ValidationSeverity.Warning,
                    "Governance",
                    "displayName",
                    displayName,
                    $"Use pattern: {patterns.First()}",
                    "Consistent naming helps with policy organization, searching, and automated management",
                    4.0
                ));
            }

            // Additional naming best practices
            ValidateNamingBestPractices(displayName, result);

            return result;
        }

        /// <summary>
        /// Gets custom naming patterns from configuration
        /// </summary>
        private string[] GetCustomNamingPatterns(ValidationContext context)
        {
            if (context.Configuration.TryGetValue("naming.patterns", out var patternsObj))
            {
                if (patternsObj is string[] patterns)
                    return patterns;
                
                if (patternsObj is string singlePattern)
                    return new[] { singlePattern };
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Validates additional naming best practices
        /// </summary>
        private void ValidateNamingBestPractices(string displayName, RuleValidationResult result)
        {
            // Check for overly long names
            if (displayName.Length > 100)
            {
                result.Issues.Add(CreateIssue(
                    ValidationSeverity.Info,
                    "Policy name is very long and may be truncated in some views",
                    "displayName",
                    $"{displayName.Length} characters",
                    "< 100 characters",
                    "Consider shortening the policy name while keeping it descriptive"
                ));
            }

            // Check for very short names
            if (displayName.Length < 10)
            {
                result.Issues.Add(CreateIssue(
                    ValidationSeverity.Info,
                    "Policy name is very short and may not be descriptive enough",
                    "displayName",
                    displayName,
                    "More descriptive name",
                    "Consider adding more context to make the policy purpose clear"
                ));
            }

            // Check for special characters that might cause issues
            var problematicChars = new[] { '<', '>', '|', '"', '*', '?', ':', '\\', '/' };
            if (problematicChars.Any(c => displayName.Contains(c)))
            {
                result.Issues.Add(CreateIssue(
                    ValidationSeverity.Warning,
                    "Policy name contains characters that may cause issues in some systems",
                    "displayName",
                    displayName,
                    "Name without special characters",
                    "Avoid using special characters in policy names for better compatibility"
                ));
            }

            // Check for duplicate words
            var words = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var duplicateWords = words.GroupBy(w => w, StringComparer.OrdinalIgnoreCase)
                                     .Where(g => g.Count() > 1)
                                     .Select(g => g.Key);

            if (duplicateWords.Any())
            {
                result.Recommendations.Add(CreateRecommendation(
                    "Remove Duplicate Words",
                    "Policy name contains duplicate words that could be simplified",
                    ValidationSeverity.Info,
                    "Clarity",
                    "displayName",
                    displayName,
                    "Remove duplicate words for clarity",
                    "Removing redundant words makes policy names cleaner and easier to read",
                    2.0
                ));
            }

            // Check for common abbreviations that should be spelled out
            var abbreviations = new Dictionary<string, string>
            {
                { "CA", "Conditional Access" },
                { "MFA", "Multi-Factor Authentication" },
                { "SSO", "Single Sign-On" },
                { "B2B", "Business-to-Business" },
                { "VPN", "Virtual Private Network" }
            };

            foreach (var abbrev in abbreviations)
            {
                if (Regex.IsMatch(displayName, $@"\b{abbrev.Key}\b", RegexOptions.IgnoreCase))
                {
                    result.Recommendations.Add(CreateRecommendation(
                        "Consider Spelling Out Abbreviations",
                        $"Policy name contains abbreviation '{abbrev.Key}' which could be spelled out as '{abbrev.Value}'",
                        ValidationSeverity.Info,
                        "Clarity",
                        "displayName",
                        displayName,
                        $"Replace '{abbrev.Key}' with '{abbrev.Value}'",
                        "Spelling out abbreviations improves clarity for users unfamiliar with technical terms",
                        1.0
                    ));
                    break; // Only suggest one abbreviation change at a time
                }
            }
        }
    }
}










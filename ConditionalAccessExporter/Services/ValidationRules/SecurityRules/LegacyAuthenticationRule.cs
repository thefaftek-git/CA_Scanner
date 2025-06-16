




using ConditionalAccessExporter.Models;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Services.ValidationRules.SecurityRules
{
    /// <summary>
    /// Validates that legacy authentication protocols are blocked
    /// </summary>
    public class LegacyAuthenticationRule : BaseValidationRule
    {
        public override string RuleId => "SEC003";
        public override string RuleName => "Legacy Authentication Blocking";
        public override string Description => "Ensures legacy authentication protocols are blocked to prevent security vulnerabilities";
        public override ValidationRuleCategory Category => ValidationRuleCategory.Security;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Error;

        public override async Task<RuleValidationResult> ValidateAsync(JObject policy, ValidationContext context, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            
            var result = new RuleValidationResult { RuleId = RuleId, Passed = true };
            
            // Check if policy is enabled
            var state = GetStringValue(policy, "state");
            if (state != "enabled")
            {
                return result; // Skip disabled policies
            }

            // Check if this policy targets legacy authentication
            var clientAppTypes = GetArrayValue(policy, "conditions.clientAppTypes");
            var targetsLegacyAuth = CheckForLegacyAuthTargeting(clientAppTypes);
            
            if (!targetsLegacyAuth)
            {
                // Check if there should be a policy blocking legacy auth
                if (ShouldBlockLegacyAuth(policy, context))
                {
                    result.Recommendations.Add(CreateRecommendation(
                        "Consider Legacy Authentication Blocking",
                        "No policy found that explicitly blocks legacy authentication protocols",
                        ValidationSeverity.Warning,
                        "Security",
                        "conditions.clientAppTypes",
                        "Not targeting legacy auth",
                        "Create policy targeting legacy auth clients",
                        "Legacy authentication protocols lack modern security features and should be blocked",
                        8.0,
                        "https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/block-legacy-authentication"
                    ));
                }
                return result;
            }

            // If targeting legacy auth, validate the policy blocks it
            var grantControls = policy.SelectToken("grantControls");
            if (grantControls == null)
            {
                result.Passed = false;
                result.Issues.Add(CreateIssue(
                    ValidationSeverity.Error,
                    "Policy targeting legacy authentication must have grant controls",
                    "grantControls",
                    "null",
                    "Block access grant control",
                    "Add grant controls with block access",
                    "https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/concept-conditional-access-grant"
                ));
                return result;
            }

            // Check if access is blocked
            var builtInControls = GetArrayValue(policy, "grantControls.builtInControls");
            var blocksAccess = ContainsAnyValue(policy, "grantControls.builtInControls", "block", "blockAccess");
            
            if (!blocksAccess)
            {
                result.Passed = false;
                var currentControls = builtInControls?.ToString() ?? "none";
                result.Issues.Add(CreateIssue(
                    ValidationSeverity.Error,
                    "Policy targeting legacy authentication should block access",
                    "grantControls.builtInControls",
                    currentControls,
                    "block",
                    "Change grant controls to block access for legacy authentication",
                    "https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/block-legacy-authentication"
                ));

                result.Recommendations.Add(CreateRecommendation(
                    "Block Legacy Authentication",
                    "Legacy authentication protocols should be blocked completely",
                    ValidationSeverity.Error,
                    "Security",
                    "grantControls.builtInControls",
                    currentControls,
                    "block",
                    "Legacy protocols don't support modern authentication methods and present security risks",
                    9.0,
                    "https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/block-legacy-authentication"
                ));
            }
            else
            {
                // Validate proper scope for legacy auth blocking
                ValidateLegacyAuthScope(policy, result);
            }

            return result;
        }

        /// <summary>
        /// Checks if the policy targets legacy authentication clients
        /// </summary>
        private bool CheckForLegacyAuthTargeting(JArray? clientAppTypes)
        {
            if (clientAppTypes == null) return false;

            var legacyAuthTypes = new[]
            {
                "exchangeActiveSync",
                "other", // Often represents legacy auth clients
                "pop",
                "imap",
                "smtp"
            };

            return clientAppTypes.Any(type => 
                legacyAuthTypes.Contains(type.ToString(), StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if legacy authentication should be blocked based on context
        /// </summary>
        private bool ShouldBlockLegacyAuth(JObject policy, ValidationContext context)
        {
            // Check if this is a comprehensive policy (all users or apps)
            if (ContainsAnyValue(policy, "conditions.users.includeUsers", "All"))
                return true;

            if (ContainsAnyValue(policy, "conditions.applications.includeApplications", "All"))
                return true;

            // Check if there are any existing legacy auth blocking policies
            var hasLegacyAuthPolicy = context.AllPolicies.Any(p => 
            {
                var clientApps = p.SelectToken("conditions.clientAppTypes") as JArray;
                return CheckForLegacyAuthTargeting(clientApps);
            });

            return !hasLegacyAuthPolicy;
        }

        /// <summary>
        /// Validates the scope of legacy authentication blocking policy
        /// </summary>
        private void ValidateLegacyAuthScope(JObject policy, RuleValidationResult result)
        {
            // Check if policy has appropriate exclusions
            var excludeUsers = GetArrayValue(policy, "conditions.users.excludeUsers");
            var excludeGroups = GetArrayValue(policy, "conditions.users.excludeGroups");
            
            var hasEmergencyExclusions = false;
            if (excludeUsers != null || excludeGroups != null)
            {
                // This is good - should have some exclusions for emergency access
                hasEmergencyExclusions = true;
            }

            if (!hasEmergencyExclusions)
            {
                result.Recommendations.Add(CreateRecommendation(
                    "Add Emergency Access Exclusions",
                    "Legacy auth blocking policies should exclude emergency access accounts",
                    ValidationSeverity.Warning,
                    "Availability",
                    "conditions.users.excludeUsers",
                    "No exclusions",
                    "Add emergency access account exclusions",
                    "Emergency access accounts may need to be excluded to prevent lockout scenarios",
                    6.0,
                    "https://docs.microsoft.com/en-us/azure/active-directory/roles/security-emergency-access"
                ));
            }

            // Check if policy is too broad
            var includesAllUsers = ContainsAnyValue(policy, "conditions.users.includeUsers", "All");
            var includesAllApps = ContainsAnyValue(policy, "conditions.applications.includeApplications", "All");
            
            if (includesAllUsers && includesAllApps && !hasEmergencyExclusions)
            {
                result.Issues.Add(CreateIssue(
                    ValidationSeverity.Warning,
                    "Legacy authentication blocking policy may be too broad without exclusions",
                    "conditions.users",
                    "All users and applications",
                    "Include emergency access exclusions",
                    "Consider adding exclusions for emergency access accounts and critical applications",
                    "https://docs.microsoft.com/en-us/azure/active-directory/roles/security-emergency-access"
                ));
            }
        }
    }
}





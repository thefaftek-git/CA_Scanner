


using ConditionalAccessExporter.Models;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Services.ValidationRules.SecurityRules
{
    /// <summary>
    /// Validates that policies require MFA for appropriate scenarios
    /// </summary>
    public class MfaRequirementRule : BaseValidationRule
    {
        public override string RuleId => "SEC001";
        public override string RuleName => "MFA Requirement Validation";
        public override string Description => "Ensures policies require Multi-Factor Authentication for appropriate access scenarios";
        public override ValidationRuleCategory Category => ValidationRuleCategory.Security;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Error;

        public override async Task<RuleValidationResult> ValidateAsync(JObject policy, ValidationContext context, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // Make async for consistency
            
            var result = new RuleValidationResult { RuleId = RuleId, Passed = true };
            
            // Check if policy is enabled
            var state = GetStringValue(policy, "state");
            if (state != "enabled")
            {
                return result; // Skip disabled policies
            }

            // Check if policy has grant controls
            var grantControls = policy.SelectToken("grantControls");
            if (grantControls == null)
            {
                result.Passed = false;
                result.Issues.Add(CreateIssue(
                    ValidationSeverity.Error,
                    "Policy does not define grant controls",
                    "grantControls",
                    "null",
                    "Grant controls with MFA requirement",
                    "Add grant controls section with MFA requirement",
                    "https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/concept-conditional-access-grant"
                ));
                return result;
            }

            // Check for built-in controls
            var builtInControls = GetArrayValue(policy, "grantControls.builtInControls");
            var hasMfaControl = ContainsAnyValue(policy, "grantControls.builtInControls", "mfa", "requireMultifactorAuthentication");
            
            // Check for high-risk scenarios that should require MFA
            var requiresMfa = ShouldRequireMfa(policy);
            
            if (requiresMfa && !hasMfaControl)
            {
                result.Passed = false;
                var currentControls = builtInControls?.ToString() ?? "none";
                result.Issues.Add(CreateIssue(
                    ValidationSeverity.Error,
                    "Policy should require MFA for this access scenario",
                    "grantControls.builtInControls",
                    currentControls,
                    "Include 'mfa' or 'requireMultifactorAuthentication'",
                    "Add MFA requirement to grant controls for security",
                    "https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/howto-conditional-access-policy-all-users-mfa"
                ));
                
                // Add recommendation
                result.Recommendations.Add(CreateRecommendation(
                    "Enable MFA Requirement",
                    "This policy covers scenarios that present security risks and should require Multi-Factor Authentication",
                    ValidationSeverity.Error,
                    "Security",
                    "grantControls.builtInControls",
                    currentControls,
                    "Add 'mfa' to built-in controls",
                    "MFA significantly reduces the risk of account compromise and unauthorized access",
                    8.5,
                    "https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/howto-conditional-access-policy-all-users-mfa"
                ));
            }
            else if (!requiresMfa && hasMfaControl)
            {
                // This might be overly restrictive - add as warning
                result.Recommendations.Add(CreateRecommendation(
                    "Review MFA Requirement",
                    "This policy requires MFA for scenarios that may not need it, potentially impacting user experience",
                    ValidationSeverity.Warning,
                    "User Experience",
                    "grantControls.builtInControls",
                    builtInControls?.ToString() ?? "",
                    "Consider if MFA is necessary for this scenario",
                    "Balance security requirements with user experience",
                    3.0
                ));
            }

            return result;
        }

        /// <summary>
        /// Determines if a policy should require MFA based on its conditions
        /// </summary>
        private bool ShouldRequireMfa(JObject policy)
        {
            // Check for external users
            if (ContainsAnyValue(policy, "conditions.users.includeUsers", "GuestsOrExternalUsers"))
                return true;

            // Check for all users
            if (ContainsAnyValue(policy, "conditions.users.includeUsers", "All"))
                return true;

            // Check for admin roles
            var adminRoles = new[]
            {
                "62e90394-69f5-4237-9190-012177145e10", // Global Administrator
                "194ae4cb-b126-40b2-bd5b-6091b380977d", // Security Administrator
                "7be44c8a-adaf-4e2a-84d6-ab2649e08a13", // Privileged Authentication Administrator
                "c4e39bd9-1100-46d3-8c65-fb160da0071f", // Authentication Administrator
                "9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3", // Application Administrator
                "cf1c38e5-3621-4004-a7cb-879624dced7c"  // Cloud Application Administrator
            };
            
            if (ContainsAnyValue(policy, "conditions.users.includeRoles", adminRoles))
                return true;

            // Check for high-risk applications
            var highRiskApps = new[]
            {
                "00000003-0000-0000-c000-000000000000", // Microsoft Graph
                "00000002-0000-0000-c000-000000000000", // Azure AD Graph
                "797f4846-ba00-4fd7-ba43-dac1f8f63013"  // Windows Azure Service Management API
            };
            
            if (ContainsAnyValue(policy, "conditions.applications.includeApplications", highRiskApps))
                return true;

            // Check for risky sign-in conditions
            var riskLevels = GetArrayValue(policy, "conditions.signInRiskLevels");
            if (riskLevels != null && riskLevels.Any(r => 
                r.ToString().Equals("high", StringComparison.OrdinalIgnoreCase) ||
                r.ToString().Equals("medium", StringComparison.OrdinalIgnoreCase)))
                return true;

            // Check for unmanaged devices
            var deviceStates = GetArrayValue(policy, "conditions.deviceStates.includeStates");
            if (deviceStates != null && deviceStates.Any(s => 
                s.ToString().Equals("All", StringComparison.OrdinalIgnoreCase)))
            {
                var excludeStates = GetArrayValue(policy, "conditions.deviceStates.excludeStates");
                if (excludeStates == null || !excludeStates.Any(s => 
                    s.ToString().Equals("domainJoined", StringComparison.OrdinalIgnoreCase) ||
                    s.ToString().Equals("hybridAzureADJoined", StringComparison.OrdinalIgnoreCase) ||
                    s.ToString().Equals("compliant", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }
    }
}



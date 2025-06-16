



using ConditionalAccessExporter.Models;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Services.ValidationRules.SecurityRules
{
    /// <summary>
    /// Validates that policies enforce device compliance requirements
    /// </summary>
    public class DeviceComplianceRule : BaseValidationRule
    {
        public override string RuleId => "SEC002";
        public override string RuleName => "Device Compliance Validation";
        public override string Description => "Ensures policies require device compliance for access to sensitive resources";
        public override ValidationRuleCategory Category => ValidationRuleCategory.Security;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Warning;

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

            // Check if this policy should require device compliance
            var requiresCompliance = ShouldRequireDeviceCompliance(policy);
            
            if (!requiresCompliance)
            {
                return result; // Policy doesn't need device compliance
            }

            // Check for device compliance controls
            var hasDeviceComplianceControl = ContainsAnyValue(policy, "grantControls.builtInControls", 
                "compliantDevice", "requireCompliantDevice");
            
            var hasDomainJoinedControl = ContainsAnyValue(policy, "grantControls.builtInControls", 
                "domainJoinedDevice", "requireDomainJoinedDevice");

            var hasHybridJoinedControl = ContainsAnyValue(policy, "grantControls.builtInControls", 
                "hybridAzureADJoinedDevice", "requireHybridAzureADJoinedDevice");

            var hasAnyDeviceControl = hasDeviceComplianceControl || hasDomainJoinedControl || hasHybridJoinedControl;

            if (!hasAnyDeviceControl)
            {
                result.Passed = false;
                var currentControls = GetArrayValue(policy, "grantControls.builtInControls")?.ToString() ?? "none";
                
                result.Issues.Add(CreateIssue(
                    ValidationSeverity.Warning,
                    "Policy should require device compliance for access to sensitive resources",
                    "grantControls.builtInControls",
                    currentControls,
                    "Include device compliance control",
                    "Add 'compliantDevice', 'domainJoinedDevice', or 'hybridAzureADJoinedDevice' to grant controls",
                    "https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/concept-conditional-access-grant#require-device-to-be-marked-as-compliant"
                ));

                result.Recommendations.Add(CreateRecommendation(
                    "Enable Device Compliance Requirement",
                    "This policy covers sensitive resources and should require devices to be compliant or managed",
                    ValidationSeverity.Warning,
                    "Security",
                    "grantControls.builtInControls",
                    currentControls,
                    "Add 'compliantDevice' or appropriate device control",
                    "Device compliance ensures that only managed and secure devices can access sensitive resources",
                    7.0,
                    "https://docs.microsoft.com/en-us/azure/active-directory/conditional-access/concept-conditional-access-grant#require-device-to-be-marked-as-compliant"
                ));
            }
            else
            {
                // Check for proper device control combinations
                ValidateDeviceControlCombinations(policy, result);
            }

            return result;
        }

        /// <summary>
        /// Validates the combination of device controls used
        /// </summary>
        private void ValidateDeviceControlCombinations(JObject policy, RuleValidationResult result)
        {
            var operator_ = GetStringValue(policy, "grantControls.operator");
            var controls = GetArrayValue(policy, "grantControls.builtInControls");
            
            if (controls == null) return;

            var controlsList = controls.Select(c => c.ToString()).ToList();
            var hasMultipleDeviceControls = controlsList.Count(c => 
                c.Contains("Device", StringComparison.OrdinalIgnoreCase) ||
                c.Contains("compliant", StringComparison.OrdinalIgnoreCase) ||
                c.Contains("domainJoined", StringComparison.OrdinalIgnoreCase)) > 1;

            if (hasMultipleDeviceControls && operator_ == "AND")
            {
                result.Recommendations.Add(CreateRecommendation(
                    "Review Device Control Operator",
                    "Multiple device controls with AND operator may be overly restrictive",
                    ValidationSeverity.Info,
                    "User Experience",
                    "grantControls.operator",
                    operator_,
                    "Consider using OR operator for device controls",
                    "Using AND with multiple device controls may prevent legitimate access",
                    4.0
                ));
            }
        }

        /// <summary>
        /// Determines if a policy should require device compliance
        /// </summary>
        private bool ShouldRequireDeviceCompliance(JObject policy)
        {
            // Check for sensitive applications
            var sensitiveApps = new[]
            {
                "00000003-0000-0000-c000-000000000000", // Microsoft Graph
                "00000002-0000-0000-c000-000000000000", // Azure AD Graph
                "797f4846-ba00-4fd7-ba43-dac1f8f63013", // Windows Azure Service Management API
                "c5393580-f805-4401-95e8-94b7a6ef2fc2", // Office 365 Management APIs
                "00000009-0000-0000-c000-000000000000"  // Power BI Service
            };
            
            if (ContainsAnyValue(policy, "conditions.applications.includeApplications", sensitiveApps))
                return true;

            // Check for all cloud apps
            if (ContainsAnyValue(policy, "conditions.applications.includeApplications", "All"))
                return true;

            // Check for privileged roles
            var privilegedRoles = new[]
            {
                "62e90394-69f5-4237-9190-012177145e10", // Global Administrator
                "194ae4cb-b126-40b2-bd5b-6091b380977d", // Security Administrator
                "7be44c8a-adaf-4e2a-84d6-ab2649e08a13", // Privileged Authentication Administrator
                "c4e39bd9-1100-46d3-8c65-fb160da0071f", // Authentication Administrator
                "9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3", // Application Administrator
                "cf1c38e5-3621-4004-a7cb-879624dced7c", // Cloud Application Administrator
                "b1be1c3e-b65d-4f19-8427-f6fa0d97feb9", // Conditional Access Administrator
                "729827e3-9c14-49f7-bb1b-9608f156bbb8"  // Helpdesk Administrator
            };
            
            if (ContainsAnyValue(policy, "conditions.users.includeRoles", privilegedRoles))
                return true;

            // Check for external users accessing internal resources
            if (ContainsAnyValue(policy, "conditions.users.includeUsers", "GuestsOrExternalUsers"))
                return true;

            // Check for high-risk sign-ins
            var riskLevels = GetArrayValue(policy, "conditions.signInRiskLevels");
            if (riskLevels != null && riskLevels.Any(r => 
                r.ToString().Equals("high", StringComparison.OrdinalIgnoreCase) ||
                r.ToString().Equals("medium", StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }
    }
}




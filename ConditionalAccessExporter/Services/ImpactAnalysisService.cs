
using ConditionalAccessExporter.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConditionalAccessExporter.Services
{
    public class ImpactAnalysisService
    {
        private readonly GraphServiceClient? _graphServiceClient;
        private readonly RiskAssessmentConfig _riskConfig;

        public ImpactAnalysisService(GraphServiceClient? graphServiceClient = null, RiskAssessmentConfig? riskConfig = null)
        {
            _graphServiceClient = graphServiceClient;
            _riskConfig = riskConfig ?? new RiskAssessmentConfig();
        }

        public async Task<ImpactAnalysis> AnalyzeImpactAsync(PolicyComparison comparison)
        {
            Logger.WriteVerbose($"Analyzing impact for policy: {comparison.PolicyName}");

            var impact = new ImpactAnalysis();

            try
            {
                // Analyze user impact
                await AnalyzeUserImpactAsync(comparison, impact);

                // Analyze application impact
                await AnalyzeApplicationImpactAsync(comparison, impact);

                // Analyze admin access impact
                AnalyzeAdminAccessImpact(comparison, impact);

                // Analyze authentication requirements
                AnalyzeAuthenticationImpact(comparison, impact);

                // Generate impact description
                impact.ImpactDescription = GenerateImpactDescription(impact);

                Logger.WriteVerbose($"Impact analysis complete - Estimated users affected: {impact.EstimatedAffectedUsers}");
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Error during impact analysis: {ex.Message}");
                impact.ImpactDescription = "Unable to calculate precise impact - manual review recommended";
            }

            return impact;
        }

        private async Task AnalyzeUserImpactAsync(PolicyComparison comparison, ImpactAnalysis impact)
        {
            var userConditions = ExtractUserConditions(comparison.ReferencePolicy ?? comparison.EntraPolicy);
            
            if (userConditions == null)
            {
                // If no specific user targeting, assume all users
                impact.EstimatedAffectedUsers = await GetTotalUserCountAsync() ?? 10000; // Default estimate
                impact.AffectedUserGroups.Add("All users");
                return;
            }

            var affectedUserCount = 0;

            // Analyze included users
            if (userConditions.IncludeUsers?.Any() == true)
            {
                foreach (var userId in userConditions.IncludeUsers)
                {
                    if (userId.Equals("All", StringComparison.OrdinalIgnoreCase))
                    {
                        affectedUserCount = await GetTotalUserCountAsync() ?? 10000;
                        impact.AffectedUserGroups.Add("All users");
                        break;
                    }
                    else if (userId.Equals("GuestsOrExternalUsers", StringComparison.OrdinalIgnoreCase))
                    {
                        affectedUserCount += await GetGuestUserCountAsync() ?? 100;
                        impact.AffectedUserGroups.Add("Guest and external users");
                    }
                    else
                    {
                        affectedUserCount += 1; // Individual user
                    }
                }
            }

            // Analyze included groups
            if (userConditions.IncludeGroups?.Any() == true)
            {
                foreach (var groupId in userConditions.IncludeGroups)
                {
                    var groupSize = await GetGroupMemberCountAsync(groupId);
                    affectedUserCount += groupSize;
                    impact.AffectedUserGroups.Add($"Group: {groupId}");
                }
            }

            // Analyze included roles
            if (userConditions.IncludeRoles?.Any() == true)
            {
                foreach (var roleId in userConditions.IncludeRoles)
                {
                    var roleSize = await GetRoleAssignmentCountAsync(roleId);
                    affectedUserCount += roleSize;
                    impact.AffectedUserGroups.Add($"Role: {roleId}");
                }
            }

            // Subtract excluded users (simplified calculation)
            var excludedUserCount = 0;
            if (userConditions.ExcludeUsers?.Any() == true)
            {
                excludedUserCount += userConditions.ExcludeUsers.Count;
            }

            impact.EstimatedAffectedUsers = Math.Max(0, affectedUserCount - excludedUserCount);

            // Estimate sessions (rough approximation: 2-3 sessions per user on average)
            impact.EstimatedAffectedSessions = impact.EstimatedAffectedUsers * 2;
        }

        private async Task AnalyzeApplicationImpactAsync(PolicyComparison comparison, ImpactAnalysis impact)
        {
            var appConditions = ExtractApplicationConditions(comparison.ReferencePolicy ?? comparison.EntraPolicy);
            
            if (appConditions?.IncludeApplications?.Any() == true)
            {
                foreach (var appId in appConditions.IncludeApplications)
                {
                    if (appId.Equals("All", StringComparison.OrdinalIgnoreCase))
                    {
                        impact.AffectedApplications.Add("All cloud applications");
                    }
                    else if (appId.Equals("Office365", StringComparison.OrdinalIgnoreCase))
                    {
                        impact.AffectedApplications.Add("Office 365");
                    }
                    else
                    {
                        var appName = await GetApplicationNameAsync(appId) ?? appId;
                        impact.AffectedApplications.Add(appName);
                    }
                }
            }
        }

        private void AnalyzeAdminAccessImpact(PolicyComparison comparison, ImpactAnalysis impact)
        {
            var userConditions = ExtractUserConditions(comparison.ReferencePolicy ?? comparison.EntraPolicy);
            
            if (userConditions?.IncludeRoles?.Any() == true)
            {
                var adminRoles = new[]
                {
                    "Global Administrator",
                    "Conditional Access Administrator",
                    "Security Administrator",
                    "Application Administrator",
                    "Cloud Application Administrator"
                };

                var hasAdminRoles = userConditions.IncludeRoles.Any(role => 
                    adminRoles.Any(adminRole => 
                        role.Contains(adminRole, StringComparison.OrdinalIgnoreCase)));

                if (hasAdminRoles)
                {
                    impact.WillBlockAdminAccess = true;
                    impact.PotentialAccessIssues.Add("May block administrative access");
                }
            }

            if (userConditions?.IncludeUsers?.Contains("All", StringComparison.OrdinalIgnoreCase) == true)
            {
                impact.WillBlockAdminAccess = true;
                impact.PotentialAccessIssues.Add("Applies to all users including administrators");
            }
        }

        private void AnalyzeAuthenticationImpact(PolicyComparison comparison, ImpactAnalysis impact)
        {
            var grantControls = ExtractGrantControls(comparison.ReferencePolicy ?? comparison.EntraPolicy);
            
            if (grantControls?.BuiltInControls?.Any() == true)
            {
                var mfaControls = new[] { "mfa", "MultiFactor", "multiFactorAuthentication" };
                var hasRequireMfa = grantControls.BuiltInControls.Any(control =>
                    mfaControls.Any(mfaControl => 
                        control.Contains(mfaControl, StringComparison.OrdinalIgnoreCase)));

                if (hasRequireMfa)
                {
                    impact.WillRequireAdditionalAuthentication = true;
                    impact.PotentialAccessIssues.Add("Will require multi-factor authentication");
                }

                var deviceControls = new[] { "compliantDevice", "domainJoinedDevice", "hybridAzureADJoined" };
                var hasDeviceRequirement = grantControls.BuiltInControls.Any(control =>
                    deviceControls.Any(deviceControl => 
                        control.Contains(deviceControl, StringComparison.OrdinalIgnoreCase)));

                if (hasDeviceRequirement)
                {
                    impact.PotentialAccessIssues.Add("Will require compliant or managed device");
                }
            }
        }

        private string GenerateImpactDescription(ImpactAnalysis impact)
        {
            var description = new List<string>();

            if (impact.EstimatedAffectedUsers > 0)
            {
                var userImpact = impact.EstimatedAffectedUsers switch
                {
                    <= 10 => "Limited user impact",
                    <= 100 => "Moderate user impact",
                    <= 1000 => "Significant user impact",
                    _ => "Extensive user impact"
                };
                description.Add($"{userImpact} ({impact.EstimatedAffectedUsers} users)");
            }

            if (impact.WillBlockAdminAccess)
            {
                description.Add("⚠️ May impact administrator access");
            }

            if (impact.WillRequireAdditionalAuthentication)
            {
                description.Add("Users will need to complete additional authentication");
            }

            if (impact.AffectedApplications.Any())
            {
                description.Add($"Affects {impact.AffectedApplications.Count} application(s)");
            }

            return description.Any() ? string.Join(". ", description) : "Minimal impact expected";
        }

        // Helper methods to extract policy conditions
        private NormalizedUsers? ExtractUserConditions(object? policy)
        {
            if (policy == null) return null;

            try
            {
                var json = JsonConvert.SerializeObject(policy);
                var policyObj = JObject.Parse(json);
                var userConditions = policyObj["conditions"]?["users"];

                if (userConditions == null) return null;

                return new NormalizedUsers
                {
                    IncludeUsers = userConditions["includeUsers"]?.ToObject<List<string>>(),
                    ExcludeUsers = userConditions["excludeUsers"]?.ToObject<List<string>>(),
                    IncludeGroups = userConditions["includeGroups"]?.ToObject<List<string>>(),
                    ExcludeGroups = userConditions["excludeGroups"]?.ToObject<List<string>>(),
                    IncludeRoles = userConditions["includeRoles"]?.ToObject<List<string>>(),
                    ExcludeRoles = userConditions["excludeRoles"]?.ToObject<List<string>>()
                };
            }
            catch
            {
                return null;
            }
        }

        private NormalizedApplications? ExtractApplicationConditions(object? policy)
        {
            if (policy == null) return null;

            try
            {
                var json = JsonConvert.SerializeString(policy);
                var policyObj = JObject.Parse(json);
                var appConditions = policyObj["conditions"]?["applications"];

                if (appConditions == null) return null;

                return new NormalizedApplications
                {
                    IncludeApplications = appConditions["includeApplications"]?.ToObject<List<string>>(),
                    ExcludeApplications = appConditions["excludeApplications"]?.ToObject<List<string>>(),
                    IncludeUserActions = appConditions["includeUserActions"]?.ToObject<List<string>>()
                };
            }
            catch
            {
                return null;
            }
        }

        private NormalizedGrantControls? ExtractGrantControls(object? policy)
        {
            if (policy == null) return null;

            try
            {
                var json = JsonConvert.SerializeObject(policy);
                var policyObj = JObject.Parse(json);
                var grantControls = policyObj["grantControls"];

                if (grantControls == null) return null;

                return new NormalizedGrantControls
                {
                    Operator = grantControls["operator"]?.ToString(),
                    BuiltInControls = grantControls["builtInControls"]?.ToObject<List<string>>(),
                    CustomAuthenticationFactors = grantControls["customAuthenticationFactors"]?.ToObject<List<string>>(),
                    TermsOfUse = grantControls["termsOfUse"]?.ToObject<List<string>>()
                };
            }
            catch
            {
                return null;
            }
        }

        // Graph API helper methods (with fallbacks for when Graph client is not available)
        private async Task<int?> GetTotalUserCountAsync()
        {
            if (_graphServiceClient == null)
                return null;

            try
            {
                var users = await _graphServiceClient.Users.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.QueryParameters.Top = 1;
                    requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                });
                return users?.OdataCount;
            }
            catch
            {
                return null;
            }
        }

        private async Task<int> GetGuestUserCountAsync()
        {
            if (_graphServiceClient == null)
                return 100; // Default estimate

            try
            {
                var users = await _graphServiceClient.Users.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = "userType eq 'Guest'";
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.QueryParameters.Top = 1;
                    requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                });
                return users?.OdataCount ?? 100;
            }
            catch
            {
                return 100;
            }
        }

        private async Task<int> GetGroupMemberCountAsync(string groupId)
        {
            if (_graphServiceClient == null)
                return 50; // Default estimate

            try
            {
                var members = await _graphServiceClient.Groups[groupId].Members.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Count = true;
                    requestConfiguration.QueryParameters.Top = 1;
                    requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                });
                return members?.OdataCount ?? 50;
            }
            catch
            {
                return 50;
            }
        }

        private async Task<int> GetRoleAssignmentCountAsync(string roleId)
        {
            if (_graphServiceClient == null)
                return 10; // Default estimate

            try
            {
                // This is a simplified approach - in reality, you'd need to query role assignments
                return 10; // Placeholder
            }
            catch
            {
                return 10;
            }
        }

        private async Task<string?> GetApplicationNameAsync(string appId)
        {
            if (_graphServiceClient == null)
                return null;

            try
            {
                var app = await _graphServiceClient.Applications[appId].GetAsync();
                return app?.DisplayName;
            }
            catch
            {
                return null;
            }
        }
    }
}


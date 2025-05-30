using ConditionalAccessExporter.Models;
using Newtonsoft.Json;

namespace ConditionalAccessExporter.Services
{
    public class TerraformConversionService
    {
        private readonly List<string> _conversionLog = new();
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        public async Task<TerraformConversionResult> ConvertToGraphJsonAsync(TerraformParseResult parseResult)
        {
            var result = new TerraformConversionResult
            {
                ConvertedAt = DateTime.UtcNow,
                SourcePath = parseResult.SourcePath
            };

            try
            {
                var convertedPolicies = new List<object>();
                var successCount = 0;
                var failureCount = 0;

                foreach (var terraformPolicy in parseResult.Policies)
                {
                    try
                    {
                        var graphPolicy = ConvertPolicyToGraphFormat(terraformPolicy, parseResult);
                        convertedPolicies.Add(graphPolicy);
                        successCount++;
                        _conversionLog.Add($"Successfully converted policy: {terraformPolicy.DisplayName ?? terraformPolicy.ResourceName}");
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _errors.Add($"Failed to convert policy '{terraformPolicy.DisplayName ?? terraformPolicy.ResourceName}': {ex.Message}");
                    }
                }

                // Create the final export structure matching the existing format
                var exportData = new
                {
                    ExportedAt = DateTime.UtcNow,
                    Source = "Terraform",
                    SourcePath = parseResult.SourcePath,
                    PoliciesCount = convertedPolicies.Count,
                    Policies = convertedPolicies,
                    ConversionSummary = new
                    {
                        SuccessfulConversions = successCount,
                        FailedConversions = failureCount,
                        TotalTerraformPolicies = parseResult.Policies.Count,
                        VariablesFound = parseResult.Variables.Count,
                        LocalsFound = parseResult.Locals.Count,
                        DataSourcesFound = parseResult.DataSources.Count
                    }
                };

                result.ConvertedPolicies = exportData;
                result.SuccessfulConversions = successCount;
                result.FailedConversions = failureCount;
            }
            catch (Exception ex)
            {
                _errors.Add($"General conversion error: {ex.Message}");
            }

            result.ConversionLog = _conversionLog;
            result.Errors = _errors;
            result.Warnings = _warnings;

            return await Task.FromResult(result);
        }

        private object ConvertPolicyToGraphFormat(TerraformConditionalAccessPolicy terraformPolicy, TerraformParseResult parseResult)
        {
            // Convert Terraform policy to Microsoft Graph format
            var graphPolicy = new
            {
                Id = GenerateGuid(terraformPolicy.ResourceName), // Generate consistent GUID for resource name
                DisplayName = ResolveValue(terraformPolicy.DisplayName, parseResult) ?? terraformPolicy.ResourceName,
                State = ConvertState(ResolveValue(terraformPolicy.State, parseResult) ?? terraformPolicy.State),
                CreatedDateTime = (DateTime?)null, // Not available in Terraform
                ModifiedDateTime = (DateTime?)null, // Not available in Terraform
                Conditions = ConvertConditions(terraformPolicy.Conditions, parseResult),
                GrantControls = ConvertGrantControls(terraformPolicy.GrantControls, parseResult),
                SessionControls = ConvertSessionControls(terraformPolicy.SessionControls, parseResult),
                TerraformMetadata = new
                {
                    ResourceName = terraformPolicy.ResourceName,
                    SourceType = "Terraform HCL",
                    ConvertedAt = DateTime.UtcNow
                }
            };

            return graphPolicy;
        }

        private object ConvertConditions(TerraformConditions? terraformConditions, TerraformParseResult parseResult)
        {
            if (terraformConditions == null) return new { };

            return new
            {
                Applications = ConvertApplicationConditions(terraformConditions.Applications, parseResult),
                Users = ConvertUserConditions(terraformConditions.Users, parseResult),
                ClientAppTypes = ResolveStringArray(terraformConditions.ClientAppTypes, parseResult),
                Platforms = ConvertPlatformConditions(terraformConditions.Platforms, parseResult),
                Locations = ConvertLocationConditions(terraformConditions.Locations, parseResult),
                SignInRiskLevels = ConvertRiskLevels(terraformConditions.SignInRiskLevels, parseResult),
                UserRiskLevels = ConvertRiskLevels(terraformConditions.UserRiskLevels, parseResult),
                ClientApplications = ConvertClientApplicationConditions(terraformConditions.ClientApplications, parseResult)
            };
        }

        private object ConvertApplicationConditions(TerraformApplications? terraformApps, TerraformParseResult parseResult)
        {
            if (terraformApps == null) return new { };

            return new
            {
                IncludeApplications = ResolveStringArray(terraformApps.IncludeApplications, parseResult),
                ExcludeApplications = ResolveStringArray(terraformApps.ExcludeApplications, parseResult),
                IncludeUserActions = ResolveStringArray(terraformApps.IncludeUserActions, parseResult),
                IncludeAuthenticationContextClassReferences = ResolveStringArray(terraformApps.IncludeAuthenticationContextClassReferences, parseResult)
            };
        }

        private object ConvertUserConditions(TerraformUsers? terraformUsers, TerraformParseResult parseResult)
        {
            if (terraformUsers == null) return new { };

            return new
            {
                IncludeUsers = ResolveStringArray(terraformUsers.IncludeUsers, parseResult),
                ExcludeUsers = ResolveStringArray(terraformUsers.ExcludeUsers, parseResult),
                IncludeGroups = ResolveStringArray(terraformUsers.IncludeGroups, parseResult),
                ExcludeGroups = ResolveStringArray(terraformUsers.ExcludeGroups, parseResult),
                IncludeRoles = ResolveStringArray(terraformUsers.IncludeRoles, parseResult),
                ExcludeRoles = ResolveStringArray(terraformUsers.ExcludeRoles, parseResult)
            };
        }

        private object ConvertPlatformConditions(TerraformPlatforms? terraformPlatforms, TerraformParseResult parseResult)
        {
            if (terraformPlatforms == null) return new { };

            return new
            {
                IncludePlatforms = ConvertPlatforms(terraformPlatforms.IncludePlatforms, parseResult),
                ExcludePlatforms = ConvertPlatforms(terraformPlatforms.ExcludePlatforms, parseResult)
            };
        }

        private object ConvertLocationConditions(TerraformLocations? terraformLocations, TerraformParseResult parseResult)
        {
            if (terraformLocations == null) return new { };

            return new
            {
                IncludeLocations = ResolveStringArray(terraformLocations.IncludeLocations, parseResult),
                ExcludeLocations = ResolveStringArray(terraformLocations.ExcludeLocations, parseResult)
            };
        }

        private object ConvertClientApplicationConditions(TerraformClientApplications? terraformClientApps, TerraformParseResult parseResult)
        {
            if (terraformClientApps == null) return new { };

            return new
            {
                IncludeServicePrincipals = ResolveStringArray(terraformClientApps.IncludeServicePrincipals, parseResult),
                ExcludeServicePrincipals = ResolveStringArray(terraformClientApps.ExcludeServicePrincipals, parseResult)
            };
        }

        private object ConvertGrantControls(TerraformGrantControls? terraformGrantControls, TerraformParseResult parseResult)
        {
            if (terraformGrantControls == null) return new { };

            return new
            {
                Operator = ResolveValue(terraformGrantControls.Operator, parseResult),
                BuiltInControls = ConvertBuiltInControls(terraformGrantControls.BuiltInControls, parseResult),
                CustomAuthenticationFactors = ResolveStringArray(terraformGrantControls.CustomAuthenticationFactors, parseResult),
                TermsOfUse = ResolveStringArray(terraformGrantControls.TermsOfUse, parseResult),
                AuthenticationStrength = ConvertAuthenticationStrength(terraformGrantControls.AuthenticationStrength, parseResult)
            };
        }

        private object ConvertSessionControls(TerraformSessionControls? terraformSessionControls, TerraformParseResult parseResult)
        {
            if (terraformSessionControls == null) return new { };

            return new
            {
                ApplicationEnforcedRestrictions = ConvertApplicationEnforcedRestrictions(terraformSessionControls.ApplicationEnforcedRestrictions),
                CloudAppSecurity = ConvertCloudAppSecurity(terraformSessionControls.CloudAppSecurity, parseResult),
                PersistentBrowser = ConvertPersistentBrowser(terraformSessionControls.PersistentBrowser, parseResult),
                SignInFrequency = ConvertSignInFrequency(terraformSessionControls.SignInFrequency, parseResult)
            };
        }

        private object? ConvertApplicationEnforcedRestrictions(TerraformApplicationEnforcedRestrictions? restrictions)
        {
            if (restrictions == null) return null;

            return new
            {
                IsEnabled = restrictions.IsEnabled
            };
        }

        private object? ConvertCloudAppSecurity(TerraformCloudAppSecurity? cloudAppSecurity, TerraformParseResult parseResult)
        {
            if (cloudAppSecurity == null) return null;

            return new
            {
                IsEnabled = cloudAppSecurity.IsEnabled,
                CloudAppSecurityType = ResolveValue(cloudAppSecurity.CloudAppSecurityType, parseResult)
            };
        }

        private object? ConvertPersistentBrowser(TerraformPersistentBrowser? persistentBrowser, TerraformParseResult parseResult)
        {
            if (persistentBrowser == null) return null;

            return new
            {
                IsEnabled = persistentBrowser.IsEnabled,
                Mode = ResolveValue(persistentBrowser.Mode, parseResult)
            };
        }

        private object? ConvertSignInFrequency(TerraformSignInFrequency? signInFrequency, TerraformParseResult parseResult)
        {
            if (signInFrequency == null) return null;

            return new
            {
                IsEnabled = signInFrequency.IsEnabled,
                Type = ResolveValue(signInFrequency.Type, parseResult),
                Value = signInFrequency.Value,
                AuthenticationType = ResolveValue(signInFrequency.AuthenticationType, parseResult)
            };
        }

        private object? ConvertAuthenticationStrength(TerraformAuthenticationStrength? authStrength, TerraformParseResult parseResult)
        {
            if (authStrength == null) return null;

            return new
            {
                Id = ResolveValue(authStrength.Id, parseResult),
                DisplayName = ResolveValue(authStrength.DisplayName, parseResult)
            };
        }

        private string ConvertState(string? terraformState)
        {
            var normalizedState = terraformState?.ToLowerInvariant();
            return normalizedState switch
            {
                "enabled" => "enabled",
                "disabled" => "disabled",
                "enabledforreportingbutnotenforced" => "enabledForReportingButNotEnforced",
                _ => "disabled" // Default to disabled if not specified
            };
        }

        private List<string>? ConvertBuiltInControls(List<string>? terraformControls, TerraformParseResult parseResult)
        {
            if (terraformControls == null) return null;

            var resolvedControls = ResolveStringArray(terraformControls, parseResult);
            if (resolvedControls == null) return null;

            // Map Terraform control names to Graph API control names
            var controlMapping = new Dictionary<string, string>
            {
                ["block"] = "block",
                ["mfa"] = "mfa",
                ["compliantDevice"] = "compliantDevice",
                ["domainJoinedDevice"] = "domainJoinedDevice",
                ["approvedApplication"] = "approvedApplication",
                ["compliantApplication"] = "compliantApplication",
                ["passwordChange"] = "passwordChange",
                ["unknownFutureValue"] = "unknownFutureValue"
            };

            return resolvedControls.Select(control => controlMapping.GetValueOrDefault(control, control)).ToList();
        }

        private List<string>? ConvertPlatforms(List<string>? terraformPlatforms, TerraformParseResult parseResult)
        {
            if (terraformPlatforms == null) return null;

            var resolvedPlatforms = ResolveStringArray(terraformPlatforms, parseResult);
            if (resolvedPlatforms == null) return null;

            // Map Terraform platform names to Graph API platform names
            var platformMapping = new Dictionary<string, string>
            {
                ["android"] = "android",
                ["iOS"] = "iOS",
                ["windows"] = "windows",
                ["windowsPhone"] = "windowsPhone",
                ["macOS"] = "macOS",
                ["linux"] = "linux",
                ["all"] = "all",
                ["unknownFutureValue"] = "unknownFutureValue"
            };

            return resolvedPlatforms.Select(platform => platformMapping.GetValueOrDefault(platform, platform)).ToList();
        }

        private List<string>? ConvertRiskLevels(List<string>? terraformRiskLevels, TerraformParseResult parseResult)
        {
            if (terraformRiskLevels == null) return null;

            var resolvedRiskLevels = ResolveStringArray(terraformRiskLevels, parseResult);
            if (resolvedRiskLevels == null) return null;

            // Map Terraform risk level names to Graph API risk level names
            var riskLevelMapping = new Dictionary<string, string>
            {
                ["low"] = "low",
                ["medium"] = "medium",
                ["high"] = "high",
                ["hidden"] = "hidden",
                ["none"] = "none",
                ["unknownFutureValue"] = "unknownFutureValue"
            };

            return resolvedRiskLevels.Select(level => riskLevelMapping.GetValueOrDefault(level, level)).ToList();
        }

        private string? ResolveValue(string? value, TerraformParseResult parseResult)
        {
            if (string.IsNullOrEmpty(value)) return value;

            // Handle Terraform variable references like var.variable_name
            if (value.StartsWith("var."))
            {
                var variableName = value.Substring(4);
                var variable = parseResult.Variables.FirstOrDefault(v => v.Name == variableName);
                if (variable?.DefaultValue != null)
                {
                    _conversionLog.Add($"Resolved variable '{variableName}' to default value");
                    return variable.DefaultValue.ToString();
                }
                else
                {
                    _warnings.Add($"Variable '{variableName}' referenced but no default value found");
                    return value; // Return original reference
                }
            }

            // Handle Terraform local references like local.local_name
            if (value.StartsWith("local."))
            {
                var localName = value.Substring(6);
                var local = parseResult.Locals.FirstOrDefault(l => l.Name == localName);
                if (local?.Value != null)
                {
                    _conversionLog.Add($"Resolved local '{localName}' to value");
                    return local.Value.ToString();
                }
                else
                {
                    _warnings.Add($"Local '{localName}' referenced but not found");
                    return value; // Return original reference
                }
            }

            // Handle data source references like data.azuread_group.group_name.object_id
            if (value.StartsWith("data."))
            {
                _warnings.Add($"Data source reference '{value}' cannot be resolved without live data");
                return value; // Return original reference
            }

            return value;
        }

        private List<string>? ResolveStringArray(List<string>? values, TerraformParseResult parseResult)
        {
            if (values == null) return null;

            return values.Select(value => ResolveValue(value, parseResult) ?? value).ToList();
        }

        private string GenerateGuid(string input)
        {
            // Generate a consistent GUID based on the input string
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            var guid = new Guid(hash.Take(16).ToArray());
            return guid.ToString();
        }

        public async Task<bool> ValidateConvertedPolicyAsync(object policy)
        {
            try
            {
                // Basic validation - check required fields
                var json = JsonConvert.SerializeObject(policy);
                var policyObj = JsonConvert.DeserializeObject<dynamic>(json);

                if (policyObj?.DisplayName == null)
                {
                    _errors.Add("Policy validation failed: DisplayName is required");
                    return false;
                }

                if (policyObj?.State == null)
                {
                    _warnings.Add("Policy validation warning: State is not specified");
                }

                // Additional schema validation could be added here
                return true;
            }
            catch (Exception ex)
            {
                _errors.Add($"Policy validation error: {ex.Message}");
                return false;
            }
        }
    }
}
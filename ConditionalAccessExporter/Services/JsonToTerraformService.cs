using ConditionalAccessExporter.Models;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace ConditionalAccessExporter.Services
{
    public class JsonToTerraformService
    {
        private readonly List<string> _conversionLog = new();
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();
        private readonly Dictionary<string, string> _variables = new();

        public async Task<JsonToTerraformResult> ConvertJsonToTerraformAsync(string jsonFilePath, TerraformOutputOptions? options = null)
        {
            var result = new JsonToTerraformResult
            {
                ConvertedAt = DateTime.UtcNow,
                SourcePath = jsonFilePath,
                Options = options ?? new TerraformOutputOptions()
            };

            try
            {
                // Read and parse JSON file
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                var policyExport = JsonConvert.DeserializeObject<JsonPolicyExport>(jsonContent);

                if (policyExport?.Policies == null || !policyExport.Policies.Any())
                {
                    _errors.Add("No policies found in the JSON file or invalid format");
                    result.Errors = _errors;
                    return result;
                }

                _conversionLog.Add($"Found {policyExport.Policies.Count} policies to convert");

                // Create output directory
                var outputDir = Path.Combine(Directory.GetCurrentDirectory(), result.Options.OutputDirectory);
                Directory.CreateDirectory(outputDir);
                result.OutputPath = outputDir;

                var successCount = 0;
                var failureCount = 0;

                // Generate Terraform files based on options
                if (result.Options.GenerateProviderConfig)
                {
                    await GenerateProviderConfigAsync(outputDir, result);
                }

                if (result.Options.GenerateVariables)
                {
                    await GenerateVariablesFileAsync(outputDir, policyExport.Policies, result);
                }

                if (result.Options.SeparateFilePerPolicy)
                {
                    // Generate separate file for each policy
                    foreach (var policy in policyExport.Policies)
                    {
                        try
                        {
                            await GeneratePolicyFileAsync(outputDir, policy, result);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            failureCount++;
                            _errors.Add($"Failed to generate file for policy '{policy.DisplayName}': {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Generate single main.tf file with all policies
                    try
                    {
                        await GenerateMainTerraformFileAsync(outputDir, policyExport.Policies, result);
                        successCount = policyExport.Policies.Count;
                    }
                    catch (Exception ex)
                    {
                        failureCount = policyExport.Policies.Count;
                        _errors.Add($"Failed to generate main Terraform file: {ex.Message}");
                    }
                }

                if (result.Options.GenerateModuleStructure)
                {
                    await GenerateModuleStructureAsync(outputDir, result);
                }

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

            return result;
        }

        private async Task GenerateProviderConfigAsync(string outputDir, JsonToTerraformResult result)
        {
            var providerContent = new StringBuilder();
            
            if (result.Options.IncludeComments)
            {
                providerContent.AppendLine("# Terraform Provider Configuration");
                providerContent.AppendLine("# This file defines the required providers and their versions");
                providerContent.AppendLine();
            }

            providerContent.AppendLine("terraform {");
            providerContent.AppendLine("  required_providers {");
            providerContent.AppendLine("    azuread = {");
            providerContent.AppendLine("      source  = \"hashicorp/azuread\"");
            providerContent.AppendLine($"      version = \"{result.Options.ProviderVersion}\"");
            providerContent.AppendLine("    }");
            providerContent.AppendLine("  }");
            providerContent.AppendLine("}");
            providerContent.AppendLine();
            providerContent.AppendLine("provider \"azuread\" {");

            if (result.Options.IncludeComments)
            {
                providerContent.AppendLine("  # Configuration options");
                providerContent.AppendLine("  # tenant_id     = var.tenant_id");
                providerContent.AppendLine("  # client_id     = var.client_id");
                providerContent.AppendLine("  # client_secret = var.client_secret");
            }

            providerContent.AppendLine("}");

            var filePath = Path.Combine(outputDir, "providers.tf");
            await File.WriteAllTextAsync(filePath, providerContent.ToString());
            result.GeneratedFiles.Add(filePath);
            _conversionLog.Add("Generated providers.tf");
        }

        private async Task GenerateVariablesFileAsync(string outputDir, List<JsonConditionalAccessPolicy> policies, JsonToTerraformResult result)
        {
            var variablesContent = new StringBuilder();

            if (result.Options.IncludeComments)
            {
                variablesContent.AppendLine("# Terraform Variables");
                variablesContent.AppendLine("# Define reusable variables for Conditional Access policies");
                variablesContent.AppendLine();
            }

            // Collect unique values that could be variables
            var uniqueGroups = new HashSet<string>();
            var uniqueUsers = new HashSet<string>();
            var uniqueApplications = new HashSet<string>();
            var uniqueLocations = new HashSet<string>();

            foreach (var policy in policies)
            {
                CollectUniqueValues(policy, uniqueGroups, uniqueUsers, uniqueApplications, uniqueLocations);
            }

            // Generate variable definitions
            if (uniqueGroups.Any())
            {
                variablesContent.AppendLine("variable \"conditional_access_groups\" {");
                variablesContent.AppendLine("  description = \"Groups used in conditional access policies\"");
                variablesContent.AppendLine("  type        = map(string)");
                variablesContent.AppendLine("  default = {");
                foreach (var group in uniqueGroups.Take(5)) // Limit examples
                {
                    var varName = SanitizeVariableName(group);
                    variablesContent.AppendLine($"    {varName} = \"{group}\"");
                }
                variablesContent.AppendLine("  }");
                variablesContent.AppendLine("}");
                variablesContent.AppendLine();
            }

            if (uniqueApplications.Any())
            {
                variablesContent.AppendLine("variable \"conditional_access_applications\" {");
                variablesContent.AppendLine("  description = \"Applications used in conditional access policies\"");
                variablesContent.AppendLine("  type        = map(string)");
                variablesContent.AppendLine("  default = {");
                foreach (var app in uniqueApplications.Take(5)) // Limit examples
                {
                    var varName = SanitizeVariableName(app);
                    variablesContent.AppendLine($"    {varName} = \"{app}\"");
                }
                variablesContent.AppendLine("  }");
                variablesContent.AppendLine("}");
                variablesContent.AppendLine();
            }

            // Add tenant configuration variables
            variablesContent.AppendLine("variable \"tenant_id\" {");
            variablesContent.AppendLine("  description = \"Azure AD Tenant ID\"");
            variablesContent.AppendLine("  type        = string");
            variablesContent.AppendLine("  sensitive   = true");
            variablesContent.AppendLine("}");
            variablesContent.AppendLine();

            var filePath = Path.Combine(outputDir, "variables.tf");
            await File.WriteAllTextAsync(filePath, variablesContent.ToString());
            result.GeneratedFiles.Add(filePath);
            _conversionLog.Add("Generated variables.tf");
        }

        private async Task GenerateMainTerraformFileAsync(string outputDir, List<JsonConditionalAccessPolicy> policies, JsonToTerraformResult result)
        {
            var terraformContent = new StringBuilder();

            if (result.Options.IncludeComments)
            {
                terraformContent.AppendLine("# Conditional Access Policies");
                terraformContent.AppendLine($"# Generated from JSON export on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                terraformContent.AppendLine($"# Total policies: {policies.Count}");
                terraformContent.AppendLine();
            }

            foreach (var policy in policies)
            {
                var resourceName = SanitizeResourceName(policy.DisplayName);
                terraformContent.AppendLine(GeneratePolicyResource(policy, resourceName, result.Options.IncludeComments));
                terraformContent.AppendLine();
            }

            var filePath = Path.Combine(outputDir, "main.tf");
            await File.WriteAllTextAsync(filePath, terraformContent.ToString());
            result.GeneratedFiles.Add(filePath);
            _conversionLog.Add("Generated main.tf");
        }

        private async Task GeneratePolicyFileAsync(string outputDir, JsonConditionalAccessPolicy policy, JsonToTerraformResult result)
        {
            var resourceName = SanitizeResourceName(policy.DisplayName);
            var fileName = $"ca_policy_{resourceName}.tf";
            var filePath = Path.Combine(outputDir, fileName);

            var content = new StringBuilder();
            
            if (result.Options.IncludeComments)
            {
                content.AppendLine($"# Conditional Access Policy: {policy.DisplayName}");
                content.AppendLine($"# Generated from JSON export on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                content.AppendLine();
            }

            content.AppendLine(GeneratePolicyResource(policy, resourceName, result.Options.IncludeComments));

            await File.WriteAllTextAsync(filePath, content.ToString());
            result.GeneratedFiles.Add(filePath);
            _conversionLog.Add($"Generated {fileName}");
        }

        private string GeneratePolicyResource(JsonConditionalAccessPolicy policy, string resourceName, bool includeComments)
        {
            var resource = new StringBuilder();

            if (includeComments)
            {
                resource.AppendLine($"# Policy: {policy.DisplayName}");
                resource.AppendLine($"# State: {policy.State}");
                if (policy.CreatedDateTime.HasValue)
                    resource.AppendLine($"# Created: {policy.CreatedDateTime:yyyy-MM-dd}");
                if (policy.ModifiedDateTime.HasValue)
                    resource.AppendLine($"# Modified: {policy.ModifiedDateTime:yyyy-MM-dd}");
            }

            resource.AppendLine($"resource \"azuread_conditional_access_policy\" \"{resourceName}\" {{");
            resource.AppendLine($"  display_name = \"{EscapeString(policy.DisplayName)}\"");
            resource.AppendLine($"  state        = \"{policy.State.ToLower()}\"");
            resource.AppendLine();

            // Generate conditions block
            if (policy.Conditions != null)
            {
                resource.AppendLine("  conditions {");
                resource.Append(GenerateConditionsBlock(policy.Conditions, includeComments));
                resource.AppendLine("  }");
                resource.AppendLine();
            }

            // Generate grant controls block
            if (policy.GrantControls != null)
            {
                resource.AppendLine("  grant_controls {");
                resource.Append(GenerateGrantControlsBlock(policy.GrantControls, includeComments));
                resource.AppendLine("  }");
                resource.AppendLine();
            }

            // Generate session controls block
            if (policy.SessionControls != null && HasSessionControls(policy.SessionControls))
            {
                resource.AppendLine("  session_controls {");
                resource.Append(GenerateSessionControlsBlock(policy.SessionControls, includeComments));
                resource.AppendLine("  }");
            }

            resource.AppendLine("}");

            return resource.ToString();
        }

        private string GenerateConditionsBlock(JsonConditions conditions, bool includeComments)
        {
            var block = new StringBuilder();

            // Applications
            if (conditions.Applications != null && HasApplicationConditions(conditions.Applications))
            {
                block.AppendLine("    applications {");
                if (conditions.Applications.IncludeApplications?.Any() == true)
                {
                    block.AppendLine($"      included_applications = {FormatStringArray(conditions.Applications.IncludeApplications)}");
                }
                if (conditions.Applications.ExcludeApplications?.Any() == true)
                {
                    block.AppendLine($"      excluded_applications = {FormatStringArray(conditions.Applications.ExcludeApplications)}");
                }
                if (conditions.Applications.IncludeUserActions?.Any() == true)
                {
                    block.AppendLine($"      included_user_actions = {FormatStringArray(conditions.Applications.IncludeUserActions)}");
                }
                block.AppendLine("    }");
                block.AppendLine();
            }

            // Users
            if (conditions.Users != null && HasUserConditions(conditions.Users))
            {
                block.AppendLine("    users {");
                if (conditions.Users.IncludeUsers?.Any() == true)
                {
                    block.AppendLine($"      included_users = {FormatStringArray(conditions.Users.IncludeUsers)}");
                }
                if (conditions.Users.ExcludeUsers?.Any() == true)
                {
                    block.AppendLine($"      excluded_users = {FormatStringArray(conditions.Users.ExcludeUsers)}");
                }
                if (conditions.Users.IncludeGroups?.Any() == true)
                {
                    block.AppendLine($"      included_groups = {FormatStringArray(conditions.Users.IncludeGroups)}");
                }
                if (conditions.Users.ExcludeGroups?.Any() == true)
                {
                    block.AppendLine($"      excluded_groups = {FormatStringArray(conditions.Users.ExcludeGroups)}");
                }
                if (conditions.Users.IncludeRoles?.Any() == true)
                {
                    block.AppendLine($"      included_roles = {FormatStringArray(conditions.Users.IncludeRoles)}");
                }
                if (conditions.Users.ExcludeRoles?.Any() == true)
                {
                    block.AppendLine($"      excluded_roles = {FormatStringArray(conditions.Users.ExcludeRoles)}");
                }
                block.AppendLine("    }");
                block.AppendLine();
            }

            // Client App Types
            if (conditions.ClientAppTypes?.Any() == true)
            {
                block.AppendLine($"    client_app_types = {FormatStringArray(conditions.ClientAppTypes)}");
                block.AppendLine();
            }

            // Platforms
            if (conditions.Platforms != null && HasPlatformConditions(conditions.Platforms))
            {
                block.AppendLine("    platforms {");
                if (conditions.Platforms.IncludePlatforms?.Any() == true)
                {
                    block.AppendLine($"      included_platforms = {FormatStringArray(conditions.Platforms.IncludePlatforms)}");
                }
                if (conditions.Platforms.ExcludePlatforms?.Any() == true)
                {
                    block.AppendLine($"      excluded_platforms = {FormatStringArray(conditions.Platforms.ExcludePlatforms)}");
                }
                block.AppendLine("    }");
                block.AppendLine();
            }

            // Locations
            if (conditions.Locations != null && HasLocationConditions(conditions.Locations))
            {
                block.AppendLine("    locations {");
                if (conditions.Locations.IncludeLocations?.Any() == true)
                {
                    block.AppendLine($"      included_locations = {FormatStringArray(conditions.Locations.IncludeLocations)}");
                }
                if (conditions.Locations.ExcludeLocations?.Any() == true)
                {
                    block.AppendLine($"      excluded_locations = {FormatStringArray(conditions.Locations.ExcludeLocations)}");
                }
                block.AppendLine("    }");
                block.AppendLine();
            }

            // Risk Levels
            if (conditions.SignInRiskLevels?.Any() == true)
            {
                block.AppendLine($"    sign_in_risk_levels = {FormatStringArray(conditions.SignInRiskLevels)}");
                block.AppendLine();
            }

            if (conditions.UserRiskLevels?.Any() == true)
            {
                block.AppendLine($"    user_risk_levels = {FormatStringArray(conditions.UserRiskLevels)}");
                block.AppendLine();
            }

            return block.ToString();
        }

        private string GenerateGrantControlsBlock(JsonGrantControls grantControls, bool includeComments)
        {
            var block = new StringBuilder();

            if (!string.IsNullOrEmpty(grantControls.Operator))
            {
                block.AppendLine($"    operator          = \"{grantControls.Operator.ToUpper()}\"");
            }

            if (grantControls.BuiltInControls?.Any() == true)
            {
                block.AppendLine($"    built_in_controls = {FormatStringArray(grantControls.BuiltInControls)}");
            }

            if (grantControls.CustomAuthenticationFactors?.Any() == true)
            {
                block.AppendLine($"    custom_authentication_factors = {FormatStringArray(grantControls.CustomAuthenticationFactors)}");
            }

            if (grantControls.TermsOfUse?.Any() == true)
            {
                block.AppendLine($"    terms_of_use = {FormatStringArray(grantControls.TermsOfUse)}");
            }

            return block.ToString();
        }

        private string GenerateSessionControlsBlock(JsonSessionControls sessionControls, bool includeComments)
        {
            var block = new StringBuilder();

            if (sessionControls.ApplicationEnforcedRestrictions?.IsEnabled == true)
            {
                block.AppendLine("    application_enforced_restrictions_enabled = true");
            }

            if (sessionControls.CloudAppSecurity != null && sessionControls.CloudAppSecurity.IsEnabled)
            {
                block.AppendLine("    cloud_app_security {");
                block.AppendLine("      is_enabled = true");
                if (!string.IsNullOrEmpty(sessionControls.CloudAppSecurity.CloudAppSecurityType))
                {
                    block.AppendLine($"      cloud_app_security_type = \"{sessionControls.CloudAppSecurity.CloudAppSecurityType}\"");
                }
                block.AppendLine("    }");
            }

            if (sessionControls.PersistentBrowser != null && sessionControls.PersistentBrowser.IsEnabled)
            {
                block.AppendLine("    persistent_browser {");
                block.AppendLine("      is_enabled = true");
                if (!string.IsNullOrEmpty(sessionControls.PersistentBrowser.Mode))
                {
                    block.AppendLine($"      mode = \"{sessionControls.PersistentBrowser.Mode}\"");
                }
                block.AppendLine("    }");
            }

            if (sessionControls.SignInFrequency != null && sessionControls.SignInFrequency.IsEnabled)
            {
                block.AppendLine("    sign_in_frequency {");
                block.AppendLine("      is_enabled = true");
                if (!string.IsNullOrEmpty(sessionControls.SignInFrequency.Type))
                {
                    block.AppendLine($"      type = \"{sessionControls.SignInFrequency.Type}\"");
                }
                if (sessionControls.SignInFrequency.Value.HasValue)
                {
                    block.AppendLine($"      value = {sessionControls.SignInFrequency.Value}");
                }
                if (!string.IsNullOrEmpty(sessionControls.SignInFrequency.AuthenticationType))
                {
                    block.AppendLine($"      authentication_type = \"{sessionControls.SignInFrequency.AuthenticationType}\"");
                }
                block.AppendLine("    }");
            }

            return block.ToString();
        }

        private async Task GenerateModuleStructureAsync(string outputDir, JsonToTerraformResult result)
        {
            var moduleDir = Path.Combine(outputDir, "modules", "conditional-access-policy");
            Directory.CreateDirectory(moduleDir);

            // Generate module main.tf
            var moduleMainContent = @"# Conditional Access Policy Module
variable ""display_name"" {
  description = ""Display name for the conditional access policy""
  type        = string
}

variable ""state"" {
  description = ""State of the conditional access policy""
  type        = string
  default     = ""enabled""
}

variable ""conditions"" {
  description = ""Conditions for the conditional access policy""
  type        = any
  default     = {}
}

variable ""grant_controls"" {
  description = ""Grant controls for the conditional access policy""
  type        = any
  default     = {}
}

variable ""session_controls"" {
  description = ""Session controls for the conditional access policy""
  type        = any
  default     = {}
}

resource ""azuread_conditional_access_policy"" ""this"" {
  display_name = var.display_name
  state        = var.state

  dynamic ""conditions"" {
    for_each = var.conditions != {} ? [var.conditions] : []
    content {
      # Implementation would go here
    }
  }

  dynamic ""grant_controls"" {
    for_each = var.grant_controls != {} ? [var.grant_controls] : []
    content {
      # Implementation would go here
    }
  }

  dynamic ""session_controls"" {
    for_each = var.session_controls != {} ? [var.session_controls] : []
    content {
      # Implementation would go here
    }
  }
}

output ""id"" {
  description = ""The ID of the conditional access policy""
  value       = azuread_conditional_access_policy.this.id
}

output ""display_name"" {
  description = ""The display name of the conditional access policy""
  value       = azuread_conditional_access_policy.this.display_name
}
";

            var moduleMainPath = Path.Combine(moduleDir, "main.tf");
            await File.WriteAllTextAsync(moduleMainPath, moduleMainContent);
            result.GeneratedFiles.Add(moduleMainPath);
            _conversionLog.Add("Generated module structure");
        }

        #region Helper Methods

        private void CollectUniqueValues(JsonConditionalAccessPolicy policy, HashSet<string> groups, HashSet<string> users, HashSet<string> applications, HashSet<string> locations)
        {
            var conditions = policy.Conditions;
            if (conditions == null) return;

            // Collect groups
            conditions.Users?.IncludeGroups?.Where(g => !string.IsNullOrEmpty(g) && g != "All").ToList().ForEach(g => groups.Add(g));
            conditions.Users?.ExcludeGroups?.Where(g => !string.IsNullOrEmpty(g) && g != "All").ToList().ForEach(g => groups.Add(g));

            // Collect users
            conditions.Users?.IncludeUsers?.Where(u => !string.IsNullOrEmpty(u) && u != "All").ToList().ForEach(u => users.Add(u));
            conditions.Users?.ExcludeUsers?.Where(u => !string.IsNullOrEmpty(u) && u != "All").ToList().ForEach(u => users.Add(u));

            // Collect applications
            conditions.Applications?.IncludeApplications?.Where(a => !string.IsNullOrEmpty(a) && a != "All").ToList().ForEach(a => applications.Add(a));
            conditions.Applications?.ExcludeApplications?.Where(a => !string.IsNullOrEmpty(a) && a != "All").ToList().ForEach(a => applications.Add(a));

            // Collect locations
            conditions.Locations?.IncludeLocations?.Where(l => !string.IsNullOrEmpty(l)).ToList().ForEach(l => locations.Add(l));
            conditions.Locations?.ExcludeLocations?.Where(l => !string.IsNullOrEmpty(l)).ToList().ForEach(l => locations.Add(l));
        }

        private string SanitizeResourceName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "unnamed_policy";

            // Replace spaces and special characters with underscores
            var sanitized = Regex.Replace(name.ToLower(), @"[^a-z0-9_]", "_");
            
            // Remove consecutive underscores
            sanitized = Regex.Replace(sanitized, @"_+", "_");
            
            // Remove leading/trailing underscores
            sanitized = sanitized.Trim('_');
            
            // Ensure it starts with a letter
            if (char.IsDigit(sanitized[0]))
                sanitized = "policy_" + sanitized;

            return sanitized;
        }

        private string SanitizeVariableName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "unknown";

            var sanitized = Regex.Replace(name.ToLower(), @"[^a-z0-9_]", "_");
            sanitized = Regex.Replace(sanitized, @"_+", "_");
            sanitized = sanitized.Trim('_');

            if (char.IsDigit(sanitized[0]))
                sanitized = "item_" + sanitized;

            return sanitized;
        }

        private string EscapeString(string value)
        {
            return value?.Replace("\"", "\\\"") ?? "";
        }

        private string FormatStringArray(List<string> values)
        {
            if (values == null || !values.Any())
                return "[]";

            var formattedValues = values.Select(v => $"\"{EscapeString(v)}\"");
            return $"[{string.Join(", ", formattedValues)}]";
        }

        private bool HasApplicationConditions(JsonApplications apps)
        {
            return apps.IncludeApplications?.Any() == true ||
                   apps.ExcludeApplications?.Any() == true ||
                   apps.IncludeUserActions?.Any() == true;
        }

        private bool HasUserConditions(JsonUsers users)
        {
            return users.IncludeUsers?.Any() == true ||
                   users.ExcludeUsers?.Any() == true ||
                   users.IncludeGroups?.Any() == true ||
                   users.ExcludeGroups?.Any() == true ||
                   users.IncludeRoles?.Any() == true ||
                   users.ExcludeRoles?.Any() == true;
        }

        private bool HasPlatformConditions(JsonPlatforms platforms)
        {
            return platforms.IncludePlatforms?.Any() == true ||
                   platforms.ExcludePlatforms?.Any() == true;
        }

        private bool HasLocationConditions(JsonLocations locations)
        {
            return locations.IncludeLocations?.Any() == true ||
                   locations.ExcludeLocations?.Any() == true;
        }

        private bool HasSessionControls(JsonSessionControls sessionControls)
        {
            return sessionControls.ApplicationEnforcedRestrictions?.IsEnabled == true ||
                   sessionControls.CloudAppSecurity?.IsEnabled == true ||
                   sessionControls.PersistentBrowser?.IsEnabled == true ||
                   sessionControls.SignInFrequency?.IsEnabled == true;
        }

        #endregion
    }
}
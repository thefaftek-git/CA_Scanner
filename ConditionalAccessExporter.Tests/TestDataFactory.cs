using Newtonsoft.Json.Linq;
using ConditionalAccessExporter.Models;
using System;
using System.Collections.Generic;

namespace ConditionalAccessExporter.Tests
{
    /// <summary>
    /// Factory class for creating consistent test data across all tests
    /// </summary>
    public static class TestDataFactory
    {
        /// <summary>
        /// Creates a basic JSON policy with standard properties
        /// </summary>
        public static JObject CreateBasicJsonPolicy(string id = "test-policy-id", string displayName = "Test Policy", string state = "enabled")
        {
            return JObject.FromObject(new
            {
                Id = id,
                DisplayName = displayName,
                State = state,
                CreatedDateTime = "2024-01-01T12:00:00Z",
                ModifiedDateTime = "2024-05-01T10:30:00Z",
                Conditions = new
                {
                    Applications = new
                    {
                        IncludeApplications = new[] { "All" },
                        ExcludeApplications = new string[0]
                    },
                    Users = new
                    {
                        IncludeUsers = new[] { "All" },
                        ExcludeUsers = new string[0]
                    }
                },
                GrantControls = new
                {
                    Operator = "OR",
                    BuiltInControls = new[] { "mfa" }
                }
            });
        }

        /// <summary>
        /// Creates a JSON policy with complex conditions
        /// </summary>
        public static JObject CreateComplexJsonPolicy(string id, string displayName, string state = "enabled", 
            string[]? includeUsers = null, string[]? excludeUsers = null, 
            string[]? includeApplications = null, string[]? excludeApplications = null)
        {
            return JObject.FromObject(new
            {
                Id = id,
                DisplayName = displayName,
                State = state,
                CreatedDateTime = "2024-01-01T12:00:00Z",
                ModifiedDateTime = "2024-05-01T10:30:00Z",
                Conditions = new
                {
                    Applications = new
                    {
                        IncludeApplications = includeApplications ?? new[] { "All" },
                        ExcludeApplications = excludeApplications ?? new string[0]
                    },
                    Users = new
                    {
                        IncludeUsers = includeUsers ?? new[] { "All" },
                        ExcludeUsers = excludeUsers ?? new string[0]
                    },
                    Locations = new
                    {
                        IncludeLocations = new[] { "All" },
                        ExcludeLocations = new[] { "trusted-location-id" }
                    },
                    Platforms = new
                    {
                        IncludePlatforms = new[] { "windows", "macOS" },
                        ExcludePlatforms = new[] { "iOS" }
                    }
                },
                GrantControls = new
                {
                    Operator = "OR",
                    BuiltInControls = new[] { "mfa", "compliantDevice" }
                },
                SessionControls = new
                {
                    ApplicationEnforcedRestrictions = new
                    {
                        IsEnabled = true
                    },
                    CloudAppSecurity = new
                    {
                        IsEnabled = true,
                        CloudAppSecurityType = "blockDownloads"
                    }
                }
            });
        }

        /// <summary>
        /// Creates a basic Terraform policy string
        /// </summary>
        public static string CreateBasicTerraformPolicy(string resourceName = "test_policy", string displayName = "Test Policy", string state = "enabled")
        {
            return $@"
resource ""azuread_conditional_access_policy"" ""{resourceName}"" {{
  display_name = ""{displayName}""
  state        = ""{state}""
  
  conditions {{
    applications {{
      included_applications = [""All""]
      excluded_applications = []
    }}
    users {{
      included_users = [""All""]
      excluded_users = []
    }}
  }}
  
  grant_controls {{
    operator          = ""OR""
    built_in_controls = [""mfa""]
  }}
}}";
        }

        /// <summary>
        /// Creates a Terraform policy with complex conditions
        /// </summary>
        public static string CreateComplexTerraformPolicy(string resourceName, string displayName, string state = "enabled",
            string[]? includeUsers = null, string[]? excludeUsers = null)
        {
            var includedUsers = includeUsers != null ? string.Join("\", \"", includeUsers) : "All";
            var excludedUsers = excludeUsers != null ? string.Join("\", \"", excludeUsers) : "";

            return $@"
resource ""azuread_conditional_access_policy"" ""{resourceName}"" {{
  display_name = ""{displayName}""
  state        = ""{state}""
  
  conditions {{
    applications {{
      included_applications = [""All""]
      excluded_applications = []
    }}
    users {{
      included_users = [""{includedUsers}""]
      excluded_users = [""{excludedUsers}""]
    }}
    locations {{
      included_locations = [""All""]
      excluded_locations = [""trusted-location-id""]
    }}
    platforms {{
      included_platforms = [""windows"", ""macOS""]
      excluded_platforms = [""iOS""]
    }}
  }}
  
  grant_controls {{
    operator          = ""OR""
    built_in_controls = [""mfa"", ""compliantDevice""]
  }}
  
  session_controls {{
    application_enforced_restrictions {{
      enabled = true
    }}
    cloud_app_security {{
      policy                = ""blockDownloads""
      cloud_app_security_type = ""blockDownloads""
    }}
  }}
}}";
        }

        /// <summary>
        /// Creates malformed JSON for error testing
        /// </summary>
        public static string CreateMalformedJson()
        {
            return @"{
                ""Id"": ""test-id"",
                ""DisplayName"": ""Test Policy"",
                ""State"": ""enabled"",
                ""MissingClosingBrace"": true
            ";
        }

        /// <summary>
        /// Creates invalid Terraform syntax for error testing
        /// </summary>
        public static string CreateInvalidTerraform()
        {
            return @"
resource ""azuread_conditional_access_policy"" ""invalid_policy"" {
  display_name = ""Invalid Policy""
  state = enabled  # Missing quotes
  
  conditions {
    applications {
      included_applications = [All]  # Missing quotes
    }
    users {
      included_users = [""All""]
    }
  # Missing closing brace
  
  grant_controls {
    operator = ""OR""
    built_in_controls = [""mfa""]
  }
";
        }

        /// <summary>
        /// Creates a large dataset for performance testing
        /// </summary>
        public static List<JObject> CreateLargePolicyDataset(int count = 1000)
        {
            var policies = new List<JObject>();
            
            for (int i = 0; i < count; i++)
            {
                policies.Add(CreateBasicJsonPolicy(
                    id: $"policy-{i:D4}",
                    displayName: $"Test Policy {i}",
                    state: i % 2 == 0 ? "enabled" : "disabled"
                ));
            }
            
            return policies;
        }

        /// <summary>
        /// Creates test matching options with various configurations
        /// </summary>
        public static CrossFormatMatchingOptions CreateMatchingOptions(
            CrossFormatMatchingStrategy strategy = CrossFormatMatchingStrategy.ByName,
            bool caseSensitive = false,
            bool enableSemanticComparison = true,
            Dictionary<string, string>? customMappings = null)
        {
            return new CrossFormatMatchingOptions
            {
                Strategy = strategy,
                CaseSensitive = caseSensitive,
                EnableSemanticComparison = enableSemanticComparison,
                CustomMappings = customMappings ?? new Dictionary<string, string>()
            };
        }

        /// <summary>
        /// Creates a proper JSON export format for JsonToTerraformService
        /// </summary>
        public static JObject CreateJsonPolicyExport(params JObject[] policies)
        {
            return JObject.FromObject(new
            {
                ExportedAt = DateTime.UtcNow,
                TenantId = "test-tenant-id",
                Source = "Test Data Factory",
                PoliciesCount = policies.Length,
                Policies = policies
            });
        }

        /// <summary>
        /// Creates a JSON export with a single basic policy
        /// </summary>
        public static JObject CreateSinglePolicyExport(string id = "test-policy-id", string displayName = "Test Policy", string state = "enabled")
        {
            var policy = CreateBasicJsonPolicy(id, displayName, state);
            return CreateJsonPolicyExport(policy);
        }

        /// <summary>
        /// Creates a JSON export with multiple policies
        /// </summary>
        public static JObject CreateMultiplePolicyExport(int count = 2)
        {
            var policies = new List<JObject>();
            for (int i = 0; i < count; i++)
            {
                policies.Add(CreateBasicJsonPolicy($"policy-{i}", $"Test Policy {i}", i % 2 == 0 ? "enabled" : "disabled"));
            }
            return CreateJsonPolicyExport(policies.ToArray());
        }
    }
}

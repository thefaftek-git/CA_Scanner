using Xunit;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Tests
{
    public class TerraformConversionServiceTests
    {
        private readonly TerraformConversionService _conversionService;

        public TerraformConversionServiceTests()
        {
            _conversionService = new TerraformConversionService();
        }

        #region Test Case 1.1: Single Fully Populated Policy

        [Fact]
        public async Task ConvertToGraphJsonAsync_SingleFullyPopulatedPolicy_ShouldConvertSuccessfully()
        {
            // Arrange
            var parseResult = CreateFullyPopulatedTerraformParseResult();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(parseResult.SourcePath, result.SourcePath);
            Assert.True(result.ConvertedAt > DateTime.MinValue);
            Assert.Equal(1, result.SuccessfulConversions);
            Assert.Equal(0, result.FailedConversions);
            Assert.Empty(result.Errors);
            Assert.Contains("Successfully converted policy", result.ConversionLog.First());

            // Verify the converted policies structure
            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            Assert.Equal(DateTime.UtcNow.Date, ((DateTime)convertedData.ExportedAt).Date);
            Assert.Equal("Terraform", convertedData.Source.ToString());
            Assert.Equal(parseResult.SourcePath, convertedData.SourcePath.ToString());
            Assert.Equal(1, (int)convertedData.PoliciesCount);
            Assert.NotNull(convertedData.Policies);
            Assert.Single(convertedData.Policies);

            // Verify the policy structure
            var policy = convertedData.Policies[0];
            Assert.Equal("Test MFA Policy", policy.DisplayName.ToString());
            Assert.Equal("enabled", policy.State.ToString());
            Assert.NotNull(policy.Id);
            Assert.NotNull(policy.Conditions);
            Assert.NotNull(policy.GrantControls);
            Assert.NotNull(policy.SessionControls);
            Assert.NotNull(policy.TerraformMetadata);
        }

        #endregion

        #region Test Case 1.2: Multiple Policies

        [Fact]
        public async Task ConvertToGraphJsonAsync_MultiplePolicies_ShouldConvertAll()
        {
            // Arrange
            var parseResult = CreateTerraformParseResultWithMultiplePolicies();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.SuccessfulConversions);
            Assert.Equal(0, result.FailedConversions);
            Assert.Empty(result.Errors);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            Assert.Equal(3, (int)convertedData.PoliciesCount);
            Assert.Equal(3, convertedData.Policies.Count);
        }

        #endregion

        #region Test Case 1.3: Empty Policies List

        [Fact]
        public async Task ConvertToGraphJsonAsync_EmptyPoliciesList_ShouldReturnEmptyList()
        {
            // Arrange
            var parseResult = new TerraformParseResult
            {
                SourcePath = "/test/path",
                Policies = new List<TerraformConditionalAccessPolicy>(),
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SuccessfulConversions);
            Assert.Equal(0, result.FailedConversions);
            Assert.Empty(result.Errors);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            Assert.Equal(0, (int)convertedData.PoliciesCount);
            Assert.Empty(convertedData.Policies);
        }

        #endregion

        #region Test Case 1.4: Minimal Required Fields

        [Fact]
        public async Task ConvertToGraphJsonAsync_MinimalRequiredFields_ShouldConvertWithDefaults()
        {
            // Arrange
            var parseResult = new TerraformParseResult
            {
                SourcePath = "/test/minimal",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "minimal_policy",
                        DisplayName = "Minimal Policy",
                        State = "enabled"
                        // No Conditions, GrantControls, or SessionControls
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.SuccessfulConversions);
            Assert.Equal(0, result.FailedConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            Assert.Equal("Minimal Policy", policy.DisplayName.ToString());
            Assert.Equal("enabled", policy.State.ToString());
            Assert.NotNull(policy.Conditions);
            Assert.NotNull(policy.GrantControls);
            Assert.NotNull(policy.SessionControls);
        }

        #endregion

        #region Test Case 1.5: Conditions Mapping Tests

        [Fact]
        public async Task ConvertToGraphJsonAsync_ApplicationsConditions_ShouldMapCorrectly()
        {
            // Arrange
            var parseResult = CreateTerraformParseResultWithApplicationsConditions();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            var applications = policy.Conditions.Applications;

            Assert.Equal(2, applications.IncludeApplications.Count);
            Assert.Contains("All", applications.IncludeApplications);
            Assert.Contains("Office365", applications.IncludeApplications);

            Assert.Single(applications.ExcludeApplications);
            Assert.Contains("excluded-app-id", applications.ExcludeApplications);

            Assert.Single(applications.IncludeUserActions);
            Assert.Contains("urn:user:registersecurityinfo", applications.IncludeUserActions);

            Assert.Single(applications.IncludeAuthenticationContextClassReferences);
            Assert.Contains("c1", applications.IncludeAuthenticationContextClassReferences);
        }

        [Fact]
        public async Task ConvertToGraphJsonAsync_UsersConditions_ShouldMapCorrectly()
        {
            // Arrange
            var parseResult = CreateTerraformParseResultWithUsersConditions();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            var users = policy.Conditions.Users;

            Assert.Single(users.IncludeUsers);
            Assert.Contains("All", users.IncludeUsers);

            Assert.Single(users.ExcludeUsers);
            Assert.Contains("admin-user-id", users.ExcludeUsers);

            Assert.Single(users.IncludeGroups);
            Assert.Contains("employees-group-id", users.IncludeGroups);

            Assert.Single(users.ExcludeGroups);
            Assert.Contains("excluded-group-id", users.ExcludeGroups);

            Assert.Single(users.IncludeRoles);
            Assert.Contains("global-administrator", users.IncludeRoles);

            Assert.Single(users.ExcludeRoles);
            Assert.Contains("excluded-role-id", users.ExcludeRoles);
        }

        [Fact]
        public async Task ConvertToGraphJsonAsync_ClientAppTypes_ShouldMapCorrectly()
        {
            // Arrange
            var parseResult = CreateTerraformParseResultWithClientAppTypes();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            var clientAppTypes = policy.Conditions.ClientAppTypes;

            Assert.Equal(2, clientAppTypes.Count);
            Assert.Contains("browser", clientAppTypes);
            Assert.Contains("mobileAppsAndDesktopClients", clientAppTypes);
        }

        [Fact]
        public async Task ConvertToGraphJsonAsync_PlatformsConditions_ShouldMapCorrectly()
        {
            // Arrange
            var parseResult = CreateTerraformParseResultWithPlatformsConditions();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            var platforms = policy.Conditions.Platforms;

            Assert.Equal(3, platforms.IncludePlatforms.Count);
            Assert.Contains("android", platforms.IncludePlatforms);
            Assert.Contains("iOS", platforms.IncludePlatforms);
            Assert.Contains("windows", platforms.IncludePlatforms);

            Assert.Single(platforms.ExcludePlatforms);
            Assert.Contains("macOS", platforms.ExcludePlatforms);
        }

        [Fact]
        public async Task ConvertToGraphJsonAsync_LocationsConditions_ShouldMapCorrectly()
        {
            // Arrange
            var parseResult = CreateTerraformParseResultWithLocationsConditions();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            var locations = policy.Conditions.Locations;

            Assert.Single(locations.IncludeLocations);
            Assert.Contains("All", locations.IncludeLocations);

            Assert.Single(locations.ExcludeLocations);
            Assert.Contains("AllTrusted", locations.ExcludeLocations);
        }

        [Fact]
        public async Task ConvertToGraphJsonAsync_RiskLevels_ShouldMapCorrectly()
        {
            // Arrange
            var parseResult = CreateTerraformParseResultWithRiskLevels();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];

            Assert.Equal(2, policy.Conditions.SignInRiskLevels.Count);
            Assert.Contains("high", policy.Conditions.SignInRiskLevels);
            Assert.Contains("medium", policy.Conditions.SignInRiskLevels);

            Assert.Single(policy.Conditions.UserRiskLevels);
            Assert.Contains("high", policy.Conditions.UserRiskLevels);
        }

        [Fact]
        public async Task ConvertToGraphJsonAsync_ClientApplicationsConditions_ShouldMapCorrectly()
        {
            // Arrange
            var parseResult = CreateTerraformParseResultWithClientApplicationsConditions();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            var clientApplications = policy.Conditions.ClientApplications;

            Assert.Single(clientApplications.IncludeServicePrincipals);
            Assert.Contains("service-principal-id", clientApplications.IncludeServicePrincipals);

            Assert.Single(clientApplications.ExcludeServicePrincipals);
            Assert.Contains("excluded-sp-id", clientApplications.ExcludeServicePrincipals);
        }

        #endregion

        #region Test Case 1.6: Grant Controls Mapping

        [Fact]
        public async Task ConvertToGraphJsonAsync_GrantControls_ShouldMapCorrectly()
        {
            // Arrange
            var parseResult = CreateTerraformParseResultWithGrantControls();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            var grantControls = policy.GrantControls;

            Assert.Equal("OR", grantControls.Operator.ToString());

            Assert.Equal(3, grantControls.BuiltInControls.Count);
            Assert.Contains("mfa", grantControls.BuiltInControls);
            Assert.Contains("compliantDevice", grantControls.BuiltInControls);
            Assert.Contains("domainJoinedDevice", grantControls.BuiltInControls);

            Assert.Single(grantControls.CustomAuthenticationFactors);
            Assert.Contains("custom-factor-id", grantControls.CustomAuthenticationFactors);

            Assert.Single(grantControls.TermsOfUse);
            Assert.Contains("terms-of-use-id", grantControls.TermsOfUse);

            Assert.NotNull(grantControls.AuthenticationStrength);
            Assert.Equal("auth-strength-id", grantControls.AuthenticationStrength.Id.ToString());
            Assert.Equal("Strong Authentication", grantControls.AuthenticationStrength.DisplayName.ToString());
        }

        #endregion

        #region Test Case 1.7: Session Controls Mapping

        [Fact]
        public async Task ConvertToGraphJsonAsync_SessionControls_ShouldMapCorrectly()
        {
            // Arrange
            var parseResult = CreateTerraformParseResultWithSessionControls();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            var sessionControls = policy.SessionControls;

            Assert.NotNull(sessionControls.ApplicationEnforcedRestrictions);
            Assert.True((bool)sessionControls.ApplicationEnforcedRestrictions.IsEnabled);

            Assert.NotNull(sessionControls.CloudAppSecurity);
            Assert.True((bool)sessionControls.CloudAppSecurity.IsEnabled);
            Assert.Equal("blockDownloads", sessionControls.CloudAppSecurity.CloudAppSecurityType.ToString());

            Assert.NotNull(sessionControls.PersistentBrowser);
            Assert.True((bool)sessionControls.PersistentBrowser.IsEnabled);
            Assert.Equal("always", sessionControls.PersistentBrowser.Mode.ToString());

            Assert.NotNull(sessionControls.SignInFrequency);
            Assert.True((bool)sessionControls.SignInFrequency.IsEnabled);
            Assert.Equal("days", sessionControls.SignInFrequency.Type.ToString());
            Assert.Equal(7, (int)sessionControls.SignInFrequency.Value);
            Assert.Equal("primaryAndSecondaryAuthentication", sessionControls.SignInFrequency.AuthenticationType.ToString());
        }

        #endregion

        #region Test Case 1.8: State Mapping

        [Theory]
        [InlineData("enabled", "enabled")]
        [InlineData("disabled", "disabled")]
        [InlineData("enabledForReportingButNotEnforced", "enabledForReportingButNotEnforced")]
        [InlineData(null, "disabled")]
        [InlineData("invalid", "disabled")]
        public async Task ConvertToGraphJsonAsync_DifferentStates_ShouldMapCorrectly(string? terraformState, string expectedState)
        {
            // Arrange
            var parseResult = new TerraformParseResult
            {
                SourcePath = "/test/state",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "state_test_policy",
                        DisplayName = "State Test Policy",
                        State = terraformState
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            Assert.Equal(expectedState, policy.State.ToString());
        }

        #endregion

        #region Test Case 1.9: Null Handling

        [Fact]
        public async Task ConvertToGraphJsonAsync_NullNestedObjects_ShouldHandleGracefully()
        {
            // Arrange
            var parseResult = new TerraformParseResult
            {
                SourcePath = "/test/nulls",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "null_test_policy",
                        DisplayName = "Null Test Policy",
                        State = "enabled",
                        Conditions = null, // Null conditions
                        GrantControls = null, // Null grant controls
                        SessionControls = null // Null session controls
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);
            Assert.Equal(0, result.FailedConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            Assert.NotNull(policy.Conditions);
            Assert.NotNull(policy.GrantControls);
            Assert.NotNull(policy.SessionControls);
        }

        #endregion

        #region Test Case 1.10: Error Handling

        [Fact]
        public async Task ConvertToGraphJsonAsync_PolicyConversionFailure_ShouldContinueWithOthers()
        {
            // Arrange - Create a scenario that might cause conversion issues
            var parseResult = new TerraformParseResult
            {
                SourcePath = "/test/errors",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "valid_policy",
                        DisplayName = "Valid Policy",
                        State = "enabled"
                    },
                    // This won't actually cause an error in the current implementation,
                    // but demonstrates the test pattern
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "another_valid_policy",
                        DisplayName = "Another Valid Policy",
                        State = "disabled"
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.NotNull(result);
            // In this case, both should succeed since we don't have actual error conditions
            Assert.Equal(2, result.SuccessfulConversions);
            Assert.Equal(0, result.FailedConversions);
        }

        #endregion

        #region Test Case 1.11: Variable and Local Resolution

        [Fact]
        public async Task ConvertToGraphJsonAsync_VariableResolution_ShouldResolveCorrectly()
        {
            // Arrange
            var parseResult = new TerraformParseResult
            {
                SourcePath = "/test/variables",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "variable_test_policy",
                        DisplayName = "var.policy_name", // Variable reference
                        State = "var.policy_state", // Variable reference
                        Conditions = new TerraformConditions
                        {
                            Applications = new TerraformApplications
                            {
                                IncludeApplications = new List<string> { "var.app_id" } // Variable reference
                            }
                        }
                    }
                },
                Variables = new List<TerraformVariable>
                {
                    new TerraformVariable
                    {
                        Name = "policy_name",
                        DefaultValue = "Variable Resolved Policy"
                    },
                    new TerraformVariable
                    {
                        Name = "policy_state",
                        DefaultValue = "enabled"
                    },
                    new TerraformVariable
                    {
                        Name = "app_id",
                        DefaultValue = "resolved-app-id"
                    }
                },
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);
            Assert.Contains("Resolved variable 'policy_name' to default value", result.ConversionLog);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            Assert.Equal("Variable Resolved Policy", policy.DisplayName.ToString());
            Assert.Equal("enabled", policy.State.ToString());
            Assert.Contains("resolved-app-id", policy.Conditions.Applications.IncludeApplications);
        }

        [Fact]
        public async Task ConvertToGraphJsonAsync_LocalResolution_ShouldResolveCorrectly()
        {
            // Arrange
            var parseResult = new TerraformParseResult
            {
                SourcePath = "/test/locals",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "local_test_policy",
                        DisplayName = "local.policy_display_name", // Local reference
                        State = "enabled"
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>
                {
                    new TerraformLocal
                    {
                        Name = "policy_display_name",
                        Value = "Local Resolved Policy"
                    }
                },
                DataSources = new List<TerraformDataSource>()
            };

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);
            Assert.Contains("Resolved local 'policy_display_name' to value", result.ConversionLog);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            Assert.Equal("Local Resolved Policy", policy.DisplayName.ToString());
        }

        [Fact]
        public async Task ConvertToGraphJsonAsync_UnresolvedVariables_ShouldGenerateWarnings()
        {
            // Arrange
            var parseResult = new TerraformParseResult
            {
                SourcePath = "/test/unresolved",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "unresolved_test_policy",
                        DisplayName = "var.undefined_variable", // Undefined variable
                        State = "enabled"
                    }
                },
                Variables = new List<TerraformVariable>(), // No variables defined
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);
            Assert.Contains("Variable 'undefined_variable' referenced but no default value found", result.Warnings);
        }

        [Fact]
        public async Task ConvertToGraphJsonAsync_DataSourceReferences_ShouldGenerateWarnings()
        {
            // Arrange
            var parseResult = new TerraformParseResult
            {
                SourcePath = "/test/datasource",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "datasource_test_policy",
                        DisplayName = "data.azuread_group.admins.object_id", // Data source reference
                        State = "enabled"
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);
            Assert.Contains("Data source reference 'data.azuread_group.admins.object_id' cannot be resolved without live data", result.Warnings);
        }

        #endregion

        #region Test Case 1.12: Special Terraform Values

        [Fact]
        public async Task ConvertToGraphJsonAsync_SpecialValues_ShouldMapCorrectly()
        {
            // Arrange
            var parseResult = new TerraformParseResult
            {
                SourcePath = "/test/special",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "special_values_policy",
                        DisplayName = "Special Values Policy",
                        State = "enabled",
                        Conditions = new TerraformConditions
                        {
                            Locations = new TerraformLocations
                            {
                                IncludeLocations = new List<string> { "All" },
                                ExcludeLocations = new List<string> { "AllTrusted" }
                            },
                            Platforms = new TerraformPlatforms
                            {
                                IncludePlatforms = new List<string> { "all" }
                            }
                        }
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.Equal(1, result.SuccessfulConversions);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            var policy = convertedData.Policies[0];
            Assert.Contains("All", policy.Conditions.Locations.IncludeLocations);
            Assert.Contains("AllTrusted", policy.Conditions.Locations.ExcludeLocations);
            Assert.Contains("all", policy.Conditions.Platforms.IncludePlatforms);
        }

        #endregion

        #region General Assertions Tests

        [Fact]
        public async Task ConvertToGraphJsonAsync_GeneralStructure_ShouldBeCorrect()
        {
            // Arrange
            var parseResult = CreateFullyPopulatedTerraformParseResult();

            // Act
            var result = await _conversionService.ConvertToGraphJsonAsync(parseResult);

            // Assert
            Assert.True(result.ConvertedAt > DateTime.MinValue);
            Assert.Equal(parseResult.SourcePath, result.SourcePath);
            Assert.NotNull(result.ConversionLog);
            Assert.NotNull(result.Errors);
            Assert.NotNull(result.Warnings);

            var convertedPoliciesJson = JsonConvert.SerializeObject(result.ConvertedPolicies);
            dynamic convertedData = JsonConvert.DeserializeObject(convertedPoliciesJson)!;

            Assert.True(((DateTime)convertedData.ExportedAt) > DateTime.MinValue);
            Assert.Equal("Terraform", convertedData.Source.ToString());
            Assert.Equal(1, (int)convertedData.PoliciesCount);
            Assert.NotNull(convertedData.Policies);
            Assert.NotNull(convertedData.ConversionSummary);

            var summary = convertedData.ConversionSummary;
            Assert.Equal(1, (int)summary.SuccessfulConversions);
            Assert.Equal(0, (int)summary.FailedConversions);
            Assert.Equal(1, (int)summary.TotalTerraformPolicies);
        }

        #endregion

        #region Helper Methods

        private TerraformParseResult CreateFullyPopulatedTerraformParseResult()
        {
            return new TerraformParseResult
            {
                SourcePath = "/test/terraform/policy.tf",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "test_mfa_policy",
                        DisplayName = "Test MFA Policy",
                        State = "enabled",
                        Conditions = new TerraformConditions
                        {
                            Applications = new TerraformApplications
                            {
                                IncludeApplications = new List<string> { "All" },
                                ExcludeApplications = new List<string> { "excluded-app" }
                            },
                            Users = new TerraformUsers
                            {
                                IncludeUsers = new List<string> { "All" },
                                ExcludeUsers = new List<string> { "admin-user" }
                            },
                            ClientAppTypes = new List<string> { "browser", "mobileAppsAndDesktopClients" },
                            Platforms = new TerraformPlatforms
                            {
                                IncludePlatforms = new List<string> { "android", "iOS" },
                                ExcludePlatforms = new List<string> { "windows" }
                            },
                            Locations = new TerraformLocations
                            {
                                IncludeLocations = new List<string> { "All" },
                                ExcludeLocations = new List<string> { "AllTrusted" }
                            },
                            SignInRiskLevels = new List<string> { "high", "medium" },
                            UserRiskLevels = new List<string> { "high" }
                        },
                        GrantControls = new TerraformGrantControls
                        {
                            Operator = "OR",
                            BuiltInControls = new List<string> { "mfa", "compliantDevice" }
                        },
                        SessionControls = new TerraformSessionControls
                        {
                            SignInFrequency = new TerraformSignInFrequency
                            {
                                IsEnabled = true,
                                Type = "days",
                                Value = 30
                            }
                        }
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };
        }

        private TerraformParseResult CreateTerraformParseResultWithMultiplePolicies()
        {
            return new TerraformParseResult
            {
                SourcePath = "/test/multiple",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "policy_one",
                        DisplayName = "Policy One",
                        State = "enabled"
                    },
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "policy_two",
                        DisplayName = "Policy Two",
                        State = "disabled"
                    },
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "policy_three",
                        DisplayName = "Policy Three",
                        State = "enabledForReportingButNotEnforced"
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };
        }

        private TerraformParseResult CreateTerraformParseResultWithApplicationsConditions()
        {
            return new TerraformParseResult
            {
                SourcePath = "/test/applications",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "applications_test_policy",
                        DisplayName = "Applications Test Policy",
                        State = "enabled",
                        Conditions = new TerraformConditions
                        {
                            Applications = new TerraformApplications
                            {
                                IncludeApplications = new List<string> { "All", "Office365" },
                                ExcludeApplications = new List<string> { "excluded-app-id" },
                                IncludeUserActions = new List<string> { "urn:user:registersecurityinfo" },
                                IncludeAuthenticationContextClassReferences = new List<string> { "c1" }
                            }
                        }
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };
        }

        private TerraformParseResult CreateTerraformParseResultWithUsersConditions()
        {
            return new TerraformParseResult
            {
                SourcePath = "/test/users",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "users_test_policy",
                        DisplayName = "Users Test Policy",
                        State = "enabled",
                        Conditions = new TerraformConditions
                        {
                            Users = new TerraformUsers
                            {
                                IncludeUsers = new List<string> { "All" },
                                ExcludeUsers = new List<string> { "admin-user-id" },
                                IncludeGroups = new List<string> { "employees-group-id" },
                                ExcludeGroups = new List<string> { "excluded-group-id" },
                                IncludeRoles = new List<string> { "global-administrator" },
                                ExcludeRoles = new List<string> { "excluded-role-id" }
                            }
                        }
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };
        }

        private TerraformParseResult CreateTerraformParseResultWithClientAppTypes()
        {
            return new TerraformParseResult
            {
                SourcePath = "/test/clientapptypes",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "clientapptypes_test_policy",
                        DisplayName = "Client App Types Test Policy",
                        State = "enabled",
                        Conditions = new TerraformConditions
                        {
                            ClientAppTypes = new List<string> { "browser", "mobileAppsAndDesktopClients" }
                        }
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };
        }

        private TerraformParseResult CreateTerraformParseResultWithPlatformsConditions()
        {
            return new TerraformParseResult
            {
                SourcePath = "/test/platforms",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "platforms_test_policy",
                        DisplayName = "Platforms Test Policy",
                        State = "enabled",
                        Conditions = new TerraformConditions
                        {
                            Platforms = new TerraformPlatforms
                            {
                                IncludePlatforms = new List<string> { "android", "iOS", "windows" },
                                ExcludePlatforms = new List<string> { "macOS" }
                            }
                        }
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };
        }

        private TerraformParseResult CreateTerraformParseResultWithLocationsConditions()
        {
            return new TerraformParseResult
            {
                SourcePath = "/test/locations",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "locations_test_policy",
                        DisplayName = "Locations Test Policy",
                        State = "enabled",
                        Conditions = new TerraformConditions
                        {
                            Locations = new TerraformLocations
                            {
                                IncludeLocations = new List<string> { "All" },
                                ExcludeLocations = new List<string> { "AllTrusted" }
                            }
                        }
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };
        }

        private TerraformParseResult CreateTerraformParseResultWithRiskLevels()
        {
            return new TerraformParseResult
            {
                SourcePath = "/test/risklevels",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "risklevels_test_policy",
                        DisplayName = "Risk Levels Test Policy",
                        State = "enabled",
                        Conditions = new TerraformConditions
                        {
                            SignInRiskLevels = new List<string> { "high", "medium" },
                            UserRiskLevels = new List<string> { "high" }
                        }
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };
        }

        private TerraformParseResult CreateTerraformParseResultWithClientApplicationsConditions()
        {
            return new TerraformParseResult
            {
                SourcePath = "/test/clientapps",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "clientapps_test_policy",
                        DisplayName = "Client Applications Test Policy",
                        State = "enabled",
                        Conditions = new TerraformConditions
                        {
                            ClientApplications = new TerraformClientApplications
                            {
                                IncludeServicePrincipals = new List<string> { "service-principal-id" },
                                ExcludeServicePrincipals = new List<string> { "excluded-sp-id" }
                            }
                        }
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };
        }

        private TerraformParseResult CreateTerraformParseResultWithGrantControls()
        {
            return new TerraformParseResult
            {
                SourcePath = "/test/grantcontrols",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "grantcontrols_test_policy",
                        DisplayName = "Grant Controls Test Policy",
                        State = "enabled",
                        GrantControls = new TerraformGrantControls
                        {
                            Operator = "OR",
                            BuiltInControls = new List<string> { "mfa", "compliantDevice", "domainJoinedDevice" },
                            CustomAuthenticationFactors = new List<string> { "custom-factor-id" },
                            TermsOfUse = new List<string> { "terms-of-use-id" },
                            AuthenticationStrength = new TerraformAuthenticationStrength
                            {
                                Id = "auth-strength-id",
                                DisplayName = "Strong Authentication"
                            }
                        }
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };
        }

        private TerraformParseResult CreateTerraformParseResultWithSessionControls()
        {
            return new TerraformParseResult
            {
                SourcePath = "/test/sessioncontrols",
                Policies = new List<TerraformConditionalAccessPolicy>
                {
                    new TerraformConditionalAccessPolicy
                    {
                        ResourceName = "sessioncontrols_test_policy",
                        DisplayName = "Session Controls Test Policy",
                        State = "enabled",
                        SessionControls = new TerraformSessionControls
                        {
                            ApplicationEnforcedRestrictions = new TerraformApplicationEnforcedRestrictions
                            {
                                IsEnabled = true
                            },
                            CloudAppSecurity = new TerraformCloudAppSecurity
                            {
                                IsEnabled = true,
                                CloudAppSecurityType = "blockDownloads"
                            },
                            PersistentBrowser = new TerraformPersistentBrowser
                            {
                                IsEnabled = true,
                                Mode = "always"
                            },
                            SignInFrequency = new TerraformSignInFrequency
                            {
                                IsEnabled = true,
                                Type = "days",
                                Value = 7,
                                AuthenticationType = "primaryAndSecondaryAuthentication"
                            }
                        }
                    }
                },
                Variables = new List<TerraformVariable>(),
                Locals = new List<TerraformLocal>(),
                DataSources = new List<TerraformDataSource>()
            };
        }

        #endregion
    }
}
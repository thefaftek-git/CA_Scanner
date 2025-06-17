













using ConditionalAccessExporter.Services.ValidationRules;
using ConditionalAccessExporter.Services.ValidationRules.SecurityRules;
using ConditionalAccessExporter.Services.ValidationRules.GovernanceRules;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ConditionalAccessExporter.Tests
{
    [Collection("ConsoleOutputTestCollection")]
    public class ValidationRulesTests
    {
        [Fact]
        public async Task MfaRequirementRule_PolicyWithoutMfa_FailsValidation()
        {
            // Arrange
            var rule = new MfaRequirementRule();
            var policy = JObject.Parse(@"{
                'displayName': 'Test Policy',
                'state': 'enabled',
                'conditions': {
                    'users': { 'includeUsers': ['All'] },
                    'applications': { 'includeApplications': ['All'] }
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['compliantDevice']
                }
            }");
            var context = new ValidationContext();

            // Act
            var result = await rule.ValidateAsync(policy, context);

            // Assert
            Assert.False(result.Passed);
            Assert.NotEmpty(result.Issues);
            Assert.Contains(result.Issues, i => i.Message.Contains("MFA"));
        }

        [Fact]
        public async Task MfaRequirementRule_PolicyWithMfa_PassesValidation()
        {
            // Arrange
            var rule = new MfaRequirementRule();
            var policy = JObject.Parse(@"{
                'displayName': 'Test Policy',
                'state': 'enabled',
                'conditions': {
                    'users': { 'includeUsers': ['All'] },
                    'applications': { 'includeApplications': ['All'] }
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['mfa']
                }
            }");
            var context = new ValidationContext();

            // Act
            var result = await rule.ValidateAsync(policy, context);

            // Assert
            Assert.True(result.Passed);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public async Task MfaRequirementRule_DisabledPolicy_SkipsValidation()
        {
            // Arrange
            var rule = new MfaRequirementRule();
            var policy = JObject.Parse(@"{
                'displayName': 'Test Policy',
                'state': 'disabled',
                'conditions': {
                    'users': { 'includeUsers': ['All'] },
                    'applications': { 'includeApplications': ['All'] }
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['compliantDevice']
                }
            }");
            var context = new ValidationContext();

            // Act
            var result = await rule.ValidateAsync(policy, context);

            // Assert
            Assert.True(result.Passed);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public async Task DeviceComplianceRule_PolicyWithoutDeviceControls_GeneratesWarning()
        {
            // Arrange
            var rule = new DeviceComplianceRule();
            var policy = JObject.Parse(@"{
                'displayName': 'Test Policy',
                'state': 'enabled',
                'conditions': {
                    'users': { 'includeRoles': ['62e90394-69f5-4237-9190-012177145e10'] },
                    'applications': { 'includeApplications': ['All'] }
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['mfa']
                }
            }");
            var context = new ValidationContext();

            // Act
            var result = await rule.ValidateAsync(policy, context);

            // Assert
            Assert.False(result.Passed);
            Assert.NotEmpty(result.Issues);
            Assert.Contains(result.Issues, i => i.Message.Contains("device compliance"));
        }

        [Fact]
        public async Task DeviceComplianceRule_PolicyWithDeviceCompliance_PassesValidation()
        {
            // Arrange
            var rule = new DeviceComplianceRule();
            var policy = JObject.Parse(@"{
                'displayName': 'Test Policy',
                'state': 'enabled',
                'conditions': {
                    'users': { 'includeRoles': ['62e90394-69f5-4237-9190-012177145e10'] },
                    'applications': { 'includeApplications': ['All'] }
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['compliantDevice']
                }
            }");
            var context = new ValidationContext();

            // Act
            var result = await rule.ValidateAsync(policy, context);

            // Assert
            Assert.True(result.Passed);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public async Task LegacyAuthenticationRule_PolicyBlockingLegacyAuth_PassesValidation()
        {
            // Arrange
            var rule = new LegacyAuthenticationRule();
            var policy = JObject.Parse(@"{
                'displayName': 'Block Legacy Auth',
                'state': 'enabled',
                'conditions': {
                    'users': { 'includeUsers': ['All'] },
                    'applications': { 'includeApplications': ['All'] },
                    'clientAppTypes': ['exchangeActiveSync', 'other']
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['block']
                }
            }");
            var context = new ValidationContext();

            // Act
            var result = await rule.ValidateAsync(policy, context);

            // Assert
            Assert.True(result.Passed);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public async Task LegacyAuthenticationRule_PolicyTargetingLegacyAuthWithoutBlock_FailsValidation()
        {
            // Arrange
            var rule = new LegacyAuthenticationRule();
            var policy = JObject.Parse(@"{
                'displayName': 'Legacy Auth Policy',
                'state': 'enabled',
                'conditions': {
                    'users': { 'includeUsers': ['All'] },
                    'applications': { 'includeApplications': ['All'] },
                    'clientAppTypes': ['exchangeActiveSync', 'other']
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['mfa']
                }
            }");
            var context = new ValidationContext();

            // Act
            var result = await rule.ValidateAsync(policy, context);

            // Assert
            Assert.False(result.Passed);
            Assert.NotEmpty(result.Issues);
            Assert.Contains(result.Issues, i => i.Message.Contains("block access"));
        }

        [Fact]
        public async Task PolicyNamingConventionRule_ValidName_PassesValidation()
        {
            // Arrange
            var rule = new PolicyNamingConventionRule();
            var policy = JObject.Parse(@"{
                'displayName': 'CA-001-Require MFA for All Users',
                'state': 'enabled',
                'conditions': {
                    'users': { 'includeUsers': ['All'] },
                    'applications': { 'includeApplications': ['All'] }
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['mfa']
                }
            }");
            var context = new ValidationContext();

            // Act
            var result = await rule.ValidateAsync(policy, context);

            // Assert
            Assert.True(result.Passed);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public async Task PolicyNamingConventionRule_InvalidName_FailsValidation()
        {
            // Arrange
            var rule = new PolicyNamingConventionRule();
            var policy = JObject.Parse(@"{
                'displayName': 'some random policy name',
                'state': 'enabled',
                'conditions': {
                    'users': { 'includeUsers': ['All'] },
                    'applications': { 'includeApplications': ['All'] }
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['mfa']
                }
            }");
            var context = new ValidationContext();

            // Act
            var result = await rule.ValidateAsync(policy, context);

            // Assert
            Assert.False(result.Passed);
            Assert.NotEmpty(result.Issues);
            Assert.Contains(result.Issues, i => i.Message.Contains("naming convention"));
        }

        [Fact]
        public async Task PolicyNamingConventionRule_CustomPattern_RespectsConfiguration()
        {
            // Arrange
            var rule = new PolicyNamingConventionRule();
            var policy = JObject.Parse(@"{
                'displayName': 'CUSTOM-001-Test Policy',
                'state': 'enabled',
                'conditions': {
                    'users': { 'includeUsers': ['All'] },
                    'applications': { 'includeApplications': ['All'] }
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['mfa']
                }
            }");
            var context = new ValidationContext
            {
                Configuration = { ["naming.patterns"] = new[] { @"^CUSTOM-\d{3}-.*" } }
            };

            // Act
            var result = await rule.ValidateAsync(policy, context);

            // Assert
            Assert.True(result.Passed);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public void ValidationRuleRegistry_RegistersBuiltInRules()
        {
            // Arrange & Act
            var registry = new ValidationRuleRegistry();
            var allRules = registry.GetAllRules().ToList();

            // Assert
            Assert.NotEmpty(allRules);
            Assert.Contains(allRules, r => r.RuleId == "SEC001"); // MFA rule
            Assert.Contains(allRules, r => r.RuleId == "SEC002"); // Device compliance rule
            Assert.Contains(allRules, r => r.RuleId == "SEC003"); // Legacy auth rule
            Assert.Contains(allRules, r => r.RuleId == "GOV001"); // Naming convention rule
        }

        [Fact]
        public void ValidationRuleRegistry_GetRulesByCategory_FiltersCorrectly()
        {
            // Arrange
            var registry = new ValidationRuleRegistry();

            // Act
            var securityRules = registry.GetRulesByCategory(ValidationRuleCategory.Security).ToList();
            var governanceRules = registry.GetRulesByCategory(ValidationRuleCategory.Governance).ToList();

            // Assert
            Assert.NotEmpty(securityRules);
            Assert.NotEmpty(governanceRules);
            Assert.All(securityRules, r => Assert.Equal(ValidationRuleCategory.Security, r.Category));
            Assert.All(governanceRules, r => Assert.Equal(ValidationRuleCategory.Governance, r.Category));
        }

        [Fact]
        public void ValidationRuleRegistry_GetEnabledRules_RespectsDisabledList()
        {
            // Arrange
            var registry = new ValidationRuleRegistry();
            var options = new ValidationOptions
            {
                DisabledRules = { "SEC001" }
            };

            // Act
            var enabledRules = registry.GetEnabledRules(options).ToList();

            // Assert
            Assert.DoesNotContain(enabledRules, r => r.RuleId == "SEC001");
            Assert.Contains(enabledRules, r => r.RuleId == "SEC002");
        }

        [Fact]
        public void ValidationRuleRegistry_GetStatistics_ReturnsValidData()
        {
            // Arrange
            var registry = new ValidationRuleRegistry();

            // Act
            var stats = registry.GetStatistics();

            // Assert
            Assert.True(stats.TotalRules > 0);
            Assert.True(stats.EnabledByDefault > 0);
            Assert.NotEmpty(stats.RulesByCategory);
            Assert.Contains("Security", stats.RulesByCategory.Keys);
            Assert.Contains("Governance", stats.RulesByCategory.Keys);
        }
    }
}














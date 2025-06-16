











using ConditionalAccessExporter.Models;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Services.ValidationRules;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ConditionalAccessExporter.Tests
{
    [Collection("ConsoleOutputTestCollection")]
    public class PolicyValidationEngineTests : IDisposable
    {
        private readonly string _testDirectory;
        private bool _disposed = false;

        public PolicyValidationEngineTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"PolicyValidationEngineTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDirectory);
        }

        [Fact]
        public async Task ValidateDirectoryAsync_WithValidPolicies_ReturnsSuccessfulReport()
        {
            // Arrange
            var engine = new PolicyValidationEngine();
            var policy = CreateValidTestPolicy();
            var policyPath = Path.Combine(_testDirectory, "valid-policy.json");
            await File.WriteAllTextAsync(policyPath, policy.ToString());

            // Act
            var result = await engine.ValidateDirectoryAsync(_testDirectory);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalPolicies);
            Assert.Equal(1, result.ValidPolicies);
            Assert.Equal(0, result.InvalidPolicies);
            Assert.True(result.SecurityPostureScore > 0);
            Assert.True(result.OverallComplianceScore > 0);
        }

        [Fact]
        public async Task ValidateDirectoryAsync_WithInvalidPolicies_ReturnsFailedReport()
        {
            // Arrange
            var engine = new PolicyValidationEngine();
            var invalidPolicy = CreateInvalidTestPolicy();
            var policyPath = Path.Combine(_testDirectory, "invalid-policy.json");
            await File.WriteAllTextAsync(policyPath, invalidPolicy.ToString());

            // Act
            var result = await engine.ValidateDirectoryAsync(_testDirectory);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalPolicies);
            Assert.Equal(0, result.ValidPolicies);
            Assert.Equal(1, result.InvalidPolicies);
            Assert.True(result.ErrorsByType.Count > 0);
        }

        [Fact]
        public async Task ValidateDirectoryAsync_WithCustomOptions_RespectsConfiguration()
        {
            // Arrange
            var engine = new PolicyValidationEngine();
            var policy = CreateTestPolicyRequiringMfa();
            var policyPath = Path.Combine(_testDirectory, "mfa-policy.json");
            await File.WriteAllTextAsync(policyPath, policy.ToString());

            var options = new ValidationOptions
            {
                StrictMode = true,
                IncludeRecommendations = true,
                DisabledRules = { "SEC001" } // Disable MFA requirement rule
            };

            // Act
            var result = await engine.ValidateDirectoryAsync(_testDirectory, options);

            // Assert
            Assert.NotNull(result);
            // Should pass since MFA rule is disabled
            Assert.Equal(1, result.ValidPolicies);
        }

        [Fact]
        public async Task ValidateDirectoryAsync_NonExistentDirectory_ReturnsErrorReport()
        {
            // Arrange
            var engine = new PolicyValidationEngine();
            var nonExistentDir = Path.Combine(_testDirectory, "does-not-exist");

            // Act
            var result = await engine.ValidateDirectoryAsync(nonExistentDir);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalPolicies);
            Assert.True(result.SecurityAssessment.IdentifiedRisks.Any(r => r.Id == "DIR001"));
        }

        [Fact]
        public async Task ValidateDirectoryAsync_GeneratesSecurityAssessment()
        {
            // Arrange
            var engine = new PolicyValidationEngine();
            var policy = CreatePolicyWithSecurityIssues();
            var policyPath = Path.Combine(_testDirectory, "security-issues.json");
            await File.WriteAllTextAsync(policyPath, policy.ToString());

            // Act
            var result = await engine.ValidateDirectoryAsync(_testDirectory);

            // Assert
            Assert.NotNull(result.SecurityAssessment);
            Assert.True(result.SecurityAssessment.OverallScore >= 0);
            Assert.True(result.SecurityAssessment.IdentifiedRisks.Any());
        }

        [Fact]
        public async Task ValidateDirectoryAsync_GeneratesComplianceAssessment()
        {
            // Arrange
            var engine = new PolicyValidationEngine();
            var policy = CreateValidTestPolicy();
            var policyPath = Path.Combine(_testDirectory, "compliance-test.json");
            await File.WriteAllTextAsync(policyPath, policy.ToString());

            // Act
            var result = await engine.ValidateDirectoryAsync(_testDirectory);

            // Assert
            Assert.NotNull(result.ComplianceAssessment);
            Assert.True(result.ComplianceAssessment.OverallScore >= 0);
            Assert.True(result.ComplianceAssessment.FrameworkScores.Count > 0);
            Assert.Contains("NIST", result.ComplianceAssessment.FrameworkScores.Keys);
            Assert.Contains("ISO27001", result.ComplianceAssessment.FrameworkScores.Keys);
        }

        [Fact]
        public async Task ValidateDirectoryAsync_WithRecommendations_GeneratesActionableAdvice()
        {
            // Arrange
            var engine = new PolicyValidationEngine();
            var policy = CreatePolicyNeedingImprovement();
            var policyPath = Path.Combine(_testDirectory, "needs-improvement.json");
            await File.WriteAllTextAsync(policyPath, policy.ToString());

            var options = new ValidationOptions
            {
                IncludeRecommendations = true
            };

            // Act
            var result = await engine.ValidateDirectoryAsync(_testDirectory, options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Recommendations.Any());
            
            // Check that recommendations have required fields
            foreach (var recommendation in result.Recommendations)
            {
                Assert.NotEmpty(recommendation.Title);
                Assert.NotEmpty(recommendation.Description);
                Assert.NotEmpty(recommendation.Category);
                Assert.True(recommendation.ImpactScore >= 0);
            }
        }

        private JObject CreateValidTestPolicy()
        {
            return JObject.Parse(@"{
                'id': '12345678-1234-1234-1234-123456789012',
                'displayName': 'Test Policy - Valid Configuration',
                'state': 'enabled',
                'conditions': {
                    'users': {
                        'includeUsers': ['All']
                    },
                    'applications': {
                        'includeApplications': ['All']
                    }
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['mfa']
                }
            }");
        }

        private JObject CreateInvalidTestPolicy()
        {
            return JObject.Parse(@"{
                'id': '12345678-1234-1234-1234-123456789012',
                'displayName': 'Invalid Policy',
                'state': 'enabled'
                // Missing required conditions and grantControls
            }");
        }

        private JObject CreateTestPolicyRequiringMfa()
        {
            return JObject.Parse(@"{
                'id': '12345678-1234-1234-1234-123456789012',
                'displayName': 'High Risk Policy Without MFA',
                'state': 'enabled',
                'conditions': {
                    'users': {
                        'includeUsers': ['All']
                    },
                    'applications': {
                        'includeApplications': ['All']
                    }
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['compliantDevice']
                }
            }");
        }

        private JObject CreatePolicyWithSecurityIssues()
        {
            return JObject.Parse(@"{
                'id': '12345678-1234-1234-1234-123456789012',
                'displayName': 'Policy with Security Issues',
                'state': 'enabled',
                'conditions': {
                    'users': {
                        'includeUsers': ['All']
                    },
                    'applications': {
                        'includeApplications': ['All']
                    },
                    'clientAppTypes': ['other']
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['requireMultifactorAuthentication']
                }
            }");
        }

        private JObject CreatePolicyNeedingImprovement()
        {
            return JObject.Parse(@"{
                'id': '12345678-1234-1234-1234-123456789012',
                'displayName': 'Policy',
                'state': 'enabled',
                'conditions': {
                    'users': {
                        'includeUsers': ['All']
                    },
                    'applications': {
                        'includeApplications': ['All']
                    }
                },
                'grantControls': {
                    'operator': 'OR',
                    'builtInControls': ['mfa']
                }
            }");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        if (Directory.Exists(_testDirectory))
                        {
                            Directory.Delete(_testDirectory, true);
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
                _disposed = true;
            }
        }
    }
}












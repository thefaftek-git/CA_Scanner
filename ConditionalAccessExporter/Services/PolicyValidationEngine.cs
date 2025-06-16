







using ConditionalAccessExporter.Models;
using ConditionalAccessExporter.Services.ValidationRules;
using ConditionalAccessExporter.Utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Services
{
    /// <summary>
    /// Advanced policy validation engine with comprehensive rule system
    /// </summary>
    public class PolicyValidationEngine
    {
        private readonly ValidationRuleRegistry _ruleRegistry;
        private readonly ILoggingService? _loggingService;

        public PolicyValidationEngine(ILoggingService? loggingService = null)
        {
            _ruleRegistry = new ValidationRuleRegistry();
            _loggingService = loggingService;
        }

        /// <summary>
        /// Validates a directory of policy files and generates a comprehensive report
        /// </summary>
        public async Task<PolicyValidationReport> ValidateDirectoryAsync(
            string directoryPath,
            ValidationOptions? options = null,
            IProgress<ParallelProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new ValidationOptions();
            var stopwatch = Stopwatch.StartNew();
            
            _loggingService?.LogInformation($"Starting policy validation for directory: {directoryPath}");

            var report = new PolicyValidationReport();
            var context = await CreateValidationContextAsync(directoryPath, options, cancellationToken);

            if (!Directory.Exists(directoryPath))
            {
                report.SecurityAssessment.IdentifiedRisks.Add(new SecurityRisk
                {
                    Id = "DIR001",
                    Title = "Directory Not Found",
                    Description = $"The specified directory does not exist: {directoryPath}",
                    Severity = ValidationSeverity.Critical,
                    Category = "Configuration"
                });
                return report;
            }

            var jsonFiles = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);
            report.TotalPolicies = jsonFiles.Length;

            if (jsonFiles.Length == 0)
            {
                _loggingService?.LogWarning($"No JSON files found in directory: {directoryPath}");
                return report;
            }

            // Load all policies for context
            await LoadPoliciesIntoContext(jsonFiles, context, cancellationToken);

            // Process files in parallel
            var parallelOptions = new ParallelProcessingOptions
            {
                ContinueOnError = true,
                ProgressReportInterval = Math.Max(1, jsonFiles.Length / 20)
            };

            var parallelResult = await ParallelProcessingService.ProcessFilesInParallelAsync(
                jsonFiles,
                async (filePath, ct) => await ValidatePolicyFileAsync(filePath, context, options, ct),
                parallelOptions,
                progress,
                cancellationToken);

            // Aggregate results
            report.PolicyResults.AddRange(parallelResult.Results.Where(r => r != null)!);
            CalculateReportMetrics(report);
            
            // Generate assessments
            report.SecurityAssessment = await GenerateSecurityAssessmentAsync(report, context);
            report.ComplianceAssessment = await GenerateComplianceAssessmentAsync(report, context);

            stopwatch.Stop();
            _loggingService?.LogInformation($"Policy validation completed in {stopwatch.ElapsedMilliseconds}ms");

            return report;
        }

        /// <summary>
        /// Validates a single policy file
        /// </summary>
        public async Task<ValidationResult?> ValidatePolicyFileAsync(
            string filePath,
            ValidationContext context,
            ValidationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new ValidationOptions();
            
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                var policy = JObject.Parse(content);
                
                var result = new ValidationResult
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    PolicyId = policy["id"]?.ToString() ?? "",
                    PolicyName = policy["displayName"]?.ToString() ?? "",
                    IsValid = true
                };

                // Run all enabled validation rules
                var enabledRules = _ruleRegistry.GetEnabledRules(options);
                await RunValidationRulesAsync(policy, context, options, enabledRules, result, cancellationToken);

                // Calculate scores
                result.SecurityScore = CalculateSecurityScore(result);
                result.ComplianceScore = CalculateComplianceScore(result);

                return result;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"Error validating policy file {filePath}: {ex.Message}");
                
                return new ValidationResult
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    IsValid = false,
                    Errors = { new ValidationError
                    {
                        Type = ValidationErrorType.UnexpectedError,
                        Message = $"Failed to validate policy: {ex.Message}",
                        Suggestion = "Check if the file contains valid JSON and policy structure"
                    }}
                };
            }
        }

        /// <summary>
        /// Runs all validation rules against a policy
        /// </summary>
        private async Task RunValidationRulesAsync(
            JObject policy,
            ValidationContext context,
            ValidationOptions options,
            IEnumerable<IValidationRule> rules,
            ValidationResult result,
            CancellationToken cancellationToken)
        {
            foreach (var rule in rules)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var ruleResult = await rule.ValidateAsync(policy, context, cancellationToken);
                    
                    if (!ruleResult.Passed)
                    {
                        result.IsValid = false;
                        
                        // Convert rule issues to validation errors/warnings
                        foreach (var issue in ruleResult.Issues)
                        {
                            if (issue.Severity >= ValidationSeverity.Error)
                            {
                                result.Errors.Add(new ValidationError
                                {
                                    Type = ValidationErrorType.SecurityIssue,
                                    Message = issue.Message,
                                    Field = issue.Field,
                                    Suggestion = issue.Suggestion
                                });
                            }
                            else
                            {
                                result.Warnings.Add(new ValidationWarning
                                {
                                    Type = ValidationWarningType.SecurityRecommendation,
                                    Message = issue.Message,
                                    Field = issue.Field,
                                    Suggestion = issue.Suggestion
                                });
                            }
                        }
                    }
                    
                    // Add recommendations
                    result.Recommendations.AddRange(ruleResult.Recommendations);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError($"Error running rule {rule.RuleId}: {ex.Message}");
                    
                    result.Errors.Add(new ValidationError
                    {
                        Type = ValidationErrorType.UnexpectedError,
                        Message = $"Rule {rule.RuleId} failed: {ex.Message}",
                        Suggestion = "Check rule configuration and policy structure"
                    });
                }
            }
        }

        /// <summary>
        /// Creates validation context for the validation run
        /// </summary>
        private async Task<ValidationContext> CreateValidationContextAsync(
            string directoryPath,
            ValidationOptions options,
            CancellationToken cancellationToken)
        {
            var context = new ValidationContext
            {
                FilePath = directoryPath,
                Options = options
            };

            // Load configuration if available
            var configPath = Path.Combine(directoryPath, "validation-config.json");
            if (File.Exists(configPath))
            {
                try
                {
                    var configContent = await File.ReadAllTextAsync(configPath, cancellationToken);
                    var config = JObject.Parse(configContent);
                    context.Configuration = config.ToObject<Dictionary<string, object>>() ?? new();
                }
                catch (Exception ex)
                {
                    _loggingService?.LogWarning($"Failed to load validation config: {ex.Message}");
                }
            }

            return context;
        }

        /// <summary>
        /// Loads all policies into the validation context
        /// </summary>
        private async Task LoadPoliciesIntoContext(
            string[] jsonFiles,
            ValidationContext context,
            CancellationToken cancellationToken)
        {
            foreach (var file in jsonFiles)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file, cancellationToken);
                    var policy = JObject.Parse(content);
                    context.AllPolicies.Add(policy);
                }
                catch (Exception ex)
                {
                    _loggingService?.LogWarning($"Failed to load policy {file} into context: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Calculates overall report metrics
        /// </summary>
        private void CalculateReportMetrics(PolicyValidationReport report)
        {
            report.ValidPolicies = report.PolicyResults.Count(r => r.IsValid);
            report.InvalidPolicies = report.PolicyResults.Count(r => !r.IsValid);
            report.PoliciesWithWarnings = report.PolicyResults.Count(r => r.Warnings.Any());
            
            // Calculate error and warning distributions
            foreach (var result in report.PolicyResults)
            {
                foreach (var error in result.Errors)
                {
                    var key = error.Type.ToString();
                    report.ErrorsByType[key] = report.ErrorsByType.GetValueOrDefault(key, 0) + 1;
                }
                
                foreach (var warning in result.Warnings)
                {
                    var key = warning.Type.ToString();
                    report.WarningsByType[key] = report.WarningsByType.GetValueOrDefault(key, 0) + 1;
                }
                
                report.Recommendations.AddRange(result.Recommendations);
            }

            // Calculate overall scores
            if (report.PolicyResults.Any())
            {
                report.OverallComplianceScore = report.PolicyResults.Average(r => r.ComplianceScore);
                report.SecurityPostureScore = report.PolicyResults.Average(r => r.SecurityScore);
            }
        }

        /// <summary>
        /// Calculates security score for a policy
        /// </summary>
        private double CalculateSecurityScore(ValidationResult result)
        {
            var baseScore = 100.0;
            
            // Deduct points for security issues
            foreach (var error in result.Errors.Where(e => e.Type == ValidationErrorType.SecurityIssue))
            {
                baseScore -= 25.0; // Major deduction for security errors
            }
            
            foreach (var warning in result.Warnings.Where(w => w.Type == ValidationWarningType.SecurityRecommendation))
            {
                baseScore -= 10.0; // Minor deduction for security warnings
            }
            
            return Math.Max(0, baseScore);
        }

        /// <summary>
        /// Calculates compliance score for a policy
        /// </summary>
        private double CalculateComplianceScore(ValidationResult result)
        {
            var baseScore = 100.0;
            
            // Deduct points for compliance issues
            foreach (var error in result.Errors)
            {
                baseScore -= 15.0;
            }
            
            foreach (var warning in result.Warnings.Where(w => w.Type == ValidationWarningType.BestPracticeViolation))
            {
                baseScore -= 5.0;
            }
            
            return Math.Max(0, baseScore);
        }

        /// <summary>
        /// Generates security assessment
        /// </summary>
        private async Task<SecurityAssessment> GenerateSecurityAssessmentAsync(
            PolicyValidationReport report,
            ValidationContext context)
        {
            await Task.CompletedTask;
            
            var assessment = new SecurityAssessment
            {
                OverallScore = report.SecurityPostureScore
            };

            // Count findings by severity
            foreach (var result in report.PolicyResults)
            {
                switch (result.HighestSeverity)
                {
                    case ValidationSeverity.Critical:
                        assessment.CriticalFindings++;
                        break;
                    case ValidationSeverity.Error:
                        assessment.HighRiskFindings++;
                        break;
                    case ValidationSeverity.Warning:
                        assessment.MediumRiskFindings++;
                        break;
                    case ValidationSeverity.Info:
                        assessment.LowRiskFindings++;
                        break;
                }
            }

            // Identify security risks
            var securityRecommendations = report.Recommendations
                .Where(r => r.Category.Equals("Security", StringComparison.OrdinalIgnoreCase))
                .GroupBy(r => r.Title)
                .Select(g => new SecurityRisk
                {
                    Id = g.First().Id,
                    Title = g.Key,
                    Description = g.First().Description,
                    Severity = g.Max(r => r.Severity),
                    Category = "Security",
                    AffectedPolicies = g.SelectMany(r => new[] { r.Field }).Distinct().ToList(),
                    Mitigation = g.First().RecommendedValue,
                    RiskScore = g.Average(r => r.ImpactScore)
                });

            assessment.IdentifiedRisks.AddRange(securityRecommendations);

            return assessment;
        }

        /// <summary>
        /// Generates compliance assessment
        /// </summary>
        private async Task<ComplianceAssessment> GenerateComplianceAssessmentAsync(
            PolicyValidationReport report,
            ValidationContext context)
        {
            await Task.CompletedTask;
            
            var assessment = new ComplianceAssessment
            {
                OverallScore = report.OverallComplianceScore,
                NextRecommendedAssessment = DateTime.UtcNow.AddMonths(3)
            };

            // Generate framework-specific scores (simplified for demo)
            var frameworks = new[] { "NIST", "ISO27001", "SOC2" };
            foreach (var framework in frameworks)
            {
                assessment.FrameworkScores[framework] = new ComplianceFrameworkScore
                {
                    Framework = framework,
                    Score = assessment.OverallScore + Random.Shared.NextDouble() * 10 - 5, // Simulate variation
                    TotalControls = 50,
                    PassingControls = (int)(50 * assessment.OverallScore / 100),
                    FailingControls = 50 - (int)(50 * assessment.OverallScore / 100)
                };
            }

            return assessment;
        }
    }
}








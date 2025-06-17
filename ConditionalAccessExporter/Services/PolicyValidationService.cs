
using ConditionalAccessExporter.Models;
using ConditionalAccessExporter.Services.ValidationRules;
using ConditionalAccessExporter.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ConditionalAccessExporter.Services
{
    public class PolicyValidationService
    {
        private readonly JSchema _policySchema;
        private readonly PolicyValidationEngine _validationEngine;
        private readonly ILoggingService? _loggingService;

        public PolicyValidationService(ILoggingService? loggingService = null)
        {
            _loggingService = loggingService;
            _validationEngine = new PolicyValidationEngine(loggingService);
            
            // In a real application, load this from a file or embedded resource
            // This is a simplified schema for demonstration
            var schemaJson = @"{
                'type': 'object',
                'properties': {
                    'displayName': {'type': 'string'},
                    'conditions': {
                        'type': 'object',
                        'properties': {
                            'users': {
                                'type': 'object',
                                'properties': {
                                    'includeUsers': {'type': 'array', 'items': {'type': 'string'}},
                                    'excludeUsers': {'type': 'array', 'items': {'type': 'string'}},
                                    'includeGroups': {'type': 'array', 'items': {'type': 'string'}},
                                    'excludeGroups': {'type': 'array', 'items': {'type': 'string'}},
                                    'includeRoles': {'type': 'array', 'items': {'type': 'string'}},
                                    'excludeRoles': {'type': 'array', 'items': {'type': 'string'}}
                                }
                            },
                            'applications': {
                                'type': 'object',
                                'properties': {
                                    'includeApplications': {'type': 'array', 'items': {'type': 'string'}},
                                    'excludeApplications': {'type': 'array', 'items': {'type': 'string'}}
                                }
                            }
                        },
                        'required': ['users', 'applications']
                    },
                    'grantControls': {
                        'type': 'object',
                        'properties': {
                            'operator': {'type': 'string', 'enum': ['OR', 'AND']},
                            'builtInControls': {'type': 'array', 'items': {'type': 'string'}}
                        },
                        'required': ['operator', 'builtInControls']
                    },
                    'state': {'type': 'string', 'enum': ['enabled', 'disabled', 'enabledForReportingButNotEnforced']}
                },
                'required': ['displayName', 'conditions', 'grantControls', 'state']
            }";
            _policySchema = JSchema.Parse(schemaJson);
        }

        public async Task<DirectoryValidationResult> ValidateDirectoryAsync(
            string directoryPath, 
            IProgress<ParallelProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // Honor cancellation before any processing begins
            cancellationToken.ThrowIfCancellationRequested();
            
            var result = new DirectoryValidationResult
            {
                DirectoryPath = directoryPath,
                IsValid = true // Assume valid until an error is found
            };

            // Pre-flight checks
            if (!Directory.Exists(directoryPath))
            {
                result.PreflightErrors.Add($"Directory not found: {directoryPath}");
                result.IsValid = false;
                return result;
            }

            var jsonFiles = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);
            result.TotalFiles = jsonFiles.Length;

            if (jsonFiles.Length == 0)
            {
                Console.WriteLine($"No JSON files found in directory: {directoryPath}. No files were processed.");
                return result;
            }

            // Use parallel processing for file validation
            var parallelOptions = new ParallelProcessingOptions
            {
                ContinueOnError = true, // Continue validation even if some files fail
                ProgressReportInterval = Math.Max(1, jsonFiles.Length / 20) // Report progress every 5%
            };

            Console.WriteLine($"Validating {jsonFiles.Length} policy files using parallel processing...");

            var parallelResult = await ParallelProcessingService.ProcessFilesInParallelAsync(
                jsonFiles,
                async (filePath, ct) => await ValidateFileAsync(filePath, ct),
                parallelOptions,
                progress,
                cancellationToken);

            // Aggregate results
            result.FileResults.AddRange(parallelResult.Results);
            result.ValidFiles = parallelResult.Results.Count(r => r.IsValid);
            result.InvalidFiles = parallelResult.Results.Count(r => !r.IsValid);
            result.IsValid = result.InvalidFiles == 0;

            // Log any processing errors
            foreach (var error in parallelResult.Errors)
            {
                result.PreflightErrors.Add($"Failed to validate file {error.Item}: {error.Exception.Message}");
                result.IsValid = false;
            }

            Console.WriteLine($"Validation completed: {result.ValidFiles} valid, {result.InvalidFiles} invalid files");
            Console.WriteLine($"Processing time: {parallelResult.ElapsedTime.TotalMilliseconds:F0}ms");
            Console.WriteLine($"Average speed: {parallelResult.AverageItemsPerSecond:F1} files/second");

            return result;
        }

        public async Task<ValidationResult> ValidateFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var result = new ValidationResult
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                IsValid = true // Assume valid until an error is found
            };

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                JObject policy;
                try
                {
                    policy = JObject.Parse(content);
                }
                catch (JsonReaderException ex)
                {
                    result.IsValid = false;
                    result.Errors.Add(new Models.ValidationError
                    {
                        Type = ValidationErrorType.JsonSyntaxError,
                        Message = $"Invalid JSON syntax: {ex.Message}",
                        LineNumber = ex.LineNumber,
                        ColumnNumber = ex.LinePosition,
                        Suggestion = "Ensure the JSON is well-formed. Check for missing commas, brackets, or quotes."
                    });
                    return result; // Stop further validation if JSON is invalid
                }

                // Schema Validation
                policy.IsValid(_policySchema, out IList<string> schemaErrors);
                if (schemaErrors.Any())
                {
                    result.IsValid = false;
                    foreach (var error in schemaErrors)
                    {
                        result.Errors.Add(new Models.ValidationError
                        {
                            Type = ValidationErrorType.JsonSchemaViolation,
                            Message = error,
                            Suggestion = "Ensure the policy structure matches the defined schema. Check field names and types."
                        });
                    }
                }

                // Custom Validations (examples)
                ValidateDisplayName(policy, result);
                ValidateGuids(policy, result);
                ValidateState(policy, result);
                // Add more custom validation rules here

                // Best Practice Warnings (examples)
                CheckEmptyIncludeUsers(policy, result);

            }
            catch (IOException ex)
            {
                result.IsValid = false;
                result.Errors.Add(new Models.ValidationError
                {
                    Type = ValidationErrorType.FileAccessError,
                    Message = $"Error reading file: {ex.Message}",
                    Suggestion = "Ensure the file exists and the application has read permissions."
                });
            }
            catch (Exception ex) // Catch-all for unexpected errors during validation
            {
                result.IsValid = false;
                result.Errors.Add(new Models.ValidationError
                {
                    Type = ValidationErrorType.UnexpectedError,
                    Message = $"An unexpected error occurred during validation: {ex.Message}",
                    Suggestion = "Review the policy for any obvious issues or report this error."
                });
            }

            return result;
        }

        private void ValidateDisplayName(JObject policy, ValidationResult result)
        {
            var displayName = policy["displayName"]?.ToString();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                result.IsValid = false;
                result.Errors.Add(new Models.ValidationError
                {
                    Type = ValidationErrorType.RequiredFieldMissing,
                    Field = "displayName",
                    Message = "Display name is missing or empty.",
                    Suggestion = "Provide a descriptive display name for the policy."
                });
            }
        }
        
        private void ValidateState(JObject policy, ValidationResult result)
        {
            var state = policy["state"]?.ToString();
            var validStates = new[] { "enabled", "disabled", "enabledForReportingButNotEnforced" };
            if (state != null && !validStates.Contains(state))
            {
                 result.IsValid = false;
                 result.Errors.Add(new Models.ValidationError
                 {
                     Type = ValidationErrorType.InvalidFieldValue,
                     Field = "state",
                     Message = $"Invalid policy state: '{state}'.",
                     Suggestion = $"State must be one of: {string.Join(", ", validStates)}."
                 });
            }
        }

        private void ValidateGuids(JObject policy, ValidationResult result)
        {
            var guidRegex = new Regex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$");

            // Example: Check includeUsers GUIDs
            var includeUsers = policy.SelectTokens("conditions.users.includeUsers[*]").Select(t => t.ToString());
            foreach (var userId in includeUsers)
            {
                if (userId != "All" && userId != "GuestsOrExternalUsers" && !guidRegex.IsMatch(userId))
                {
                    result.IsValid = false;
                    result.Errors.Add(new Models.ValidationError
                    {
                        Type = ValidationErrorType.InvalidGuid,
                        Field = "conditions.users.includeUsers",
                        Message = $"Invalid GUID format for user ID: {userId}",
                        Suggestion = "Ensure user IDs are valid GUIDs or predefined values like 'All' or 'GuestsOrExternalUsers'."
                    });
                }
            }
            // Add similar checks for other GUID fields (groups, roles, applications)
        }
        
        private void CheckEmptyIncludeUsers(JObject policy, ValidationResult result)
        {
            var includeUsers = policy.SelectTokens("conditions.users.includeUsers[*]").ToList();
            var includeGroups = policy.SelectTokens("conditions.users.includeGroups[*]").ToList();
            var includeRoles = policy.SelectTokens("conditions.users.includeRoles[*]").ToList();

            if (!includeUsers.Any() && !includeGroups.Any() && !includeRoles.Any())
            {
                 result.Warnings.Add(new ValidationWarning
                 {
                     Type = ValidationWarningType.BestPracticeViolation,
                     Field = "conditions.users",
                     Message = "The policy does not include any users, groups, or roles. It may not apply to anyone.",
                     Suggestion = "Ensure the policy targets the intended users, groups, or roles."
                 });
            }
        }

        /// <summary>
        /// Validates policies using the enhanced validation engine with comprehensive rule system
        /// </summary>
        public async Task<PolicyValidationReport> ValidateWithEngineAsync(
            string directoryPath,
            ValidationOptions? options = null,
            IProgress<ParallelProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _loggingService?.LogInformation($"Starting enhanced policy validation for directory: {directoryPath}");
            return await _validationEngine.ValidateDirectoryAsync(directoryPath, options, progress, cancellationToken);
        }

        /// <summary>
        /// Validates a single policy file using the enhanced validation engine
        /// </summary>
        public async Task<ValidationResult?> ValidatePolicyWithEngineAsync(
            string filePath,
            ValidationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new ValidationOptions();
            var context = new ValidationContext
            {
                FilePath = Path.GetDirectoryName(filePath),
                Options = options
            };

            return await _validationEngine.ValidatePolicyFileAsync(filePath, context, options, cancellationToken);
        }

        /// <summary>
        /// Generates a comprehensive validation report with security and compliance assessments
        /// </summary>
        public async Task<PolicyValidationReport> GenerateComprehensiveReportAsync(
            string directoryPath,
            bool includeRecommendations = true,
            bool generateRemediationScripts = false,
            CancellationToken cancellationToken = default)
        {
            var options = new ValidationOptions
            {
                IncludeRecommendations = includeRecommendations,
                GenerateRemediationScripts = generateRemediationScripts,
                StrictMode = false
            };

            return await ValidateWithEngineAsync(directoryPath, options, null, cancellationToken);
        }

        /// <summary>
        /// Validates policies against specific compliance frameworks
        /// </summary>
        public async Task<Dictionary<string, ComplianceFrameworkScore>> ValidateComplianceAsync(
            string directoryPath,
            string[]? frameworks = null,
            CancellationToken cancellationToken = default)
        {
            frameworks ??= new[] { "NIST", "ISO27001", "SOC2" };
            
            var options = new ValidationOptions
            {
                IncludeRecommendations = true,
                StrictMode = true
            };

            // Add framework-specific configuration
            foreach (var framework in frameworks)
            {
                options.Configuration[$"compliance.{framework}"] = true;
            }

            var report = await ValidateWithEngineAsync(directoryPath, options, null, cancellationToken);
            return report.ComplianceAssessment.FrameworkScores;
        }

        /// <summary>
        /// Performs a security audit of policies
        /// </summary>
        public async Task<SecurityAssessment> PerformSecurityAuditAsync(
            string directoryPath,
            bool includeDetailedAnalysis = true,
            CancellationToken cancellationToken = default)
        {
            var options = new ValidationOptions
            {
                StrictMode = true,
                IncludeRecommendations = includeDetailedAnalysis
            };

            // Enable all security rules
            options.Configuration["security.strict"] = true;
            options.Configuration["security.detailed"] = includeDetailedAnalysis;

            var report = await ValidateWithEngineAsync(directoryPath, options, null, cancellationToken);
            return report.SecurityAssessment;
        }

        /// <summary>
        /// Gets available validation rules and their statistics
        /// </summary>
        public ValidationRuleStatistics GetValidationRuleStatistics()
        {
            // Access the rule registry from the validation engine
            var registry = new ValidationRuleRegistry();
            return registry.GetStatistics();
        }

        /// <summary>
        /// Validates policies with custom rule configuration
        /// </summary>
        public async Task<PolicyValidationReport> ValidateWithCustomRulesAsync(
            string directoryPath,
            Dictionary<string, object> customRuleConfig,
            string[]? disabledRules = null,
            CancellationToken cancellationToken = default)
        {
            var options = new ValidationOptions
            {
                Configuration = customRuleConfig,
                DisabledRules = disabledRules?.ToList() ?? new List<string>(),
                IncludeRecommendations = true
            };

            return await ValidateWithEngineAsync(directoryPath, options, null, cancellationToken);
        }
    }
}

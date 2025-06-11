using ConditionalAccessExporter.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace ConditionalAccessExporter.Services
{
    public class RemediationService
    {
        private readonly PolicyComparisonService _comparisonService;
        private readonly ImpactAnalysisService _impactAnalysisService;
        private readonly ScriptGenerationService _scriptGenerationService;
        private readonly RiskAssessmentConfig _riskConfig;

        public RemediationService(
            PolicyComparisonService comparisonService,
            ImpactAnalysisService impactAnalysisService,
            ScriptGenerationService scriptGenerationService,
            RiskAssessmentConfig? riskConfig = null)
        {
            _comparisonService = comparisonService;
            _impactAnalysisService = impactAnalysisService;
            _scriptGenerationService = scriptGenerationService;
            _riskConfig = riskConfig ?? new RiskAssessmentConfig();
        }

        public async Task<RemediationResult> GenerateRemediationPlanAsync(
            ComparisonResult comparisonResult,
            RemediationOptions options)
        {
            Logger.WriteInfo("Generating remediation plan...");

            var remediationResult = new RemediationResult
            {
                TenantId = comparisonResult.TenantId,
                ReferenceDirectory = comparisonResult.ReferenceDirectory
            };

            // Generate backup if requested
            if (options.GenerateBackup)
            {
                remediationResult.BackupInfo = GenerateBackup(options.OutputDirectory);
            }

            // Process each policy comparison to create remediation actions
            foreach (var policyComparison in comparisonResult.PolicyComparisons)
            {
                if (policyComparison.Status == ComparisonStatus.Identical)
                    continue;

                var remediation = await CreatePolicyRemediationAsync(policyComparison, options);
                if (remediation != null)
                {
                    remediationResult.PolicyRemediations.Add(remediation);
                }
            }

            // Calculate summary
            remediationResult.Summary = CalculateRemediationSummary(remediationResult.PolicyRemediations);

            // Generate scripts for each format
            GenerateRemediationScripts(remediationResult, options);

            Logger.WriteInfo($"Generated remediation plan for {remediationResult.PolicyRemediations.Count} policies");
            
            return remediationResult;
        }

        private async Task<PolicyRemediation?> CreatePolicyRemediationAsync(
            PolicyComparison comparison,
            RemediationOptions options)
        {
            var action = DetermineRemediationAction(comparison);
            if (action == RemediationAction.NoAction)
                return null;

            var remediation = new PolicyRemediation
            {
                PolicyId = comparison.PolicyId,
                PolicyName = comparison.PolicyName,
                Action = action,
                CurrentPolicy = comparison.EntraPolicy,
                TargetPolicy = comparison.ReferencePolicy,
                Differences = comparison.Differences
            };

            // Assess risk level
            remediation.RiskLevel = AssessRiskLevel(comparison);

            // Perform impact analysis if enabled
            if (options.IncludeImpactAnalysis)
            {
                remediation.Impact = await _impactAnalysisService.AnalyzeImpactAsync(comparison);
            }

            // Generate remediation steps
            remediation.Steps = GenerateRemediationSteps(remediation);

            // Add warnings and prerequisites
            remediation.Warnings = GenerateWarnings(remediation);
            remediation.Prerequisites = GeneratePrerequisites(remediation);

            return remediation;
        }

        private RemediationAction DetermineRemediationAction(PolicyComparison comparison)
        {
            return comparison.Status switch
            {
                ComparisonStatus.ReferenceOnly => RemediationAction.Create,
                ComparisonStatus.EntraOnly => RemediationAction.Delete,
                ComparisonStatus.Different => RemediationAction.Update,
                ComparisonStatus.Identical => RemediationAction.NoAction,
                _ => RemediationAction.NoAction
            };
        }

        private Models.RiskLevel AssessRiskLevel(PolicyComparison comparison)
        {
            if (comparison.HasCriticalDifferences)
                return Models.RiskLevel.Critical;

            if (comparison.Status == ComparisonStatus.EntraOnly)
                return Models.RiskLevel.High; // Deleting policies is high risk

            if (comparison.Status == ComparisonStatus.ReferenceOnly)
                return Models.RiskLevel.Medium; // Creating new policies is medium risk

            // For updates, analyze the specific differences
            return AssessUpdateRiskLevel(comparison.Differences);
        }

        private Models.RiskLevel AssessUpdateRiskLevel(object? differences)
        {
            if (differences == null)
                return Models.RiskLevel.Low;

            var maxRisk = Models.RiskLevel.Low;

            // Try to process differences directly without JSON serialization
            if (differences is IDictionary<string, object> diffDictionary)
            {
                foreach (var kvp in diffDictionary)
                {
                    var fieldPath = kvp.Key.ToLowerInvariant();
                    
                    // Check if this field has a defined risk level
                    var fieldRisk = _riskConfig.FieldRiskLevels
                        .Where(kvp => fieldPath.Contains(kvp.Key.ToLowerInvariant()))
                        .Select(kvp => kvp.Value)
                        .DefaultIfEmpty(Models.RiskLevel.Low)
                        .Max();

                    if (fieldRisk > maxRisk)
                        maxRisk = fieldRisk;
                }
            }
            else
            {
                // Fallback to JSON serialization for other types
                var diffJson = JsonConvert.SerializeObject(differences);
                var diffObject = JObject.Parse(diffJson);

                foreach (var property in diffObject.Properties())
                {
                    var fieldPath = property.Name.ToLowerInvariant();
                    
                    // Check if this field has a defined risk level
                    var fieldRisk = _riskConfig.FieldRiskLevels
                        .Where(kvp => fieldPath.Contains(kvp.Key.ToLowerInvariant()))
                        .Select(kvp => kvp.Value)
                        .DefaultIfEmpty(Models.RiskLevel.Low)
                        .Max();

                    if (fieldRisk > maxRisk)
                        maxRisk = fieldRisk;
                }
            }

            return maxRisk;
        }

        private List<RemediationStep> GenerateRemediationSteps(PolicyRemediation remediation)
        {
            var steps = new List<RemediationStep>();
            var order = 1;

            // Backup step
            steps.Add(new RemediationStep
            {
                Order = order++,
                Type = RemediationStepType.Backup,
                Description = $"Create backup of current policy: {remediation.PolicyName}",
                Action = "Backup-ConditionalAccessPolicy",
                RequiresElevatedPermissions = true
            });

            // Validation step
            steps.Add(new RemediationStep
            {
                Order = order++,
                Type = RemediationStepType.Validation,
                Description = "Validate target policy configuration",
                Action = "Test-ConditionalAccessPolicy",
                RequiresElevatedPermissions = false
            });

            // Main action step
            var actionDescription = remediation.Action switch
            {
                RemediationAction.Create => $"Create new policy: {remediation.PolicyName}",
                RemediationAction.Update => $"Update existing policy: {remediation.PolicyName}",
                RemediationAction.Delete => $"Delete policy: {remediation.PolicyName}",
                _ => "No action required"
            };

            steps.Add(new RemediationStep
            {
                Order = order++,
                Type = RemediationStepType.Update,
                Description = actionDescription,
                Action = GetActionCommand(remediation.Action),
                RequiresElevatedPermissions = true,
                RequiresUserConfirmation = remediation.RiskLevel >= Models.RiskLevel.High
            });

            // Verification step
            steps.Add(new RemediationStep
            {
                Order = order++,
                Type = RemediationStepType.Verification,
                Description = "Verify policy changes were applied correctly",
                Action = "Verify-ConditionalAccessPolicy",
                RequiresElevatedPermissions = false
            });

            return steps;
        }

        private string GetActionCommand(RemediationAction action)
        {
            return action switch
            {
                RemediationAction.Create => "New-ConditionalAccessPolicy",
                RemediationAction.Update => "Set-ConditionalAccessPolicy",
                RemediationAction.Delete => "Remove-ConditionalAccessPolicy",
                _ => "Write-Host 'No action required'"
            };
        }

        private List<string> GenerateWarnings(PolicyRemediation remediation)
        {
            var warnings = new List<string>();

            if (remediation.RiskLevel >= Models.RiskLevel.High)
            {
                warnings.Add($"HIGH RISK: This change has {remediation.RiskLevel} risk level");
            }

            if (remediation.Impact.WillBlockAdminAccess)
            {
                warnings.Add("⚠️  This change may block administrator access");
            }

            if (remediation.Impact.EstimatedAffectedUsers > 1000)
            {
                warnings.Add($"⚠️  This change will affect {remediation.Impact.EstimatedAffectedUsers} users");
            }

            if (remediation.Action == RemediationAction.Delete)
            {
                warnings.Add("⚠️  Policy deletion cannot be undone - ensure backup is created");
            }

            return warnings;
        }

        private List<string> GeneratePrerequisites(PolicyRemediation remediation)
        {
            var prerequisites = new List<string>();

            prerequisites.Add("Azure AD Premium P1 or P2 license");
            prerequisites.Add("Conditional Access Administrator role or higher");

            if (remediation.RiskLevel >= Models.RiskLevel.High)
            {
                prerequisites.Add("Security Administrator approval");
            }

            if (remediation.Impact.WillBlockAdminAccess)
            {
                prerequisites.Add("Emergency access account configured");
                prerequisites.Add("Alternative admin access method verified");
            }

            return prerequisites;
        }

        private RemediationSummary CalculateRemediationSummary(List<PolicyRemediation> remediations)
        {
            return new RemediationSummary
            {
                TotalPoliciesNeedingRemediation = remediations.Count,
                LowRiskChanges = remediations.Count(r => r.RiskLevel == Models.RiskLevel.Low),
                MediumRiskChanges = remediations.Count(r => r.RiskLevel == Models.RiskLevel.Medium),
                HighRiskChanges = remediations.Count(r => r.RiskLevel == Models.RiskLevel.High),
                CriticalRiskChanges = remediations.Count(r => r.RiskLevel == Models.RiskLevel.Critical),
                EstimatedAffectedUsers = remediations.Sum(r => r.Impact.EstimatedAffectedUsers),
                EstimatedAffectedSessions = remediations.Sum(r => r.Impact.EstimatedAffectedSessions)
            };
        }

        private void GenerateRemediationScripts(RemediationResult result, RemediationOptions options)
        {
            foreach (var format in options.OutputFormats)
            {
                foreach (var remediation in result.PolicyRemediations)
                {
                    var script = _scriptGenerationService.GenerateScript(remediation, format, options);
                    remediation.GeneratedScripts[format] = script.Script;
                }
            }
        }

        public RemediationResult AnalyzePolicy(ConditionalAccessPolicy policy)
        {
            var result = new RemediationResult
            {
                TenantId = "current", // TODO: Get actual tenant ID
                GeneratedAt = DateTime.UtcNow
            };

            // For now, create a placeholder remediation
            // TODO: Implement actual policy analysis logic
            var remediation = new PolicyRemediation
            {
                PolicyId = policy.Id ?? "unknown",
                PolicyName = policy.DisplayName ?? "Unnamed Policy",
                Action = RemediationAction.NoAction,
                RiskLevel = ConditionalAccessExporter.Models.RiskLevel.Low
            };

            result.PolicyRemediations.Add(remediation);
            return result;
        }

        private BackupInfo GenerateBackup(string outputDirectory)
        {
            var backupDir = Path.Combine(outputDirectory, "backups", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(backupDir);

            return new BackupInfo
            {
                BackupPath = backupDir,
                BackupTimestamp = DateTime.UtcNow,
                BackupFiles = new List<string>(),
                RollbackScriptPath = Path.Combine(backupDir, "rollback.ps1")
            };
        }

        public InteractiveSession StartInteractiveSession(RemediationResult result)
        {
            var session = new InteractiveSession
            {
                PendingActions = result.PolicyRemediations.ToList()
            };

            Logger.WriteInfo($"Started interactive remediation session: {session.SessionId}");
            Logger.WriteInfo($"Found {session.PendingActions.Count} policies requiring remediation");

            return session;
        }

        public UserDecision PromptUserDecision(PolicyRemediation remediation)
        {
            Console.WriteLine();
            Console.WriteLine($"=== Policy Remediation Required ===");
            Console.WriteLine($"Policy: {remediation.PolicyName}");
            Console.WriteLine($"Action: {remediation.Action}");
            Console.WriteLine($"Risk Level: {remediation.RiskLevel}");
            Console.WriteLine($"Affected Users: {remediation.Impact.EstimatedAffectedUsers}");
            
            if (remediation.Warnings.Any())
            {
                Console.WriteLine("Warnings:");
                foreach (var warning in remediation.Warnings)
                {
                    Console.WriteLine($"  {warning}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  [A]pprove - Apply this change");
            Console.WriteLine("  [R]eject - Skip this change");
            Console.WriteLine("  [D]etails - Show detailed differences");
            Console.WriteLine("  [S]kip - Skip for now");
            Console.Write("Your choice: ");

            var input = Console.ReadLine()?.ToUpperInvariant();
            var action = input switch
            {
                "A" => InteractiveAction.Approve,
                "R" => InteractiveAction.Reject,
                "D" => InteractiveAction.ShowDetails,
                "S" => InteractiveAction.Skip,
                _ => InteractiveAction.Skip
            };

            return new UserDecision
            {
                PolicyId = remediation.PolicyId,
                Action = action
            };
        }
    }
}

using Newtonsoft.Json;

namespace ConditionalAccessExporter.Models
{
    // Main remediation result containing all remediation data
    public class RemediationResult
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string TenantId { get; set; } = string.Empty;
        public string ReferenceDirectory { get; set; } = string.Empty;
        public RemediationSummary Summary { get; set; } = new();
        public List<PolicyRemediation> PolicyRemediations { get; set; } = new();
        public BackupInfo? BackupInfo { get; set; }
        public List<string> PreflightChecks { get; set; } = new();
    }

    // Summary of remediation actions required
    public class RemediationSummary
    {
        public int TotalPoliciesNeedingRemediation { get; set; }
        public int LowRiskChanges { get; set; }
        public int MediumRiskChanges { get; set; }
        public int HighRiskChanges { get; set; }
        public int CriticalRiskChanges { get; set; }
        public int EstimatedAffectedUsers { get; set; }
        public int EstimatedAffectedSessions { get; set; }
        public bool RequiresApproval => HighRiskChanges > 0 || CriticalRiskChanges > 0;
    }

    // Remediation information for a specific policy
    public class PolicyRemediation
    {
        public string PolicyId { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public RemediationAction Action { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public ImpactAnalysis Impact { get; set; } = new();
        public List<RemediationStep> Steps { get; set; } = new();
        public Dictionary<RemediationFormat, string> GeneratedScripts { get; set; } = new();
        public object? CurrentPolicy { get; set; }
        public object? TargetPolicy { get; set; }
        public object? Differences { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Prerequisites { get; set; } = new();
    }

    // Types of remediation actions
    public enum RemediationAction
    {
        Create,      // Policy exists in reference but not in tenant
        Update,      // Policy exists in both but differs
        Delete,      // Policy exists in tenant but not in reference
        NoAction     // Policy is identical or differences are ignored
    }

    // Risk levels for changes
    public enum RiskLevel
    {
        Low,      // Description updates, non-functional changes
        Medium,   // Condition modifications, scope changes
        High,     // Grant control changes, policy enablement
        Critical  // Broad user impact, admin access changes
    }

    // Impact analysis for a policy change
    public class ImpactAnalysis
    {
        public int EstimatedAffectedUsers { get; set; }
        public int EstimatedAffectedSessions { get; set; }
        public List<string> AffectedUserGroups { get; set; } = new();
        public List<string> AffectedApplications { get; set; } = new();
        public List<string> PotentialAccessIssues { get; set; } = new();
        public bool WillBlockAdminAccess { get; set; }
        public bool WillRequireAdditionalAuthentication { get; set; }
        public string ImpactDescription { get; set; } = string.Empty;
    }

    // Individual remediation step
    public class RemediationStep
    {
        public int Order { get; set; }
        public string Description { get; set; } = string.Empty;
        public RemediationStepType Type { get; set; }
        public string Action { get; set; } = string.Empty;
        public bool RequiresElevatedPermissions { get; set; }
        public bool RequiresUserConfirmation { get; set; }
        public List<string> Prerequisites { get; set; } = new();
    }

    public enum RemediationStepType
    {
        Backup,
        Validation,
        Update,
        Verification,
        Rollback
    }

    // Supported output formats for remediation scripts
    public enum RemediationFormat
    {
        PowerShell,
        AzureCLI,
        Terraform,
        RestAPI,
        ManualInstructions
    }

    // Backup information
    public class BackupInfo
    {
        public string BackupPath { get; set; } = string.Empty;
        public DateTime BackupTimestamp { get; set; }
        public List<string> BackupFiles { get; set; } = new();
        public string RollbackScriptPath { get; set; } = string.Empty;
    }

    // Options for remediation generation
    public class RemediationOptions
    {
        public List<RemediationFormat> OutputFormats { get; set; } = new() { RemediationFormat.PowerShell };
        public bool GenerateBackup { get; set; } = true;
        public bool GenerateRollbackScript { get; set; } = true;
        public bool DryRun { get; set; } = true;
        public bool InteractiveMode { get; set; } = false;
        public bool RequireApprovalForHighRisk { get; set; } = true;
        public bool IncludeImpactAnalysis { get; set; } = true;
        public string OutputDirectory { get; set; } = "remediation-output";
        public List<RiskLevel> AutoApprovedRiskLevels { get; set; } = new() { RiskLevel.Low };
        public string? WebhookUrl { get; set; }
        public Dictionary<string, string> CustomVariables { get; set; } = new();
    }

    // Interactive remediation session
    public class InteractiveSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public List<PolicyRemediation> PendingActions { get; set; } = new();
        public List<PolicyRemediation> ApprovedActions { get; set; } = new();
        public List<PolicyRemediation> RejectedActions { get; set; } = new();
        public UserDecision? CurrentDecision { get; set; }
    }

    // User decision for interactive mode
    public class UserDecision
    {
        public string PolicyId { get; set; } = string.Empty;
        public InteractiveAction Action { get; set; }
        public string? Reason { get; set; }
        public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
    }

    public enum InteractiveAction
    {
        Approve,
        Reject,
        Skip,
        ShowDetails,
        ModifyRisk
    }

    // Script generation result
    public class ScriptGenerationResult
    {
        public RemediationFormat Format { get; set; }
        public string Script { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string ExecutionInstructions { get; set; } = string.Empty;
    }

    // Remediation execution result
    public class RemediationExecutionResult
    {
        public bool Success { get; set; }
        public List<PolicyExecutionResult> PolicyResults { get; set; } = new();
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public string? ErrorMessage { get; set; }
        public BackupInfo? BackupCreated { get; set; }
    }

    // Result of executing remediation for a single policy
    public class PolicyExecutionResult
    {
        public string PolicyId { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ExecutedSteps { get; set; } = new();
        public List<string> FailedSteps { get; set; } = new();
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }

    // Risk assessment configuration
    public class RiskAssessmentConfig
    {
        public Dictionary<string, RiskLevel> FieldRiskLevels { get; set; } = new()
        {
            ["state"] = RiskLevel.High,
            ["conditions.users.includeUsers"] = RiskLevel.Medium,
            ["conditions.users.excludeUsers"] = RiskLevel.High,
            ["conditions.applications.includeApplications"] = RiskLevel.Medium,
            ["grantControls.builtInControls"] = RiskLevel.High,
            ["grantControls.operator"] = RiskLevel.High,
            ["displayName"] = RiskLevel.Low,
            ["description"] = RiskLevel.Low
        };

        public Dictionary<string, int> UserImpactThresholds { get; set; } = new()
        {
            ["low"] = 10,
            ["medium"] = 100,
            ["high"] = 1000,
            ["critical"] = 5000
        };
    }
}

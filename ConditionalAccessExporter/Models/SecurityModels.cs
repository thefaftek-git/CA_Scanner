


using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ConditionalAccessExporter.Models
{
    /// <summary>
    /// Represents a security event for audit logging
    /// </summary>
    public class SecurityEvent
    {
        public string EventId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        [JsonConverter(typeof(StringEnumConverter))]
        public SecurityEventSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? SessionId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
        public string? Hash { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedBy { get; set; }
        public string? Resolution { get; set; }
    }

    /// <summary>
    /// Security event severity levels
    /// </summary>
    public enum SecurityEventSeverity
    {
        Info = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Represents a compliance event for audit logging
    /// </summary>
    public class ComplianceEvent
    {
        public string EventId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ComplianceStandard ComplianceStandard { get; set; }
        public string ControlId { get; set; } = string.Empty;
        public string ControlDescription { get; set; } = string.Empty;
        [JsonConverter(typeof(StringEnumConverter))]
        public ComplianceStatus ComplianceStatus { get; set; }
        public string Evidence { get; set; } = string.Empty;
        public DateTime AssessmentDate { get; set; }
        public string? AssessedBy { get; set; }
        public string? Remediation { get; set; }
        public DateTime? RemediationDueDate { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Compliance standards supported by the system
    /// </summary>
    public enum ComplianceStandard
    {
        SOC2,
        ISO27001,
        NIST,
        GDPR,
        HIPAA,
        CCPA,
        OWASP,
        Custom
    }

    /// <summary>
    /// Compliance status values
    /// </summary>
    public enum ComplianceStatus
    {
        Compliant,
        NonCompliant,
        PartiallyCompliant,
        NotApplicable,
        NotAssessed
    }

    /// <summary>
    /// Represents an access event for audit logging
    /// </summary>
    public class AccessEvent
    {
        public string EventId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ResourceAccessed { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? SessionId { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object>? RequestData { get; set; }
        public Dictionary<string, object>? ResponseData { get; set; }
        public string? RiskScore { get; set; }
        public List<string>? RiskFactors { get; set; }
    }

    /// <summary>
    /// Represents a vulnerability detection event
    /// </summary>
    public class VulnerabilityEvent
    {
        public string VulnerabilityId { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        [JsonConverter(typeof(StringEnumConverter))]
        public VulnerabilitySeverity Severity { get; set; }
        public double CvssScore { get; set; }
        public string AffectedComponent { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public List<string> References { get; set; } = new();
        public string RecommendedAction { get; set; } = string.Empty;
        public DateTime? PatchAvailableDate { get; set; }
        public bool HasExploit { get; set; }
        public bool IsExploitActive { get; set; }
        public List<string> AffectedPlatforms { get; set; } = new();
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Vulnerability severity levels
    /// </summary>
    public enum VulnerabilitySeverity
    {
        Info = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Represents a security configuration change event
    /// </summary>
    public class ConfigurationChangeEvent
    {
        public string ChangeId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ConfigurationItem { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string ChangeReason { get; set; } = string.Empty;
        public string? ApprovalId { get; set; }
        public bool RequiresApproval { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public List<string> AffectedSystems { get; set; } = new();
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Security audit report
    /// </summary>
    public class SecurityAuditReport
    {
        public string ReportId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string GeneratedBy { get; set; } = "System";
        public int TotalSecurityEvents { get; set; }
        public int TotalComplianceEvents { get; set; }
        public int TotalAccessEvents { get; set; }
        public int CriticalSecurityEvents { get; set; }
        public int HighSecurityEvents { get; set; }
        public int ComplianceViolations { get; set; }
        public int FailedAccessAttempts { get; set; }
        public Dictionary<string, int> SecurityEventsByType { get; set; } = new();
        public Dictionary<ComplianceStandard, int> ComplianceByStandard { get; set; } = new();
        public Dictionary<string, int> TopAccessedResources { get; set; } = new();
        public SecurityTrends SecurityTrends { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public List<SecurityEvent> TopSecurityEvents { get; set; } = new();
        public List<ComplianceEvent> RecentComplianceEvents { get; set; } = new();
        public SecurityMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// Compliance report
    /// </summary>
    public class ComplianceReport
    {
        public string ReportId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ComplianceStandard ComplianceStandard { get; set; }
        public string GeneratedBy { get; set; } = "System";
        public int TotalControls { get; set; }
        public int CompliantControls { get; set; }
        public int NonCompliantControls { get; set; }
        public int PartiallyCompliantControls { get; set; }
        public double CompliancePercentage { get; set; }
        public DateTime LastAssessmentDate { get; set; }
        public DateTime NextAssessmentDate { get; set; }
        public List<ComplianceControl> ControlDetails { get; set; } = new();
        public List<ComplianceEvent> RecentEvents { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public ComplianceMetrics Metrics { get; set; } = new();
        public List<ComplianceGap> IdentifiedGaps { get; set; } = new();
    }

    /// <summary>
    /// Compliance control details
    /// </summary>
    public class ComplianceControl
    {
        public string ControlId { get; set; } = string.Empty;
        public string ControlName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        [JsonConverter(typeof(StringEnumConverter))]
        public ComplianceStatus Status { get; set; }
        public DateTime LastAssessed { get; set; }
        public string AssessedBy { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new();
        public List<string> ImplementationNotes { get; set; } = new();
        public string? Remediation { get; set; }
        public DateTime? RemediationDueDate { get; set; }
        public string? ResponsibleParty { get; set; }
        public int Priority { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Security trends analysis
    /// </summary>
    public class SecurityTrends
    {
        public TrendDirection OverallTrend { get; set; }
        public Dictionary<SecurityEventSeverity, TrendDirection> SeverityTrends { get; set; } = new();
        public Dictionary<string, TrendDirection> EventTypeTrends { get; set; } = new();
        public double IncidentResolutionTimeAverage { get; set; }
        public TrendDirection ResolutionTimeTrend { get; set; }
        public int VulnerabilityCount { get; set; }
        public TrendDirection VulnerabilityTrend { get; set; }
        public List<SecurityPattern> DetectedPatterns { get; set; } = new();
        public List<SecurityAnomaly> DetectedAnomalies { get; set; } = new();
    }

    /// <summary>
    /// Trend direction enumeration
    /// </summary>
    public enum TrendDirection
    {
        Improving,
        Stable,
        Degrading,
        Unknown
    }

    /// <summary>
    /// Security pattern detection
    /// </summary>
    public class SecurityPattern
    {
        public string PatternId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Frequency { get; set; }
        public DateTime FirstDetected { get; set; }
        public DateTime LastDetected { get; set; }
        public SecurityEventSeverity Severity { get; set; }
        public List<string> AffectedResources { get; set; } = new();
        public string RecommendedAction { get; set; } = string.Empty;
    }

    /// <summary>
    /// Security anomaly detection
    /// </summary>
    public class SecurityAnomaly
    {
        public string AnomalyId { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public double AnomalyScore { get; set; }
        public string AnomalyType { get; set; } = string.Empty;
        public List<string> AffectedMetrics { get; set; } = new();
        public Dictionary<string, object> BaselineValues { get; set; } = new();
        public Dictionary<string, object> CurrentValues { get; set; } = new();
        public string RecommendedAction { get; set; } = string.Empty;
    }

    /// <summary>
    /// Security metrics
    /// </summary>
    public class SecurityMetrics
    {
        public double MeanTimeToDetection { get; set; }
        public double MeanTimeToResponse { get; set; }
        public double MeanTimeToResolution { get; set; }
        public double SecurityPostureScore { get; set; }
        public double VulnerabilityDensity { get; set; }
        public double ComplianceScore { get; set; }
        public int ActiveThreats { get; set; }
        public int MitigatedThreats { get; set; }
        public DateTime LastSecurityUpdate { get; set; }
        public int DaysSinceLastIncident { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
    }

    /// <summary>
    /// Compliance metrics
    /// </summary>
    public class ComplianceMetrics
    {
        public double OverallComplianceScore { get; set; }
        public Dictionary<string, double> ControlCategoryScores { get; set; } = new();
        public int TotalControlsAssessed { get; set; }
        public int ControlsPassingRate { get; set; }
        public double AverageControlMaturity { get; set; }
        public int DaysToNextAssessment { get; set; }
        public List<ComplianceRisk> IdentifiedRisks { get; set; } = new();
        public ComplianceMaturityLevel MaturityLevel { get; set; }
    }

    /// <summary>
    /// Compliance gap identification
    /// </summary>
    public class ComplianceGap
    {
        public string GapId { get; set; } = string.Empty;
        public string ControlId { get; set; } = string.Empty;
        public string GapDescription { get; set; } = string.Empty;
        public ComplianceRiskLevel RiskLevel { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
        public TimeSpan EstimatedEffort { get; set; }
        public DateTime TargetResolutionDate { get; set; }
        public string ResponsibleParty { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
    }

    /// <summary>
    /// Compliance risk assessment
    /// </summary>
    public class ComplianceRisk
    {
        public string RiskId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ComplianceRiskLevel RiskLevel { get; set; }
        public double RiskScore { get; set; }
        public List<string> AffectedControls { get; set; } = new();
        public string Mitigation { get; set; } = string.Empty;
        public DateTime IdentifiedDate { get; set; }
        public DateTime? MitigationDate { get; set; }
        public string Owner { get; set; } = string.Empty;
    }

    /// <summary>
    /// Compliance risk levels
    /// </summary>
    public enum ComplianceRiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Compliance maturity levels
    /// </summary>
    public enum ComplianceMaturityLevel
    {
        Initial = 1,
        Managed = 2,
        Defined = 3,
        Quantitatively_Managed = 4,
        Optimizing = 5
    }

    /// <summary>
    /// Filter for security events
    /// </summary>
    public class SecurityEventFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public SecurityEventSeverity? MinimumSeverity { get; set; }
        public List<string>? EventTypes { get; set; }
        public List<string>? Sources { get; set; }
        public string? UserId { get; set; }
        public bool? IsResolved { get; set; }
        public int? MaxResults { get; set; }
        public string? SearchTerm { get; set; }
    }

    /// <summary>
    /// Security scanning configuration
    /// </summary>
    public class SecurityScanConfiguration
    {
        public bool EnableCodeQLScanning { get; set; } = true;
        public bool EnableDependencyScanning { get; set; } = true;
        public bool EnableSecretsScanning { get; set; } = true;
        public bool EnableContainerScanning { get; set; } = false;
        public bool EnableInfrastructureScanning { get; set; } = false;
        public List<string> ExcludedPaths { get; set; } = new();
        public List<string> ScanningTools { get; set; } = new();
        public Dictionary<string, object> ToolConfigurations { get; set; } = new();
        public TimeSpan ScanFrequency { get; set; } = TimeSpan.FromDays(1);
        public SecurityEventSeverity MinimumReportingSeverity { get; set; } = SecurityEventSeverity.Medium;
        public bool EnableRealTimeMonitoring { get; set; } = true;
        public bool EnableComplianceReporting { get; set; } = true;
        public List<ComplianceStandard> EnabledComplianceStandards { get; set; } = new();
    }

    /// <summary>
    /// Security incident management
    /// </summary>
    public class SecurityIncident
    {
        public string IncidentId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public SecurityEventSeverity Severity { get; set; }
        public IncidentStatus Status { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public List<SecurityEvent> RelatedEvents { get; set; } = new();
        public List<string> AffectedSystems { get; set; } = new();
        public List<IncidentAction> Actions { get; set; } = new();
        public string RootCause { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public List<string> LessonsLearned { get; set; } = new();
        public double ImpactScore { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public TimeSpan ResolutionTime { get; set; }
    }

    /// <summary>
    /// Incident status enumeration
    /// </summary>
    public enum IncidentStatus
    {
        Open,
        InProgress,
        Resolved,
        Closed,
        Escalated
    }

    /// <summary>
    /// Incident action tracking
    /// </summary>
    public class IncidentAction
    {
        public string ActionId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsAutomated { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
}




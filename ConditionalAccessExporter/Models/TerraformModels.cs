using Newtonsoft.Json;

namespace ConditionalAccessExporter.Models
{
    public class TerraformParseResult
    {
        public DateTime ParsedAt { get; set; }
        public string SourcePath { get; set; } = string.Empty;
        public List<TerraformConditionalAccessPolicy> Policies { get; set; } = new();
        public List<TerraformVariable> Variables { get; set; } = new();
        public List<TerraformLocal> Locals { get; set; } = new();
        public List<TerraformDataSource> DataSources { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class TerraformConditionalAccessPolicy
    {
        public string ResourceName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? State { get; set; }
        public TerraformConditions? Conditions { get; set; }
        public TerraformGrantControls? GrantControls { get; set; }
        public TerraformSessionControls? SessionControls { get; set; }
        public Dictionary<string, object> RawAttributes { get; set; } = new();
    }

    public class TerraformConditions
    {
        public TerraformApplications? Applications { get; set; }
        public TerraformUsers? Users { get; set; }
        public List<string>? ClientAppTypes { get; set; }
        public TerraformPlatforms? Platforms { get; set; }
        public TerraformLocations? Locations { get; set; }
        public List<string>? SignInRiskLevels { get; set; }
        public List<string>? UserRiskLevels { get; set; }
        public TerraformClientApplications? ClientApplications { get; set; }
    }

    public class TerraformApplications
    {
        public List<string>? IncludeApplications { get; set; }
        public List<string>? ExcludeApplications { get; set; }
        public List<string>? IncludeUserActions { get; set; }
        public List<string>? IncludeAuthenticationContextClassReferences { get; set; }
    }

    public class TerraformUsers
    {
        public List<string>? IncludeUsers { get; set; }
        public List<string>? ExcludeUsers { get; set; }
        public List<string>? IncludeGroups { get; set; }
        public List<string>? ExcludeGroups { get; set; }
        public List<string>? IncludeRoles { get; set; }
        public List<string>? ExcludeRoles { get; set; }
    }

    public class TerraformPlatforms
    {
        public List<string>? IncludePlatforms { get; set; }
        public List<string>? ExcludePlatforms { get; set; }
    }

    public class TerraformLocations
    {
        public List<string>? IncludeLocations { get; set; }
        public List<string>? ExcludeLocations { get; set; }
    }

    public class TerraformClientApplications
    {
        public List<string>? IncludeServicePrincipals { get; set; }
        public List<string>? ExcludeServicePrincipals { get; set; }
    }

    public class TerraformGrantControls
    {
        public string? Operator { get; set; }
        public List<string>? BuiltInControls { get; set; }
        public List<string>? CustomAuthenticationFactors { get; set; }
        public List<string>? TermsOfUse { get; set; }
        public TerraformAuthenticationStrength? AuthenticationStrength { get; set; }
    }

    public class TerraformAuthenticationStrength
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
    }

    public class TerraformSessionControls
    {
        public TerraformApplicationEnforcedRestrictions? ApplicationEnforcedRestrictions { get; set; }
        public TerraformCloudAppSecurity? CloudAppSecurity { get; set; }
        public TerraformPersistentBrowser? PersistentBrowser { get; set; }
        public TerraformSignInFrequency? SignInFrequency { get; set; }
    }

    public class TerraformApplicationEnforcedRestrictions
    {
        public bool IsEnabled { get; set; }
    }

    public class TerraformCloudAppSecurity
    {
        public bool IsEnabled { get; set; }
        public string? CloudAppSecurityType { get; set; }
    }

    public class TerraformPersistentBrowser
    {
        public bool IsEnabled { get; set; }
        public string? Mode { get; set; }
    }

    public class TerraformSignInFrequency
    {
        public bool IsEnabled { get; set; }
        public string? Type { get; set; }
        public int? Value { get; set; }
        public string? AuthenticationType { get; set; }
    }

    public class TerraformVariable
    {
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public object? DefaultValue { get; set; }
        public string? Description { get; set; }
        public bool Sensitive { get; set; }
    }

    public class TerraformLocal
    {
        public string Name { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    public class TerraformDataSource
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    public class TerraformConversionResult
    {
        public DateTime ConvertedAt { get; set; }
        public string SourcePath { get; set; } = string.Empty;
        public object ConvertedPolicies { get; set; } = new();
        public List<string> ConversionLog { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int SuccessfulConversions { get; set; }
        public int FailedConversions { get; set; }
    }
}
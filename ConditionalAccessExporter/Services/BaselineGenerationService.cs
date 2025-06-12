using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using ConditionalAccessExporter.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ConditionalAccessExporter.Services
{
    public class BaselineGenerationService
    {
        public async Task<int> GenerateBaselineAsync(BaselineGenerationOptions options, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Baseline Generation");
            Console.WriteLine("==================");
            Console.WriteLine();

            try
            {
                // Validate output directory
                if (string.IsNullOrEmpty(options.OutputDirectory))
                {
                    Console.WriteLine("Error: Output directory is required but was not provided.");
                    return 1;
                }

                // Create output directory if it doesn't exist
                if (!Directory.Exists(options.OutputDirectory))
                {
                    Directory.CreateDirectory(options.OutputDirectory);
                    Console.WriteLine($"Created output directory: {options.OutputDirectory}");
                }

                // Fetch policies from tenant
                Console.WriteLine("Fetching policies from tenant...");
                var policies = await FetchPoliciesAsync(cancellationToken);

                if (policies?.Value == null || !policies.Value.Any())
                {
                    Console.WriteLine("No Conditional Access Policies found.");
                    return 0;
                }

                Console.WriteLine($"Found {policies.Value.Count} policies in tenant");

                // Filter policies based on options
                var filteredPolicies = FilterPolicies(policies.Value, options);
                Console.WriteLine($"After filtering: {filteredPolicies.Count} policies selected");

                // Generate baseline files using parallel processing
                var generatedFiles = new List<string>();
                
                Console.WriteLine($"Generating baseline files for {filteredPolicies.Count} policies using parallel processing...");

                var parallelOptions = new ParallelProcessingOptions
                {
                    ContinueOnError = true, // Continue processing other policies even if some fail
                    ProgressReportInterval = Math.Max(1, filteredPolicies.Count / 10) // Report progress every 10%
                };

                var progress = new Progress<ParallelProcessingProgress>(p => 
                {
                    if (p.Completed % parallelOptions.ProgressReportInterval == 0 || p.Completed == p.Total)
                    {
                        Console.WriteLine($"Generating baseline files: {p}");
                    }
                });

                var parallelResult = await ParallelProcessingService.ProcessInParallelAsync(
                    filteredPolicies,
                    async (policy, ct) => await GeneratePolicyFileAsync(policy, options, ct),
                    parallelOptions,
                    progress,
                    cancellationToken);

                // Collect successful results
                generatedFiles.AddRange(parallelResult.Results.Where(f => !string.IsNullOrEmpty(f))!);

                // Log errors for failed policies
                foreach (var error in parallelResult.Errors)
                {
                    Console.WriteLine($"Warning: Failed to generate file for policy: {error.Exception.Message}");
                }

                Console.WriteLine($"Baseline file generation completed in {parallelResult.ElapsedTime.TotalMilliseconds:F0}ms");
                Console.WriteLine($"Average speed: {parallelResult.AverageItemsPerSecond:F1} policies/second");

                // Summary
                Console.WriteLine();
                Console.WriteLine("Baseline Generation Summary:");
                Console.WriteLine("============================");
                Console.WriteLine($"Total policies fetched: {policies.Value.Count}");
                Console.WriteLine($"Policies after filtering: {filteredPolicies.Count}");
                Console.WriteLine($"Files generated: {generatedFiles.Count}");
                Console.WriteLine($"Output directory: {Path.GetFullPath(options.OutputDirectory)}");

                if (generatedFiles.Any())
                {
                    Console.WriteLine();
                    Console.WriteLine("Generated files:");
                    foreach (var file in generatedFiles)
                    {
                        Console.WriteLine($"  - {file}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Baseline generation completed successfully!");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during baseline generation: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return 1;
            }
        }

        private async Task<ConditionalAccessPolicyCollectionResponse?> FetchPoliciesAsync(CancellationToken cancellationToken = default)
        {
            // Get Azure credentials from environment variables
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("Missing required environment variables. Please ensure AZURE_TENANT_ID, AZURE_CLIENT_ID, and AZURE_CLIENT_SECRET are set.");
            }

            Console.WriteLine($"Tenant ID: {tenantId?.Substring(0, 4)}***");
            Console.WriteLine($"Client ID: {clientId?.Substring(0, 4)}***");
            Console.WriteLine("Client Secret: [HIDDEN]");
            Console.WriteLine();

            // Create the Graph client with client credentials
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var graphClient = new GraphServiceClient(credential);

            Console.WriteLine("Authenticating to Microsoft Graph...");

            // Get all conditional access policies
            return await graphClient.Identity.ConditionalAccess.Policies.GetAsync(cancellationToken: cancellationToken);
        }

        private List<ConditionalAccessPolicy> FilterPolicies(IList<ConditionalAccessPolicy> policies, BaselineGenerationOptions options)
        {
            var filtered = policies.AsEnumerable();

            // Filter by enabled state
            if (options.FilterEnabledOnly)
            {
                filtered = filtered.Where(p => p.State == ConditionalAccessPolicyState.Enabled);
                Console.WriteLine("Applied filter: Enabled policies only");
            }

            // Filter by policy names
            if (options.PolicyNames?.Any() == true)
            {
                var policyNameSet = new HashSet<string>(options.PolicyNames, StringComparer.OrdinalIgnoreCase);
                filtered = filtered.Where(p => !string.IsNullOrEmpty(p.DisplayName) && 
                                             policyNameSet.Contains(p.DisplayName));
                Console.WriteLine($"Applied filter: Specific policy names ({string.Join(", ", options.PolicyNames)})");
            }

            return filtered.ToList();
        }

        private async Task<string?> GeneratePolicyFileAsync(ConditionalAccessPolicy policy, BaselineGenerationOptions options, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(policy.DisplayName))
            {
                Console.WriteLine("Warning: Skipping policy with empty DisplayName");
                return null;
            }

            // Create policy object in the same format expected by comparison service
            var policyData = CreatePolicyObject(policy, options);

            // Generate safe filename from DisplayName
            var fileName = GenerateSafeFileName(policy.DisplayName);
            var filePath = Path.Combine(options.OutputDirectory, fileName);

            // Serialize to JSON with pretty formatting
            var json = JsonConvert.SerializeObject(policyData, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });

            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, cancellationToken);
            
            Console.WriteLine($"Generated: {fileName}");
            return fileName;
        }

        private object CreatePolicyObject(ConditionalAccessPolicy policy, BaselineGenerationOptions options)
        {
            var policyData = new
            {
                Id = options.Anonymize ? "baseline-generated-id" : policy.Id,
                DisplayName = policy.DisplayName,
                State = policy.State?.ToString(),
                CreatedDateTime = options.Anonymize ? (DateTime?)null : policy.CreatedDateTime,
                ModifiedDateTime = options.Anonymize ? (DateTime?)null : policy.ModifiedDateTime,
                Conditions = new
                {
                    Applications = new
                    {
                        IncludeApplications = policy.Conditions?.Applications?.IncludeApplications,
                        ExcludeApplications = policy.Conditions?.Applications?.ExcludeApplications,
                        IncludeUserActions = policy.Conditions?.Applications?.IncludeUserActions,
                        IncludeAuthenticationContextClassReferences = policy.Conditions?.Applications?.IncludeAuthenticationContextClassReferences
                    },
                    Users = new
                    {
                        IncludeUsers = policy.Conditions?.Users?.IncludeUsers,
                        ExcludeUsers = policy.Conditions?.Users?.ExcludeUsers,
                        IncludeGroups = policy.Conditions?.Users?.IncludeGroups,
                        ExcludeGroups = policy.Conditions?.Users?.ExcludeGroups,
                        IncludeRoles = policy.Conditions?.Users?.IncludeRoles,
                        ExcludeRoles = policy.Conditions?.Users?.ExcludeRoles
                    },
                    ClientAppTypes = policy.Conditions?.ClientAppTypes,
                    Platforms = new
                    {
                        IncludePlatforms = policy.Conditions?.Platforms?.IncludePlatforms,
                        ExcludePlatforms = policy.Conditions?.Platforms?.ExcludePlatforms
                    },
                    Locations = new
                    {
                        IncludeLocations = policy.Conditions?.Locations?.IncludeLocations,
                        ExcludeLocations = policy.Conditions?.Locations?.ExcludeLocations
                    },
                    SignInRiskLevels = policy.Conditions?.SignInRiskLevels,
                    UserRiskLevels = policy.Conditions?.UserRiskLevels,
                    ClientApplications = new
                    {
                        IncludeServicePrincipals = policy.Conditions?.ClientApplications?.IncludeServicePrincipals,
                        ExcludeServicePrincipals = policy.Conditions?.ClientApplications?.ExcludeServicePrincipals
                    }
                },
                GrantControls = new
                {
                    Operator = policy.GrantControls?.Operator,
                    BuiltInControls = policy.GrantControls?.BuiltInControls,
                    CustomAuthenticationFactors = policy.GrantControls?.CustomAuthenticationFactors,
                    TermsOfUse = policy.GrantControls?.TermsOfUse,
                    AuthenticationStrength = policy.GrantControls?.AuthenticationStrength
                },
                SessionControls = new
                {
                    ApplicationEnforcedRestrictions = policy.SessionControls?.ApplicationEnforcedRestrictions != null ? new
                    {
                        IsEnabled = policy.SessionControls.ApplicationEnforcedRestrictions.IsEnabled
                    } : null,
                    CloudAppSecurity = policy.SessionControls?.CloudAppSecurity != null ? new
                    {
                        IsEnabled = policy.SessionControls.CloudAppSecurity.IsEnabled,
                        CloudAppSecurityType = policy.SessionControls.CloudAppSecurity.CloudAppSecurityType?.ToString()
                    } : null,
                    PersistentBrowser = policy.SessionControls?.PersistentBrowser != null ? new
                    {
                        IsEnabled = policy.SessionControls.PersistentBrowser.IsEnabled,
                        Mode = policy.SessionControls.PersistentBrowser.Mode?.ToString()
                    } : null,
                    SignInFrequency = policy.SessionControls?.SignInFrequency != null ? new
                    {
                        IsEnabled = policy.SessionControls.SignInFrequency.IsEnabled,
                        Type = policy.SessionControls.SignInFrequency.Type?.ToString(),
                        Value = policy.SessionControls.SignInFrequency.Value,
                        AuthenticationType = policy.SessionControls.SignInFrequency.AuthenticationType?.ToString()
                    } : null
                }
            };

            return policyData;
        }

        private string GenerateSafeFileName(string displayName)
        {
            // Remove invalid characters and replace with underscores
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(displayName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
            
            // Remove multiple consecutive underscores
            safeName = Regex.Replace(safeName, "_+", "_");
            
            // Remove leading/trailing underscores
            safeName = safeName.Trim('_');
            
            // Ensure it's not empty
            if (string.IsNullOrEmpty(safeName))
            {
                safeName = "unnamed_policy";
            }
            
            // Add .json extension
            return $"{safeName}.json";
        }
    }

    public class BaselineGenerationOptions
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public bool Anonymize { get; set; } = false;
        public bool FilterEnabledOnly { get; set; } = false;
        public List<string>? PolicyNames { get; set; }
    }
}

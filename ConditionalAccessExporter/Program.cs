using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using Newtonsoft.Json;
using System.Text;
using System.CommandLine;
using ConditionalAccessExporter.Models;
using ConditionalAccessExporter.Services;

namespace ConditionalAccessExporter
{
    class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Conditional Access Policy Exporter and Comparator");

            // Export command
            var exportCommand = new Command("export", "Export Conditional Access policies from Entra ID");
            var outputOption = new Option<string>(
                name: "--output",
                description: "Output file path",
                getDefaultValue: () => $"ConditionalAccessPolicies_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json"
            );
            exportCommand.AddOption(outputOption);
            exportCommand.SetHandler(ExportPoliciesAsync, outputOption);

            // Compare command
            var compareCommand = new Command("compare", "Compare Entra policies with reference JSON files");
            var referenceDirectoryOption = new Option<string>(
                name: "--reference-dir",
                description: "Directory containing reference JSON files"
            ) { IsRequired = true };
            var entraFileOption = new Option<string>(
                name: "--entra-file",
                description: "Path to exported Entra policies JSON file (if not provided, will fetch live data)"
            );
            var outputDirectoryOption = new Option<string>(
                name: "--output-dir",
                description: "Output directory for comparison reports",
                getDefaultValue: () => "comparison-reports"
            );
            var reportFormatsOption = new Option<string[]>(
                name: "--formats",
                description: "Report formats to generate",
                getDefaultValue: () => new[] { "console", "json", "html" }
            );
            var matchingStrategyOption = new Option<MatchingStrategy>(
                name: "--matching",
                description: "Strategy for matching policies",
                getDefaultValue: () => MatchingStrategy.ByName
            );
            var caseSensitiveOption = new Option<bool>(
                name: "--case-sensitive",
                description: "Case sensitive policy name matching",
                getDefaultValue: () => false
            );

            compareCommand.AddOption(referenceDirectoryOption);
            compareCommand.AddOption(entraFileOption);
            compareCommand.AddOption(outputDirectoryOption);
            compareCommand.AddOption(reportFormatsOption);
            compareCommand.AddOption(matchingStrategyOption);
            compareCommand.AddOption(caseSensitiveOption);

            compareCommand.SetHandler(ComparePoliciesAsync, 
                referenceDirectoryOption, 
                entraFileOption, 
                outputDirectoryOption, 
                reportFormatsOption, 
                matchingStrategyOption, 
                caseSensitiveOption);

            rootCommand.AddCommand(exportCommand);
            rootCommand.AddCommand(compareCommand);

            // If no arguments provided, default to export for backward compatibility
            if (args.Length == 0)
            {
                return await ExportPoliciesAsync($"ConditionalAccessPolicies_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            }

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> ExportPoliciesAsync(string outputPath)
        {
            Console.WriteLine("Conditional Access Policy Exporter");
            Console.WriteLine("==================================");

            try
            {
                var exportData = await FetchEntraPoliciesAsync();
                
                // Serialize to JSON with pretty formatting
                var json = JsonConvert.SerializeObject(exportData, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                });

                // Write to file
                await File.WriteAllTextAsync(outputPath, json, Encoding.UTF8);

                Console.WriteLine($"Conditional Access Policies exported successfully to: {outputPath}");
                Console.WriteLine($"File size: {new FileInfo(outputPath).Length / 1024.0:F2} KB");
                Console.WriteLine("Export completed successfully!");
                
                return 0;
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
                return 1;
            }
        }

        private static async Task<int> ComparePoliciesAsync(
            string referenceDirectory,
            string? entraFile,
            string outputDirectory,
            string[] reportFormats,
            MatchingStrategy matchingStrategy,
            bool caseSensitive)
        {
            Console.WriteLine("Conditional Access Policy Comparison");
            Console.WriteLine("===================================");

            try
            {
                object entraExport;

                if (!string.IsNullOrEmpty(entraFile))
                {
                    Console.WriteLine($"Loading Entra policies from file: {entraFile}");
                    if (!File.Exists(entraFile))
                    {
                        Console.WriteLine($"Error: Entra file '{entraFile}' not found.");
                        return 1;
                    }

                    var fileContent = await File.ReadAllTextAsync(entraFile);
                    entraExport = JsonConvert.DeserializeObject(fileContent) ?? new object();
                }
                else
                {
                    Console.WriteLine("Fetching live Entra policies...");
                    entraExport = await FetchEntraPoliciesAsync();
                }

                var matchingOptions = new MatchingOptions
                {
                    Strategy = matchingStrategy,
                    CaseSensitive = caseSensitive
                };

                var comparisonService = new PolicyComparisonService();
                var result = await comparisonService.CompareAsync(entraExport, referenceDirectory, matchingOptions);

                var reportService = new ReportGenerationService();
                await reportService.GenerateReportsAsync(result, outputDirectory, reportFormats.ToList());

                Console.WriteLine("Comparison completed successfully!");
                return 0;
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
                return 1;
            }
        }

        private static async Task<object> FetchEntraPoliciesAsync()
        {
            // Get Azure credentials from environment variables
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("Missing required environment variables. Please ensure AZURE_TENANT_ID, AZURE_CLIENT_ID, and AZURE_CLIENT_SECRET are set.");
            }

            Console.WriteLine($"Tenant ID: {tenantId}");
            Console.WriteLine($"Client ID: {clientId}");
            Console.WriteLine("Client Secret: [HIDDEN]");
            Console.WriteLine();

            // Create the Graph client with client credentials
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var graphClient = new GraphServiceClient(credential);

            Console.WriteLine("Authenticating to Microsoft Graph...");
            Console.WriteLine("Fetching Conditional Access Policies...");

            // Get all conditional access policies
            var policies = await graphClient.Identity.ConditionalAccess.Policies.GetAsync();

            if (policies?.Value == null || !policies.Value.Any())
            {
                Console.WriteLine("No Conditional Access Policies found.");
                return new { ExportedAt = DateTime.UtcNow, TenantId = tenantId, PoliciesCount = 0, Policies = new List<object>() };
            }

            Console.WriteLine($"Found {policies.Value.Count} Conditional Access Policies");
            Console.WriteLine();

            // Create a simplified representation of the policies for JSON export
            var exportData = new
            {
                ExportedAt = DateTime.UtcNow,
                TenantId = tenantId,
                PoliciesCount = policies.Value.Count,
                Policies = policies.Value.Select(policy => new
                {
                    Id = policy.Id,
                    DisplayName = policy.DisplayName,
                    State = policy.State?.ToString(),
                    CreatedDateTime = policy.CreatedDateTime,
                    ModifiedDateTime = policy.ModifiedDateTime,
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
                }).ToList()
            };

            // Print summary
            Console.WriteLine("Policy Summary:");
            Console.WriteLine("================");
            foreach (var policy in policies.Value)
            {
                Console.WriteLine($"- {policy.DisplayName} (State: {policy.State})");
            }
            Console.WriteLine();

            return exportData;
        }

        private static async Task HandleExceptionAsync(Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            
            if (ex.Message.Contains("required scopes are missing"))
            {
                Console.WriteLine();
                Console.WriteLine("PERMISSION REQUIRED:");
                Console.WriteLine("===================");
                Console.WriteLine("The application registration needs Microsoft Graph API permissions to read Conditional Access Policies.");
                Console.WriteLine("Required permissions (Application permissions):");
                Console.WriteLine("- Policy.Read.All");
                Console.WriteLine("- OR Policy.ReadWrite.ConditionalAccess");
                Console.WriteLine();
                Console.WriteLine("To add these permissions:");
                Console.WriteLine("1. Go to Azure Portal -> App Registrations");
                Console.WriteLine("2. Find your app registration");
                Console.WriteLine("3. Go to 'API permissions'");
                Console.WriteLine("4. Click 'Add a permission' -> Microsoft Graph -> Application permissions");
                Console.WriteLine("5. Search for and add 'Policy.Read.All'");
                Console.WriteLine("6. Click 'Grant admin consent'");
                Console.WriteLine();
            }
            
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}

using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using Newtonsoft.Json;
using System.Text;

namespace ConditionalAccessExporter
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Conditional Access Policy Exporter");
            Console.WriteLine("==================================");

            try
            {
                // Get Azure credentials from environment variables
                var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
                var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    Console.WriteLine("Error: Missing required environment variables.");
                    Console.WriteLine("Please ensure AZURE_TENANT_ID, AZURE_CLIENT_ID, and AZURE_CLIENT_SECRET are set.");
                    return;
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
                    return;
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

                // Serialize to JSON with pretty formatting
                var json = JsonConvert.SerializeObject(exportData, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                });

                // Write to file
                var fileName = $"ConditionalAccessPolicies_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                await File.WriteAllTextAsync(fileName, json, Encoding.UTF8);

                Console.WriteLine($"Conditional Access Policies exported successfully to: {fileName}");
                Console.WriteLine($"File size: {new FileInfo(fileName).Length / 1024.0:F2} KB");
                Console.WriteLine();
                Console.WriteLine("Export completed successfully!");
            }
            catch (Exception ex)
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
                Environment.Exit(1);
            }
        }
    }
}

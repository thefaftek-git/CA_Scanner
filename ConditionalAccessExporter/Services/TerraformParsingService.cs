using ConditionalAccessExporter.Models;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ConditionalAccessExporter.Services
{
    public class TerraformParsingService
    {
        private readonly List<string> _logs = new();
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        public async Task<TerraformParseResult> ParseTerraformFileAsync(string filePath)
        {
            var result = new TerraformParseResult
            {
                ParsedAt = DateTime.UtcNow,
                SourcePath = filePath
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    _errors.Add($"File not found: {filePath}");
                    result.Errors = _errors;
                    return result;
                }

                var content = await File.ReadAllTextAsync(filePath);
                
                // Parse different types of Terraform content
                if (filePath.EndsWith(".tf", StringComparison.OrdinalIgnoreCase))
                {
                    ParseHclFile(content, result);
                }
                else if (filePath.EndsWith(".tfstate", StringComparison.OrdinalIgnoreCase) || 
                         filePath.EndsWith(".tfstate.backup", StringComparison.OrdinalIgnoreCase))
                {
                    ParseTerraformState(content, result);
                }
                else
                {
                    _warnings.Add($"Unknown file type for: {filePath}. Attempting HCL parsing.");
                    ParseHclFile(content, result);
                }
            }
            catch (Exception ex)
            {
                _errors.Add($"Error parsing file {filePath}: {ex.Message}");
            }

            result.Errors = _errors;
            result.Warnings = _warnings;
            return result;
        }

        public async Task<TerraformParseResult> ParseTerraformDirectoryAsync(string directoryPath)
        {
            var result = new TerraformParseResult
            {
                ParsedAt = DateTime.UtcNow,
                SourcePath = directoryPath
            };

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    _errors.Add($"Directory not found: {directoryPath}");
                    result.Errors = _errors;
                    return result;
                }

                var terraformFiles = Directory.GetFiles(directoryPath, "*.tf", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(directoryPath, "*.tfstate", SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(directoryPath, "*.tfstate.backup", SearchOption.AllDirectories));

                foreach (var file in terraformFiles)
                {
                    var fileResult = await ParseTerraformFileAsync(file);
                    
                    // Merge results
                    result.Policies.AddRange(fileResult.Policies);
                    result.Variables.AddRange(fileResult.Variables);
                    result.Locals.AddRange(fileResult.Locals);
                    result.DataSources.AddRange(fileResult.DataSources);
                    result.Errors.AddRange(fileResult.Errors);
                    result.Warnings.AddRange(fileResult.Warnings);
                }
            }
            catch (Exception ex)
            {
                _errors.Add($"Error parsing directory {directoryPath}: {ex.Message}");
            }

            result.Errors = _errors;
            result.Warnings = _warnings;
            return result;
        }

        private void ParseHclFile(string content, TerraformParseResult result)
        {
            // Parse azuread_conditional_access_policy resources using a more robust approach
            var policyBlocks = ExtractResourceBlocks(content, "azuread_conditional_access_policy");

            foreach (var (resourceName, resourceBody) in policyBlocks)
            {
                try
                {
                    var policy = ParseConditionalAccessPolicyResource(resourceName, resourceBody);
                    result.Policies.Add(policy);
                    _logs.Add($"Parsed policy resource: {resourceName}");
                }
                catch (Exception ex)
                {
                    _errors.Add($"Error parsing policy resource '{resourceName}': {ex.Message}");
                }
            }

            // Parse variables
            var variableBlocks = ExtractVariableBlocks(content);
            foreach (var (variableName, variableBody) in variableBlocks)
            {
                try
                {
                    var variable = ParseVariable(variableName, variableBody);
                    result.Variables.Add(variable);
                    _logs.Add($"Parsed variable: {variableName}");
                }
                catch (Exception ex)
                {
                    _errors.Add($"Error parsing variable '{variableName}': {ex.Message}");
                }
            }

            // Parse locals
            var localsBlocks = ExtractLocalsBlocks(content);
            foreach (var localsBody in localsBlocks)
            {
                try
                {
                    var locals = ParseLocals(localsBody);
                    result.Locals.AddRange(locals);
                    _logs.Add($"Parsed {locals.Count} local values");
                }
                catch (Exception ex)
                {
                    _errors.Add($"Error parsing locals: {ex.Message}");
                }
            }
        }

        private void ParseTerraformState(string content, TerraformParseResult result)
        {
            try
            {
                var stateData = JsonConvert.DeserializeObject<dynamic>(content);
                
                if (stateData?.resources != null)
                {
                    foreach (var resource in stateData.resources)
                    {
                        if (resource?.type == "azuread_conditional_access_policy")
                        {
                            foreach (var instance in resource.instances ?? new dynamic[0])
                            {
                                try
                                {
                                    // Convert dynamic objects to string and object explicitly to avoid binding issues
                                    string resourceName = resource.name?.ToString() ?? "unknown";
                                    object attributes = instance.attributes;
                                    
                                    var policy = ParsePolicyFromTerraformState(resourceName, attributes);
                                    result.Policies.Add(policy);
                                    _logs.Add($"Parsed policy from state: {resourceName}");
                                }
                                catch (ArgumentException ex)
                                {
                                    _errors.Add($"Error parsing policy from state '{resource.name}': {ex.Message}");
                                }
                                catch (Exception ex)
                                {
                                    _errors.Add($"Error parsing policy from state '{resource.name}': {ex.GetType().Name} - {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errors.Add($"Error parsing Terraform state: {ex.Message}");
            }
        }

        private TerraformConditionalAccessPolicy ParseConditionalAccessPolicyResource(string resourceName, string resourceBody)
        {
            var policy = new TerraformConditionalAccessPolicy { ResourceName = resourceName };
            
            // Parse display_name
            var displayNameMatch = Regex.Match(resourceBody, @"display_name\s*=\s*""([^""]*)""");
            if (displayNameMatch.Success)
            {
                policy.DisplayName = displayNameMatch.Groups[1].Value;
            }

            // Parse state
            var stateMatch = Regex.Match(resourceBody, @"state\s*=\s*""([^""]*)""");
            if (stateMatch.Success)
            {
                policy.State = stateMatch.Groups[1].Value;
            }

            // Parse conditions block
            var conditionsMatch = Regex.Match(resourceBody, @"conditions\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}");
            if (conditionsMatch.Success)
            {
                policy.Conditions = ParseConditions(conditionsMatch.Groups[1].Value);
            }

            // Parse grant_controls block
            var grantControlsMatch = Regex.Match(resourceBody, @"grant_controls\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}");
            if (grantControlsMatch.Success)
            {
                policy.GrantControls = ParseGrantControls(grantControlsMatch.Groups[1].Value);
            }

            // Parse session_controls block
            var sessionControlsMatch = Regex.Match(resourceBody, @"session_controls\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}");
            if (sessionControlsMatch.Success)
            {
                policy.SessionControls = ParseSessionControls(sessionControlsMatch.Groups[1].Value);
            }

            return policy;
        }

        private TerraformConditionalAccessPolicy ParsePolicyFromTerraformState(string resourceName, object attributes)
        {
            var policy = new TerraformConditionalAccessPolicy { ResourceName = resourceName };
            
            // Handle both JObject and dynamic objects
            if (attributes is Newtonsoft.Json.Linq.JObject jObj)
            {
                // Use JObject properties directly instead of dynamic conversion
                policy.DisplayName = jObj.Value<string>("display_name") ?? string.Empty;
                policy.State = jObj.Value<string>("state") ?? string.Empty;

                // Parse conditions from state
                var conditionsToken = jObj["conditions"];
                if (conditionsToken != null)
                {
                    policy.Conditions = ParseConditionsFromStateToken(conditionsToken);
                }

                // Parse grant_controls from state
                var grantControlsToken = jObj["grant_controls"];
                if (grantControlsToken != null)
                {
                    policy.GrantControls = ParseGrantControlsFromStateToken(grantControlsToken);
                }

                // Parse session_controls from state
                var sessionControlsToken = jObj["session_controls"];
                if (sessionControlsToken != null)
                {
                    policy.SessionControls = ParseSessionControlsFromStateToken(sessionControlsToken);
                }
            }
            else
            {
                // Handle dynamic objects using reflection-based approach
                var dynAttributes = attributes;
                
                // Use reflection to safely get properties
                var displayNameProp = dynAttributes?.GetType().GetProperty("display_name");
                if (displayNameProp != null)
                {
                    var displayNameValue = displayNameProp.GetValue(dynAttributes);
                    if (displayNameValue != null)
                        policy.DisplayName = displayNameValue.ToString() ?? string.Empty;
                }
                
                var stateProp = dynAttributes?.GetType().GetProperty("state");
                if (stateProp != null)
                {
                    var stateValue = stateProp.GetValue(dynAttributes);
                    if (stateValue != null)
                        policy.State = stateValue.ToString();
                }

                // For now, we'll skip conditions/grant_controls for non-JObject types
                // to avoid complex reflection handling
            }

            return policy;
        }

        private TerraformConditions ParseConditions(string conditionsBody)
        {
            var conditions = new TerraformConditions();

            // Parse applications block
            var applicationsMatch = Regex.Match(conditionsBody, @"applications\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}");
            if (applicationsMatch.Success)
            {
                conditions.Applications = ParseApplications(applicationsMatch.Groups[1].Value);
            }

            // Parse users block
            var usersMatch = Regex.Match(conditionsBody, @"users\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}");
            if (usersMatch.Success)
            {
                conditions.Users = ParseUsers(usersMatch.Groups[1].Value);
            }

            // Parse other conditions...
            conditions.ClientAppTypes = ParseStringArray(conditionsBody, "client_app_types");
            conditions.SignInRiskLevels = ParseStringArray(conditionsBody, "sign_in_risk_levels");
            conditions.UserRiskLevels = ParseStringArray(conditionsBody, "user_risk_levels");

            return conditions;
        }

        private TerraformConditions ParseConditionsFromState(dynamic conditions)
        {
            var result = new TerraformConditions();

            if (conditions?.applications != null)
            {
                result.Applications = ParseApplicationsFromState(conditions.applications);
            }

            if (conditions?.users != null)
            {
                result.Users = ParseUsersFromState(conditions.users);
            }

            if (conditions?.client_app_types != null)
            {
                result.ClientAppTypes = ParseStringArrayFromState(conditions.client_app_types);
            }

            return result;
        }

        private TerraformConditions ParseConditionsFromStateToken(Newtonsoft.Json.Linq.JToken conditionsToken)
        {
            var result = new TerraformConditions();

            var applicationsToken = conditionsToken["applications"];
            if (applicationsToken != null)
            {
                result.Applications = ParseApplicationsFromStateToken(applicationsToken);
            }

            var usersToken = conditionsToken["users"];
            if (usersToken != null)
            {
                result.Users = ParseUsersFromStateToken(usersToken);
            }

            var clientAppTypesToken = conditionsToken["client_app_types"];
            if (clientAppTypesToken != null)
            {
                result.ClientAppTypes = ParseStringArrayFromStateToken(clientAppTypesToken);
            }

            return result;
        }

        private TerraformApplications ParseApplications(string applicationsBody)
        {
            return new TerraformApplications
            {
                IncludeApplications = ParseStringArray(applicationsBody, "include_applications"),
                ExcludeApplications = ParseStringArray(applicationsBody, "exclude_applications"),
                IncludeUserActions = ParseStringArray(applicationsBody, "include_user_actions")
            };
        }

        private TerraformApplications ParseApplicationsFromState(dynamic applications)
        {
            return new TerraformApplications
            {
                IncludeApplications = ParseStringArrayFromState(applications?.include_applications),
                ExcludeApplications = ParseStringArrayFromState(applications?.exclude_applications),
                IncludeUserActions = ParseStringArrayFromState(applications?.include_user_actions)
            };
        }

        private TerraformApplications ParseApplicationsFromStateToken(Newtonsoft.Json.Linq.JToken applicationsToken)
        {
            return new TerraformApplications
            {
                IncludeApplications = ParseStringArrayFromStateToken(applicationsToken["include_applications"]),
                ExcludeApplications = ParseStringArrayFromStateToken(applicationsToken["exclude_applications"]),
                IncludeUserActions = ParseStringArrayFromStateToken(applicationsToken["include_user_actions"])
            };
        }

        private TerraformUsers ParseUsers(string usersBody)
        {
            return new TerraformUsers
            {
                IncludeUsers = ParseStringArray(usersBody, "include_users"),
                ExcludeUsers = ParseStringArray(usersBody, "exclude_users"),
                IncludeGroups = ParseStringArray(usersBody, "include_groups"),
                ExcludeGroups = ParseStringArray(usersBody, "exclude_groups"),
                IncludeRoles = ParseStringArray(usersBody, "include_roles"),
                ExcludeRoles = ParseStringArray(usersBody, "exclude_roles")
            };
        }

        private TerraformUsers ParseUsersFromState(dynamic users)
        {
            return new TerraformUsers
            {
                IncludeUsers = ParseStringArrayFromState(users?.include_users),
                ExcludeUsers = ParseStringArrayFromState(users?.exclude_users),
                IncludeGroups = ParseStringArrayFromState(users?.include_groups),
                ExcludeGroups = ParseStringArrayFromState(users?.exclude_groups),
                IncludeRoles = ParseStringArrayFromState(users?.include_roles),
                ExcludeRoles = ParseStringArrayFromState(users?.exclude_roles)
            };
        }

        private TerraformUsers ParseUsersFromStateToken(Newtonsoft.Json.Linq.JToken usersToken)
        {
            return new TerraformUsers
            {
                IncludeUsers = ParseStringArrayFromStateToken(usersToken["include_users"]),
                ExcludeUsers = ParseStringArrayFromStateToken(usersToken["exclude_users"]),
                IncludeGroups = ParseStringArrayFromStateToken(usersToken["include_groups"]),
                ExcludeGroups = ParseStringArrayFromStateToken(usersToken["exclude_groups"]),
                IncludeRoles = ParseStringArrayFromStateToken(usersToken["include_roles"]),
                ExcludeRoles = ParseStringArrayFromStateToken(usersToken["exclude_roles"])
            };
        }

        private TerraformGrantControls ParseGrantControls(string grantControlsBody)
        {
            return new TerraformGrantControls
            {
                Operator = ParseStringValue(grantControlsBody, "operator"),
                BuiltInControls = ParseStringArray(grantControlsBody, "built_in_controls"),
                CustomAuthenticationFactors = ParseStringArray(grantControlsBody, "custom_authentication_factors"),
                TermsOfUse = ParseStringArray(grantControlsBody, "terms_of_use")
            };
        }

        private TerraformGrantControls ParseGrantControlsFromState(dynamic grantControls)
        {
            return new TerraformGrantControls
            {
                Operator = grantControls?.@operator,
                BuiltInControls = ParseStringArrayFromState(grantControls?.built_in_controls),
                CustomAuthenticationFactors = ParseStringArrayFromState(grantControls?.custom_authentication_factors),
                TermsOfUse = ParseStringArrayFromState(grantControls?.terms_of_use)
            };
        }

        private TerraformGrantControls ParseGrantControlsFromStateToken(Newtonsoft.Json.Linq.JToken grantControlsToken)
        {
            return new TerraformGrantControls
            {
                Operator = grantControlsToken.Value<string>("operator"),
                BuiltInControls = ParseStringArrayFromStateToken(grantControlsToken["built_in_controls"]),
                CustomAuthenticationFactors = ParseStringArrayFromStateToken(grantControlsToken["custom_authentication_factors"]),
                TermsOfUse = ParseStringArrayFromStateToken(grantControlsToken["terms_of_use"])
            };
        }

        private TerraformSessionControls ParseSessionControls(string sessionControlsBody)
        {
            // This is a simplified implementation - in a real scenario, you'd parse nested blocks
            return new TerraformSessionControls();
        }

        private TerraformSessionControls ParseSessionControlsFromState(dynamic sessionControls)
        {
            return new TerraformSessionControls();
        }

        private TerraformSessionControls ParseSessionControlsFromStateToken(Newtonsoft.Json.Linq.JToken sessionControlsToken)
        {
            return new TerraformSessionControls();
        }

        private TerraformVariable ParseVariable(string name, string variableBody)
        {
            return new TerraformVariable
            {
                Name = name,
                Type = ParseStringValue(variableBody, "type"),
                Description = ParseStringValue(variableBody, "description"),
                Sensitive = ParseBoolValue(variableBody, "sensitive"),
                DefaultValue = ParseVariableDefaultValue(variableBody)
            };
        }
        
        private object? ParseVariableDefaultValue(string variableBody)
        {
            // Try to parse default value - could be quoted string, unquoted value, or complex structure
            var quotedPattern = @"default\s*=\s*""([^""]*)""";
            var quotedMatch = Regex.Match(variableBody, quotedPattern);
            if (quotedMatch.Success)
            {
                return quotedMatch.Groups[1].Value;
            }
            
            // Try list/array pattern
            var listPattern = @"default\s*=\s*\[(.*?)\]";
            var listMatch = Regex.Match(variableBody, listPattern, RegexOptions.Singleline);
            if (listMatch.Success)
            {
                var listContent = listMatch.Groups[1].Value;
                // Simple parsing - split by comma and clean up quotes
                var items = listContent.Split(',')
                    .Select(item => item.Trim().Trim('"'))
                    .Where(item => !string.IsNullOrEmpty(item))
                    .ToList();
                return items;
            }
            
            // Try simple unquoted values (numbers, booleans, etc.)
            var simplePattern = @"default\s*=\s*([^\s\n]+)";
            var simpleMatch = Regex.Match(variableBody, simplePattern);
            if (simpleMatch.Success)
            {
                var value = simpleMatch.Groups[1].Value;
                if (bool.TryParse(value, out bool boolValue))
                    return boolValue;
                if (int.TryParse(value, out int intValue))
                    return intValue;
                return value; // Return as string if can't parse as other types
            }
            
            return null;
        }

        private List<TerraformLocal> ParseLocals(string localsBody)
        {
            var locals = new List<TerraformLocal>();
            
            // Use a more sophisticated parsing approach to handle nested structures
            var lines = localsBody.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var i = 0;
            
            while (i < lines.Length)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("#"))
                {
                    i++;
                    continue;
                }
                
                // Look for key = value pattern
                var equalIndex = line.IndexOf('=');
                if (equalIndex > 0)
                {
                    var key = line.Substring(0, equalIndex).Trim();
                    var valueStart = line.Substring(equalIndex + 1).Trim();
                    
                    // If value starts with {, need to find the matching }
                    if (valueStart.StartsWith("{"))
                    {
                        var braceCount = 1;
                        var value = valueStart;
                        i++;
                        
                        while (i < lines.Length && braceCount > 0)
                        {
                            var nextLine = lines[i];
                            value += "\n" + nextLine;
                            
                            foreach (char c in nextLine)
                            {
                                if (c == '{') braceCount++;
                                if (c == '}') braceCount--;
                            }
                            i++;
                        }
                        
                        locals.Add(new TerraformLocal
                        {
                            Name = key,
                            Value = value
                        });
                    }
                    else
                    {
                        locals.Add(new TerraformLocal
                        {
                            Name = key,
                            Value = valueStart
                        });
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }

            return locals;
        }

        private List<string>? ParseStringArray(string content, string attributeName)
        {
            var pattern = $@"{attributeName}\s*=\s*\[(.*?)\]";
            var match = Regex.Match(content, pattern, RegexOptions.Singleline);
            
            if (!match.Success) return null;

            var arrayContent = match.Groups[1].Value;
            var values = Regex.Matches(arrayContent, @"""([^""]*)""")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList();

            return values.Any() ? values : null;
        }

        private List<string>? ParseStringArrayFromState(dynamic array)
        {
            if (array == null) return null;
            
            var result = new List<string>();
            foreach (var item in array)
            {
                if (item != null)
                {
                    result.Add(item.ToString());
                }
            }
            
            return result.Any() ? result : null;
        }

        private List<string>? ParseStringArrayFromStateToken(Newtonsoft.Json.Linq.JToken? arrayToken)
        {
            if (arrayToken == null) return null;
            
            var result = new List<string>();
            
            if (arrayToken.Type == Newtonsoft.Json.Linq.JTokenType.Array)
            {
                foreach (var item in arrayToken.Children())
                {
                    if (item != null)
                        result.Add(item.ToString());
                }
            }
            
            return result.Any() ? result : null;
        }

        private string? ParseStringValue(string content, string attributeName)
        {
            // First try quoted strings
            var quotedPattern = $@"{attributeName}\s*=\s*""([^""]*)""";
            var quotedMatch = Regex.Match(content, quotedPattern);
            if (quotedMatch.Success)
            {
                return quotedMatch.Groups[1].Value;
            }
            
            // Then try unquoted values (for types like string, bool, numbers, etc.)
            var unquotedPattern = $@"{attributeName}\s*=\s*([^\s\n]+)";
            var unquotedMatch = Regex.Match(content, unquotedPattern);
            if (unquotedMatch.Success)
            {
                return unquotedMatch.Groups[1].Value;
            }
            
            return null;
        }

        private bool ParseBoolValue(string content, string attributeName)
        {
            var pattern = $@"{attributeName}\s*=\s*(true|false)";
            var match = Regex.Match(content, pattern);
            return match.Success && match.Groups[1].Value == "true";
        }

        private List<(string resourceName, string resourceBody)> ExtractResourceBlocks(string content, string resourceType)
        {
            var results = new List<(string, string)>();
            var pattern = $@"resource\s+""{resourceType}""\s+""([^""]+)""\s*\{{";
            var matches = Regex.Matches(content, pattern);

            foreach (Match match in matches)
            {
                var resourceName = match.Groups[1].Value;
                var startIndex = match.Index + match.Length - 1; // Position of the opening brace
                
                // Find the matching closing brace
                var braceCount = 1;
                var currentIndex = startIndex + 1;
                
                while (currentIndex < content.Length && braceCount > 0)
                {
                    var currentChar = content[currentIndex];
                    if (currentChar == '{')
                        braceCount++;
                    else if (currentChar == '}')
                        braceCount--;
                    currentIndex++;
                }
                
                if (braceCount == 0)
                {
                    var resourceBody = content.Substring(startIndex + 1, currentIndex - startIndex - 2);
                    results.Add((resourceName, resourceBody));
                }
                else
                {
                    _errors.Add($"Unmatched braces in resource '{resourceName}'");
                }
            }

            return results;
        }

        private List<(string variableName, string variableBody)> ExtractVariableBlocks(string content)
        {
            var results = new List<(string, string)>();
            var pattern = @"variable\s+""([^""]+)""\s*\{";
            var matches = Regex.Matches(content, pattern);

            foreach (Match match in matches)
            {
                var variableName = match.Groups[1].Value;
                var startIndex = match.Index + match.Length - 1;
                
                var braceCount = 1;
                var currentIndex = startIndex + 1;
                
                while (currentIndex < content.Length && braceCount > 0)
                {
                    var currentChar = content[currentIndex];
                    if (currentChar == '{')
                        braceCount++;
                    else if (currentChar == '}')
                        braceCount--;
                    currentIndex++;
                }
                
                if (braceCount == 0)
                {
                    var variableBody = content.Substring(startIndex + 1, currentIndex - startIndex - 2);
                    results.Add((variableName, variableBody));
                }
            }

            return results;
        }

        private List<string> ExtractLocalsBlocks(string content)
        {
            var results = new List<string>();
            var pattern = @"locals\s*\{";
            var matches = Regex.Matches(content, pattern);

            foreach (Match match in matches)
            {
                var startIndex = match.Index + match.Length - 1;
                
                var braceCount = 1;
                var currentIndex = startIndex + 1;
                
                while (currentIndex < content.Length && braceCount > 0)
                {
                    var currentChar = content[currentIndex];
                    if (currentChar == '{')
                        braceCount++;
                    else if (currentChar == '}')
                        braceCount--;
                    currentIndex++;
                }
                
                if (braceCount == 0)
                {
                    var localsBody = content.Substring(startIndex + 1, currentIndex - startIndex - 2);
                    results.Add(localsBody);
                }
            }

            return results;
        }
    }
}
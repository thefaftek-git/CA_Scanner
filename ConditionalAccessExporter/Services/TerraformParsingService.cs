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
                    await ParseHclFileAsync(content, result);
                }
                else if (filePath.EndsWith(".tfstate", StringComparison.OrdinalIgnoreCase) || 
                         filePath.EndsWith(".tfstate.backup", StringComparison.OrdinalIgnoreCase))
                {
                    await ParseTerraformStateAsync(content, result);
                }
                else
                {
                    _warnings.Add($"Unknown file type for: {filePath}. Attempting HCL parsing.");
                    await ParseHclFileAsync(content, result);
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

        private async Task ParseHclFileAsync(string content, TerraformParseResult result)
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

            await Task.CompletedTask;
        }

        private async Task ParseTerraformStateAsync(string content, TerraformParseResult result)
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
                                    // Handle the dynamic type issue - JObject needs special handling
                                    var attributesObj = instance.attributes;
                                    // Convert JObject to string and back to dynamic for proper deserialization
                                    string attributesJson = attributesObj.ToString();
                                    dynamic attributesDynamic = JsonConvert.DeserializeObject(attributesJson)!;
                                    var policy = ParsePolicyFromTerraformState(resource.name, attributesDynamic);
                                    result.Policies.Add(policy);
                                    _logs.Add($"Parsed policy from state: {resource.name}");
                                }
                                catch (Exception ex)
                                {
                                    _errors.Add($"Error parsing policy from state '{resource.name}': {ex.Message}");
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

            await Task.CompletedTask;
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

        private TerraformConditionalAccessPolicy ParsePolicyFromTerraformState(string resourceName, dynamic attributes)
        {
            var policy = new TerraformConditionalAccessPolicy { ResourceName = resourceName };
            
            if (attributes?.display_name != null)
                policy.DisplayName = attributes.display_name.ToString();
            
            if (attributes?.state != null)
                policy.State = attributes.state.ToString();

            // Parse conditions from state
            if (attributes?.conditions != null)
            {
                policy.Conditions = ParseConditionsFromState(attributes.conditions);
            }

            // Parse grant_controls from state
            if (attributes?.grant_controls != null)
            {
                policy.GrantControls = ParseGrantControlsFromState(attributes.grant_controls);
            }

            // Parse session_controls from state
            if (attributes?.session_controls != null)
            {
                policy.SessionControls = ParseSessionControlsFromState(attributes.session_controls);
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

        private TerraformSessionControls ParseSessionControls(string sessionControlsBody)
        {
            // This is a simplified implementation - in a real scenario, you'd parse nested blocks
            return new TerraformSessionControls();
        }

        private TerraformSessionControls ParseSessionControlsFromState(dynamic sessionControls)
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
                result.Add(item.ToString());
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
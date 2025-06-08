using Newtonsoft.Json;
using System.Text;
using ConditionalAccessExporter.Models;

namespace ConditionalAccessExporter.Services
{
    public class TemplateService
    {
        private readonly string _templateBaseDirectory;

        public TemplateService(string? templateBaseDirectory = null)
        {
            if (templateBaseDirectory != null)
            {
                _templateBaseDirectory = templateBaseDirectory;
            }
            else
            {
                // Try to find reference-templates directory in common locations
                var possiblePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "reference-templates"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reference-templates"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "reference-templates"),
                    Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "reference-templates")
                };

                _templateBaseDirectory = possiblePaths.FirstOrDefault(Directory.Exists) 
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "reference-templates");
            }
        }

        public async Task<List<TemplateInfo>> ListAvailableTemplatesAsync()
        {
            var templates = new List<TemplateInfo>();
            
            if (!Directory.Exists(_templateBaseDirectory))
            {
                return templates;
            }

            var templateFiles = Directory.GetFiles(_templateBaseDirectory, "*.json", SearchOption.AllDirectories);
            
            foreach (var file in templateFiles)
            {
                var relativePath = Path.GetRelativePath(_templateBaseDirectory, file)
                    .Replace(Path.DirectorySeparatorChar, '/');
                var templateName = relativePath.Replace(".json", "");
                var category = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '/') ?? "unknown";
                
                // Try to get description from the JSON file
                var description = "No description available";
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    dynamic? policy = JsonConvert.DeserializeObject(content);
                    if (policy?.DisplayName != null)
                    {
                        description = policy.DisplayName.ToString();
                    }
                }
                catch
                {
                    // Use default description if can't read file
                }

                templates.Add(new TemplateInfo
                {
                    Name = templateName,
                    Category = category,
                    Description = description,
                    FilePath = file
                });
            }

            return templates.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();
        }

        public async Task<TemplateCreationResult> CreateTemplateAsync(string templateName, string outputDirectory)
        {
            var result = new TemplateCreationResult();
            
            try
            {
                var templatePath = Path.Combine(_templateBaseDirectory, $"{templateName}.json");
                
                if (!File.Exists(templatePath))
                {
                    result.Errors.Add($"Template '{templateName}' not found at {templatePath}");
                    return result;
                }

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var outputPath = Path.Combine(outputDirectory, $"{Path.GetFileName(templateName)}.json");
                
                // Read template and copy to output directory
                var templateContent = await File.ReadAllTextAsync(templatePath);
                await File.WriteAllTextAsync(outputPath, templateContent);
                
                result.Success = true;
                result.OutputPath = outputPath;
                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error creating template '{templateName}': {ex.Message}");
                return result;
            }
        }

        public async Task<BaselineCreationResult> CreateBaselineSetAsync(string outputDirectory)
        {
            var result = new BaselineCreationResult();
            
            try
            {
                var baselineTemplates = new[]
                {
                    "basic/require-mfa-all-users",
                    "basic/block-legacy-auth",
                    "basic/require-compliant-device",
                    "role-based/admin-protection",
                    "application/office365-protection"
                };

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var success = true;
                foreach (var template in baselineTemplates)
                {
                    var templateResult = await CreateTemplateAsync(template, outputDirectory);
                    if (templateResult.Success)
                    {
                        result.CreatedFiles.Add(templateResult.OutputPath);
                    }
                    else
                    {
                        success = false;
                        result.Errors.AddRange(templateResult.Errors);
                    }
                    result.Warnings.AddRange(templateResult.Warnings);
                }

                result.Success = success;
                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error creating baseline template set: {ex.Message}");
                return result;
            }
        }

        public async Task<TemplateValidationResult> ValidateTemplateAsync(string templatePath)
        {
            var result = new TemplateValidationResult();
            
            try
            {
                if (!File.Exists(templatePath))
                {
                    result.Errors.Add($"Template file not found: {templatePath}");
                    return result;
                }

                var content = await File.ReadAllTextAsync(templatePath);
                
                // Try to parse as JSON
                var jsonObject = JsonConvert.DeserializeObject(content);
                
                if (jsonObject == null)
                {
                    result.Errors.Add($"Template contains invalid JSON: {templatePath}");
                    return result;
                }

                // Basic validation for conditional access policy structure
                dynamic? policy = jsonObject;
                if (policy?.DisplayName == null)
                {
                    result.Errors.Add($"Template missing required 'DisplayName' field: {templatePath}");
                }

                if (policy?.Conditions == null)
                {
                    result.Errors.Add($"Template missing required 'Conditions' field: {templatePath}");
                }

                // Check for placeholder values that need to be replaced
                var contentLower = content.ToLower();
                if (contentLower.Contains("{{") && contentLower.Contains("}}"))
                {
                    result.Warnings.Add("Template contains placeholder values that need to be replaced before deployment");
                }

                result.IsValid = !result.Errors.Any();
                return result;
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"JSON parsing error in template {templatePath}: {ex.Message}");
                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error validating template {templatePath}: {ex.Message}");
                return result;
            }
        }

        public string GetTemplateDocumentation(string templateName)
        {
            var docPath = Path.Combine(_templateBaseDirectory, $"{templateName}.md");
            
            if (File.Exists(docPath))
            {
                return File.ReadAllText(docPath);
            }

            // Return basic template info if no documentation file exists
            return $"Template: {templateName}\nNo additional documentation available.";
        }
    }
}

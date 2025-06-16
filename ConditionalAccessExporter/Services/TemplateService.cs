using Newtonsoft.Json;
using System.Text;
using ConditionalAccessExporter.Models;
using System.Security;

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
                var possiblePaths = new List<string>();
                
                // Safe method to get current directory
                try
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    possiblePaths.Add(GetSanitizedPath(currentDir, "reference-templates"));
                    possiblePaths.Add(GetSanitizedPath(currentDir, "..", "reference-templates"));
                }
                catch (SecurityException)
                {
                    // Ignore if GetCurrentDirectory fails due to security restrictions
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore if GetCurrentDirectory fails due to access restrictions
                }
                catch (FileNotFoundException)
                {
                    // Ignore if GetCurrentDirectory fails in test environments
                }
                catch (DirectoryNotFoundException)
                {
                    // Ignore if GetCurrentDirectory fails in containerized environments
                }
                
                // Add other safe paths
                possiblePaths.Add(GetSanitizedPath(AppDomain.CurrentDomain.BaseDirectory, "reference-templates"));
                
                var assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    possiblePaths.Add(GetSanitizedPath(assemblyLocation, "reference-templates"));
                }
                
                // Use first existing directory or fallback to a temp directory
                _templateBaseDirectory = possiblePaths.FirstOrDefault(Directory.Exists) 
                    ?? GetSanitizedPath(Path.GetTempPath(), "reference-templates");
            }
        }

        public async Task<List<TemplateInfo>> ListAvailableTemplatesAsync(CancellationToken cancellationToken = default)
        {
            var templates = new List<TemplateInfo>();
            
            if (!Directory.Exists(_templateBaseDirectory))
            {
                return templates;
            }

            var templateFiles = Directory.GetFiles(_templateBaseDirectory, "*.json", SearchOption.AllDirectories);
            
            foreach (var file in templateFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var relativePath = Path.GetRelativePath(_templateBaseDirectory, file)
                    .Replace(Path.DirectorySeparatorChar, '/');
                var templateName = relativePath.Replace(".json", "");
                var category = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '/') ?? "unknown";
                
                // Try to get description from the JSON file
                var description = "No description available";
                try
                {
                    var content = await File.ReadAllTextAsync(file, cancellationToken);
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

        public async Task<TemplateCreationResult> CreateTemplateAsync(string templateName, string outputDirectory, CancellationToken cancellationToken = default)
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
                var templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);
                await File.WriteAllTextAsync(outputPath, templateContent, cancellationToken);
                
                result.Success = true;
                result.OutputPath = outputPath;
                return result;
            }
            catch (OperationCanceledException)
            {
                result.Errors.Add($"Operation was cancelled while creating template '{templateName}'");
                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error creating template '{templateName}': {ex.Message}");
                return result;
            }
        }

        public async Task<BaselineCreationResult> CreateBaselineSetAsync(string outputDirectory, CancellationToken cancellationToken = default)
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
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var templateResult = await CreateTemplateAsync(template, outputDirectory, cancellationToken);
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
            catch (OperationCanceledException)
            {
                result.Errors.Add("Operation was cancelled while creating baseline template set");
                return result;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error creating baseline template set: {ex.Message}");
                return result;
            }
        }

        public async Task<TemplateValidationResult> ValidateTemplateAsync(string templatePath, CancellationToken cancellationToken = default)
        {
            var result = new TemplateValidationResult();
            
            try
            {
                if (!File.Exists(templatePath))
                {
                    result.Errors.Add($"Template file not found: {templatePath}");
                    return result;
                }

                var content = await File.ReadAllTextAsync(templatePath, cancellationToken);
                
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
            catch (OperationCanceledException)
            {
                result.Errors.Add($"Operation was cancelled while validating template: {templatePath}");
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

        public async Task<string> GetTemplateDocumentationAsync(string templateName, CancellationToken cancellationToken = default)
        {
            var docPath = Path.Combine(_templateBaseDirectory, $"{templateName}.md");
            
            if (File.Exists(docPath))
            {
                return await File.ReadAllTextAsync(docPath, cancellationToken);
            }

            // Return basic template info if no documentation file exists
            return $"Template: {templateName}\nNo additional documentation available.";
        }

        /// <summary>
        /// Safely combines path components and validates the result to prevent path traversal attacks
        /// </summary>
        private static string GetSanitizedPath(params string[] pathComponents)
        {
            if (pathComponents == null || pathComponents.Length == 0)
                throw new ArgumentException("Path components cannot be null or empty", nameof(pathComponents));

            // Filter out null or empty components
            var validComponents = pathComponents.Where(p => !string.IsNullOrEmpty(p)).ToArray();
            if (validComponents.Length == 0)
                throw new ArgumentException("No valid path components provided", nameof(pathComponents));

            // Combine paths safely
            var combinedPath = Path.Join(validComponents);
            
            // Get the full path to normalize it and prevent traversal attacks
            var fullPath = Path.GetFullPath(combinedPath);
            
            // Additional validation to ensure the path doesn't contain dangerous sequences
            var normalizedPath = Path.GetFullPath(fullPath);
            if (normalizedPath.Contains("..") || normalizedPath.Contains("~"))
            {
                throw new InvalidOperationException("Path contains potentially dangerous sequences");
            }
            
            return normalizedPath;
        }
    }
}

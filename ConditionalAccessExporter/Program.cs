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
    // Simple logging helper to manage verbose versus quiet output
    public static class Logger
    {
        private static bool _quietMode = false;

        public static void SetQuietMode(bool quietMode)
        {
            _quietMode = quietMode;
        }

        public static void WriteInfo(string message)
        {
            if (!_quietMode)
            {
                Console.WriteLine(message);
            }
        }

        public static void WriteError(string message)
        {
            // Errors are always written regardless of quiet mode
            Console.WriteLine(message);
        }

        public static void WriteVerbose(string message, bool verbose = false)
        {
            if (!_quietMode && verbose)
            {
                Console.WriteLine(message);
            }
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
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

            // Terraform command
            var terraformCommand = new Command("terraform", "Convert Terraform conditional access policies to JSON");
            var terraformInputOption = new Option<string>(
                name: "--input",
                description: "Terraform file or directory path containing conditional access policies"
            ) { IsRequired = true };
            var terraformOutputOption = new Option<string>(
                name: "--output",
                description: "Output file path for converted JSON",
                getDefaultValue: () => $"TerraformConditionalAccessPolicies_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json"
            );
            var validateOption = new Option<bool>(
                name: "--validate",
                description: "Validate converted policies against Microsoft Graph schema",
                getDefaultValue: () => true
            );
            var verboseOption = new Option<bool>(
                name: "--verbose",
                description: "Enable verbose logging during conversion",
                getDefaultValue: () => false
            );

            terraformCommand.AddOption(terraformInputOption);
            terraformCommand.AddOption(terraformOutputOption);
            terraformCommand.AddOption(validateOption);
            terraformCommand.AddOption(verboseOption);
            terraformCommand.SetHandler(ConvertTerraformAsync, terraformInputOption, terraformOutputOption, validateOption, verboseOption);

            // JSON to Terraform command
            var jsonToTerraformCommand = new Command("json-to-terraform", "Convert JSON conditional access policies to Terraform HCL");
            var jsonInputOption = new Option<string>(
                name: "--input",
                description: "JSON file path containing conditional access policies"
            ) { IsRequired = true };
            var terraformOutputDirOption = new Option<string>(
                name: "--output-dir",
                description: "Output directory for generated Terraform files",
                getDefaultValue: () => "terraform-output"
            );
            var generateVariablesOption = new Option<bool>(
                name: "--generate-variables",
                description: "Generate variables.tf file for reusable configurations",
                getDefaultValue: () => true
            );
            var generateProviderOption = new Option<bool>(
                name: "--generate-provider",
                description: "Generate providers.tf file with version constraints",
                getDefaultValue: () => true
            );
            var separateFilesOption = new Option<bool>(
                name: "--separate-files",
                description: "Generate separate .tf file for each policy",
                getDefaultValue: () => false
            );
            var generateModuleOption = new Option<bool>(
                name: "--generate-module",
                description: "Generate Terraform module structure",
                getDefaultValue: () => false
            );
            var includeCommentsOption = new Option<bool>(
                name: "--include-comments",
                description: "Include descriptive comments in generated Terraform code",
                getDefaultValue: () => true
            );
            var providerVersionOption = new Option<string>(
                name: "--provider-version",
                description: "AzureAD provider version constraint",
                getDefaultValue: () => "~> 2.0"
            );

            jsonToTerraformCommand.AddOption(jsonInputOption);
            jsonToTerraformCommand.AddOption(terraformOutputDirOption);
            jsonToTerraformCommand.AddOption(generateVariablesOption);
            jsonToTerraformCommand.AddOption(generateProviderOption);
            jsonToTerraformCommand.AddOption(separateFilesOption);
            jsonToTerraformCommand.AddOption(generateModuleOption);
            jsonToTerraformCommand.AddOption(includeCommentsOption);
            jsonToTerraformCommand.AddOption(providerVersionOption);
            jsonToTerraformCommand.SetHandler(ConvertJsonToTerraformAsync, 
                jsonInputOption, 
                terraformOutputDirOption, 
                generateVariablesOption,
                generateProviderOption,
                separateFilesOption,
                generateModuleOption,
                includeCommentsOption,
                providerVersionOption);

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
                description: "Report formats to generate (space or comma-separated)",
                getDefaultValue: () => new[] { "console", "json", "html" }
            );
            reportFormatsOption.AllowMultipleArgumentsPerToken = true;
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
            var explainOption = new Option<bool>(
                name: "--explain",
                description: "Decode numeric values in console output with human-readable explanations",
                getDefaultValue: () => false
            );
            var exitOnDifferencesOption = new Option<bool>(
                name: "--exit-on-differences",
                description: "Return non-zero exit codes based on comparison results",
                getDefaultValue: () => false
            );
            var maxDifferencesOption = new Option<int?>(
                name: "--max-differences",
                description: "Fail if more than specified number of policies differ"
            );
            var failOnOption = new Option<string[]>(
                name: "--fail-on",
                description: "Fail on specific types of changes (comma or space-separated)",
                getDefaultValue: () => Array.Empty<string>()
            );
            failOnOption.AllowMultipleArgumentsPerToken = true;
            var ignoreOption = new Option<string[]>(
                name: "--ignore",
                description: "Ignore specific types of differences (comma or space-separated)",
                getDefaultValue: () => Array.Empty<string>()
            );
            ignoreOption.AllowMultipleArgumentsPerToken = true;
            var quietOption = new Option<bool>(
                name: "--quiet",
                description: "Minimal output for pipeline usage",
                getDefaultValue: () => false
            );

            compareCommand.AddOption(referenceDirectoryOption);
            compareCommand.AddOption(entraFileOption);
            compareCommand.AddOption(outputDirectoryOption);
            compareCommand.AddOption(reportFormatsOption);
            compareCommand.AddOption(matchingStrategyOption);
            compareCommand.AddOption(caseSensitiveOption);
            compareCommand.AddOption(explainOption);
            compareCommand.AddOption(exitOnDifferencesOption);
            compareCommand.AddOption(maxDifferencesOption);
            compareCommand.AddOption(failOnOption);
            compareCommand.AddOption(ignoreOption);
            compareCommand.AddOption(quietOption);

            compareCommand.SetHandler(async (context) =>
            {
                var referenceDirectory = context.ParseResult.GetValueForOption(referenceDirectoryOption);
                var entraFile = context.ParseResult.GetValueForOption(entraFileOption);
                var outputDirectory = context.ParseResult.GetValueForOption(outputDirectoryOption);
                var reportFormats = context.ParseResult.GetValueForOption(reportFormatsOption);
                var matchingStrategy = context.ParseResult.GetValueForOption(matchingStrategyOption);
                var caseSensitive = context.ParseResult.GetValueForOption(caseSensitiveOption);
                var explainValues = context.ParseResult.GetValueForOption(explainOption);
                var exitOnDifferences = context.ParseResult.GetValueForOption(exitOnDifferencesOption);
                var maxDifferences = context.ParseResult.GetValueForOption(maxDifferencesOption);
                var failOn = context.ParseResult.GetValueForOption(failOnOption);
                var ignore = context.ParseResult.GetValueForOption(ignoreOption);
                var quiet = context.ParseResult.GetValueForOption(quietOption);

                var exitCode = await ComparePoliciesAsync(
                    referenceDirectory!,
                    entraFile,
                    outputDirectory!,
                    reportFormats!,
                    matchingStrategy,
                    caseSensitive,
                    explainValues,
                    exitOnDifferences,
                    maxDifferences,
                    failOn!,
                    ignore!,
                    quiet);
                
                context.ExitCode = exitCode;
            });

            // Cross-format compare command
            var crossFormatCompareCommand = new Command("cross-compare", "Compare policies across different formats (JSON vs Terraform)");
            var sourceDirectoryOption = new Option<string>(
                name: "--source-dir",
                description: "Source directory containing policies (JSON or Terraform)"
            ) { IsRequired = true };
            var crossReferenceDirectoryOption = new Option<string>(
                name: "--reference-dir",
                description: "Reference directory containing policies (JSON or Terraform)"
            ) { IsRequired = true };
            var crossOutputDirectoryOption = new Option<string>(
                name: "--output-dir",
                description: "Output directory for cross-format comparison reports",
                getDefaultValue: () => "cross-format-reports"
            );
            var crossReportFormatsOption = new Option<string[]>(
                name: "--formats",
                description: "Report formats to generate (space or comma-separated)",
                getDefaultValue: () => new[] { "console", "json", "html", "markdown" }
            );
            crossReportFormatsOption.AllowMultipleArgumentsPerToken = true;
            var crossMatchingStrategyOption = new Option<string>(
                name: "--matching",
                description: "Strategy for matching policies (ByName, ById, SemanticSimilarity, CustomMapping)",
                getDefaultValue: () => "ByName"
            );
            var crossCaseSensitiveOption = new Option<bool>(
                name: "--case-sensitive",
                description: "Case sensitive policy name matching",
                getDefaultValue: () => false
            );
            var enableSemanticOption = new Option<bool>(
                name: "--enable-semantic",
                description: "Enable semantic equivalence checking",
                getDefaultValue: () => true
            );
            var similarityThresholdOption = new Option<double>(
                name: "--similarity-threshold",
                description: "Similarity threshold for semantic matching (0.0-1.0)",
                getDefaultValue: () => 0.8
            );

            crossFormatCompareCommand.AddOption(sourceDirectoryOption);
            crossFormatCompareCommand.AddOption(crossReferenceDirectoryOption);
            crossFormatCompareCommand.AddOption(crossOutputDirectoryOption);
            crossFormatCompareCommand.AddOption(crossReportFormatsOption);
            crossFormatCompareCommand.AddOption(crossMatchingStrategyOption);
            crossFormatCompareCommand.AddOption(crossCaseSensitiveOption);
            crossFormatCompareCommand.AddOption(enableSemanticOption);
            crossFormatCompareCommand.AddOption(similarityThresholdOption);

            crossFormatCompareCommand.SetHandler(CrossFormatComparePoliciesAsync,
                sourceDirectoryOption,
                crossReferenceDirectoryOption,
                crossOutputDirectoryOption,
                crossReportFormatsOption,
                crossMatchingStrategyOption,
                crossCaseSensitiveOption,
                enableSemanticOption,
                similarityThresholdOption);

            // Baseline command
            var baselineCommand = new Command("baseline", "Generate baseline reference policies from current tenant");
            var baselineOutputDirOption = new Option<string>(
                name: "--output-dir",
                description: "Directory to save baseline reference files",
                getDefaultValue: () => "reference-policies"
            );
            var anonymizeOption = new Option<bool>(
                name: "--anonymize",
                description: "Remove tenant-specific identifiers (IDs, timestamps, tenant references)",
                getDefaultValue: () => false
            );
            var filterEnabledOnlyOption = new Option<bool>(
                name: "--filter-enabled-only",
                description: "Export only enabled policies",
                getDefaultValue: () => false
            );
            var policyNamesOption = new Option<string[]>(
                name: "--policy-names",
                description: "Export specific policies by name (space or comma-separated)"
            );
            policyNamesOption.AllowMultipleArgumentsPerToken = true;

            baselineCommand.AddOption(baselineOutputDirOption);
            baselineCommand.AddOption(anonymizeOption);
            baselineCommand.AddOption(filterEnabledOnlyOption);
            baselineCommand.AddOption(policyNamesOption);
            baselineCommand.SetHandler(GenerateBaselineAsync,
                baselineOutputDirOption,
                anonymizeOption,
                filterEnabledOnlyOption,
                policyNamesOption);

            rootCommand.AddCommand(exportCommand);
            rootCommand.AddCommand(terraformCommand);
            rootCommand.AddCommand(jsonToTerraformCommand);
            rootCommand.AddCommand(compareCommand);
            rootCommand.AddCommand(crossFormatCompareCommand);
            rootCommand.AddCommand(baselineCommand);

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

        private static async Task<int> ConvertTerraformAsync(
            string inputPath,
            string outputPath,
            bool validate,
            bool verbose)
        {
            Console.WriteLine("Terraform to JSON Conversion");
            Console.WriteLine("============================");

            try
            {
                // Validate required input parameter
                if (string.IsNullOrEmpty(inputPath))
                {
                    Console.WriteLine("Error: Input path is required but was not provided.");
                    return 1;
                }
                
                Console.WriteLine("Converting Terraform policies...");

// Removed redundant null-or-empty check for inputPath
                
                var parsingService = new TerraformParsingService();
                var conversionService = new TerraformConversionService();

                Console.WriteLine($"Input path: {inputPath}");
                Console.WriteLine($"Output path: {outputPath}");
                Console.WriteLine($"Validation: {(validate ? "Enabled" : "Disabled")}");
                Console.WriteLine($"Verbose logging: {(verbose ? "Enabled" : "Disabled")}");
                Console.WriteLine();

                // Parse Terraform files
                Console.WriteLine("Parsing Terraform files...");
                TerraformParseResult parseResult;

                if (File.Exists(inputPath))
                {
                    parseResult = await parsingService.ParseTerraformFileAsync(inputPath);
                }
                else if (Directory.Exists(inputPath))
                {
                    parseResult = await parsingService.ParseTerraformDirectoryAsync(inputPath);
                }
                else
                {
                    Console.WriteLine($"Error: Input path '{inputPath}' not found.");
                    return 1;
                }

                if (parseResult.Errors.Any())
                {
                    Console.WriteLine("Parsing errors encountered:");
                    foreach (var error in parseResult.Errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                    return 1;
                }

                if (parseResult.Warnings.Any() && verbose)
                {
                    Console.WriteLine("Parsing warnings:");
                    foreach (var warning in parseResult.Warnings)
                    {
                        Console.WriteLine($"  - {warning}");
                    }
                }

                Console.WriteLine($"Found {parseResult.Policies.Count} conditional access policies");
                Console.WriteLine($"Found {parseResult.Variables.Count} variables");
                Console.WriteLine($"Found {parseResult.Locals.Count} locals");
                Console.WriteLine($"Found {parseResult.DataSources.Count} data sources");
                Console.WriteLine();

                if (!parseResult.Policies.Any())
                {
                    Console.WriteLine("No conditional access policies found to convert.");
                    return 0;
                }

                // Convert to Graph JSON format
                Console.WriteLine("Converting to Microsoft Graph JSON format...");
                var conversionResult = await conversionService.ConvertToGraphJsonAsync(parseResult);

                if (conversionResult.Errors.Any())
                {
                    Console.WriteLine("Conversion errors encountered:");
                    foreach (var error in conversionResult.Errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                }

                if (conversionResult.Warnings.Any() && verbose)
                {
                    Console.WriteLine("Conversion warnings:");
                    foreach (var warning in conversionResult.Warnings)
                    {
                        Console.WriteLine($"  - {warning}");
                    }
                }

                if (verbose && conversionResult.ConversionLog.Any())
                {
                    Console.WriteLine("Conversion log:");
                    foreach (var log in conversionResult.ConversionLog)
                    {
                        Console.WriteLine($"  - {log}");
                    }
                }

                // Validate if requested
                if (validate)
                {
                    Console.WriteLine("Validating converted policies...");
                    // Additional validation could be implemented here
                }

                // Serialize and save
                var json = JsonConvert.SerializeObject(conversionResult.ConvertedPolicies, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                });

                await File.WriteAllTextAsync(outputPath, json, Encoding.UTF8);

                Console.WriteLine();
                Console.WriteLine("Conversion Summary:");
                Console.WriteLine("==================");
                Console.WriteLine($"Successful conversions: {conversionResult.SuccessfulConversions}");
                Console.WriteLine($"Failed conversions: {conversionResult.FailedConversions}");
                Console.WriteLine($"Output file: {outputPath}");
                Console.WriteLine($"File size: {new FileInfo(outputPath).Length / 1024.0:F2} KB");
                Console.WriteLine("Terraform conversion completed successfully!");

                // Return failure code if any conversions failed
                return conversionResult.FailedConversions > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
                return 1;
            }
        }

        private static async Task<int> ConvertJsonToTerraformAsync(
            string inputPath,
            string outputDirectory,
            bool generateVariables,
            bool generateProvider,
            bool separateFiles,
            bool generateModule,
            bool includeComments,
            string providerVersion)
        {
            Console.WriteLine("JSON to Terraform Conversion");
            Console.WriteLine("============================");

            try
            {
                // Validate required input parameter
                if (string.IsNullOrEmpty(inputPath))
                {
                    Console.WriteLine("Error: Input file is required but was not provided.");
                    return 1;
                }
                
                Console.WriteLine($"Input file: {inputPath}");
                Console.WriteLine($"Output directory: {outputDirectory}");
                Console.WriteLine($"Generate variables: {(generateVariables ? "Yes" : "No")}");
                Console.WriteLine($"Generate provider: {(generateProvider ? "Yes" : "No")}");
                Console.WriteLine($"Separate files: {(separateFiles ? "Yes" : "No")}");
                Console.WriteLine($"Generate module: {(generateModule ? "Yes" : "No")}");
                Console.WriteLine($"Include comments: {(includeComments ? "Yes" : "No")}");
                Console.WriteLine($"Provider version: {providerVersion}");
                Console.WriteLine();
                
                Console.WriteLine("Converting JSON to Terraform HCL...");
                
                var jsonToTerraformService = new JsonToTerraformService();

                if (!File.Exists(inputPath))
                {
                    Console.WriteLine($"Error: Input file '{inputPath}' not found.");
                    return 1;
                }

                var options = new TerraformOutputOptions
                {
                    GenerateVariables = generateVariables,
                    GenerateProviderConfig = generateProvider,
                    SeparateFilePerPolicy = separateFiles,
                    GenerateModuleStructure = generateModule,
                    IncludeComments = includeComments,
                    OutputDirectory = outputDirectory,
                    ProviderVersion = providerVersion
                };

                var result = await jsonToTerraformService.ConvertJsonToTerraformAsync(inputPath, options);

                if (result.Errors.Any())
                {
                    Console.WriteLine("Conversion errors encountered:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                    return 1;
                }

                if (result.Warnings.Any())
                {
                    Console.WriteLine("Conversion warnings:");
                    foreach (var warning in result.Warnings)
                    {
                        Console.WriteLine($"  - {warning}");
                    }
                }

                if (result.ConversionLog.Any())
                {
                    Console.WriteLine("Conversion log:");
                    foreach (var log in result.ConversionLog)
                    {
                        Console.WriteLine($"  - {log}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Conversion Summary:");
                Console.WriteLine("==================");
                Console.WriteLine($"Successful conversions: {result.SuccessfulConversions}");
                Console.WriteLine($"Failed conversions: {result.FailedConversions}");
                Console.WriteLine($"Output directory: {result.OutputPath}");
                Console.WriteLine($"Generated files: {result.GeneratedFiles.Count}");
                
                if (result.GeneratedFiles.Any())
                {
                    Console.WriteLine("Generated files:");
                    foreach (var file in result.GeneratedFiles)
                    {
                        Console.WriteLine($"  - {Path.GetFileName(file)}");
                    }
                }

                Console.WriteLine("JSON to Terraform conversion completed successfully!");

                return result.FailedConversions > 0 ? 1 : 0;
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
            bool caseSensitive,
            bool explainValues,
            bool exitOnDifferences,
            int? maxDifferences,
            string[] failOn,
            string[] ignore,
            bool quiet)
        {
            Console.WriteLine("Conditional Access Policy Comparison");
            Console.WriteLine("====================================");

            try
            {
                // Validate reference directory is provided
                if (string.IsNullOrEmpty(referenceDirectory))
                {
                    Logger.WriteError("Error: Reference directory is required but was not provided.");
                    return (int)ExitCode.Error;
                }

                // Validate reference directory exists
                if (!Directory.Exists(referenceDirectory))
                {
                    Logger.WriteError($"Error: Reference directory '{referenceDirectory}' not found.");
                    return (int)ExitCode.Error;
                }
                
                // Handle comma-separated formats in addition to space-separated ones
                reportFormats = ProcessReportFormats(reportFormats);
                
                // Process CI/CD options
                var cicdOptions = new CiCdOptions
                {
                    ExitOnDifferences = exitOnDifferences,
                    MaxDifferences = maxDifferences,
                    FailOnChangeTypes = ProcessCommaSeparatedArray(failOn),
                    IgnoreChangeTypes = ProcessCommaSeparatedArray(ignore),
                    QuietMode = quiet
                };

                // Set logger quiet mode based on CI/CD options
                Logger.SetQuietMode(quiet);

                Logger.WriteInfo($"Reference directory: {referenceDirectory}");
                
                if (!string.IsNullOrEmpty(entraFile))
                {
                    // Validate Entra file exists if specified
                    if (!File.Exists(entraFile))
                    {
                        Logger.WriteError($"Error: Entra file '{entraFile}' not found.");
                        return (int)ExitCode.Error;
                    }
                    Logger.WriteInfo($"Entra file: {entraFile}");
                }
                else
                {
                    Logger.WriteInfo("Entra file: <fetching from live Entra ID>");
                }
                    
                Logger.WriteInfo($"Output directory: {outputDirectory}");
                Logger.WriteInfo($"Report formats: {string.Join(", ", reportFormats)}");
                Logger.WriteInfo($"Matching strategy: {matchingStrategy}");
                Logger.WriteInfo($"Case sensitivity: {(caseSensitive ? "On" : "Off")}");
                Logger.WriteInfo($"Explain numeric values: {(explainValues ? "On" : "Off")}");
                
                if (exitOnDifferences)
                {
                    Logger.WriteInfo("CI/CD Mode: Enabled");
                    if (maxDifferences.HasValue)
                        Logger.WriteInfo($"Max differences threshold: {maxDifferences.Value}");
                    if (cicdOptions.FailOnChangeTypes.Any())
                        Logger.WriteInfo($"Fail on change types: {string.Join(", ", cicdOptions.FailOnChangeTypes)}");
                    if (cicdOptions.IgnoreChangeTypes.Any())
                        Logger.WriteInfo($"Ignore change types: {string.Join(", ", cicdOptions.IgnoreChangeTypes)}");
                }
                Logger.WriteInfo("");
                
                Logger.WriteInfo("Comparing policies...");
                
                object entraExport;

                if (!string.IsNullOrEmpty(entraFile))
                {
                    Logger.WriteInfo($"Loading Entra policies from file: {entraFile}");
                    if (!File.Exists(entraFile))
                    {
                        Logger.WriteError($"Error: Entra file '{entraFile}' not found.");
                        return (int)ExitCode.Error;
                    }

                    var fileContent = await File.ReadAllTextAsync(entraFile);
                    entraExport = JsonConvert.DeserializeObject(fileContent) ?? new object();
                }
                else
                {
                    if (!quiet) Console.WriteLine("Fetching live Entra policies...");
                    entraExport = await FetchEntraPoliciesAsync();
                }

                var matchingOptions = new MatchingOptions
                {
                    Strategy = matchingStrategy,
                    CaseSensitive = caseSensitive
                };

                var comparisonService = new PolicyComparisonService();
                var result = await comparisonService.CompareAsync(entraExport, referenceDirectory, matchingOptions);

                // Perform CI/CD analysis
                var cicdAnalysisService = new CiCdAnalysisService();
                var analysis = cicdAnalysisService.AnalyzeComparison(result, cicdOptions);

                // Add pipeline-json format if needed
                var finalReportFormats = reportFormats.ToList();
                if (reportFormats.Contains("pipeline-json"))
                {
                    var pipelineOutput = cicdAnalysisService.GeneratePipelineOutput(analysis, result);
                    var pipelineJson = JsonConvert.SerializeObject(pipelineOutput, Formatting.Indented);
                    
                    // Ensure output directory exists
                    Directory.CreateDirectory(outputDirectory);
                    var pipelineOutputPath = Path.Combine(outputDirectory, "pipeline-output.json");
                    await File.WriteAllTextAsync(pipelineOutputPath, pipelineJson);
                    
                    if (!quiet)
                    {
                        Console.WriteLine($"Pipeline output written to: {pipelineOutputPath}");
                    }
                    
                    // Remove from formats list as it's handled separately
                    finalReportFormats.Remove("pipeline-json");
                }

                // Generate standard reports
                if (finalReportFormats.Any())
                {
                    var reportService = new ReportGenerationService();
                    await reportService.GenerateReportsAsync(result, outputDirectory, finalReportFormats, explainValues, includeJsonMetadata: true);
                }

                // Output results based on mode
                if (quiet)
                {
                    // Minimal output for pipelines
                    if (analysis.CriticalDifferences > 0)
                    {
                        Console.WriteLine($"CRITICAL: {analysis.CriticalDifferences} critical differences found");
                    }
                    else if (analysis.TotalDifferences > 0)
                    {
                        Console.WriteLine($"WARNING: {analysis.TotalDifferences} differences found");
                    }
                    else
                    {
                        Console.WriteLine("SUCCESS: No differences found");
                    }
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Comparison Results Summary:");
                    Console.WriteLine("==========================");
                    Console.WriteLine($"Total policies compared: {result.Summary.TotalEntraPolicies}");
                    Console.WriteLine($"Policies with differences: {result.Summary.PoliciesWithDifferences}");
                    Console.WriteLine($"Critical differences: {analysis.CriticalDifferences}");
                    Console.WriteLine($"Non-critical differences: {analysis.NonCriticalDifferences}");
                    
                    if (analysis.CriticalPolicies.Any())
                    {
                        Console.WriteLine($"Policies with critical changes: {string.Join(", ", analysis.CriticalPolicies)}");
                    }
                    
                    Console.WriteLine($"Status: {analysis.Status}");
                    Console.WriteLine("Comparison completed successfully!");
                }

                return analysis.ExitCode;
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
                return (int)ExitCode.Error;
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

        private static async Task<int> CrossFormatComparePoliciesAsync(
            string sourceDirectory,
            string referenceDirectory,
            string outputDirectory,
            string[] reportFormats,
            string matchingStrategy,
            bool caseSensitive,
            bool enableSemantic,
            double similarityThreshold)
        {
            Console.WriteLine("Cross-Format Policy Comparison");
            Console.WriteLine("============================");
            
            try
            {
                // Validate source directory exists
                if (string.IsNullOrEmpty(sourceDirectory))
                {
                    Console.WriteLine("Error: Source directory is required but was not provided.");
                    return 1;
                }

                // Validate reference directory exists
                if (string.IsNullOrEmpty(referenceDirectory))
                {
                    Console.WriteLine("Error: Reference directory is required but was not provided.");
                    return 1;
                }
                
                // Handle comma-separated formats in addition to space-separated ones
                reportFormats = ProcessReportFormats(reportFormats);
                
                Console.WriteLine($"Source directory: {sourceDirectory}");
                Console.WriteLine($"Reference directory: {referenceDirectory}");
                Console.WriteLine($"Output directory: {outputDirectory}");
                Console.WriteLine($"Report formats: {string.Join(", ", reportFormats)}");
                Console.WriteLine($"Matching strategy: {matchingStrategy}");
                Console.WriteLine($"Case sensitivity: {(caseSensitive ? "On" : "Off")}");
                Console.WriteLine($"Semantic comparison: {(enableSemantic ? "Enabled" : "Disabled")}");
                Console.WriteLine($"Similarity threshold: {similarityThreshold}");
                Console.WriteLine();
                
                Console.WriteLine("Cross-comparing policies...");

                // Validate source directory exists
                if (!Directory.Exists(sourceDirectory))
                {
                    Console.WriteLine($"Error: Source directory '{sourceDirectory}' not found.");
                    return 1;
                }

                // Validate reference directory exists
                if (!Directory.Exists(referenceDirectory))
                {
                    Console.WriteLine($"Error: Reference directory '{referenceDirectory}' not found.");
                    return 1;
                }
                
                // Create output directory if it doesn't exist
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Initialize services
                var jsonComparisonService = new PolicyComparisonService();
                var terraformParsingService = new TerraformParsingService();
                var terraformConversionService = new TerraformConversionService();
                var crossFormatService = new CrossFormatPolicyComparisonService(
                    jsonComparisonService,
                    terraformParsingService,
                    terraformConversionService);
                var reportService = new CrossFormatReportGenerationService();

                Console.WriteLine("Services initialized successfully.");
                
                // Configure matching options
                var matchingOptions = new CrossFormatMatchingOptions
                {
                    Strategy = Enum.Parse<CrossFormatMatchingStrategy>(matchingStrategy, true),
                    CaseSensitive = caseSensitive,
                    EnableSemanticComparison = enableSemantic,
                    SemanticSimilarityThreshold = similarityThreshold
                };
                
                Console.WriteLine("Starting cross-format comparison...");
                
                try
                {
                    // Run the comparison
                    var comparisonResult = await crossFormatService.CompareAsync(sourceDirectory, referenceDirectory, matchingOptions);
                    
                    // Generate reports
                    foreach (var format in reportFormats)
                    {
                        try
                        {
                            var reportPath = Path.Combine(outputDirectory, $"cross_comparison_report.{format.ToLower()}");
                            Console.WriteLine($"Generating {format} report: {reportPath}");
                            // Convert string format to ReportFormat enum
                            ReportFormat reportFormat;
                            switch (format.ToLowerInvariant())
                            {
                                case "json":
                                    reportFormat = ReportFormat.Json;
                                    break;
                                case "html":
                                    reportFormat = ReportFormat.Html;
                                    break;
                                case "markdown":
                                case "md":
                                    reportFormat = ReportFormat.Markdown;
                                    break;
                                case "csv":
                                    reportFormat = ReportFormat.Csv;
                                    break;
                                default:
                                    Console.WriteLine($"Warning: Unknown report format '{format}' ignored.");
                                    continue;
                            }
                            await reportService.GenerateReportAsync(comparisonResult, outputDirectory, reportFormat);
                        }
                        catch (Exception reportEx)
                        {
                            Console.WriteLine($"Error generating {format} report: {reportEx.Message}");
                        }
                    }
                    
                    // Print summary
                    Console.WriteLine();
                    Console.WriteLine("Comparison Complete");
                    Console.WriteLine("==================");
                    Console.WriteLine($"Total source policies: {comparisonResult.Summary.TotalSourcePolicies}");
                    Console.WriteLine($"Total reference policies: {comparisonResult.Summary.TotalReferencePolicies}");
                    Console.WriteLine($"Matching policies: {comparisonResult.Summary.MatchingPolicies}");
                    Console.WriteLine($"Semantically equivalent policies: {comparisonResult.Summary.SemanticallyEquivalentPolicies}");
                    Console.WriteLine($"Different policies: {comparisonResult.Summary.PoliciesWithDifferences}");
                    Console.WriteLine($"Source-only policies: {comparisonResult.Summary.SourceOnlyPolicies}");
                    Console.WriteLine($"Reference-only policies: {comparisonResult.Summary.ReferenceOnlyPolicies}");
                    
                    return 0;
                }
                catch (Exception serviceEx)
                {
                    Console.WriteLine($"Error during cross-format comparison: {serviceEx.Message}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
                return 1;
            }
        }

        private static void DisplayCrossFormatComparisonSummary(CrossFormatComparisonResult result)
        {
            Console.WriteLine();
            Console.WriteLine("Cross-Format Comparison Summary");
            Console.WriteLine("===============================");
            Console.WriteLine($"Source Format: {result.SourceFormat}");
            Console.WriteLine($"Reference Format: {result.ReferenceFormat}");
            Console.WriteLine($"Total Source Policies: {result.Summary.TotalSourcePolicies}");
            Console.WriteLine($"Total Reference Policies: {result.Summary.TotalReferencePolicies}");
            Console.WriteLine($"Identical Policies: {result.Summary.MatchingPolicies}");
            Console.WriteLine($"Semantically Equivalent: {result.Summary.SemanticallyEquivalentPolicies}");
            Console.WriteLine($"Policies with Differences: {result.Summary.PoliciesWithDifferences}");
            Console.WriteLine($"Source-Only Policies: {result.Summary.SourceOnlyPolicies}");
            Console.WriteLine($"Reference-Only Policies: {result.Summary.ReferenceOnlyPolicies}");
            Console.WriteLine();

            // Display policy-by-policy results
            Console.WriteLine("Policy Comparison Details");
            Console.WriteLine("=========================");
            
            var groupedComparisons = result.PolicyComparisons.GroupBy(c => c.Status);
            
            foreach (var group in groupedComparisons)
            {
                Console.WriteLine($"\n{group.Key} ({group.Count()}):");
                foreach (var comparison in group.Take(10)) // Limit to first 10 for console display
                {
                    Console.WriteLine($"  - {comparison.PolicyName}");
                    if (comparison.ConversionSuggestions?.Any() == true)
                    {
                        Console.WriteLine($"    Suggestions: {string.Join("; ", comparison.ConversionSuggestions.Take(2))}");
                    }
                }
                if (group.Count() > 10)
                {
                    Console.WriteLine($"    ... and {group.Count() - 10} more policies");
                }
            }
            Console.WriteLine();
        }

        private static string[] ProcessReportFormats(string[] reportFormats)
        {
            var processedFormats = new List<string>();
            foreach (var format in reportFormats)
            {
                if (format.Contains(','))
                {
                    // Split comma-separated values
                    var splitFormats = ProcessCommaSeparatedValues(format)
                        .Select(f => f.ToLowerInvariant());
                    processedFormats.AddRange(splitFormats);
                }
                else
                {
                    processedFormats.Add(format.Trim().ToLowerInvariant());
                }
            }
            return processedFormats.Distinct().ToArray();
        }

        private static List<string> ProcessCommaSeparatedValues(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new List<string>();

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();
        }

        private static List<string> ProcessCommaSeparatedArray(string[] input)
        {
            var result = new List<string>();
            
            foreach (var item in input)
            {
                result.AddRange(ProcessCommaSeparatedValues(item));
            }
            
            return result;
        }

        private static async Task<int> GenerateBaselineAsync(
            string outputDirectory,
            bool anonymize,
            bool filterEnabledOnly,
            string[] policyNames)
        {
            try
            {
                // Handle comma-separated policy names in addition to space-separated ones
                var processedPolicyNames = new List<string>();
                if (policyNames != null && policyNames.Any())
                {
                    foreach (var name in policyNames)
                    {
                        if (name.Contains(','))
                        {
                            processedPolicyNames.AddRange(ProcessCommaSeparatedValues(name));
                        }
                        else
                        {
                            processedPolicyNames.Add(name.Trim());
                        }
                    }
                }

                var options = new BaselineGenerationOptions
                {
                    OutputDirectory = outputDirectory,
                    Anonymize = anonymize,
                    FilterEnabledOnly = filterEnabledOnly,
                    PolicyNames = processedPolicyNames.Any() ? processedPolicyNames : null
                };

                var baselineService = new BaselineGenerationService();
                return await baselineService.GenerateBaselineAsync(options);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
                return 1;
            }
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

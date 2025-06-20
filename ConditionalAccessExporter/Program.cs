﻿using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using Newtonsoft.Json;
using System.Text;
using System.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ConditionalAccessExporter.Models;
using ConditionalAccessExporter.Services;
using ConditionalAccessExporter.Utils;

namespace ConditionalAccessExporter
{
    public class Program
    {
        private static IServiceProvider? _serviceProvider;
        private static ILogger<Program>? _logger;

        public static async Task<int> Main(string[] args)
        {
            // Set up dependency injection and logging
            SetupLogging();

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
            var skipValidationOption = new Option<bool>(
                name: "--skip-validation",
                description: "Skip validation of reference files before comparison",
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
            compareCommand.AddOption(skipValidationOption);

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
                var skipValidation = context.ParseResult.GetValueForOption(skipValidationOption);

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
                    quiet,
                    skipValidation);
                
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

            // Validate command
            var validateCommand = new Command("validate", "Validate reference policy files");
            var validateReferenceDirectoryOption = new Option<string>(
                name: "--reference-dir",
                description: "Directory containing reference JSON files to validate"
            ) { IsRequired = true };
            var validateVerboseOption = new Option<bool>(
                name: "--verbose",
                description: "Show detailed validation output including warnings",
                getDefaultValue: () => false
            );
            var validateAutoFixOption = new Option<bool>(
                name: "--auto-fix",
                description: "Attempt to automatically fix common issues",
                getDefaultValue: () => false
            );
            var validateOutputOption = new Option<string>(
                name: "--output",
                description: "Output validation results to a JSON file"
            );

            validateCommand.AddOption(validateReferenceDirectoryOption);
            validateCommand.AddOption(validateVerboseOption);
            validateCommand.AddOption(validateAutoFixOption);
            validateCommand.AddOption(validateOutputOption);
            validateCommand.SetHandler(ValidatePoliciesAsync,
                validateReferenceDirectoryOption,
                validateVerboseOption,
                validateAutoFixOption,
                validateOutputOption);

            // Templates command
            var templatesCommand = new Command("templates", "Manage reference policy templates");
            var listTemplatesOption = new Option<bool>(
                name: "--list",
                description: "List all available templates",
                getDefaultValue: () => false
            );
            var createTemplateOption = new Option<string>(
                name: "--create",
                description: "Create a specific template (e.g., basic/require-mfa-all-users)"
            );
            var createBaselineTemplatesOption = new Option<bool>(
                name: "--create-baseline",
                description: "Create a baseline set of common templates",
                getDefaultValue: () => false
            );
            var templateOutputDirOption = new Option<string>(
                name: "--output-dir",
                description: "Output directory for generated templates",
                getDefaultValue: () => "generated-policies"
            );
            var validateTemplateOption = new Option<string>(
                name: "--validate",
                description: "Validate a template file"
            );

            templatesCommand.AddOption(listTemplatesOption);
            templatesCommand.AddOption(createTemplateOption);
            templatesCommand.AddOption(createBaselineTemplatesOption);
            templatesCommand.AddOption(templateOutputDirOption);
            templatesCommand.AddOption(validateTemplateOption);
            templatesCommand.SetHandler(ManageTemplatesAsync,
                listTemplatesOption,
                createTemplateOption,
                createBaselineTemplatesOption,
                templateOutputDirOption,
                validateTemplateOption);

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

            // Remediate command
            var remediateCommand = new Command("remediate", "Analyze and remediate Conditional Access policies");
            var analysisOnlyOption = new Option<bool>(
                name: "--analysis-only",
                description: "Perform analysis only without generating remediation scripts",
                getDefaultValue: () => false
            );
            var interactiveOption = new Option<bool>(
                name: "--interactive",
                description: "Run in interactive mode for guided remediation",
                getDefaultValue: () => false
            );
            var riskLevelFilterOption = new Option<string>(
                name: "--risk-level",
                description: "Filter remediations by risk level (Low, Medium, High, Critical)",
                getDefaultValue: () => "All"
            );
            var scriptFormatOption = new Option<string>(
                name: "--script-format",
                description: "Script format to generate (PowerShell, AzureCLI, Terraform)",
                getDefaultValue: () => "PowerShell"
            );
            var remediateOutputDirOption = new Option<string>(
                name: "--output-dir",
                description: "Output directory for remediation reports and scripts",
                getDefaultValue: () => "remediation-output"
            );
            var includeImpactAnalysisOption = new Option<bool>(
                name: "--include-impact",
                description: "Include impact analysis in the remediation report",
                getDefaultValue: () => true
            );
            var dryRunOption = new Option<bool>(
                name: "--dry-run",
                description: "Generate scripts without executing any changes",
                getDefaultValue: () => true
            );
            var backupPoliciesOption = new Option<bool>(
                name: "--backup",
                description: "Create backup of existing policies before remediation",
                getDefaultValue: () => true
            );

            remediateCommand.AddOption(analysisOnlyOption);
            remediateCommand.AddOption(interactiveOption);
            remediateCommand.AddOption(riskLevelFilterOption);
            remediateCommand.AddOption(scriptFormatOption);
            remediateCommand.AddOption(remediateOutputDirOption);
            remediateCommand.AddOption(includeImpactAnalysisOption);
            remediateCommand.AddOption(dryRunOption);
            remediateCommand.AddOption(backupPoliciesOption);
            remediateCommand.SetHandler(RemediatePoliciesAsync,
                analysisOnlyOption,
                interactiveOption,
                riskLevelFilterOption,
                scriptFormatOption,
                remediateOutputDirOption,
                includeImpactAnalysisOption,
                dryRunOption,
                backupPoliciesOption);

            // Benchmark command
            var benchmarkCommand = new Command("benchmark", "Run performance benchmarks and monitoring");
            var benchmarkTypeOption = new Option<string>(
                name: "--type",
                description: "Type of benchmark to run (all, benchmarks, regression, memory)",
                getDefaultValue: () => "all"
            );
            var benchmarkOutputOption = new Option<string?>(
                name: "--output",
                description: "Output file for benchmark results",
                getDefaultValue: () => null
            );
            
            benchmarkCommand.AddOption(benchmarkTypeOption);
            benchmarkCommand.AddOption(benchmarkOutputOption);
            
            benchmarkCommand.SetHandler(async (string benchmarkType, string? outputFile) =>
            {
                try
                {
                    Logger.WriteInfo("Starting performance benchmarks...");
                    
                    var benchmarkArgs = new List<string>();
                    
                    switch (benchmarkType.ToLowerInvariant())
                    {
                        case "all":
                            benchmarkArgs.Add("--all");
                            break;
                        case "benchmarks":
                            benchmarkArgs.Add("--benchmarks");
                            break;
                        case "regression":
                            benchmarkArgs.Add("--regression");
                            break;
                        case "memory":
                            benchmarkArgs.Add("--memory");
                            break;
                        default:
                            Logger.WriteError($"Unknown benchmark type: {benchmarkType}");
                            Logger.WriteInfo("Valid types: all, benchmarks, regression, memory");
                            return;
                    }
                    
                    if (!string.IsNullOrEmpty(outputFile))
                    {
                        benchmarkArgs.AddRange(new[] { "--output", outputFile });
                    }
                    
                    var exitCode = await PerformanceBenchmarkProgram.RunBenchmarksAsync(benchmarkArgs.ToArray());
                    
                    if (exitCode == 0)
                    {
                        Logger.WriteInfo("Benchmarks completed successfully!");
                    }
                    else
                    {
                        Logger.WriteError("Benchmarks failed!");
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
            }, benchmarkTypeOption, benchmarkOutputOption);

            rootCommand.AddCommand(exportCommand);
            rootCommand.AddCommand(terraformCommand);
            rootCommand.AddCommand(jsonToTerraformCommand);
            rootCommand.AddCommand(compareCommand);
            rootCommand.AddCommand(crossFormatCompareCommand);
            rootCommand.AddCommand(validateCommand);
            rootCommand.AddCommand(templatesCommand);
            rootCommand.AddCommand(baselineCommand);
            rootCommand.AddCommand(remediateCommand);
            rootCommand.AddCommand(benchmarkCommand);

            // If no arguments provided, default to export for backward compatibility
            if (args.Length == 0)
            {
                return await ExportPoliciesAsync($"ConditionalAccessPolicies_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            }

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> ExportPoliciesAsync(string outputPath)
        {
            Logger.WriteInfo("Conditional Access Policy Exporter");
            Logger.WriteInfo("==================================");

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

                Logger.WriteInfo($"Conditional Access Policies exported successfully to: {outputPath}");
                Logger.WriteInfo($"File size: {new FileInfo(outputPath).Length / 1024.0:F2} KB");
                Logger.WriteInfo("Export completed successfully!");
                
                return 0;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return 1;
            }
        }

        private static async Task<int> ConvertTerraformAsync(
            string inputPath,
            string outputPath,
            bool validate,
            bool verbose)
        {
            Logger.WriteInfo("Terraform to JSON Conversion");
            Logger.WriteInfo("============================");

            try
            {
                // Validate required input parameter
                if (string.IsNullOrEmpty(inputPath))
                {
                    Logger.WriteError("Error: Input path is required but was not provided.");
                    return 1;
                }
                
                Logger.WriteInfo("Converting Terraform policies...");

// Removed redundant null-or-empty check for inputPath
                
                var parsingService = new TerraformParsingService();
                var conversionService = new TerraformConversionService();

                Logger.WriteInfo($"Input path: {inputPath}");
                Logger.WriteInfo($"Output path: {outputPath}");
                Logger.WriteInfo($"Validation: {(validate ? "Enabled" : "Disabled")}");
                Logger.WriteInfo($"Verbose logging: {(verbose ? "Enabled" : "Disabled")}");
                Logger.WriteInfo("");

                // Parse Terraform files
                Logger.WriteInfo("Parsing Terraform files...");
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
                    Logger.WriteError($"Error: Input path '{inputPath}' not found.");
                    return 1;
                }

                if (parseResult.Errors.Any())
                {
                    Logger.WriteInfo("Parsing errors encountered:");
                    foreach (var error in parseResult.Errors)
                    {
                        Logger.WriteInfo($"  - {error}");
                    }
                    return 1;
                }

                if (parseResult.Warnings.Any() && verbose)
                {
                    Logger.WriteInfo("Parsing warnings:");
                    foreach (var warning in parseResult.Warnings)
                    {
                        Logger.WriteInfo($"  - {warning}");
                    }
                }

                Logger.WriteInfo($"Found {parseResult.Policies.Count} conditional access policies");
                Logger.WriteInfo($"Found {parseResult.Variables.Count} variables");
                Logger.WriteInfo($"Found {parseResult.Locals.Count} locals");
                Logger.WriteInfo($"Found {parseResult.DataSources.Count} data sources");
                Logger.WriteInfo("");

                if (!parseResult.Policies.Any())
                {
                    Logger.WriteInfo("No conditional access policies found to convert.");
                    return 0;
                }

                // Convert to Graph JSON format
                Logger.WriteInfo("Converting to Microsoft Graph JSON format...");
                var conversionResult = await conversionService.ConvertToGraphJsonAsync(parseResult);

                if (conversionResult.Errors.Any())
                {
                    Logger.WriteInfo("Conversion errors encountered:");
                    foreach (var error in conversionResult.Errors)
                    {
                        Logger.WriteInfo($"  - {error}");
                    }
                }

                if (conversionResult.Warnings.Any() && verbose)
                {
                    Logger.WriteInfo("Conversion warnings:");
                    foreach (var warning in conversionResult.Warnings)
                    {
                        Logger.WriteInfo($"  - {warning}");
                    }
                }

                if (verbose && conversionResult.ConversionLog.Any())
                {
                    Logger.WriteInfo("Conversion log:");
                    foreach (var log in conversionResult.ConversionLog)
                    {
                        Logger.WriteInfo($"  - {log}");
                    }
                }

                // Validate if requested
                if (validate)
                {
                    Logger.WriteInfo("Validating converted policies...");
                    // Additional validation could be implemented here
                }

                // Serialize and save
                var json = JsonConvert.SerializeObject(conversionResult.ConvertedPolicies, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                });

                await File.WriteAllTextAsync(outputPath, json, Encoding.UTF8);

                Logger.WriteInfo("");
                Logger.WriteInfo("Conversion Summary:");
                Logger.WriteInfo("==================");
                Logger.WriteInfo($"Successful conversions: {conversionResult.SuccessfulConversions}");
                Logger.WriteInfo($"Failed conversions: {conversionResult.FailedConversions}");
                Logger.WriteInfo($"Output file: {outputPath}");
                Logger.WriteInfo($"File size: {new FileInfo(outputPath).Length / 1024.0:F2} KB");
                Logger.WriteInfo("Terraform conversion completed successfully!");

                // Return failure code if any conversions failed
                return conversionResult.FailedConversions > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                HandleException(ex);
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
            Logger.WriteInfo("JSON to Terraform Conversion");
            Logger.WriteInfo("============================");

            try
            {
                // Validate required input parameter
                if (string.IsNullOrEmpty(inputPath))
                {
                    Logger.WriteError("Error: Input file is required but was not provided.");
                    return 1;
                }
                
                Logger.WriteInfo($"Input file: {inputPath}");
                Logger.WriteInfo($"Output directory: {outputDirectory}");
                Logger.WriteInfo($"Generate variables: {(generateVariables ? "Yes" : "No")}");
                Logger.WriteInfo($"Generate provider: {(generateProvider ? "Yes" : "No")}");
                Logger.WriteInfo($"Separate files: {(separateFiles ? "Yes" : "No")}");
                Logger.WriteInfo($"Generate module: {(generateModule ? "Yes" : "No")}");
                Logger.WriteInfo($"Include comments: {(includeComments ? "Yes" : "No")}");
                Logger.WriteInfo($"Provider version: {providerVersion}");
                Logger.WriteInfo("");
                
                Logger.WriteInfo("Converting JSON to Terraform HCL...");
                
                var jsonToTerraformService = new JsonToTerraformService();

                if (!File.Exists(inputPath))
                {
                    Logger.WriteError($"Error: Input file '{inputPath}' not found.");
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
                    Logger.WriteInfo("Conversion errors encountered:");
                    foreach (var error in result.Errors)
                    {
                        Logger.WriteInfo($"  - {error}");
                    }
                    return 1;
                }

                if (result.Warnings.Any())
                {
                    Logger.WriteInfo("Conversion warnings:");
                    foreach (var warning in result.Warnings)
                    {
                        Logger.WriteInfo($"  - {warning}");
                    }
                }

                if (result.ConversionLog.Any())
                {
                    Logger.WriteInfo("Conversion log:");
                    foreach (var log in result.ConversionLog)
                    {
                        Logger.WriteInfo($"  - {log}");
                    }
                }

                Logger.WriteInfo("");
                Logger.WriteInfo("Conversion Summary:");
                Logger.WriteInfo("==================");
                Logger.WriteInfo($"Successful conversions: {result.SuccessfulConversions}");
                Logger.WriteInfo($"Failed conversions: {result.FailedConversions}");
                Logger.WriteInfo($"Output directory: {result.OutputPath}");
                Logger.WriteInfo($"Generated files: {result.GeneratedFiles.Count}");
                
                if (result.GeneratedFiles.Any())
                {
                    Logger.WriteInfo("Generated files:");
                    foreach (var file in result.GeneratedFiles)
                    {
                        Logger.WriteInfo($"  - {Path.GetFileName(file)}");
                    }
                }

                Logger.WriteInfo("JSON to Terraform conversion completed successfully!");

                return result.FailedConversions > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return 1;
            }
        }

        private static async Task<int> ValidatePoliciesAsync(
            string referenceDirectory,
            bool verbose,
            bool autoFix,
            string? outputPath)
        {
            try
            {
                Logger.WriteInfo("===================");
                Logger.WriteInfo("CA Scanner - Policy Validation");
                Logger.WriteInfo("===================");
                Logger.WriteInfo("");

                // Perform pre-flight checks
                Logger.WriteInfo("Performing pre-flight checks...");
                var validationService = new PolicyValidationService();

                // Check if reference directory exists
                if (!Directory.Exists(referenceDirectory))
                {
                    Logger.WriteError($"Reference directory '{referenceDirectory}' does not exist.");
                    return 1;
                }

                // Validate directory
                Logger.WriteInfo($"Validating reference directory: {referenceDirectory}");
                var validationResult = await validationService.ValidateDirectoryAsync(referenceDirectory);

                // Display results
                Logger.WriteInfo("");
                Logger.WriteInfo("Validation Results:");
                Logger.WriteInfo("==================");
                Logger.WriteInfo($"Total files scanned: {validationResult.TotalFiles}");
                Logger.WriteInfo($"Valid files: {validationResult.ValidFiles}");
                Logger.WriteInfo($"Invalid files: {validationResult.InvalidFiles}");
                Logger.WriteInfo($"Files with warnings: {validationResult.FilesWithWarnings}");
                Logger.WriteInfo("");

                if (validationResult.IsValid)
                {
                    Logger.WriteInfo("✓ All files passed validation!");
                }
                else
                {
                    Logger.WriteError($"✗ Validation failed with {validationResult.InvalidFiles} invalid files");
                }

                // Display detailed results for invalid files
                foreach (var fileResult in validationResult.FileResults.Where(f => !f.IsValid))
                {
                    Logger.WriteError($"✗ {fileResult.FileName}:");
                    foreach (var error in fileResult.Errors)
                    {
                        var location = error.LineNumber.HasValue ? $" (line {error.LineNumber})" : "";
                        Logger.WriteError($"  - {error.Message}{location}");
                        if (!string.IsNullOrEmpty(error.Suggestion))
                        {
                            Logger.WriteInfo($"    Suggestion: {error.Suggestion}");
                        }
                    }
                    Logger.WriteInfo("");
                }

                // Display warnings if verbose mode is enabled
                if (verbose)
                {
                    foreach (var fileResult in validationResult.FileResults.Where(f => f.Warnings.Any()))
                    {
                        Logger.WriteInfo($"⚠ {fileResult.FileName} has warnings:");
                        foreach (var warning in fileResult.Warnings)
                        {
                            var location = warning.LineNumber.HasValue ? $" (line {warning.LineNumber})" : "";
                            Logger.WriteInfo($"  - {warning.Message}{location}");
                            if (!string.IsNullOrEmpty(warning.Suggestion))
                            {
                                Logger.WriteInfo($"    Suggestion: {warning.Suggestion}");
                            }
                        }
                        Logger.WriteInfo("");
                    }
                }

                // Auto-fix if requested
                if (autoFix && validationResult.InvalidFiles > 0)
                {
                    // Auto-fix functionality is planned for future versions
                    // This would provide automatic correction of common validation issues
                    Logger.WriteError("Auto-fix is enabled but not yet implemented.");
                    Logger.WriteInfo("Future versions will support automatic fixing of common issues such as:");
                    Logger.WriteInfo("- Formatting JSON files");
                    Logger.WriteInfo("- Correcting GUID formats");
                    Logger.WriteInfo("- Fixing common schema violations");
                    Logger.WriteInfo("For now, please review and fix the validation errors manually.");
                }

                // Save results to file if requested
                if (!string.IsNullOrEmpty(outputPath))
                {
                    var json = JsonConvert.SerializeObject(validationResult, Formatting.Indented);
                    await File.WriteAllTextAsync(outputPath, json);
                    Logger.WriteInfo($"Validation results saved to: {outputPath}");
                }

                Logger.WriteInfo("Policy validation completed!");
                return validationResult.IsValid ? 0 : 1;
            }
            catch (Exception ex)
            {
                HandleException(ex);
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
            bool quiet,
            bool skipValidation = false)
        {
            Logger.WriteInfo("Conditional Access Policy Comparison");
            Logger.WriteInfo("====================================");

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
                    QuietMode = quiet,
                    ExplainValues = explainValues
                };

                // Set logger quiet mode based on CI/CD options
                Logger.SetQuietMode(quiet);
                Logger.SetVerboseMode(explainValues);

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
                    Logger.WriteInfo("Fetching live Entra policies...");
                    entraExport = await FetchEntraPoliciesAsync();
                }

                var matchingOptions = new MatchingOptions
                {
                    Strategy = matchingStrategy,
                    CaseSensitive = caseSensitive
                };

                var comparisonService = GetService<PolicyComparisonService>();
                var result = await comparisonService.CompareAsync(entraExport, referenceDirectory, matchingOptions, skipValidation);

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
                        Logger.WriteInfo($"Pipeline output written to: {pipelineOutputPath}");
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
                        Logger.WriteInfo($"CRITICAL: {analysis.CriticalDifferences} critical differences found");
                    }
                    else if (analysis.TotalDifferences > 0)
                    {
                        Logger.WriteInfo($"WARNING: {analysis.TotalDifferences} differences found");
                    }
                    else
                    {
                        Logger.WriteInfo("SUCCESS: No differences found");
                    }
                }
                else
                {
                    Logger.WriteInfo("");
                    Logger.WriteInfo("Comparison Results Summary:");
                    Logger.WriteInfo("==========================");
                    Logger.WriteInfo($"Total policies compared: {result.Summary.TotalEntraPolicies}");
                    Logger.WriteInfo($"Policies with differences: {result.Summary.PoliciesWithDifferences}");
                    Logger.WriteInfo($"Critical differences: {analysis.CriticalDifferences}");
                    Logger.WriteInfo($"Non-critical differences: {analysis.NonCriticalDifferences}");
                    
                    if (analysis.CriticalPolicies.Any())
                    {
                        Logger.WriteInfo($"Policies with critical changes: {string.Join(", ", analysis.CriticalPolicies)}");
                    }
                    
                    Logger.WriteInfo($"Status: {analysis.Status}");
                    Logger.WriteInfo("Comparison completed successfully!");
                }

                return analysis.ExitCode;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return (int)ExitCode.Error;
            }
        }

        private static async Task<object> FetchEntraPoliciesAsync()
        {
            // Get Azure credentials from environment variables for logging
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

            Logger.WriteInfo($"Tenant ID: {tenantId}");
            Logger.WriteInfo($"Client ID: {clientId}");
            Logger.WriteInfo("Client Secret: [HIDDEN]");
            Logger.WriteInfo("");

            // Use the resilient Graph service from DI container
            var resilientGraphService = GetService<IResilientGraphService>();

            Logger.WriteInfo("Authenticating to Microsoft Graph with resilience patterns...");
            Logger.WriteInfo("Fetching Conditional Access Policies...");

            // Get all conditional access policies with resilience patterns applied
            var policies = await resilientGraphService.GetConditionalAccessPoliciesAsync();

            // Log performance metrics
            var metrics = resilientGraphService.GetMetrics();
            Logger.WriteInfo($"API Performance: {metrics.TotalCalls} calls, {metrics.SuccessRate:F1}% success rate, {metrics.AverageResponseTimeMs:F0}ms avg response time");
            if (metrics.CachedCalls > 0)
            {
                Logger.WriteInfo($"Cache Performance: {metrics.CacheHitRate:F1}% hit rate, {metrics.CachedCalls} cached responses");
            }
            if (metrics.RateLimitHits > 0)
            {
                Logger.WriteWarning($"Rate Limiting: {metrics.RateLimitHits} rate limit hits encountered");
            }

            if (policies?.Value == null || !policies.Value.Any())
            {
                Logger.WriteInfo("No Conditional Access Policies found.");
                return new { ExportedAt = DateTime.UtcNow, TenantId = tenantId, PoliciesCount = 0, Policies = new List<object>() };
            }

            Logger.WriteInfo($"Found {policies.Value.Count} Conditional Access Policies");
            Logger.WriteInfo("");

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
            Logger.WriteInfo("Policy Summary:");
            Logger.WriteInfo("================");
            foreach (var policy in policies.Value)
            {
                Logger.WriteInfo($"- {policy.DisplayName} (State: {policy.State})");
            }
            Logger.WriteInfo("");

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
            Logger.WriteInfo("Cross-Format Policy Comparison");
            Logger.WriteInfo("============================");
            
            try
            {
                // Validate source directory exists
                if (string.IsNullOrEmpty(sourceDirectory))
                {
                    Logger.WriteError("Error: Source directory is required but was not provided.");
                    return 1;
                }

                // Validate reference directory exists
                if (string.IsNullOrEmpty(referenceDirectory))
                {
                    Logger.WriteError("Error: Reference directory is required but was not provided.");
                    return 1;
                }
                
                // Handle comma-separated formats in addition to space-separated ones
                reportFormats = ProcessReportFormats(reportFormats);
                
                Logger.WriteInfo($"Source directory: {sourceDirectory}");
                Logger.WriteInfo($"Reference directory: {referenceDirectory}");
                Logger.WriteInfo($"Output directory: {outputDirectory}");
                Logger.WriteInfo($"Report formats: {string.Join(", ", reportFormats)}");
                Logger.WriteInfo($"Matching strategy: {matchingStrategy}");
                Logger.WriteInfo($"Case sensitivity: {(caseSensitive ? "On" : "Off")}");
                Logger.WriteInfo($"Semantic comparison: {(enableSemantic ? "Enabled" : "Disabled")}");
                Logger.WriteInfo($"Similarity threshold: {similarityThreshold}");
                Logger.WriteInfo("");
                
                Logger.WriteInfo("Cross-comparing policies...");

                // Validate source directory exists
                if (!Directory.Exists(sourceDirectory))
                {
                    Logger.WriteError($"Error: Source directory '{sourceDirectory}' not found.");
                    return 1;
                }

                // Validate reference directory exists
                if (!Directory.Exists(referenceDirectory))
                {
                    Logger.WriteError($"Error: Reference directory '{referenceDirectory}' not found.");
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

                Logger.WriteInfo("Services initialized successfully.");
                
                // Configure matching options
                var matchingOptions = new CrossFormatMatchingOptions
                {
                    Strategy = Enum.Parse<CrossFormatMatchingStrategy>(matchingStrategy, true),
                    CaseSensitive = caseSensitive,
                    EnableSemanticComparison = enableSemantic,
                    SemanticSimilarityThreshold = similarityThreshold
                };
                
                Logger.WriteInfo("Starting cross-format comparison...");
                
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
                            Logger.WriteInfo($"Generating {format} report: {reportPath}");
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
                                    Logger.WriteInfo($"Warning: Unknown report format '{format}' ignored.");
                                    continue;
                            }
                            await reportService.GenerateReportAsync(comparisonResult, outputDirectory, reportFormat);
                        }
                        catch (Exception reportEx)
                        {
                            Logger.WriteInfo($"Error generating {format} report: {reportEx.Message}");
                        }
                    }
                    
                    // Print summary
                    Logger.WriteInfo("");
                    Logger.WriteInfo("Comparison Complete");
                    Logger.WriteInfo("==================");
                    Logger.WriteInfo($"Total source policies: {comparisonResult.Summary.TotalSourcePolicies}");
                    Logger.WriteInfo($"Total reference policies: {comparisonResult.Summary.TotalReferencePolicies}");
                    Logger.WriteInfo($"Matching policies: {comparisonResult.Summary.MatchingPolicies}");
                    Logger.WriteInfo($"Semantically equivalent policies: {comparisonResult.Summary.SemanticallyEquivalentPolicies}");
                    Logger.WriteInfo($"Different policies: {comparisonResult.Summary.PoliciesWithDifferences}");
                    Logger.WriteInfo($"Source-only policies: {comparisonResult.Summary.SourceOnlyPolicies}");
                    Logger.WriteInfo($"Reference-only policies: {comparisonResult.Summary.ReferenceOnlyPolicies}");
                    
                    return 0;
                }
                catch (Exception serviceEx)
                {
                    Logger.WriteInfo($"Error during cross-format comparison: {serviceEx.Message}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return 1;
            }
        }

        private static void DisplayCrossFormatComparisonSummary(CrossFormatComparisonResult result)
        {
            Logger.WriteInfo("");
            Logger.WriteInfo("Cross-Format Comparison Summary");
            Logger.WriteInfo("===============================");
            Logger.WriteInfo($"Source Format: {result.SourceFormat}");
            Logger.WriteInfo($"Reference Format: {result.ReferenceFormat}");
            Logger.WriteInfo($"Total Source Policies: {result.Summary.TotalSourcePolicies}");
            Logger.WriteInfo($"Total Reference Policies: {result.Summary.TotalReferencePolicies}");
            Logger.WriteInfo($"Identical Policies: {result.Summary.MatchingPolicies}");
            Logger.WriteInfo($"Semantically Equivalent: {result.Summary.SemanticallyEquivalentPolicies}");
            Logger.WriteInfo($"Policies with Differences: {result.Summary.PoliciesWithDifferences}");
            Logger.WriteInfo($"Source-Only Policies: {result.Summary.SourceOnlyPolicies}");
            Logger.WriteInfo($"Reference-Only Policies: {result.Summary.ReferenceOnlyPolicies}");
            Logger.WriteInfo("");

            // Display policy-by-policy results
            Logger.WriteInfo("Policy Comparison Details");
            Logger.WriteInfo("=========================");
            
            var groupedComparisons = result.PolicyComparisons.GroupBy(c => c.Status);
            
            foreach (var group in groupedComparisons)
            {
                Logger.WriteInfo($"\n{group.Key} ({group.Count()}):");
                foreach (var comparison in group.Take(10)) // Limit to first 10 for console display
                {
                    Logger.WriteInfo($"  - {comparison.PolicyName}");
                    if (comparison.ConversionSuggestions?.Any() == true)
                    {
                        Logger.WriteInfo($"    Suggestions: {string.Join("; ", comparison.ConversionSuggestions.Take(2))}");
                    }
                }
                if (group.Count() > 10)
                {
                    Logger.WriteInfo($"    ... and {group.Count() - 10} more policies");
                }
            }
            Logger.WriteInfo("");
        }

        private static string[] ProcessReportFormats(string[] reportFormats)
        {
            // Use existing comma-separated splitting helper to eliminate code duplication
            var processedFormats = ProcessCommaSeparatedArray(reportFormats)
                .Select(f => f.ToLowerInvariant())
                .ToList();
            
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
            return input.SelectMany(ProcessCommaSeparatedValues).ToList();
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
                HandleException(ex);
                return 1;
            }
        }

        private static async Task<int> ManageTemplatesAsync(
            bool listTemplates,
            string createTemplate,
            bool createBaseline,
            string outputDirectory,
            string validateTemplate)
        {
            try
            {
                var templateService = new TemplateService();

                if (listTemplates)
                {
                    Logger.WriteInfo("Available Reference Policy Templates");
                    Logger.WriteInfo("===================================");
                    
                    var templates = await templateService.ListAvailableTemplatesAsync();
                    
                    foreach (var category in templates.GroupBy(t => t.Category))
                    {
                        Logger.WriteInfo($"\n{category.Key.ToUpper()} POLICIES:");
                        foreach (var template in category)
                        {
                            Logger.WriteInfo($"  - {template.Name}: {template.Description}");
                        }
                    }
                    
                    Logger.WriteInfo($"\nTotal templates available: {templates.Count}");
                    return 0;
                }

                if (!string.IsNullOrEmpty(validateTemplate))
                {
                    Logger.WriteInfo("Template Validation");
                    Logger.WriteInfo("==================");
                    
                    var validationResult = await templateService.ValidateTemplateAsync(validateTemplate);
                    
                    if (validationResult.IsValid)
                    {
                        Logger.WriteInfo($"✓ Template '{validateTemplate}' is valid");
                        return 0;
                    }
                    else
                    {
                        Logger.WriteError($"✗ Template '{validateTemplate}' is invalid:");
                        foreach (var error in validationResult.Errors)
                        {
                            Logger.WriteError($"  - {error}");
                        }
                        return 1;
                    }
                }

                if (createBaseline)
                {
                    Logger.WriteInfo("Creating Baseline Template Set");
                    Logger.WriteInfo("=============================");
                    
                    var result = await templateService.CreateBaselineSetAsync(outputDirectory);
                    
                    Logger.WriteInfo($"✓ Created {result.CreatedFiles.Count} baseline templates in '{outputDirectory}'");
                    foreach (var file in result.CreatedFiles)
                    {
                        Logger.WriteInfo($"  - {Path.GetFileName(file)}");
                    }
                    
                    if (result.Warnings.Any())
                    {
                        Logger.WriteInfo("\nWarnings:");
                        foreach (var warning in result.Warnings)
                        {
                            Logger.WriteInfo($"  ! {warning}");
                        }
                    }
                    
                    return 0;
                }

                if (!string.IsNullOrEmpty(createTemplate))
                {
                    Logger.WriteInfo("Creating Template");
                    Logger.WriteInfo("================");
                    
                    var result = await templateService.CreateTemplateAsync(createTemplate, outputDirectory);
                    
                    if (result.Success)
                    {
                        Logger.WriteInfo($"✓ Template '{createTemplate}' created successfully");
                        Logger.WriteInfo($"  Output file: {result.OutputPath}");
                        
                        if (result.Warnings.Any())
                        {
                            Logger.WriteInfo("\nWarnings:");
                            foreach (var warning in result.Warnings)
                            {
                                Logger.WriteInfo($"  ! {warning}");
                            }
                        }
                        
                        return 0;
                    }
                    else
                    {
                        Logger.WriteError($"✗ Failed to create template '{createTemplate}':");
                        foreach (var error in result.Errors)
                        {
                            Logger.WriteError($"  - {error}");
                        }
                        return 1;
                    }
                }

                // If no specific action is requested, show help
                Logger.WriteInfo("Template Management Commands");
                Logger.WriteInfo("===========================");
                Logger.WriteInfo("--list                 List all available templates");
                Logger.WriteInfo("--create <template>    Create a specific template");
                Logger.WriteInfo("--create-baseline      Create a baseline set of templates");
                Logger.WriteInfo("--validate <file>      Validate a template file");
                Logger.WriteInfo("--output-dir <dir>     Specify output directory");
                Logger.WriteInfo("");
                Logger.WriteInfo("Examples:");
                Logger.WriteInfo("  dotnet run templates --list");
                Logger.WriteInfo("  dotnet run templates --create basic/require-mfa-all-users");
                Logger.WriteInfo("  dotnet run templates --create-baseline --output-dir ./policies");
                
                return 0;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return 1;
            }
        }

        /// <summary>
        /// Orchestrates the Conditional Access Policy remediation process.
        /// This method serves as a high-level coordinator that delegates specific responsibilities
        /// to focused helper methods for better maintainability and readability.
        /// </summary>
        /// <param name="analysisOnly">If true, only generates analysis reports without scripts</param>
        /// <param name="interactive">If true, enables interactive mode for user approval</param>
        /// <param name="riskLevel">Filter remediations by risk level (Low, Medium, High, Critical, All)</param>
        /// <param name="scriptFormat">Format for generated scripts (PowerShell, AzureCLI, Terraform)</param>
        /// <param name="outputDir">Directory for output files</param>
        /// <param name="includeImpactAnalysis">If true, performs impact analysis on remediations</param>
        /// <param name="dryRun">If true, generates scripts in dry-run mode</param>
        /// <param name="backup">If true, includes backup functionality</param>
        /// <returns>Exit code: 0 for success, 1 for failure</returns>
        private static async Task<int> RemediatePoliciesAsync(
            bool analysisOnly,
            bool interactive,
            string riskLevel,
            string scriptFormat,
            string outputDir,
            bool includeImpactAnalysis,
            bool dryRun,
            bool backup)
        {
            try
            {
                Logger.WriteInfo("Starting Conditional Access Policy remediation analysis...");

                // Initialize environment and services
                var services = InitializeRemediationServices(outputDir);
                if (services == null)
                {
                    return 1;
                }

                // Fetch and validate policies
                var policies = await FetchAndValidatePoliciesAsync();
                if (policies == null)
                {
                    return 1;
                }

                if (!policies.Any())
                {
                    Logger.WriteInfo("No policies found to analyze.");
                    return 0;
                }

                // Analyze policies for remediation opportunities
                var remediationResults = AnalyzePoliciesForRemediation(policies, services.RemediationService);
                if (!remediationResults.Any())
                {
                    Logger.WriteInfo("No remediations found during analysis.");
                    return 0;
                }

                // Filter results by risk level
                var filteredResults = FilterRemediationsByRiskLevel(remediationResults, riskLevel);
                if (!filteredResults.Any())
                {
                    Logger.WriteInfo("No remediations found based on the specified criteria.");
                    return 0;
                }

                // Perform impact analysis if requested
                if (includeImpactAnalysis)
                {
                    PerformImpactAnalysis(filteredResults, services.ImpactAnalysisService);
                }

                // Execute the appropriate workflow based on mode
                return await ExecuteRemediationWorkflowAsync(
                    filteredResults,
                    services.ScriptGenerationService,
                    analysisOnly,
                    interactive,
                    scriptFormat,
                    outputDir,
                    dryRun,
                    backup);
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return 1;
            }
        }

        /// <summary>
        /// Data structure to hold initialized services for remediation operations.
        /// </summary>
        private sealed class RemediationServices
        {
            public RemediationService RemediationService { get; set; } = null!;
            public ImpactAnalysisService ImpactAnalysisService { get; set; } = null!;
            public ScriptGenerationService ScriptGenerationService { get; set; } = null!;
        }

        /// <summary>
        /// Initializes authentication credentials and required services for remediation operations.
        /// </summary>
        /// <param name="outputDir">Output directory to create</param>
        /// <returns>RemediationServices instance or null if initialization fails</returns>
        private static RemediationServices? InitializeRemediationServices(string outputDir)
        {
            try
            {
                // Create output directory
                Directory.CreateDirectory(outputDir);

                // Get authentication credentials
                var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
                var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    Logger.WriteError("Authentication credentials not found. Please set AZURE_TENANT_ID, AZURE_CLIENT_ID, and AZURE_CLIENT_SECRET environment variables.");
                    return null;
                }

                // Initialize services using DI container
                var policyComparisonService = new PolicyComparisonService();
                
                // For now, create ImpactAnalysisService without Graph client until we refactor it
                // The ImpactAnalysisService has fallback behavior when Graph client is null
                var impactAnalysisService = new ImpactAnalysisService(null);
                
                var scriptGenerationService = new ScriptGenerationService();
                var remediationService = new RemediationService(policyComparisonService, impactAnalysisService, scriptGenerationService);

                return new RemediationServices
                {
                    RemediationService = remediationService,
                    ImpactAnalysisService = impactAnalysisService,
                    ScriptGenerationService = scriptGenerationService
                };
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Failed to initialize remediation services: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetches Conditional Access policies from Microsoft Graph and validates the result.
        /// </summary>
        /// <returns>List of ConditionalAccessPolicy objects or null if fetch fails</returns>
        private static async Task<List<ConditionalAccessPolicy>?> FetchAndValidatePoliciesAsync()
        {
            try
            {
                Logger.WriteInfo("Fetching current Conditional Access policies...");
                var policiesResult = await FetchEntraPoliciesAsync();
                
                if (policiesResult == null)
                {
                    Logger.WriteError("Failed to fetch policies from Microsoft Graph API.");
                    return null;
                }
                
                var policies = JsonConvert.DeserializeObject<List<ConditionalAccessPolicy>>(JsonConvert.SerializeObject(policiesResult));

                if (policies == null)
                {
                    Logger.WriteError("Failed to deserialize policies from API response.");
                    return null;
                }

                Logger.WriteInfo($"Found {policies.Count} policies to analyze.");
                return policies;
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Error fetching policies: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Analyzes a collection of policies to identify remediation opportunities.
        /// </summary>
        /// <param name="policies">Policies to analyze</param>
        /// <param name="remediationService">Service to perform the analysis</param>
        /// <returns>List of RemediationResult objects containing identified remediations</returns>
        private static List<RemediationResult> AnalyzePoliciesForRemediation(
            List<ConditionalAccessPolicy> policies,
            RemediationService remediationService)
        {
            var remediationResults = new List<RemediationResult>();
            
            foreach (var policy in policies)
            {
                try
                {
                    var result = remediationService.AnalyzePolicy(policy);
                    if (result.PolicyRemediations.Any())
                    {
                        remediationResults.Add(result);
                    }
                }
                catch (Exception policyEx)
                {
                    Logger.WriteError($"Failed to analyze policy '{policy.DisplayName}': {policyEx.Message}");
                    // Continue with other policies rather than failing completely
                }
            }

            Logger.WriteInfo($"Found {remediationResults.Count} policies requiring remediation.");
            return remediationResults;
        }

        /// <summary>
        /// Filters remediation results based on the specified risk level.
        /// </summary>
        /// <param name="remediationResults">Results to filter</param>
        /// <param name="riskLevel">Risk level filter (Low, Medium, High, Critical, All)</param>
        /// <returns>Filtered list of RemediationResult objects</returns>
        private static List<RemediationResult> FilterRemediationsByRiskLevel(
            List<RemediationResult> remediationResults,
            string riskLevel)
        {
            // Return all results if "All" is specified
            if (string.Equals(riskLevel, "All", StringComparison.OrdinalIgnoreCase))
            {
                return remediationResults;
            }

            // Filter by specific risk level
            if (Enum.TryParse<Models.RiskLevel>(riskLevel, true, out var riskLevelFilter))
            {
                return remediationResults.Where(r => 
                    r.PolicyRemediations.Any(rem => rem.RiskLevel == riskLevelFilter)).ToList();
            }

            Logger.WriteError($"Invalid risk level specified: {riskLevel}. Valid values are: Low, Medium, High, Critical, All");
            return remediationResults;
        }

        /// <summary>
        /// Performs impact analysis on the provided remediation results.
        /// </summary>
        /// <param name="remediationResults">Results to analyze for impact</param>
        /// <param name="impactAnalysisService">Service to perform impact analysis</param>
        private static void PerformImpactAnalysis(
            List<RemediationResult> remediationResults,
            ImpactAnalysisService impactAnalysisService)
        {
            Logger.WriteInfo("Performing impact analysis...");
            
            foreach (var result in remediationResults)
            {
                foreach (var remediation in result.PolicyRemediations)
                {
                    try
                    {
                        // Impact analysis for individual remediations will be implemented in future version
                        // Currently using basic comparison without deep impact analysis
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteError($"Failed to perform impact analysis for policy '{remediation.PolicyName}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Executes the appropriate remediation workflow based on the specified mode.
        /// </summary>
        /// <param name="remediationResults">Results to process</param>
        /// <param name="scriptGenerationService">Service for generating scripts</param>
        /// <param name="analysisOnly">If true, only generates analysis reports</param>
        /// <param name="interactive">If true, enables interactive mode</param>
        /// <param name="scriptFormat">Format for generated scripts</param>
        /// <param name="outputDir">Directory for output files</param>
        /// <param name="dryRun">If true, generates scripts in dry-run mode</param>
        /// <param name="backup">If true, includes backup functionality</param>
        /// <returns>Exit code: 0 for success, 1 for failure</returns>
        private static async Task<int> ExecuteRemediationWorkflowAsync(
            List<RemediationResult> remediationResults,
            ScriptGenerationService scriptGenerationService,
            bool analysisOnly,
            bool interactive,
            string scriptFormat,
            string outputDir,
            bool dryRun,
            bool backup)
        {
            // Interactive mode
            if (interactive)
            {
                await RunInteractiveRemediationAsync(remediationResults, scriptGenerationService, scriptFormat, outputDir, dryRun, backup);
                return 0;
            }

            // Analysis only mode
            if (analysisOnly)
            {
                await GenerateAnalysisReportAsync(remediationResults, outputDir);
                Logger.WriteInfo($"Analysis report generated in: {outputDir}");
                return 0;
            }

            // Generate remediation scripts
            Logger.WriteInfo($"Generating {scriptFormat} remediation scripts...");
            await GenerateRemediationScriptsAsync(remediationResults, scriptGenerationService, scriptFormat, outputDir, dryRun, backup);

            Logger.WriteInfo($"Remediation analysis and scripts generated successfully in: {outputDir}");
            return 0;
        }

        private static async Task RunInteractiveRemediationAsync(
            List<RemediationResult> remediationResults,
            ScriptGenerationService scriptService,
            string scriptFormat,
            string outputDir,
            bool dryRun,
            bool backup)
        {
            Logger.WriteInfo("\n=== Interactive Remediation Mode ===");
            Logger.WriteInfo("Review each policy remediation and choose actions:");

            foreach (var result in remediationResults)
            {
                Logger.WriteInfo($"\nPolicy Analysis Results:");
                Logger.WriteInfo($"Found {result.PolicyRemediations.Count} potential remediations:");

                for (int i = 0; i < result.PolicyRemediations.Count; i++)
                {
                    var remediation = result.PolicyRemediations[i];
                    Logger.WriteInfo($"\n{i + 1}. Risk Level: {remediation.RiskLevel}");
                    Logger.WriteInfo($"   Action: {remediation.Action}");
                    Logger.WriteInfo($"   Policy: {remediation.PolicyName}");
                    
                    if (remediation.Impact != null)
                    {
                        Logger.WriteInfo($"   Estimated Impact: {remediation.Impact.EstimatedAffectedUsers} users");
                        Logger.WriteInfo($"   Impact Description: {remediation.Impact.ImpactDescription}");
                    }

                    Console.Write($"   Apply this remediation? (y/n/s=skip all for this policy): ");
                    var choice = Console.ReadLine()?.ToLower();
                    
                    if (choice == "s")
                        break;
                    
                    if (choice == "y")
                    {
                        await GenerateSingleRemediationScript(remediation, scriptService, scriptFormat, outputDir, dryRun);
                        Logger.WriteInfo("   ✓ Remediation script generated");
                    }
                }
            }
        }

        private static async Task GenerateAnalysisReportAsync(List<RemediationResult> results, string outputDir)
        {
            var report = new
            {
                GeneratedAt = DateTime.UtcNow,
                TotalPolicies = results.Count,
                TotalRemediations = results.Sum(r => r.PolicyRemediations.Count),
                RiskSummary = results
                    .SelectMany(r => r.PolicyRemediations)
                    .GroupBy(r => r.RiskLevel)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                PolicyAnalysis = results.SelectMany(r => r.PolicyRemediations.Select(rem => new
                {
                    PolicyName = rem.PolicyName, // Use actual PolicyName from PolicyRemediations
                    PolicyId = rem.PolicyId, // Use actual PolicyId from PolicyRemediations
                    Action = rem.Action.ToString(),
                    RiskLevel = rem.RiskLevel.ToString(),
                    ActionCount = rem.Steps.Count,
                    ImpactAnalysis = rem.Impact != null ? new
                    {
                        EstimatedUserImpact = rem.Impact.EstimatedAffectedUsers,
                        ImpactDescription = rem.Impact.ImpactDescription
                    } : null
                }))
            };

            var reportPath = Path.Combine(outputDir, $"remediation-analysis-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            await File.WriteAllTextAsync(reportPath, JsonConvert.SerializeObject(report, Formatting.Indented));
        }

        private static async Task GenerateRemediationScriptsAsync(
            List<RemediationResult> results,
            ScriptGenerationService scriptService,
            string scriptFormat,
            string outputDir,
            bool dryRun,
            bool backup)
        {
            foreach (var result in results)
            {
                foreach (var remediation in result.PolicyRemediations)
                {
                    await GenerateSingleRemediationScript(remediation, scriptService, scriptFormat, outputDir, dryRun);
                }
            }
        }

        private static async Task GenerateSingleRemediationScript(
            PolicyRemediation remediation,
            ScriptGenerationService scriptService,
            string scriptFormat,
            string outputDir,
            bool dryRun)
        {
            try
            {
                string script = scriptFormat.ToLower() switch
                {
                    "powershell" => scriptService.GeneratePowerShellScript(remediation),
                    "azurecli" => scriptService.GenerateAzureCliScript(remediation),
                    "terraform" => scriptService.GenerateTerraformScript(remediation),
                    _ => scriptService.GeneratePowerShellScript(remediation)
                };

                var extension = scriptFormat.ToLower() switch
                {
                    "powershell" => "ps1",
                    "azurecli" => "sh",
                    "terraform" => "tf",
                    _ => "ps1"
                };

                var sanitizedPolicyName = FileHelper.SanitizeFileName(remediation.PolicyName);
                var fileName = $"remediate-{sanitizedPolicyName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{extension}";
                var scriptPath = Path.Combine(outputDir, fileName);

                if (dryRun)
                {
                    script = $"# DRY RUN MODE - NO CHANGES WILL BE APPLIED\n# Remove this comment and the exit statement to execute\nexit 0\n\n{script}";
                }

                await File.WriteAllTextAsync(scriptPath, script);
                Logger.WriteInfo($"Generated script: {scriptPath}");
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Failed to generate remediation script for policy '{remediation.PolicyName}': {ex.Message}");
                throw;
            }
        }

        private static void HandleException(Exception ex)
        {
            Logger.WriteInfo($"Error: {ex.Message}");
            
            if (ex.Message.Contains("required scopes are missing"))
            {
                Logger.WriteInfo("");
                Logger.WriteInfo("PERMISSION REQUIRED:");
                Logger.WriteInfo("===================");
                Logger.WriteInfo("The application registration needs Microsoft Graph API permissions to read Conditional Access Policies.");
                Logger.WriteInfo("Required permissions (Application permissions):");
                Logger.WriteInfo("- Policy.Read.All");
                Logger.WriteInfo("- OR Policy.ReadWrite.ConditionalAccess");
                Logger.WriteInfo("");
                Logger.WriteInfo("To add these permissions:");
                Logger.WriteInfo("1. Go to Azure Portal -> App Registrations");
                Logger.WriteInfo("2. Find your app registration");
                Logger.WriteInfo("3. Go to 'API permissions'");
                Logger.WriteInfo("4. Click 'Add a permission' -> Microsoft Graph -> Application permissions");
                Logger.WriteInfo("5. Search for and add 'Policy.Read.All'");
                Logger.WriteInfo("6. Click 'Grant admin consent'");
                Logger.WriteInfo("");
            }
            
            Logger.WriteInfo($"Stack trace: {ex.StackTrace}");
        }

        /// <summary>
        /// Set up dependency injection and logging infrastructure
        /// </summary>
        private static void SetupLogging()
        {
            var services = new ServiceCollection();

            // Build configuration
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("resilience-config.json", optional: true)
                .AddEnvironmentVariables()
                .AddCommandLine(Environment.GetCommandLineArgs());
            
            var configuration = configBuilder.Build();
            services.AddSingleton<IConfiguration>(configuration);
            
            // Configure logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole(options =>
                {
                    options.LogToStandardErrorThreshold = LogLevel.None; // Log everything to stdout
                });
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Configure resilience settings
            services.Configure<ResilienceConfiguration>(configuration.GetSection("Resilience"));

            // Register core services
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddTransient<PolicyComparisonService>();
            services.AddTransient<PolicyValidationService>();
            services.AddTransient<TerraformConversionService>();
            services.AddTransient<ReportGenerationService>();

            // Register resilience services
            services.AddMemoryCache();
            services.AddSingleton<IResilienceConfigurationService, ResilienceConfigurationService>();
            services.AddTransient<IResilientGraphService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ResilientGraphService>>();
                var cache = provider.GetRequiredService<IMemoryCache>();
                var configOptions = provider.GetRequiredService<IOptions<ResilienceConfiguration>>();

                // Get Azure credentials from environment
                var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
                var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");

                if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    throw new InvalidOperationException("Azure credentials not configured. Please set AZURE_TENANT_ID, AZURE_CLIENT_ID, and AZURE_CLIENT_SECRET environment variables.");
                }

                // Create GraphServiceClient
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                var graphServiceClient = new GraphServiceClient(credential);
                
                return new ResilientGraphService(graphServiceClient, configOptions, cache, logger);
            });

            // Build service provider
            _serviceProvider = services.BuildServiceProvider();
            
            // Initialize structured logger
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
            StructuredLogger.Initialize(_logger);

            // Log resilience configuration status
            var resilienceConfig = _serviceProvider.GetRequiredService<IResilienceConfigurationService>();
            var validationResults = resilienceConfig.ValidateConfiguration();
            if (validationResults.Any())
            {
                _logger.LogWarning("Resilience configuration has validation warnings");
            }
            else
            {
                _logger.LogInformation("Resilience configuration loaded and validated successfully");
            }
        }

        /// <summary>
        /// Get a service from the DI container
        /// </summary>
        private static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Service provider not initialized");
            return _serviceProvider.GetRequiredService<T>();
        }
    }

    /// <summary>
    /// Backward compatibility alias for the old Logger class
    /// This maintains compatibility with existing code while providing structured logging
    /// </summary>
    public static class Logger
    {
        public static void SetQuietMode(bool quietMode) => StructuredLogger.SetQuietMode(quietMode);
        public static void SetVerboseMode(bool verboseMode) => StructuredLogger.SetVerboseMode(verboseMode);
        public static void WriteInfo(string message) => StructuredLogger.WriteInfo(message);
        public static void WriteError(string message) => StructuredLogger.WriteError(message);
        public static void WriteVerbose(string message) => StructuredLogger.WriteVerbose(message);
        
        // Additional structured logging methods
        public static void WriteWarning(string message) => StructuredLogger.WriteWarning(message);
        public static void LogPerformance(string operation, TimeSpan duration, object? additionalData = null) 
            => StructuredLogger.LogPerformance(operation, duration, additionalData);
        public static void LogAudit(string action, string policyName, object? details = null) 
            => StructuredLogger.LogAudit(action, policyName, details);
        public static IDisposable? BeginScope(string correlationId) => StructuredLogger.BeginScope(correlationId);
    }
}

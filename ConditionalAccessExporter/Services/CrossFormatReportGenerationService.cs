using ConditionalAccessExporter.Models;
using Newtonsoft.Json;
using System.Text;

namespace ConditionalAccessExporter.Services
{
    public class CrossFormatReportGenerationService
    {
        public async Task<string> GenerateReportAsync(CrossFormatComparisonResult comparisonResult, string outputPath, ReportFormat format = ReportFormat.Json)
        {
            switch (format)
            {
                case ReportFormat.Json:
                    return await GenerateJsonReportAsync(comparisonResult, outputPath);
                case ReportFormat.Html:
                    return await GenerateHtmlReportAsync(comparisonResult, outputPath);
                case ReportFormat.Markdown:
                    return await GenerateMarkdownReportAsync(comparisonResult, outputPath);
                case ReportFormat.Csv:
                    return await GenerateCsvReportAsync(comparisonResult, outputPath);
                default:
                    return await GenerateJsonReportAsync(comparisonResult, outputPath);
            }
        }

        private async Task<string> GenerateJsonReportAsync(CrossFormatComparisonResult comparisonResult, string outputPath)
        {
            var fileName = Path.Combine(outputPath, $"cross-format-comparison-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };

            var jsonContent = JsonConvert.SerializeObject(comparisonResult, jsonSettings);
            
            await File.WriteAllTextAsync(fileName, jsonContent);
            
            Console.WriteLine($"Cross-format comparison report generated: {fileName}");
            return fileName;
        }

        private async Task<string> GenerateHtmlReportAsync(CrossFormatComparisonResult comparisonResult, string outputPath)
        {
            var fileName = Path.Combine(outputPath, $"cross-format-comparison-{DateTime.UtcNow:yyyyMMdd-HHmmss}.html");
            
            var html = GenerateHtmlContent(comparisonResult);
            
            await File.WriteAllTextAsync(fileName, html);
            
            Console.WriteLine($"HTML cross-format comparison report generated: {fileName}");
            return fileName;
        }

        private async Task<string> GenerateMarkdownReportAsync(CrossFormatComparisonResult comparisonResult, string outputPath)
        {
            var fileName = Path.Combine(outputPath, $"cross-format-comparison-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md");
            
            var markdown = GenerateMarkdownContent(comparisonResult);
            
            await File.WriteAllTextAsync(fileName, markdown);
            
            Console.WriteLine($"Markdown cross-format comparison report generated: {fileName}");
            return fileName;
        }

        private async Task<string> GenerateCsvReportAsync(CrossFormatComparisonResult comparisonResult, string outputPath)
        {
            var fileName = Path.Combine(outputPath, $"cross-format-comparison-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
            
            var csv = GenerateCsvContent(comparisonResult);
            
            await File.WriteAllTextAsync(fileName, csv);
            
            Console.WriteLine($"CSV cross-format comparison report generated: {fileName}");
            return fileName;
        }

        private string GenerateHtmlContent(CrossFormatComparisonResult comparisonResult)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("    <title>Cross-Format Policy Comparison Report</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("        .header { background-color: #f5f5f5; padding: 20px; border-radius: 5px; margin-bottom: 20px; }");
            html.AppendLine("        .summary { background-color: #e8f4f8; padding: 15px; border-radius: 5px; margin-bottom: 20px; }");
            html.AppendLine("        .policy { border: 1px solid #ddd; margin-bottom: 15px; border-radius: 5px; overflow: hidden; }");
            html.AppendLine("        .policy-header { background-color: #f8f9fa; padding: 10px; font-weight: bold; }");
            html.AppendLine("        .policy-content { padding: 10px; }");
            html.AppendLine("        .status-identical { border-left: 4px solid #28a745; }");
            html.AppendLine("        .status-equivalent { border-left: 4px solid #17a2b8; }");
            html.AppendLine("        .status-different { border-left: 4px solid #ffc107; }");
            html.AppendLine("        .status-source-only { border-left: 4px solid #dc3545; }");
            html.AppendLine("        .status-reference-only { border-left: 4px solid #6c757d; }");
            html.AppendLine("        .differences { background-color: #fff3cd; padding: 10px; margin-top: 10px; border-radius: 3px; }");
            html.AppendLine("        .suggestions { background-color: #d1ecf1; padding: 10px; margin-top: 10px; border-radius: 3px; }");
            html.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
            html.AppendLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("        th { background-color: #f2f2f2; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header
            html.AppendLine("    <div class=\"header\">");
            html.AppendLine("        <h1>Cross-Format Policy Comparison Report</h1>");
            html.AppendLine($"        <p><strong>Generated:</strong> {comparisonResult.ComparedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");
            html.AppendLine($"        <p><strong>Source:</strong> {comparisonResult.SourceDirectory} ({comparisonResult.SourceFormat})</p>");
            html.AppendLine($"        <p><strong>Reference:</strong> {comparisonResult.ReferenceDirectory} ({comparisonResult.ReferenceFormat})</p>");
            html.AppendLine("    </div>");
            
            // Summary
            html.AppendLine("    <div class=\"summary\">");
            html.AppendLine("        <h2>Comparison Summary</h2>");
            html.AppendLine("        <table>");
            html.AppendLine("            <tr><th>Metric</th><th>Count</th></tr>");
            html.AppendLine($"            <tr><td>Total Source Policies</td><td>{comparisonResult.Summary.TotalSourcePolicies}</td></tr>");
            html.AppendLine($"            <tr><td>Total Reference Policies</td><td>{comparisonResult.Summary.TotalReferencePolicies}</td></tr>");
            html.AppendLine($"            <tr><td>Identical Policies</td><td>{comparisonResult.Summary.MatchingPolicies}</td></tr>");
            html.AppendLine($"            <tr><td>Semantically Equivalent Policies</td><td>{comparisonResult.Summary.SemanticallyEquivalentPolicies}</td></tr>");
            html.AppendLine($"            <tr><td>Policies with Differences</td><td>{comparisonResult.Summary.PoliciesWithDifferences}</td></tr>");
            html.AppendLine($"            <tr><td>Source-Only Policies</td><td>{comparisonResult.Summary.SourceOnlyPolicies}</td></tr>");
            html.AppendLine($"            <tr><td>Reference-Only Policies</td><td>{comparisonResult.Summary.ReferenceOnlyPolicies}</td></tr>");
            html.AppendLine("        </table>");
            html.AppendLine("    </div>");
            
            // Policy Comparisons
            html.AppendLine("    <h2>Policy Comparisons</h2>");
            
            foreach (var comparison in comparisonResult.PolicyComparisons)
            {
                var statusClass = comparison.Status.ToString().ToLower().Replace("_", "-");
                html.AppendLine($"    <div class=\"policy status-{statusClass}\">");
                html.AppendLine($"        <div class=\"policy-header\">");
                html.AppendLine($"            {comparison.PolicyName} - {comparison.Status}");
                html.AppendLine($"        </div>");
                html.AppendLine($"        <div class=\"policy-content\">");
                
                if (comparison.SourcePolicy != null)
                {
                    html.AppendLine($"            <p><strong>Source:</strong> {comparison.SourcePolicy.SourceFile} ({comparison.SourcePolicy.SourceFormat})</p>");
                }
                
                if (comparison.ReferencePolicy != null)
                {
                    html.AppendLine($"            <p><strong>Reference:</strong> {comparison.ReferencePolicy.SourceFile} ({comparison.ReferencePolicy.SourceFormat})</p>");
                }
                
                if (comparison.Differences != null && comparison.Differences.Any())
                {
                    html.AppendLine("            <div class=\"differences\">");
                    html.AppendLine("                <strong>Differences:</strong>");
                    html.AppendLine("                <ul>");
                    foreach (var difference in comparison.Differences)
                    {
                        html.AppendLine($"                    <li>{difference}</li>");
                    }
                    html.AppendLine("                </ul>");
                    html.AppendLine("            </div>");
                }
                
                if (comparison.ConversionSuggestions != null && comparison.ConversionSuggestions.Any())
                {
                    html.AppendLine("            <div class=\"suggestions\">");
                    html.AppendLine("                <strong>Conversion Suggestions:</strong>");
                    html.AppendLine("                <ul>");
                    foreach (var suggestion in comparison.ConversionSuggestions)
                    {
                        html.AppendLine($"                    <li>{suggestion}</li>");
                    }
                    html.AppendLine("                </ul>");
                    html.AppendLine("            </div>");
                }
                
                html.AppendLine("        </div>");
                html.AppendLine("    </div>");
            }
            
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        private string GenerateMarkdownContent(CrossFormatComparisonResult comparisonResult)
        {
            var md = new StringBuilder();
            
            md.AppendLine("# Cross-Format Policy Comparison Report");
            md.AppendLine();
            md.AppendLine($"**Generated:** {comparisonResult.ComparedAt:yyyy-MM-dd HH:mm:ss} UTC");
            md.AppendLine($"**Source:** {comparisonResult.SourceDirectory} ({comparisonResult.SourceFormat})");
            md.AppendLine($"**Reference:** {comparisonResult.ReferenceDirectory} ({comparisonResult.ReferenceFormat})");
            md.AppendLine();
            
            // Summary
            md.AppendLine("## Comparison Summary");
            md.AppendLine();
            md.AppendLine("| Metric | Count |");
            md.AppendLine("|--------|-------|");
            md.AppendLine($"| Total Source Policies | {comparisonResult.Summary.TotalSourcePolicies} |");
            md.AppendLine($"| Total Reference Policies | {comparisonResult.Summary.TotalReferencePolicies} |");
            md.AppendLine($"| Identical Policies | {comparisonResult.Summary.MatchingPolicies} |");
            md.AppendLine($"| Semantically Equivalent Policies | {comparisonResult.Summary.SemanticallyEquivalentPolicies} |");
            md.AppendLine($"| Policies with Differences | {comparisonResult.Summary.PoliciesWithDifferences} |");
            md.AppendLine($"| Source-Only Policies | {comparisonResult.Summary.SourceOnlyPolicies} |");
            md.AppendLine($"| Reference-Only Policies | {comparisonResult.Summary.ReferenceOnlyPolicies} |");
            md.AppendLine();
            
            // Policy Comparisons
            md.AppendLine("## Policy Comparisons");
            md.AppendLine();
            
            foreach (var comparison in comparisonResult.PolicyComparisons)
            {
                md.AppendLine($"### {comparison.PolicyName} - {comparison.Status}");
                md.AppendLine();
                
                if (comparison.SourcePolicy != null)
                {
                    md.AppendLine($"**Source:** {comparison.SourcePolicy.SourceFile} ({comparison.SourcePolicy.SourceFormat})");
                }
                
                if (comparison.ReferencePolicy != null)
                {
                    md.AppendLine($"**Reference:** {comparison.ReferencePolicy.SourceFile} ({comparison.ReferencePolicy.SourceFormat})");
                }
                
                if (comparison.Differences != null && comparison.Differences.Any())
                {
                    md.AppendLine();
                    md.AppendLine("**Differences:**");
                    foreach (var difference in comparison.Differences)
                    {
                        md.AppendLine($"- {difference}");
                    }
                }
                
                if (comparison.ConversionSuggestions != null && comparison.ConversionSuggestions.Any())
                {
                    md.AppendLine();
                    md.AppendLine("**Conversion Suggestions:**");
                    foreach (var suggestion in comparison.ConversionSuggestions)
                    {
                        md.AppendLine($"- {suggestion}");
                    }
                }
                
                md.AppendLine();
                md.AppendLine("---");
                md.AppendLine();
            }
            
            return md.ToString();
        }

        private string GenerateCsvContent(CrossFormatComparisonResult comparisonResult)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("PolicyName,Status,SourceFile,SourceFormat,ReferenceFile,ReferenceFormat,DifferenceCount,HasSuggestions");
            
            // Data rows
            foreach (var comparison in comparisonResult.PolicyComparisons)
            {
                var sourceName = comparison.SourcePolicy?.SourceFile ?? "";
                var sourceFormat = comparison.SourcePolicy?.SourceFormat.ToString() ?? "";
                var referenceName = comparison.ReferencePolicy?.SourceFile ?? "";
                var referenceFormat = comparison.ReferencePolicy?.SourceFormat.ToString() ?? "";
                var differenceCount = comparison.Differences?.Count ?? 0;
                var hasSuggestions = comparison.ConversionSuggestions?.Any() == true;
                
                csv.AppendLine($"\"{comparison.PolicyName}\",\"{comparison.Status}\",\"{sourceName}\",\"{sourceFormat}\",\"{referenceName}\",\"{referenceFormat}\",{differenceCount},{hasSuggestions}");
            }
            
            return csv.ToString();
        }
    }

    public enum ReportFormat
    {
        Json,
        Html,
        Markdown,
        Csv
    }
}
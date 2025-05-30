using ConditionalAccessExporter.Models;
using Newtonsoft.Json;
using System.Text;

namespace ConditionalAccessExporter.Services
{
    public class ReportGenerationService
    {
        public async Task GenerateReportsAsync(ComparisonResult result, string outputDirectory, List<string> formats)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var baseFileName = $"CA_Comparison_{timestamp}";

            foreach (var format in formats)
            {
                switch (format.ToLowerInvariant())
                {
                    case "json":
                        await GenerateJsonReportAsync(result, Path.Combine(outputDirectory, $"{baseFileName}.json"));
                        break;
                    case "html":
                        await GenerateHtmlReportAsync(result, Path.Combine(outputDirectory, $"{baseFileName}.html"));
                        break;
                    case "console":
                        GenerateConsoleReport(result);
                        break;
                    case "csv":
                        await GenerateCsvReportAsync(result, Path.Combine(outputDirectory, $"{baseFileName}.csv"));
                        break;
                    default:
                        Console.WriteLine($"Warning: Unknown report format '{format}'");
                        break;
                }
            }
        }

        private async Task GenerateJsonReportAsync(ComparisonResult result, string filePath)
        {
            var json = JsonConvert.SerializeObject(result, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });

            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
            Console.WriteLine($"JSON report generated: {filePath}");
        }

        private async Task GenerateHtmlReportAsync(ComparisonResult result, string filePath)
        {
            var html = GenerateHtmlContent(result);
            await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);
            Console.WriteLine($"HTML report generated: {filePath}");
        }

        private async Task GenerateCsvReportAsync(ComparisonResult result, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("PolicyName,PolicyId,Status,ReferenceFile,HasDifferences");

            foreach (var comparison in result.PolicyComparisons)
            {
                csv.AppendLine($"\"{comparison.PolicyName}\",\"{comparison.PolicyId}\",\"{comparison.Status}\",\"{comparison.ReferenceFileName ?? ""}\",\"{comparison.Differences != null}\"");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
            Console.WriteLine($"CSV report generated: {filePath}");
        }

        public void GenerateConsoleReport(ComparisonResult result)
        {
            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine("CONDITIONAL ACCESS POLICY COMPARISON REPORT");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($"Compared At: {result.ComparedAt:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"Tenant ID: {result.TenantId}");
            Console.WriteLine($"Reference Directory: {result.ReferenceDirectory}");
            Console.WriteLine();

            // Summary
            Console.WriteLine("SUMMARY:");
            Console.WriteLine("-".PadRight(40, '-'));
            Console.WriteLine($"Total Entra Policies: {result.Summary.TotalEntraPolicies}");
            Console.WriteLine($"Total Reference Policies: {result.Summary.TotalReferencePolicies}");
            Console.WriteLine($"Policies only in Entra: {result.Summary.EntraOnlyPolicies}");
            Console.WriteLine($"Policies only in Reference: {result.Summary.ReferenceOnlyPolicies}");
            Console.WriteLine($"Matching Policies: {result.Summary.MatchingPolicies}");
            Console.WriteLine($"Policies with Differences: {result.Summary.PoliciesWithDifferences}");
            Console.WriteLine();

            // Detailed results
            if (result.Summary.EntraOnlyPolicies > 0)
            {
                Console.WriteLine("POLICIES ONLY IN ENTRA:");
                Console.WriteLine("-".PadRight(40, '-'));
                foreach (var policy in result.PolicyComparisons.Where(p => p.Status == ComparisonStatus.EntraOnly))
                {
                    Console.WriteLine($"  • {policy.PolicyName} (ID: {policy.PolicyId})");
                }
                Console.WriteLine();
            }

            if (result.Summary.ReferenceOnlyPolicies > 0)
            {
                Console.WriteLine("POLICIES ONLY IN REFERENCE:");
                Console.WriteLine("-".PadRight(40, '-'));
                foreach (var policy in result.PolicyComparisons.Where(p => p.Status == ComparisonStatus.ReferenceOnly))
                {
                    Console.WriteLine($"  • {policy.PolicyName} (File: {policy.ReferenceFileName})");
                }
                Console.WriteLine();
            }

            if (result.Summary.PoliciesWithDifferences > 0)
            {
                Console.WriteLine("POLICIES WITH DIFFERENCES:");
                Console.WriteLine("-".PadRight(40, '-'));
                foreach (var policy in result.PolicyComparisons.Where(p => p.Status == ComparisonStatus.Different))
                {
                    Console.WriteLine($"  • {policy.PolicyName}");
                    Console.WriteLine($"    Reference File: {policy.ReferenceFileName}");
                    Console.WriteLine($"    Policy ID: {policy.PolicyId}");
                    Console.WriteLine();
                }
            }

            if (result.Summary.MatchingPolicies > 0)
            {
                Console.WriteLine("IDENTICAL POLICIES:");
                Console.WriteLine("-".PadRight(40, '-'));
                foreach (var policy in result.PolicyComparisons.Where(p => p.Status == ComparisonStatus.Identical))
                {
                    Console.WriteLine($"  ✓ {policy.PolicyName}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("=".PadRight(80, '='));
        }

        private string GenerateHtmlContent(ComparisonResult result)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head>");
            html.AppendLine("<title>Conditional Access Policy Comparison Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; margin: 20px 0; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #f2f2f2; }");
            html.AppendLine(".status-entra-only { background-color: #ffe6e6; }");
            html.AppendLine(".status-reference-only { background-color: #e6f3ff; }");
            html.AppendLine(".status-different { background-color: #fff2e6; }");
            html.AppendLine(".status-identical { background-color: #e6ffe6; }");
            html.AppendLine(".summary { background-color: #f9f9f9; padding: 15px; border-radius: 5px; }");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");

            html.AppendLine("<h1>Conditional Access Policy Comparison Report</h1>");
            
            // Metadata
            html.AppendLine("<div class=\"summary\">");
            html.AppendLine($"<p><strong>Compared At:</strong> {result.ComparedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");
            html.AppendLine($"<p><strong>Tenant ID:</strong> {result.TenantId}</p>");
            html.AppendLine($"<p><strong>Reference Directory:</strong> {result.ReferenceDirectory}</p>");
            html.AppendLine("</div>");

            // Summary table
            html.AppendLine("<h2>Summary</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Metric</th><th>Count</th></tr>");
            html.AppendLine($"<tr><td>Total Entra Policies</td><td>{result.Summary.TotalEntraPolicies}</td></tr>");
            html.AppendLine($"<tr><td>Total Reference Policies</td><td>{result.Summary.TotalReferencePolicies}</td></tr>");
            html.AppendLine($"<tr><td>Policies only in Entra</td><td>{result.Summary.EntraOnlyPolicies}</td></tr>");
            html.AppendLine($"<tr><td>Policies only in Reference</td><td>{result.Summary.ReferenceOnlyPolicies}</td></tr>");
            html.AppendLine($"<tr><td>Matching Policies</td><td>{result.Summary.MatchingPolicies}</td></tr>");
            html.AppendLine($"<tr><td>Policies with Differences</td><td>{result.Summary.PoliciesWithDifferences}</td></tr>");
            html.AppendLine("</table>");

            // Detailed comparison table
            html.AppendLine("<h2>Detailed Comparison</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Policy Name</th><th>Policy ID</th><th>Status</th><th>Reference File</th><th>Has Differences</th></tr>");

            foreach (var comparison in result.PolicyComparisons.OrderBy(p => p.PolicyName))
            {
                var cssClass = comparison.Status switch
                {
                    ComparisonStatus.EntraOnly => "status-entra-only",
                    ComparisonStatus.ReferenceOnly => "status-reference-only",
                    ComparisonStatus.Different => "status-different",
                    ComparisonStatus.Identical => "status-identical",
                    _ => ""
                };

                html.AppendLine($"<tr class=\"{cssClass}\">");
                html.AppendLine($"<td>{comparison.PolicyName}</td>");
                html.AppendLine($"<td>{comparison.PolicyId}</td>");
                html.AppendLine($"<td>{comparison.Status}</td>");
                html.AppendLine($"<td>{comparison.ReferenceFileName ?? ""}</td>");
                html.AppendLine($"<td>{(comparison.Differences != null ? "Yes" : "No")}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</table>");
            html.AppendLine("</body></html>");

            return html.ToString();
        }
    }
}
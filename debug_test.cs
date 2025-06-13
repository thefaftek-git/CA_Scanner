using System;
using System.IO;
using ConditionalAccessExporter.Tests;
using ConditionalAccessExporter.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var service = new CrossFormatPolicyComparisonService();
        
        // Create temp directories
        var sourceDir = Path.Combine(Path.GetTempPath(), "debug_source");
        var referenceDir = Path.Combine(Path.GetTempPath(), "debug_reference");
        
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(referenceDir);
        
        try
        {
            // Create one JSON policy
            var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("policy-001", "Test Policy 1");
            File.WriteAllText(Path.Combine(sourceDir, "policy-001.json"), jsonPolicy.ToString());
            
            // Create one Terraform policy
            var terraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("policy_001", "Test Policy 1");
            File.WriteAllText(Path.Combine(referenceDir, "policy-001.tf"), terraformPolicy);
            
            // Create matching options
            var matchingOptions = TestDataFactory.CreateMatchingOptions();
            
            // Compare
            var result = await service.CompareAsync(sourceDir, referenceDir, matchingOptions);
            
            Console.WriteLine($"Total source policies: {result.Summary.TotalSourcePolicies}");
            Console.WriteLine($"Total reference policies: {result.Summary.TotalReferencePolicies}");
            Console.WriteLine($"Matching policies: {result.Summary.MatchingPolicies}");
            Console.WriteLine($"Semantically equivalent: {result.Summary.SemanticallyEquivalentPolicies}");
            
            if (result.PolicyComparisons.Count > 0)
            {
                var comparison = result.PolicyComparisons[0];
                Console.WriteLine($"First comparison status: {comparison.Status}");
                if (comparison.SourcePolicy != null)
                {
                    Console.WriteLine($"Source policy name: '{comparison.SourcePolicy.DisplayName}'");
                }
                if (comparison.ReferencePolicy != null)
                {
                    Console.WriteLine($"Reference policy name: '{comparison.ReferencePolicy.DisplayName}'");
                }
                else
                {
                    Console.WriteLine("No reference policy found for matching");
                }
            }
        }
        finally
        {
            Directory.Delete(sourceDir, true);
            Directory.Delete(referenceDir, true);
        }
    }
}

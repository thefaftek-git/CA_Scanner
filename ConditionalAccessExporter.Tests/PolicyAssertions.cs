
using Xunit;
using ConditionalAccessExporter.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConditionalAccessExporter.Tests
{
    /// <summary>
    /// Custom assertion helpers for policy comparison scenarios
    /// </summary>
    public static class PolicyAssertions
    {
        /// <summary>
        /// Asserts that a comparison result has the expected status
        /// </summary>
        public static void AssertComparisonStatus(CrossFormatPolicyComparison comparison, params CrossFormatComparisonStatus[] expectedStatuses)
        {
            Assert.NotNull(comparison);
            Assert.Contains(comparison.Status, expectedStatuses);
        }

        /// <summary>
        /// Asserts that a comparison result indicates identical or semantically equivalent policies
        /// </summary>
        public static void AssertPoliciesMatch(CrossFormatPolicyComparison comparison)
        {
            AssertComparisonStatus(comparison, 
                CrossFormatComparisonStatus.Identical, 
                CrossFormatComparisonStatus.SemanticallyEquivalent);
        }

        /// <summary>
        /// Asserts that a comparison result indicates different policies
        /// </summary>
        public static void AssertPoliciesDiffer(CrossFormatPolicyComparison comparison)
        {
            AssertComparisonStatus(comparison, CrossFormatComparisonStatus.Different);
            Assert.NotNull(comparison.Differences);
            Assert.NotEmpty(comparison.Differences);
        }

        /// <summary>
        /// Asserts that a summary has positive counts for matching policies
        /// </summary>
        public static void AssertHasMatchingPolicies(CrossFormatComparisonSummary summary)
        {
            Assert.NotNull(summary);
            var totalMatching = summary.MatchingPolicies + summary.SemanticallyEquivalentPolicies;
            Assert.True(totalMatching > 0, 
                $"Expected matching policies but found: Matching={summary.MatchingPolicies}, SemanticallyEquivalent={summary.SemanticallyEquivalentPolicies}");
        }

        /// <summary>
        /// Asserts that a summary indicates no matching policies
        /// </summary>
        public static void AssertNoMatchingPolicies(CrossFormatComparisonSummary summary)
        {
            Assert.NotNull(summary);
            Assert.Equal(0, summary.MatchingPolicies);
            Assert.Equal(0, summary.SemanticallyEquivalentPolicies);
        }

        /// <summary>
        /// Asserts that a comparison result contains a minimum number of source policies
        /// </summary>
        public static void AssertMinimumSourcePolicies(CrossFormatComparisonResult result, int minimumCount)
        {
            Assert.NotNull(result);
            Assert.NotNull(result.Summary);
            Assert.True(result.Summary.TotalSourcePolicies >= minimumCount,
                $"Expected at least {minimumCount} source policies but found {result.Summary.TotalSourcePolicies}");
        }

        /// <summary>
        /// Asserts that a comparison result contains a minimum number of reference policies
        /// </summary>
        public static void AssertMinimumReferencePolicies(CrossFormatComparisonResult result, int minimumCount)
        {
            Assert.NotNull(result);
            Assert.NotNull(result.Summary);
            Assert.True(result.Summary.TotalReferencePolicies >= minimumCount,
                $"Expected at least {minimumCount} reference policies but found {result.Summary.TotalReferencePolicies}");
        }

        /// <summary>
        /// Asserts that the comparison result has valid metadata
        /// </summary>
        public static void AssertValidComparisonMetadata(CrossFormatComparisonResult result, string expectedSourceDir, string expectedReferenceDir)
        {
            Assert.NotNull(result);
            Assert.Equal(expectedSourceDir, result.SourceDirectory);
            Assert.Equal(expectedReferenceDir, result.ReferenceDirectory);
            Assert.True(result.ComparedAt > DateTime.MinValue);
            Assert.True(result.ComparedAt <= DateTime.UtcNow.AddMinutes(1)); // Allow small time buffer
        }

        /// <summary>
        /// Asserts that a policy comparison contains expected differences in specific fields
        /// </summary>
        public static void AssertContainsDifference(CrossFormatPolicyComparison comparison, string fieldName)
        {
            Assert.NotNull(comparison);
            Assert.NotNull(comparison.Differences);
            Assert.Contains(comparison.Differences, d => d.Contains(fieldName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Asserts that a comparison result has the expected format detection
        /// </summary>
        public static void AssertFormatsDetected(CrossFormatComparisonResult result, PolicyFormat expectedSourceFormat, PolicyFormat expectedReferenceFormat)
        {
            Assert.NotNull(result);
            Assert.Equal(expectedSourceFormat, result.SourceFormat);
            Assert.Equal(expectedReferenceFormat, result.ReferenceFormat);
        }

        /// <summary>
        /// Asserts that execution time is within acceptable bounds
        /// </summary>
        public static void AssertExecutionTimeWithinBounds(TimeSpan executionTime, TimeSpan maxExpected)
        {
            Assert.True(executionTime <= maxExpected,
                $"Execution time {executionTime.TotalSeconds:F2}s exceeded maximum expected {maxExpected.TotalSeconds:F2}s");
        }

        /// <summary>
        /// Asserts that a JSON policy has the expected basic structure
        /// </summary>
        public static void AssertValidJsonPolicy(JObject policy)
        {
            Assert.NotNull(policy);
            Assert.NotNull(policy["Id"]);
            Assert.NotNull(policy["DisplayName"]);
            Assert.NotNull(policy["State"]);
            Assert.NotEmpty(policy["Id"].ToString());
            Assert.NotEmpty(policy["DisplayName"].ToString());
        }

        /// <summary>
        /// Asserts that a summary has expected total counts
        /// </summary>
        public static void AssertSummaryTotals(CrossFormatComparisonSummary summary, int expectedSource, int expectedReference)
        {
            Assert.NotNull(summary);
            Assert.Equal(expectedSource, summary.TotalSourcePolicies);
            Assert.Equal(expectedReference, summary.TotalReferencePolicies);
        }

        /// <summary>
        /// Asserts that a comparison result contains exactly the expected number of comparisons
        /// </summary>
        public static void AssertComparisonCount(CrossFormatComparisonResult result, int expectedCount)
        {
            Assert.NotNull(result);
            Assert.NotNull(result.PolicyComparisons);
            Assert.Equal(expectedCount, result.PolicyComparisons.Count);
        }

        /// <summary>
        /// Asserts that file operations completed successfully
        /// </summary>
        public static void AssertFileExists(string filePath, string fileDescription = "file")
        {
            Assert.True(System.IO.File.Exists(filePath), 
                $"Expected {fileDescription} to exist at path: {filePath}");
        }

        /// <summary>
        /// Asserts that directory operations completed successfully
        /// </summary>
        public static void AssertDirectoryExists(string directoryPath, string directoryDescription = "directory")
        {
            Assert.True(System.IO.Directory.Exists(directoryPath),
                $"Expected {directoryDescription} to exist at path: {directoryPath}");
        }

        /// <summary>
        /// Asserts that a collection is not empty with a descriptive message
        /// </summary>
        public static void AssertNotEmpty<T>(IEnumerable<T> collection, string itemDescription)
        {
            Assert.NotNull(collection);
            Assert.NotEmpty(collection);
            Assert.True(collection.Any(), $"Expected to find {itemDescription} but collection was empty");
        }

        /// <summary>
        /// Asserts that a percentage value is within valid bounds
        /// </summary>
        public static void AssertValidPercentage(double percentage, string percentageDescription)
        {
            Assert.True(percentage >= 0.0 && percentage <= 100.0,
                $"{percentageDescription} should be between 0 and 100, but was {percentage:F2}");
        }
    }
}


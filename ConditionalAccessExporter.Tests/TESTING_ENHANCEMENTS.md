
# Testing Enhancements for CA_Scanner

This document provides detailed implementation examples and best practices for improving test assertions and adding comprehensive error scenario coverage.

## Overview

This enhancement addresses Issue #94 by:
1. Replacing generic `Assert.True()` calls with specific assertions
2. Adding comprehensive error scenario coverage  
3. Creating edge case testing
4. Implementing test data factory for consistent test data management
5. Adding custom assertion helpers for policy comparison
6. Adding performance tests for large datasets

## Test Assertion Improvements

### Before and After Examples

#### Generic Assert.True() → Specific Assertions

**Before:**
```csharp
Assert.True(result.Summary.TotalSourcePolicies >= 1);
Assert.True(comparison.Status == CrossFormatComparisonStatus.Identical || 
           comparison.Status == CrossFormatComparisonStatus.SemanticallyEquivalent);
Assert.True(result.SuccessRate >= 99.0);
```

**After:**
```csharp
PolicyAssertions.AssertMinimumSourcePolicies(result, 1);
PolicyAssertions.AssertPoliciesMatch(comparison);
Assert.InRange(result.SuccessRate, 99.0, 100.0);
```

### Custom Assertion Helpers

Created `PolicyAssertions.cs` with specific assertion methods:

```csharp
// Policy comparison assertions
PolicyAssertions.AssertPoliciesMatch(comparison);
PolicyAssertions.AssertPoliciesDiffer(comparison);
PolicyAssertions.AssertComparisonStatus(comparison, CrossFormatComparisonStatus.Identical);

// Summary assertions
PolicyAssertions.AssertHasMatchingPolicies(summary);
PolicyAssertions.AssertSummaryTotals(summary, expectedSource, expectedReference);

// Metadata assertions
PolicyAssertions.AssertValidComparisonMetadata(result, sourceDir, referenceDir);
PolicyAssertions.AssertFormatsDetected(result, PolicyFormat.Json, PolicyFormat.Terraform);

// Performance assertions
PolicyAssertions.AssertExecutionTimeWithinBounds(elapsed, maxExpected);
```

## Test Data Factory

Created `TestDataFactory.cs` for consistent test data creation:

```csharp
// Basic policy creation
var policy = TestDataFactory.CreateBasicJsonPolicy("id", "name", "enabled");
var terraform = TestDataFactory.CreateBasicTerraformPolicy("resource", "name", "enabled");

// Complex policies with conditions
var complexPolicy = TestDataFactory.CreateComplexJsonPolicy(
    id: "policy-id",
    displayName: "Complex Policy", 
    includeUsers: new[] { "user1", "user2" },
    excludeUsers: new[] { "excluded-user" },
    includeApplications: new[] { "All" },
    excludeApplications: new[] { "excluded-app" }
);

// Error scenarios
var malformedJson = TestDataFactory.CreateMalformedJson();
var invalidTerraform = TestDataFactory.CreateInvalidTerraform();

// Large datasets
var largePolicySet = TestDataFactory.CreateLargePolicyDataset(1000);

// Matching options
var options = TestDataFactory.CreateMatchingOptions(
    strategy: CrossFormatMatchingStrategy.ByName,
    caseSensitive: false,
    enableSemanticComparison: true
);
```

## Error Scenario Tests

### File System Error Handling

```csharp
[Fact]
public async Task ComparePoliciesAsync_NonExistentSourceDirectory_ShouldHandleGracefully()
{
    // Arrange
    var nonExistentSource = Path.Combine(_testDirectory, "nonexistent-source");
    var referenceDir = CreateReferenceDirectory();
    var matchingOptions = TestDataFactory.CreateMatchingOptions();

    // Act
    var result = await _service.CompareAsync(nonExistentSource, referenceDir, matchingOptions);

    // Assert
    Assert.NotNull(result);
    PolicyAssertions.AssertSummaryTotals(result.Summary, 0, expectedReference);
}
```

### Malformed Input Handling

```csharp
[Fact]
public async Task ComparePoliciesAsync_MalformedJsonFile_ShouldSkipAndContinue()
{
    // Arrange
    var sourceDir = CreateSourceDirectory();
    var malformedJsonPath = Path.Combine(sourceDir, "malformed.json");
    await File.WriteAllTextAsync(malformedJsonPath, TestDataFactory.CreateMalformedJson());
    
    // Create valid policy to ensure processing continues
    var validPolicy = TestDataFactory.CreateBasicJsonPolicy("valid-policy", "Valid Policy");
    WriteJsonPolicyToFile(sourceDir, "valid.json", validPolicy);

    // Act
    var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

    // Assert
    Assert.NotNull(result);
    PolicyAssertions.AssertMinimumSourcePolicies(result, 1); // Should load valid policy
}
```

### Invalid Terraform Syntax

```csharp
[Fact]
public async Task ComparePoliciesAsync_InvalidTerraformSyntax_ShouldSkipAndContinue()
{
    // Arrange
    var referenceDir = CreateReferenceDirectory();
    var invalidTerraformPath = Path.Combine(referenceDir, "invalid.tf");
    await File.WriteAllTextAsync(invalidTerraformPath, TestDataFactory.CreateInvalidTerraform());
    
    // Create valid Terraform file
    var validTerraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("valid_policy", "Valid Policy");
    WriteTerraformPolicyToFile(referenceDir, "valid.tf", validTerraformPolicy);

    // Act & Assert
    var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);
    PolicyAssertions.AssertMinimumReferencePolicies(result, 1);
}
```

## Edge Case Testing

### Empty Directories

```csharp
[Fact]
public async Task ComparePoliciesAsync_BothDirectoriesEmpty_ShouldReturnEmptyResult()
{
    // Arrange
    var sourceDir = CreateSourceDirectory(); // Empty
    var referenceDir = CreateReferenceDirectory(); // Empty
    var matchingOptions = TestDataFactory.CreateMatchingOptions();

    // Act
    var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);

    // Assert
    PolicyAssertions.AssertSummaryTotals(result.Summary, 0, 0);
    Assert.Empty(result.PolicyComparisons);
}
```

### Special Characters and Unicode

```csharp
[Fact]
public async Task ComparePoliciesAsync_UnicodeCharacters_ShouldHandleCorrectly()
{
    // Arrange
    var unicodeName = "Política de Acesso Condicional 测试政策 πολιτική اسياسة";
    var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("unicode-policy", unicodeName);
    var terraformPolicy = TestDataFactory.CreateBasicTerraformPolicy("unicode_policy", unicodeName);

    // Act & Assert
    var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);
    PolicyAssertions.AssertMinimumSourcePolicies(result, 1);
    PolicyAssertions.AssertMinimumReferencePolicies(result, 1);
}
```

### Very Long Policy Names

```csharp
[Fact]
public async Task ComparePoliciesAsync_VeryLongPolicyNames_ShouldHandleCorrectly()
{
    // Arrange
    var longName = new string('A', 500); // Very long policy name
    var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy("long-policy", longName);

    // Act & Assert - Should handle without truncation or errors
    var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);
    Assert.NotNull(result);
}
```

## Performance Testing

### Large Dataset Handling

```csharp
[Fact]
public async Task ComparePoliciesAsync_LargeNumberOfPolicies_ShouldHandleEfficiently()
{
    // Arrange
    const int policyCount = 100;
    
    // Create many policies using factory
    for (int i = 0; i < policyCount; i++)
    {
        var jsonPolicy = TestDataFactory.CreateBasicJsonPolicy($"policy-{i:D3}", $"Test Policy {i}");
        WriteJsonPolicyToFile(sourceDir, $"policy-{i:D3}.json", jsonPolicy);
    }

    // Act with timing
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);
    stopwatch.Stop();

    // Assert performance bounds
    PolicyAssertions.AssertExecutionTimeWithinBounds(stopwatch.Elapsed, TimeSpan.FromSeconds(30));
    PolicyAssertions.AssertSummaryTotals(result.Summary, policyCount, policyCount);
}
```

### Complex Policy Performance

```csharp
[Fact]
public async Task ComparePoliciesAsync_PerformanceWithManyComplexPolicies_ShouldCompleteWithinReasonableTime()
{
    // Arrange
    const int policyCount = 50;
    
    for (int i = 0; i < policyCount; i++)
    {
        var complexPolicy = TestDataFactory.CreateComplexJsonPolicy(
            $"policy-{i:D3}", 
            $"Complex Test Policy {i}",
            includeUsers: new[] { $"user-{i}", $"user-{i + 1000}" },
            excludeUsers: new[] { $"excluded-user-{i}" },
            includeApplications: new[] { "All" },
            excludeApplications: new[] { $"excluded-app-{i}" }
        );
        WriteJsonPolicyToFile(sourceDir, $"complex-policy-{i:D3}.json", complexPolicy);
    }

    // Act & Assert with performance bounds
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);
    stopwatch.Stop();

    PolicyAssertions.AssertExecutionTimeWithinBounds(stopwatch.Elapsed, TimeSpan.FromMinutes(2));
    PolicyAssertions.AssertHasMatchingPolicies(result.Summary);
}
```

## Async Testing Best Practices

### Proper Cancellation Testing

```csharp
[Fact]
public async Task ComparePoliciesAsync_WithCancellationToken_ShouldRespectCancellation()
{
    // Arrange
    using var cts = new CancellationTokenSource();
    var largeDataset = TestDataFactory.CreateLargePolicyDataset(1000);
    
    // Create many files to ensure operation takes time
    foreach (var policy in largeDataset.Take(100))
    {
        WriteJsonPolicyToFile(sourceDir, $"policy-{policy["Id"]}.json", policy);
    }

    // Act - Cancel after short delay
    var task = _service.CompareAsync(sourceDir, referenceDir, matchingOptions, cts.Token);
    await Task.Delay(100); // Let operation start
    cts.Cancel();

    // Assert
    await Assert.ThrowsAsync<OperationCanceledException>(() => task);
}
```

### Timeout Testing

```csharp
[Fact]
public async Task ComparePoliciesAsync_WithTimeout_ShouldCompleteWithinBounds()
{
    // Arrange
    var timeout = TimeSpan.FromSeconds(10);
    using var cts = new CancellationTokenSource(timeout);

    // Act
    var task = _service.CompareAsync(sourceDir, referenceDir, matchingOptions, cts.Token);
    
    // Assert - Should not timeout for reasonable dataset
    var result = await task; // Will throw if timeout exceeded
    Assert.NotNull(result);
}
```

## Integration with Existing Test Infrastructure

### Using Console Output Test Collection

```csharp
[Collection("Console Output Tests")]
public class EnhancedCrossFormatPolicyComparisonServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    
    public EnhancedCrossFormatPolicyComparisonServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ComparePoliciesAsync_WithVerboseOutput_ShouldCaptureDetails()
    {
        // Arrange with test output helper for debugging
        _output.WriteLine("Starting comparison test with verbose output");
        
        // Act
        var result = await _service.CompareAsync(sourceDir, referenceDir, matchingOptions);
        
        // Assert with detailed output
        _output.WriteLine($"Comparison completed. Source: {result.Summary.TotalSourcePolicies}, Reference: {result.Summary.TotalReferencePolicies}");
        PolicyAssertions.AssertHasMatchingPolicies(result.Summary);
    }
}
```

## Implementation Guidelines for Future Tests

### 1. Use Specific Assertions
- Replace `Assert.True(condition)` with `Assert.InRange()`, `Assert.Contains()`, or custom assertions
- Use `PolicyAssertions` helpers for policy-specific validations
- Provide meaningful error messages in assertions

### 2. Test Error Scenarios
- Always test what happens with malformed input
- Test file system errors (missing directories, permission issues)
- Test network failures for remote operations
- Test resource exhaustion scenarios

### 3. Test Edge Cases
- Empty inputs, null values
- Very large datasets
- Special characters, Unicode text
- Boundary conditions (min/max values)

### 4. Use Test Data Factory
- Use `TestDataFactory` methods instead of inline test data creation
- Create reusable test data builders for complex scenarios
- Keep test data focused and minimal

### 5. Performance Considerations
- Add timing assertions for operations that should complete quickly
- Test with realistic dataset sizes
- Use `Assert.InRange()` for performance bounds
- Consider memory usage in large dataset tests

### 6. Async Testing
- Properly test cancellation token support
- Test timeout scenarios
- Use appropriate async/await patterns
- Handle exceptions correctly in async tests

## Benefits

These enhancements provide:

1. **Better Test Failures**: More descriptive error messages when tests fail
2. **Higher Confidence**: Better coverage of error conditions and edge cases
3. **Maintainability**: Easier to understand and maintain tests
4. **Quality**: Catch more potential issues before production
5. **Performance Awareness**: Ensure operations complete within acceptable timeframes
6. **Consistency**: Standardized test data creation and assertion patterns

## Future Enhancements

Consider adding:
- Property-based testing for comprehensive input validation
- Load testing with realistic Azure policy datasets
- Memory usage monitoring in performance tests
- Integration tests with actual Azure services (using test tenants)
- Automated test data generation from real policy schemas


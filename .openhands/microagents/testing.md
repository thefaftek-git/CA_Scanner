---
triggers:
  - test
  - testing
  - "unit test"
  - "integration test"
  - xunit
  - mock
  - "test data"
---

# Testing Microagent

This microagent provides specialized guidance for testing within the CA_Scanner project, including unit tests, integration tests, and test patterns.

## Testing Architecture Overview

### Test Project
- **Test Project**: `ConditionalAccessExporter.Tests/` - Main test project
- **Test Framework**: Uses xUnit testing framework
- **Test Organization**: Separate test classes for each service
- **Test Collection**: `ConsoleOutputTestCollection.cs` for managing console output in tests

### Key Test Files and Patterns

#### Service Tests
Each service has corresponding `*Tests.cs` file:
- `BaselineGenerationServiceTests.cs`
- `PolicyComparisonServiceTests.cs`
- `TerraformConversionServiceTests.cs`
- `ReportGenerationServiceTests.cs`
- `CrossFormatPolicyComparisonServiceTests.cs`
- `CrossFormatReportGenerationServiceTests.cs`
- `JsonToTerraformServiceTests.cs`
- `TerraformParsingServiceTests.cs`

#### Integration Tests
- `*IntegrationTests.cs` files for end-to-end testing
- `CrossFormatPolicyComparisonServiceIntegrationTests.cs`

#### Program Tests
- `ProgramTests.cs` for CLI argument testing
- `ProgramTestHelper.cs` for common test utilities and reflection-based private method testing

#### Specialized Tests
- `RemediatePoliciesAsyncTests.cs` for remediation functionality
- `CrossFormatPolicyComparisonServiceSpecificMethodTests.cs` for targeted method testing

## Testing Commands and Workflow

### Basic Test Commands
```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "ClassName=PolicyComparisonServiceTests"

# Run tests with coverage (if configured)
dotnet test --collect:"XPlat Code Coverage"

# Run tests from test project directory
cd ConditionalAccessExporter.Tests
dotnet test
```

### Advanced Test Commands
```bash
# Debug test failures with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test method
dotnet test --filter "TestMethodName"

# Collect coverage data with TRX logger
dotnet test --collect:"XPlat Code Coverage" --logger trx

# Run tests in specific collection
dotnet test --filter "TestCategory=Integration"
```

## Test Data Management

### Mock Data Creation
Create representative policy JSON for testing:
```csharp
private static JObject CreateTestEntraPolicy(string id, string displayName, string state = "Enabled")
{
    return JObject.FromObject(new
    {
        Id = id,
        DisplayName = displayName,
        State = state,
        CreatedDateTime = "2024-01-01T12:00:00Z",
        ModifiedDateTime = "2024-05-01T10:30:00Z",
        Conditions = new
        {
            Applications = new
            {
                IncludeApplications = new[] { "All" },
                ExcludeApplications = new string[0]
            },
            Users = new
            {
                IncludeUsers = new[] { "All" },
                ExcludeUsers = new string[0]
            }
        },
        GrantControls = new
        {
            Operator = "OR",
            BuiltInControls = new[] { "mfa" }
        }
    });
}
```

### Test Fixtures
- Use consistent test data across test classes
- Create helper methods for common test data scenarios
- Implement data builders for complex objects

### Azure Mocking
- Mock Microsoft Graph API calls for unit tests
- Use dependency injection patterns for testable services
- Avoid real Azure credentials in tests

### File System Tests
- Use temporary directories for file I/O tests
- Use `System.IO.Abstractions.TestingHelpers` for file system mocking
- Clean up test files in test teardown

## Common Testing Patterns

### Service Testing Pattern
```csharp
[Fact]
public void ServiceMethod_WithValidInput_ReturnsExpectedResult()
{
    // Arrange
    var service = new ServiceUnderTest();
    var testInput = CreateTestData();
    
    // Act
    var result = service.Method(testInput);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedValue, result.Property);
}
```

### Integration Testing Pattern
```csharp
[Fact]
public void EndToEndWorkflow_WithRealData_ProducesCorrectOutput()
{
    // Arrange - Set up test environment
    var tempDir = Path.GetTempPath();
    var testData = CreateTestPolicyData();
    
    // Act - Execute full workflow
    var result = await service.ProcessAsync(testData);
    
    // Assert - Verify end-to-end results
    Assert.True(result.Success);
    Assert.NotEmpty(result.OutputFiles);
}
```

### Console Output Testing Pattern
```csharp
[Collection("Console Output Tests")]
public class ConsoleOutputTests
{
    [Fact]
    public async Task Method_WritesToConsole_CapturesExpectedOutput()
    {
        // Arrange
        var expectedOutput = "Expected console message";
        
        // Act
        var output = await ProgramTestHelper.CaptureConsoleOutputAsync(async () =>
        {
            await SomeMethodThatWritesToConsole();
        });
        
        // Assert
        Assert.Contains(expectedOutput, output);
    }
}
```

## Testing Specific Services

### Azure/Graph API Testing
- **Mock Graph Client**: Create mock implementations for Microsoft Graph
```csharp
[Fact]
public void GetPolicies_WithMockClient_ReturnsExpectedPolicies()
{
    // Arrange
    var mockClient = new Mock<GraphServiceClient>();
    var expectedPolicies = CreateTestPolicyCollection();
    mockClient.Setup(x => x.Identity.ConditionalAccess.Policies.GetAsync())
              .ReturnsAsync(expectedPolicies);
    
    // Act & Assert
    var service = new PolicyService(mockClient.Object);
    var result = await service.GetPoliciesAsync();
    Assert.Equal(expectedPolicies.Value.Count, result.Count);
}
```

- **Authentication Testing**: Test without real Azure credentials
- **API Response Testing**: Use sample JSON responses from Graph API
- **Error Scenario Testing**: Test authentication failures, permission errors

### Comparison Service Testing
- **Policy Diff Testing**: Test various policy difference scenarios
```csharp
[Fact]
public void ComparePolicies_WithDifferentPolicies_DetectsDifferences()
{
    // Arrange
    var policy1 = CreateTestPolicy("id1", "Policy A");
    var policy2 = CreateTestPolicy("id1", "Policy B");
    
    // Act
    var differences = _service.Compare(policy1, policy2);
    
    // Assert
    Assert.NotEmpty(differences);
    Assert.Contains("DisplayName", differences.Select(d => d.PropertyName));
}
```

- **Format Comparison**: Test JSON vs Terraform comparison logic
- **Edge Cases**: Empty policies, malformed data, missing fields
- **Performance Testing**: Large policy sets, bulk comparisons

### Conversion Service Testing
- **Round-Trip Testing**: JSON→Terraform→JSON should be equivalent
```csharp
[Fact]
public void ConvertToTerraform_ThenBackToJson_PreservesData()
{
    // Arrange
    var originalJson = CreateTestPolicyJson();
    
    // Act
    var terraform = conversionService.ConvertToTerraform(originalJson);
    var convertedBack = conversionService.ConvertToJson(terraform);
    
    // Assert
    Assert.Equal(originalJson["displayName"], convertedBack["displayName"]);
}
```

- **Field Mapping Testing**: Verify numeric codes convert to correct strings
- **Terraform Syntax Testing**: Ensure generated HCL is valid
- **Error Handling**: Invalid JSON, unsupported policy features

### Report Generation Testing
- **Output Format Testing**: JSON, HTML, CSV report generation
```csharp
[Theory]
[InlineData("json")]
[InlineData("html")]
[InlineData("csv")]
public void GenerateReport_WithFormat_ProducesValidOutput(string format)
{
    // Arrange
    var reportData = CreateTestReportData();
    
    // Act
    var report = _service.GenerateReport(reportData, format);
    
    // Assert
    Assert.NotNull(report);
    Assert.True(report.Length > 0);
}
```

- **Template Testing**: Verify report templates work correctly
- **File Generation Testing**: Test file creation and content
- **Large Data Testing**: Performance with many policies

## Test Environment Setup

### Principles
- **No Real Azure**: Tests should not require actual Azure credentials
- **Isolated Tests**: Each test should be independent
- **Cleanup**: Tests should clean up temporary files/directories
- **Deterministic**: Tests should produce consistent results

### Setup Patterns
```csharp
public class ServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly ServiceUnderTest _service;
    
    public ServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _service = new ServiceUnderTest();
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
```

## Testing Best Practices

### Test Naming
- Use descriptive test method names
- Follow pattern: `MethodName_Scenario_ExpectedResult`
- Example: `ComparePolicies_WithIdenticalPolicies_ReturnsNoDifferences`

### AAA Pattern
- **Arrange**: Set up test data and dependencies
- **Act**: Execute the method under test
- **Assert**: Verify the expected outcome

### Single Responsibility
- One assertion per test when possible
- Test one specific behavior or scenario per test method
- Use `[Theory]` with `[InlineData]` for multiple similar scenarios

### Test Data
- Use builder pattern or factory methods for test data
- Create reusable test data helpers
- Keep test data simple and focused

### Error Testing
- Test both success and failure scenarios
```csharp
[Fact]
public void ProcessPolicy_WithInvalidData_ThrowsArgumentException()
{
    // Arrange
    var invalidPolicy = CreateInvalidPolicyData();
    
    // Act & Assert
    Assert.Throws<ArgumentException>(() => _service.ProcessPolicy(invalidPolicy));
}
```

## Common Test Scenarios

### 1. Service Construction
```csharp
[Fact]
public void Constructor_WithValidParameters_InitializesSuccessfully()
{
    // Act
    var service = new PolicyComparisonService();
    
    // Assert
    Assert.NotNull(service);
}
```

### 2. Input Validation
```csharp
[Fact]
public void Method_WithNullInput_ThrowsArgumentNullException()
{
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() => _service.Method(null));
}
```

### 3. Business Logic
```csharp
[Fact]
public void CalculateRisk_WithHighRiskPolicy_ReturnsHighRiskLevel()
{
    // Arrange
    var highRiskPolicy = CreateHighRiskPolicy();
    
    // Act
    var riskLevel = _service.CalculateRisk(highRiskPolicy);
    
    // Assert
    Assert.Equal(RiskLevel.High, riskLevel);
}
```

### 4. Integration Points
```csharp
[Fact]
public async Task ServiceIntegration_WithMockDependencies_WorksCorrectly()
{
    // Arrange
    var mockService = new Mock<IDependentService>();
    mockService.Setup(x => x.GetDataAsync()).ReturnsAsync(expectedData);
    
    // Act
    var result = await _service.ProcessWithDependency(mockService.Object);
    
    // Assert
    Assert.Equal(expectedResult, result);
}
```

### 5. File Operations
```csharp
[Fact]
public async Task SaveToFile_WithValidData_CreatesCorrectFile()
{
    // Arrange
    var testData = CreateTestData();
    var filePath = Path.Combine(_tempDirectory, "test.json");
    
    // Act
    await _service.SaveToFileAsync(testData, filePath);
    
    // Assert
    Assert.True(File.Exists(filePath));
    var content = await File.ReadAllTextAsync(filePath);
    Assert.Contains("expectedContent", content);
}
```

### 6. Error Conditions
```csharp
[Fact]
public async Task ProcessAsync_WithNetworkError_HandlesGracefully()
{
    // Arrange
    var mockService = new Mock<INetworkService>();
    mockService.Setup(x => x.CallApiAsync()).ThrowsAsync(new HttpRequestException());
    
    // Act & Assert
    var exception = await Assert.ThrowsAsync<ServiceException>(
        () => _service.ProcessWithNetworkAsync(mockService.Object));
    Assert.Contains("Network error", exception.Message);
}
```

## Mock Data Examples

### Policy Test Data
```csharp
// Example policy for testing
var testPolicy = new ConditionalAccessPolicy
{
    Id = "test-policy-id",
    DisplayName = "Test MFA Policy",
    State = ConditionalAccessPolicyState.Enabled,
    GrantControls = new ConditionalAccessGrantControls
    {
        BuiltInControls = new[] { ConditionalAccessGrantControl.Mfa }
    },
    Conditions = new ConditionalAccessConditionSet
    {
        Applications = new ConditionalAccessApplications
        {
            IncludeApplications = new[] { "All" }
        },
        Users = new ConditionalAccessUsers
        {
            IncludeUsers = new[] { "All" }
        }
    }
};
```

### Terraform Test Data
```csharp
var testTerraformContent = @"
resource ""azuread_conditional_access_policy"" ""test_policy"" {
  display_name = ""Test MFA Policy""
  state        = ""enabled""
  
  conditions {
    applications {
      included_applications = [""All""]
    }
    users {
      included_users = [""All""]
    }
  }
  
  grant_controls {
    operator          = ""OR""
    built_in_controls = [""mfa""]
  }
}";
```

## Continuous Integration Testing

### Build Pipeline
- Tests run automatically on commits
- Parallel test execution where possible
- Test result reporting and analysis

### Test Results
- Generate test result reports in CI/CD
- Track test coverage metrics over time
- Fail builds on test failures

### Coverage
- Monitor test coverage metrics
- Set coverage thresholds for quality gates
- Generate coverage reports for review

### Fast Feedback
- Quick test execution for rapid development
- Separate fast unit tests from slower integration tests
- Run critical tests first in CI pipeline

## Debugging Tests

### Test Explorer
- Use Visual Studio or VS Code test explorer
- Run and debug individual tests
- View test output and results

### Breakpoints
- Debug failing tests step by step
- Inspect variable values during test execution
- Step through service logic to identify issues

### Output
- Use `ITestOutputHelper` for test debugging
```csharp
public class ServiceTests
{
    private readonly ITestOutputHelper _output;
    
    public ServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void TestMethod()
    {
        _output.WriteLine("Debug message for test execution");
        // Test logic
    }
}
```

### Logging
- Capture service logs during test execution
- Configure logging levels for testing
- Use structured logging for better debugging

## Benefits for OpenHands

This microagent will help OpenHands:
- Understand the testing architecture and patterns used in CA_Scanner
- Write effective unit and integration tests following established conventions
- Properly mock Azure dependencies and external services
- Create appropriate test data and scenarios for comprehensive coverage
- Debug and troubleshoot test failures efficiently
- Maintain test quality and coverage standards
- Follow best practices for test organization and execution
- Understand the relationship between tests and services
- Use the established testing infrastructure effectively

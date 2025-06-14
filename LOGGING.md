# Structured Logging Framework

## Overview

CA_Scanner now uses Microsoft.Extensions.Logging for structured logging while maintaining backward compatibility with existing Logger calls.

## Features

- **Structured Logging**: Support for parameterized logging with structured data
- **Multiple Providers**: Console logging with extensible provider support
- **Dependency Injection**: Services can inject `ILogger<T>` for typed logging
- **Backward Compatibility**: Existing `Logger.WriteX()` calls continue to work
- **Performance Logging**: Built-in performance tracking capabilities
- **Audit Logging**: Structured audit trail for policy operations
- **Correlation IDs**: Request tracking across service boundaries

## Architecture

### Core Components

1. **ILoggingService**: Main logging interface with structured methods
2. **LoggingService**: Implementation using Microsoft.Extensions.Logging
3. **StructuredLogger**: Static wrapper for backward compatibility
4. **Logger**: Alias for backward compatibility (redirects to StructuredLogger)

### Dependency Injection Setup

The logging framework is configured in `Program.cs`:

```csharp
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.None;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

services.AddSingleton<ILoggingService, LoggingService>();
```

## Usage Examples

### Structured Logging in Services

```csharp
public class PolicyComparisonService
{
    private readonly ILogger<PolicyComparisonService> _logger;

    public PolicyComparisonService(ILogger<PolicyComparisonService> logger)
    {
        _logger = logger;
    }

    public async Task<ComparisonResult> CompareAsync(string policyName, int policyCount)
    {
        _logger.LogInformation("Starting policy comparison for {PolicyName} with {PolicyCount} policies", 
            policyName, policyCount);
        
        // Implementation...
        
        _logger.LogInformation("Policy comparison completed for {PolicyName} in {Duration}ms", 
            policyName, stopwatch.ElapsedMilliseconds);
    }
}
```

### Using ILoggingService

```csharp
public class SomeService
{
    private readonly ILoggingService _loggingService;

    public SomeService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public async Task ProcessPolicyAsync(string policyName)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        using var scope = _loggingService.BeginScope(correlationId);
        
        _loggingService.LogPerformance("PolicyProcessing", TimeSpan.FromMilliseconds(150), 
            new { PolicyName = policyName });
            
        _loggingService.LogAudit("PolicyModified", "user123", policyName, 
            new { Action = "Updated", FieldsChanged = new[] { "State", "Conditions" } });
    }
}
```

### Backward Compatible Logger Usage

Existing code continues to work without changes:

```csharp
Logger.WriteInfo("Processing policies...");
Logger.WriteError("Failed to process policy: {0}", policyName);
Logger.WriteVerbose("Detailed processing information");
Logger.WriteWarning("Policy validation warning");
```

## Log Levels

- **LogLevel.Debug**: Verbose/detailed information (`WriteVerbose`)
- **LogLevel.Information**: General information (`WriteInfo`)
- **LogLevel.Warning**: Warning messages (`WriteWarning`)
- **LogLevel.Error**: Error messages (`WriteError`)

## Configuration

### Console Provider

Currently configured for console output with all messages going to stdout for test compatibility.

### Future Providers

The framework supports additional providers:
- File logging
- Application Insights
- Serilog integration
- Custom providers

## Testing

The logging framework maintains full backward compatibility with existing tests by:
- Writing to Console.WriteLine/Console.Error for test capture
- Preserving existing output formats
- Supporting all original Logger methods

## Migration Guide

### For New Services

1. Inject `ILogger<T>` in constructor
2. Use structured logging with named parameters
3. Register service in DI container

### For Existing Code

No changes required - existing Logger calls continue to work.

### Service Registration

Add services to DI container in Program.cs:

```csharp
services.AddTransient<YourService>();
```

Use DI container to get services:

```csharp
var service = GetService<YourService>();
```

## Performance Considerations

- Structured logging has minimal overhead
- Console provider is synchronous
- Use log level filtering to control output volume
- Consider async providers for high-volume scenarios

## Future Enhancements

- Application Insights integration
- Configuration-based log level management
- Custom log formatters
- Log aggregation and analysis tools
- Performance monitoring dashboard


using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ConditionalAccessExporter.Models;
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;
using System.ComponentModel.DataAnnotations;

namespace ConditionalAccessExporter.Services
{
    /// <summary>
    /// Service for managing resilience configuration with validation and environment-specific settings
    /// </summary>
    public interface IResilienceConfigurationService
    {
        /// <summary>
        /// Get the current resilience configuration
        /// </summary>
        ResilienceConfiguration GetConfiguration();

        /// <summary>
        /// Validate the configuration and return validation results
        /// </summary>
        IEnumerable<DataAnnotationsValidationResult> ValidateConfiguration();

        /// <summary>
        /// Update configuration at runtime
        /// </summary>
        void UpdateConfiguration(ResilienceConfiguration configuration);

        /// <summary>
        /// Get configuration for specific environment
        /// </summary>
        ResilienceConfiguration GetEnvironmentConfiguration(string environment);

        /// <summary>
        /// Save configuration to file
        /// </summary>
        Task SaveConfigurationAsync(ResilienceConfiguration configuration, string filePath);

        /// <summary>
        /// Load configuration from file
        /// </summary>
        Task<ResilienceConfiguration> LoadConfigurationAsync(string filePath);
    }

    /// <summary>
    /// Implementation of resilience configuration service
    /// </summary>
    public class ResilienceConfigurationService : IResilienceConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResilienceConfigurationService> _logger;
        private ResilienceConfiguration _currentConfiguration;
        private readonly object _configLock = new();

        public ResilienceConfigurationService(
            IConfiguration configuration,
            ILogger<ResilienceConfigurationService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _currentConfiguration = LoadConfigurationFromSources();
            ValidateConfigurationOnLoad();
        }

        /// <summary>
        /// Load configuration from multiple sources with precedence
        /// </summary>
        private ResilienceConfiguration LoadConfigurationFromSources()
        {
            var config = new ResilienceConfiguration();

            // Load from configuration sources (appsettings.json, environment variables, etc.)
            var section = _configuration.GetSection("Resilience");
            if (section.Exists())
            {
                section.Bind(config);
                _logger.LogInformation("Loaded resilience configuration from configuration sources");
            }
            else
            {
                _logger.LogInformation("No resilience configuration found in configuration sources, using defaults");
            }

            // Apply environment-specific overrides
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            ApplyEnvironmentSpecificSettings(config, environment);

            return config;
        }

        /// <summary>
        /// Apply environment-specific configuration settings
        /// </summary>
        private void ApplyEnvironmentSpecificSettings(ResilienceConfiguration config, string environment)
        {
            switch (environment.ToLowerInvariant())
            {
                case "development":
                    ApplyDevelopmentSettings(config);
                    break;
                case "staging":
                    ApplyStagingSettings(config);
                    break;
                case "production":
                    ApplyProductionSettings(config);
                    break;
                default:
                    _logger.LogWarning("Unknown environment: {Environment}, using production settings", environment);
                    ApplyProductionSettings(config);
                    break;
            }

            _logger.LogInformation("Applied {Environment} environment settings to resilience configuration", environment);
        }

        /// <summary>
        /// Apply development environment settings (more relaxed for testing)
        /// </summary>
        private static void ApplyDevelopmentSettings(ResilienceConfiguration config)
        {
            // More aggressive retry policy for development
            config.RetryPolicy.MaxRetryAttempts = 5;
            config.RetryPolicy.BaseDelayMs = 500;
            config.RetryPolicy.MaxDelayMs = 10000;

            // Shorter circuit breaker settings
            config.CircuitBreaker.FailureThreshold = 3;
            config.CircuitBreaker.DurationOfBreakSeconds = 15;

            // Higher rate limits for development
            config.RateLimiting.MaxRequestsPerMinute = 1200;
            config.RateLimiting.MaxConcurrentRequests = 20;

            // Shorter cache duration
            config.Caching.DefaultCacheDurationMinutes = 5;

            // Enable all telemetry
            config.GraphApi.EnableTelemetry = true;
            config.GraphApi.EnableBatchRequests = true;
        }

        /// <summary>
        /// Apply staging environment settings (production-like but with more monitoring)
        /// </summary>
        private static void ApplyStagingSettings(ResilienceConfiguration config)
        {
            // Conservative retry policy
            config.RetryPolicy.MaxRetryAttempts = 3;
            config.RetryPolicy.BaseDelayMs = 1000;
            config.RetryPolicy.MaxDelayMs = 30000;

            // Standard circuit breaker settings
            config.CircuitBreaker.FailureThreshold = 5;
            config.CircuitBreaker.DurationOfBreakSeconds = 30;

            // Moderate rate limits
            config.RateLimiting.MaxRequestsPerMinute = 800;
            config.RateLimiting.MaxConcurrentRequests = 15;

            // Standard cache duration
            config.Caching.DefaultCacheDurationMinutes = 15;

            // Enable telemetry for monitoring
            config.GraphApi.EnableTelemetry = true;
        }

        /// <summary>
        /// Apply production environment settings (conservative and stable)
        /// </summary>
        private static void ApplyProductionSettings(ResilienceConfiguration config)
        {
            // Conservative retry policy
            config.RetryPolicy.MaxRetryAttempts = 3;
            config.RetryPolicy.BaseDelayMs = 1000;
            config.RetryPolicy.MaxDelayMs = 30000;
            config.RetryPolicy.UseJitter = true;

            // Conservative circuit breaker settings
            config.CircuitBreaker.FailureThreshold = 5;
            config.CircuitBreaker.DurationOfBreakSeconds = 60;
            config.CircuitBreaker.MinimumThroughput = 10;

            // Conservative rate limits to avoid throttling
            config.RateLimiting.MaxRequestsPerMinute = 600;
            config.RateLimiting.MaxConcurrentRequests = 10;
            config.RateLimiting.BufferPercentage = 0.8;

            // Longer cache duration for production
            config.Caching.DefaultCacheDurationMinutes = 30;

            // Enable production optimizations
            config.GraphApi.EnableBatchRequests = true;
            config.GraphApi.EnableRequestDeduplication = true;
        }

        /// <summary>
        /// Validate configuration on load and log any issues
        /// </summary>
        private void ValidateConfigurationOnLoad()
        {
            var validationResults = ValidateConfiguration();
            var errors = validationResults.Where(vr => !string.IsNullOrEmpty(vr.ErrorMessage)).ToList();

            if (errors.Count > 0)
            {
                _logger.LogError("Configuration validation failed with {ErrorCount} errors:", errors.Count);
                foreach (var error in errors)
                {
                    _logger.LogError("- {ErrorMessage}", error.ErrorMessage);
                }
                throw new InvalidOperationException($"Resilience configuration validation failed with {errors.Count} errors");
            }

            _logger.LogInformation("Resilience configuration validation passed");
        }

        /// <summary>
        /// Get the current resilience configuration
        /// </summary>
        public ResilienceConfiguration GetConfiguration()
        {
            lock (_configLock)
            {
                return CloneConfiguration(_currentConfiguration);
            }
        }

        /// <summary>
        /// Validate the configuration and return validation results
        /// </summary>
        public IEnumerable<DataAnnotationsValidationResult> ValidateConfiguration()
        {
            lock (_configLock)
            {
                return ValidateConfigurationInternal(_currentConfiguration);
            }
        }

        /// <summary>
        /// Internal configuration validation
        /// </summary>
        private static IEnumerable<DataAnnotationsValidationResult> ValidateConfigurationInternal(ResilienceConfiguration config)
        {
            var context = new ValidationContext(config);
            var results = new List<DataAnnotationsValidationResult>();

            // Validate the main configuration object
            Validator.TryValidateObject(config, context, results, validateAllProperties: true);

            // Validate nested objects
            ValidateNestedObject(config.RetryPolicy, "RetryPolicy", results);
            ValidateNestedObject(config.CircuitBreaker, "CircuitBreaker", results);
            ValidateNestedObject(config.RateLimiting, "RateLimiting", results);
            ValidateNestedObject(config.Timeout, "Timeout", results);
            ValidateNestedObject(config.Caching, "Caching", results);
            ValidateNestedObject(config.GraphApi, "GraphApi", results);

            // Custom validation rules
            ValidateCustomRules(config, results);

            return results;
        }

        /// <summary>
        /// Validate nested configuration objects
        /// </summary>
        private static void ValidateNestedObject(object obj, string memberName, List<DataAnnotationsValidationResult> results)
        {
            var context = new ValidationContext(obj) { MemberName = memberName };
            var nestedResults = new List<DataAnnotationsValidationResult>();
            
            Validator.TryValidateObject(obj, context, nestedResults, validateAllProperties: true);
            
            foreach (var result in nestedResults)
            {
                results.Add(new DataAnnotationsValidationResult(
                    $"{memberName}.{result.ErrorMessage}",
                    result.MemberNames?.Select(m => $"{memberName}.{m}")
                ));
            }
        }

        /// <summary>
        /// Apply custom validation rules
        /// </summary>
        private static void ValidateCustomRules(ResilienceConfiguration config, List<DataAnnotationsValidationResult> results)
        {
            // Validate that base delay is less than max delay
            if (config.RetryPolicy.BaseDelayMs >= config.RetryPolicy.MaxDelayMs)
            {
                results.Add(new DataAnnotationsValidationResult(
                    "RetryPolicy.BaseDelayMs must be less than MaxDelayMs",
                    new[] { "RetryPolicy.BaseDelayMs", "RetryPolicy.MaxDelayMs" }));
            }

            // Validate that failure threshold is reasonable
            if (config.CircuitBreaker.FailureThreshold > config.CircuitBreaker.MinimumThroughput)
            {
                results.Add(new DataAnnotationsValidationResult(
                    "CircuitBreaker.FailureThreshold should not exceed MinimumThroughput",
                    new[] { "CircuitBreaker.FailureThreshold", "CircuitBreaker.MinimumThroughput" }));
            }

            // Validate rate limiting settings
            if (config.RateLimiting.MaxConcurrentRequests > config.RateLimiting.MaxRequestsPerMinute / 10)
            {
                results.Add(new DataAnnotationsValidationResult(
                    "RateLimiting.MaxConcurrentRequests seems too high compared to MaxRequestsPerMinute",
                    new[] { "RateLimiting.MaxConcurrentRequests", "RateLimiting.MaxRequestsPerMinute" }));
            }

            // Validate timeout settings
            if (config.Timeout.RequestTimeoutSeconds >= config.Timeout.BulkOperationTimeoutSeconds)
            {
                results.Add(new DataAnnotationsValidationResult(
                    "Timeout.RequestTimeoutSeconds should be less than BulkOperationTimeoutSeconds",
                    new[] { "Timeout.RequestTimeoutSeconds", "Timeout.BulkOperationTimeoutSeconds" }));
            }
        }

        /// <summary>
        /// Update configuration at runtime
        /// </summary>
        public void UpdateConfiguration(ResilienceConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var validationResults = ValidateConfigurationInternal(configuration);
            var errors = validationResults.Where(vr => !string.IsNullOrEmpty(vr.ErrorMessage)).ToList();

            if (errors.Count > 0)
            {
                throw new ArgumentException($"Configuration validation failed with {errors.Count} errors: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
            }

            lock (_configLock)
            {
                _currentConfiguration = CloneConfiguration(configuration);
            }

            _logger.LogInformation("Resilience configuration updated at runtime");
        }

        /// <summary>
        /// Get configuration for specific environment
        /// </summary>
        public ResilienceConfiguration GetEnvironmentConfiguration(string environment)
        {
            var config = new ResilienceConfiguration();
            ApplyEnvironmentSpecificSettings(config, environment);
            return config;
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        public async Task SaveConfigurationAsync(ResilienceConfiguration configuration, string filePath)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var validationResults = ValidateConfigurationInternal(configuration);
            var errors = validationResults.Where(vr => !string.IsNullOrEmpty(vr.ErrorMessage)).ToList();

            if (errors.Count > 0)
            {
                throw new ArgumentException($"Cannot save invalid configuration with {errors.Count} errors");
            }

            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(configuration, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(filePath, json);
                _logger.LogInformation("Saved resilience configuration to {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration to {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        public async Task<ResilienceConfiguration> LoadConfigurationAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Configuration file not found: {filePath}");

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var configuration = System.Text.Json.JsonSerializer.Deserialize<ResilienceConfiguration>(json, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                }) ?? new ResilienceConfiguration();

                var validationResults = ValidateConfigurationInternal(configuration);
                var errors = validationResults.Where(vr => !string.IsNullOrEmpty(vr.ErrorMessage)).ToList();

                if (errors.Count > 0)
                {
                    throw new InvalidOperationException($"Loaded configuration is invalid with {errors.Count} errors: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                }

                _logger.LogInformation("Loaded resilience configuration from {FilePath}", filePath);
                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration from {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Create a deep clone of the configuration
        /// </summary>
        private static ResilienceConfiguration CloneConfiguration(ResilienceConfiguration source)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(source);
            return System.Text.Json.JsonSerializer.Deserialize<ResilienceConfiguration>(json) ?? new ResilienceConfiguration();
        }
    }
}


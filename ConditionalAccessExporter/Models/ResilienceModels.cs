using System.ComponentModel.DataAnnotations;

namespace ConditionalAccessExporter.Models
{
    /// <summary>
    /// Configuration model for API resilience patterns including rate limiting, retry policies, and circuit breaker settings.
    /// </summary>
    public class ResilienceConfiguration
    {
        /// <summary>
        /// Retry policy configuration
        /// </summary>
        public RetryPolicyConfiguration RetryPolicy { get; set; } = new();

        /// <summary>
        /// Circuit breaker configuration
        /// </summary>
        public CircuitBreakerConfiguration CircuitBreaker { get; set; } = new();

        /// <summary>
        /// Rate limiting configuration
        /// </summary>
        public RateLimitingConfiguration RateLimiting { get; set; } = new();

        /// <summary>
        /// Timeout configuration
        /// </summary>
        public TimeoutConfiguration Timeout { get; set; } = new();

        /// <summary>
        /// Caching configuration
        /// </summary>
        public CachingConfiguration Caching { get; set; } = new();

        /// <summary>
        /// Microsoft Graph API specific settings
        /// </summary>
        public GraphApiConfiguration GraphApi { get; set; } = new();
    }

    /// <summary>
    /// Retry policy configuration with exponential backoff
    /// </summary>
    public class RetryPolicyConfiguration
    {
        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        [Range(1, 10)]
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay for exponential backoff in milliseconds
        /// </summary>
        [Range(100, 10000)]
        public int BaseDelayMs { get; set; } = 1000;

        /// <summary>
        /// Maximum delay for exponential backoff in milliseconds
        /// </summary>
        [Range(1000, 300000)]
        public int MaxDelayMs { get; set; } = 30000;

        /// <summary>
        /// Backoff multiplier for exponential backoff
        /// </summary>
        [Range(1.1, 5.0)]
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Jitter to add randomness to retry delays
        /// </summary>
        public bool UseJitter { get; set; } = true;

        /// <summary>
        /// HTTP status codes that should trigger retries
        /// </summary>
        public int[] RetriableStatusCodes { get; set; } = { 429, 500, 502, 503, 504 };
    }

    /// <summary>
    /// Circuit breaker pattern configuration
    /// </summary>
    public class CircuitBreakerConfiguration
    {
        /// <summary>
        /// Number of consecutive failures before opening circuit
        /// </summary>
        [Range(1, 20)]
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Duration in seconds to keep circuit open before attempting reset
        /// </summary>
        [Range(10, 300)]
        public int RecoveryTimeSeconds { get; set; } = 30;

        /// <summary>
        /// Minimum throughput required before circuit breaker activates
        /// </summary>
        [Range(1, 100)]
        public int MinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Sampling duration in seconds for failure rate calculation
        /// </summary>
        [Range(10, 300)]
        public int SamplingDurationSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Rate limiting configuration for Microsoft Graph API
    /// </summary>
    public class RateLimitingConfiguration
    {
        /// <summary>
        /// Enable adaptive rate limiting based on response headers
        /// </summary>
        public bool EnableAdaptiveRateLimiting { get; set; } = true;

        /// <summary>
        /// Maximum requests per minute
        /// </summary>
        [Range(1, 10000)]
        public int MaxRequestsPerMinute { get; set; } = 600;

        /// <summary>
        /// Maximum concurrent requests
        /// </summary>
        [Range(1, 50)]
        public int MaxConcurrentRequests { get; set; } = 10;

        /// <summary>
        /// Buffer percentage to keep below actual limits
        /// </summary>
        [Range(0.1, 0.9)]
        public double BufferPercentage { get; set; } = 0.8;

        /// <summary>
        /// Enable request queuing when rate limit is reached
        /// </summary>
        public bool EnableRequestQueuing { get; set; } = true;

        /// <summary>
        /// Maximum queue size for pending requests
        /// </summary>
        [Range(10, 1000)]
        public int MaxQueueSize { get; set; } = 100;
    }

    /// <summary>
    /// Timeout configuration for API calls
    /// </summary>
    public class TimeoutConfiguration
    {
        /// <summary>
        /// Overall timeout for individual requests in seconds
        /// </summary>
        [Range(5, 300)]
        public int RequestTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Timeout for bulk operations in seconds
        /// </summary>
        [Range(30, 1800)]
        public int BulkOperationTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        [Range(5, 60)]
        public int ConnectionTimeoutSeconds { get; set; } = 10;
    }

    /// <summary>
    /// Caching configuration for API responses
    /// </summary>
    public class CachingConfiguration
    {
        /// <summary>
        /// Enable response caching
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Default cache duration in minutes
        /// </summary>
        [Range(1, 1440)]
        public int DefaultCacheDurationMinutes { get; set; } = 15;

        /// <summary>
        /// Maximum cache size in megabytes
        /// </summary>
        [Range(10, 1000)]
        public int MaxCacheSizeMb { get; set; } = 100;

        /// <summary>
        /// Enable cache compression
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Cache key prefix
        /// </summary>
        public string CacheKeyPrefix { get; set; } = "CAExporter";
    }

    /// <summary>
    /// Microsoft Graph API specific configuration
    /// </summary>
    public class GraphApiConfiguration
    {
        /// <summary>
        /// API version to use
        /// </summary>
        public string ApiVersion { get; set; } = "v1.0";

        /// <summary>
        /// Enable batch requests where possible
        /// </summary>
        public bool EnableBatchRequests { get; set; } = true;

        /// <summary>
        /// Maximum batch size for batch requests
        /// </summary>
        [Range(1, 20)]
        public int MaxBatchSize { get; set; } = 20;

        /// <summary>
        /// Enable request deduplication
        /// </summary>
        public bool EnableRequestDeduplication { get; set; } = true;

        /// <summary>
        /// Deduplication window in seconds
        /// </summary>
        [Range(1, 300)]
        public int DeduplicationWindowSeconds { get; set; } = 60;

        /// <summary>
        /// Enable telemetry and monitoring
        /// </summary>
        public bool EnableTelemetry { get; set; } = true;

        /// <summary>
        /// Custom headers to include with requests
        /// </summary>
        public Dictionary<string, string> CustomHeaders { get; set; } = new();
    }

    /// <summary>
    /// API call metrics for monitoring and observability
    /// </summary>
    public class ApiCallMetrics
    {
        /// <summary>
        /// Request identifier
        /// </summary>
        public string RequestId { get; set; } = string.Empty;

        /// <summary>
        /// Operation name or endpoint
        /// </summary>
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Start time of the request
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the request
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of the request
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// HTTP status code returned
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Whether the request was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Number of retry attempts made
        /// </summary>
        public int RetryAttempts { get; set; }

        /// <summary>
        /// Whether request was served from cache
        /// </summary>
        public bool ServedFromCache { get; set; }

        /// <summary>
        /// Rate limit information from response headers
        /// </summary>
        public RateLimitInfo? RateLimitInfo { get; set; }

        /// <summary>
        /// Error message if request failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Exception type if request failed
        /// </summary>
        public string? ExceptionType { get; set; }
    }

    /// <summary>
    /// Rate limit information extracted from response headers
    /// </summary>
    public class RateLimitInfo
    {
        /// <summary>
        /// Requests remaining in current window
        /// </summary>
        public int? Remaining { get; set; }

        /// <summary>
        /// Total requests allowed in window
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Time when rate limit window resets
        /// </summary>
        public DateTime? ResetTime { get; set; }

        /// <summary>
        /// Retry after value in seconds
        /// </summary>
        public int? RetryAfterSeconds { get; set; }

        /// <summary>
        /// Throttle scope (user, tenant, application)
        /// </summary>
        public string? ThrottleScope { get; set; }

        /// <summary>
        /// Additional throttle information
        /// </summary>
        public Dictionary<string, string> AdditionalInfo { get; set; } = new();
    }

    /// <summary>
    /// Performance metrics aggregation
    /// </summary>
    public class PerformanceMetrics
    {
        /// <summary>
        /// Total number of API calls made
        /// </summary>
        public long TotalCalls { get; set; }

        /// <summary>
        /// Number of successful calls
        /// </summary>
        public long SuccessfulCalls { get; set; }

        /// <summary>
        /// Number of failed calls
        /// </summary>
        public long FailedCalls { get; set; }

        /// <summary>
        /// Number of calls served from cache
        /// </summary>
        public long CachedCalls { get; set; }

        /// <summary>
        /// Total retry attempts across all calls
        /// </summary>
        public long TotalRetryAttempts { get; set; }

        /// <summary>
        /// Average response time in milliseconds
        /// </summary>
        public double AverageResponseTimeMs { get; set; }

        /// <summary>
        /// 95th percentile response time in milliseconds
        /// </summary>
        public double P95ResponseTimeMs { get; set; }

        /// <summary>
        /// Rate limit hits count
        /// </summary>
        public long RateLimitHits { get; set; }

        /// <summary>
        /// Circuit breaker trips count
        /// </summary>
        public long CircuitBreakerTrips { get; set; }

        /// <summary>
        /// Metrics collection start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Metrics collection end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Success rate percentage
        /// </summary>
        public double SuccessRate => TotalCalls > 0 ? (double)SuccessfulCalls / TotalCalls * 100 : 0;

        /// <summary>
        /// Cache hit rate percentage
        /// </summary>
        public double CacheHitRate => TotalCalls > 0 ? (double)CachedCalls / TotalCalls * 100 : 0;
    }
}

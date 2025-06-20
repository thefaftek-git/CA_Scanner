using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ConditionalAccessExporter.Models;

namespace ConditionalAccessExporter.Services
{
    /// <summary>
    /// Interface for resilient Graph service operations
    /// </summary>
    public interface IResilientGraphService
    {
        Task<ConditionalAccessPolicyCollectionResponse?> GetConditionalAccessPoliciesAsync(CancellationToken cancellationToken = default);
        Task<ConditionalAccessPolicy?> GetConditionalAccessPolicyAsync(string policyId, CancellationToken cancellationToken = default);
        PerformanceMetrics GetMetrics();
        void ResetMetrics();
    }

    /// <summary>
    /// Simplified resilient wrapper around GraphServiceClient with basic retry logic
    /// </summary>
    public class ResilientGraphService : IResilientGraphService, IDisposable
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ResilienceConfiguration _config;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ResilientGraphService> _logger;
        private readonly SemaphoreSlim _rateLimitSemaphore;
        
        // Performance metrics
        private long _totalRequests;
        private long _failedRequests;
        private long _cacheHits;
        private DateTime _lastReset = DateTime.UtcNow;

        // Static random instance for jitter calculation to avoid seed issues with frequent creation
        private static readonly Random _random = new();

        public ResilientGraphService(
            GraphServiceClient graphServiceClient,
            IOptions<ResilienceConfiguration> config,
            IMemoryCache cache,
            ILogger<ResilientGraphService> logger)
        {
            _graphServiceClient = graphServiceClient ?? throw new ArgumentNullException(nameof(graphServiceClient));
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _rateLimitSemaphore = new SemaphoreSlim(_config.RateLimiting.MaxConcurrentRequests, _config.RateLimiting.MaxConcurrentRequests);
        }

        public async Task<ConditionalAccessPolicyCollectionResponse?> GetConditionalAccessPoliciesAsync(CancellationToken cancellationToken = default)
        {
            const string cacheKey = "conditional_access_policies";
            
            // Check cache first
            if (_cache.TryGetValue(cacheKey, out ConditionalAccessPolicyCollectionResponse? cachedResponse))
            {
                Interlocked.Increment(ref _cacheHits);
                _logger.LogDebug("Cache hit for conditional access policies");
                return cachedResponse;
            }

            return await ExecuteWithRetryAsync(async () =>
            {
                _logger.LogDebug("Fetching conditional access policies from Microsoft Graph");
                var response = await _graphServiceClient.Identity.ConditionalAccess.Policies.GetAsync(cancellationToken: cancellationToken);
                
                if (response != null && _config.Caching.EnableCaching)
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_config.Caching.DefaultCacheDurationMinutes)
                    };
                    _cache.Set(cacheKey, response, cacheOptions);
                }
                
                return response;
            }, cancellationToken);
        }

        public async Task<ConditionalAccessPolicy?> GetConditionalAccessPolicyAsync(string policyId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(policyId))
                throw new ArgumentException("Policy ID cannot be null or empty", nameof(policyId));

            var cacheKey = $"conditional_access_policy_{policyId}";
            
            // Check cache first
            if (_cache.TryGetValue(cacheKey, out ConditionalAccessPolicy? cachedPolicy))
            {
                Interlocked.Increment(ref _cacheHits);
                _logger.LogDebug("Cache hit for conditional access policy {PolicyId}", policyId);
                return cachedPolicy;
            }

            return await ExecuteWithRetryAsync(async () =>
            {
                _logger.LogDebug("Fetching conditional access policy {PolicyId} from Microsoft Graph", policyId);
                var policy = await _graphServiceClient.Identity.ConditionalAccess.Policies[policyId].GetAsync(cancellationToken: cancellationToken);
                
                if (policy != null && _config.Caching.EnableCaching)
                {
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_config.Caching.DefaultCacheDurationMinutes)
                    };
                    _cache.Set(cacheKey, policy, cacheOptions);
                }
                
                return policy;
            }, cancellationToken);
        }

        public PerformanceMetrics GetMetrics()
        {
            return new PerformanceMetrics
            {
                TotalCalls = _totalRequests,
                FailedCalls = _failedRequests,
                SuccessfulCalls = _totalRequests - _failedRequests,
                CachedCalls = _cacheHits,
                StartTime = _lastReset,
                EndTime = DateTime.UtcNow
            };
        }

        public void ResetMetrics()
        {
            Interlocked.Exchange(ref _totalRequests, 0);
            Interlocked.Exchange(ref _failedRequests, 0);
            Interlocked.Exchange(ref _cacheHits, 0);
            _lastReset = DateTime.UtcNow;
            _logger.LogInformation("Resilience metrics reset");
        }

        private async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T?>> operation, CancellationToken cancellationToken = default)
        {
            await _rateLimitSemaphore.WaitAsync(cancellationToken);
            
            try
            {
                Interlocked.Increment(ref _totalRequests);
                
                for (int attempt = 0; attempt <= _config.RetryPolicy.MaxRetryAttempts; attempt++)
                {
                    try
                    {
                        // Apply timeout
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_config.Timeout.RequestTimeoutSeconds));
                        
                        var result = await operation();
                        
                        if (attempt > 0)
                        {
                            _logger.LogInformation("Operation succeeded on retry attempt {Attempt}", attempt);
                        }
                        
                        return result;
                    }
                    catch (Exception ex) when (ShouldRetry(ex, attempt))
                    {
                        var delay = CalculateDelay(attempt);
                        _logger.LogWarning("Attempt {Attempt} failed: {Error}. Retrying in {Delay}ms", 
                            attempt + 1, ex.Message, delay);
                        
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                
                Interlocked.Increment(ref _failedRequests);
                throw new InvalidOperationException($"Operation failed after {_config.RetryPolicy.MaxRetryAttempts + 1} attempts");
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }

        private bool ShouldRetry(Exception ex, int attempt)
        {
            if (attempt >= _config.RetryPolicy.MaxRetryAttempts)
                return false;

            return ex switch
            {
                HttpRequestException httpEx => IsRetriableHttpException(httpEx),
                TaskCanceledException => true,
                OperationCanceledException => false, // Don't retry user cancellation
                _ => false
            };
        }

        private bool IsRetriableHttpException(HttpRequestException httpEx)
        {
            // Simple heuristic - retry on network errors
            var message = httpEx.Message.ToLowerInvariant();
            return message.Contains("timeout") || 
                   message.Contains("network") || 
                   message.Contains("connection") ||
                   message.Contains("429") || // Rate limited
                   message.Contains("503") || // Service unavailable
                   message.Contains("502");   // Bad gateway
        }

        private int CalculateDelay(int attempt)
        {
            var baseDelay = _config.RetryPolicy.BaseDelayMs;
            var maxDelay = _config.RetryPolicy.MaxDelayMs;
            
            // Exponential backoff
            var delay = (int)(baseDelay * Math.Pow(2, attempt));
            
            // Add jitter if enabled
            if (_config.RetryPolicy.UseJitter)
            {
                // Using static _random instance declared at class level
                delay = (int)(delay * (0.5 + _random.NextDouble() * 0.5));
            }
            
            return Math.Min(delay, maxDelay);
        }

        public void Dispose()
        {
            _rateLimitSemaphore?.Dispose();
        }
    }
}

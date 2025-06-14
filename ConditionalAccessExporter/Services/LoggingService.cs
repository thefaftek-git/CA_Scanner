
using Microsoft.Extensions.Logging;

namespace ConditionalAccessExporter.Services
{
    /// <summary>
    /// Implementation of structured logging service
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly ILogger<LoggingService> _logger;

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void LogInformation(string message, object? data, params object[] args)
        {
            if (data != null)
            {
                _logger.LogInformation("{Message} {Data}", string.Format(message, args), data);
            }
            else
            {
                _logger.LogInformation(message, args);
            }
        }

        public void LogError(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void LogPerformance(string operation, TimeSpan duration, object? additionalData = null)
        {
            if (additionalData != null)
            {
                _logger.LogInformation("Performance: {Operation} completed in {Duration}ms {AdditionalData}", 
                    operation, duration.TotalMilliseconds, additionalData);
            }
            else
            {
                _logger.LogInformation("Performance: {Operation} completed in {Duration}ms", 
                    operation, duration.TotalMilliseconds);
            }
        }

        public void LogAudit(string action, string policyName, object? details = null)
        {
            if (details != null)
            {
                _logger.LogInformation("Audit: {Action} on policy {PolicyName} {Details}", 
                    action, policyName, details);
            }
            else
            {
                _logger.LogInformation("Audit: {Action} on policy {PolicyName}", 
                    action, policyName);
            }
        }

        public IDisposable BeginScope(string correlationId)
        {
            return _logger.BeginScope("CorrelationId: {CorrelationId}", correlationId) ?? new NullScope();
        }

        private class NullScope : IDisposable
        {
            public void Dispose() { }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }
    }
}


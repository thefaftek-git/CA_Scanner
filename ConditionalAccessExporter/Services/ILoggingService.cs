using Microsoft.Extensions.Logging;

namespace ConditionalAccessExporter.Services
{
    /// <summary>
    /// Service interface for structured logging throughout the application
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Log information message
        /// </summary>
        void LogInformation(string message, params object[] args);

        /// <summary>
        /// Log information message with structured data
        /// </summary>
        void LogInformation(string message, object? data, params object[] args);

        /// <summary>
        /// Log error message
        /// </summary>
        void LogError(string message, params object[] args);

        /// <summary>
        /// Log error with exception
        /// </summary>
        void LogError(Exception exception, string message, params object[] args);

        /// <summary>
        /// Log verbose/debug message
        /// </summary>
        void LogDebug(string message, params object[] args);

        /// <summary>
        /// Log warning message
        /// </summary>
        void LogWarning(string message, params object[] args);

        /// <summary>
        /// Log performance metrics
        /// </summary>
        void LogPerformance(string operation, TimeSpan duration, object? additionalData = null);

        /// <summary>
        /// Log audit event for policy changes
        /// </summary>
        void LogAudit(string action, string policyName, object? details = null);

        /// <summary>
        /// Create a scoped logger with correlation ID
        /// </summary>
        IDisposable BeginScope(string correlationId);

        /// <summary>
        /// Check if logging level is enabled
        /// </summary>
        bool IsEnabled(LogLevel logLevel);
    }
}

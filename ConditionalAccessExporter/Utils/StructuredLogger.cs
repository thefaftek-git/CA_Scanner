
using Microsoft.Extensions.Logging;

namespace ConditionalAccessExporter.Utils
{
    /// <summary>
    /// Static wrapper for structured logging that maintains compatibility with existing code
    /// while providing modern logging capabilities
    /// </summary>
    public static class StructuredLogger
    {
        private static ILogger? _logger;
        private static bool _quietMode = false;
        private static bool _verboseMode = false;

        /// <summary>
        /// Initialize the structured logger with an ILogger instance
        /// </summary>
        public static void Initialize(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Set quiet mode (minimal output)
        /// </summary>
        public static void SetQuietMode(bool quietMode)
        {
            _quietMode = quietMode;
        }

        /// <summary>
        /// Set verbose mode (detailed output)
        /// </summary>
        public static void SetVerboseMode(bool verboseMode)
        {
            _verboseMode = verboseMode;
        }

        /// <summary>
        /// Write information message (respects quiet mode)
        /// </summary>
        public static void WriteInfo(string message)
        {
            if (!_quietMode)
            {
                // Always write to Console for backward compatibility with tests
                Console.WriteLine(message);
                
                // Also log via structured logger if available
                _logger?.LogInformation(message);
            }
        }

        /// <summary>
        /// Write structured information message
        /// </summary>
        public static void WriteInfo(string message, params object[] args)
        {
            if (!_quietMode)
            {
                // Format for console output
                Console.WriteLine(message, args);
                
                // Also log via structured logger if available
                _logger?.LogInformation(message, args);
            }
        }

        /// <summary>
        /// Write error message (always shown regardless of quiet mode)
        /// </summary>
        public static void WriteError(string message)
        {
            // Always write to Console for backward compatibility with tests
            Console.Error.WriteLine(message);
            
            // Also log via structured logger if available
            _logger?.LogError(message);
        }

        /// <summary>
        /// Write structured error message
        /// </summary>
        public static void WriteError(string message, params object[] args)
        {
            // Always write to Console for backward compatibility with tests
            Console.Error.WriteLine(message, args);
            
            // Also log via structured logger if available
            _logger?.LogError(message, args);
        }

        /// <summary>
        /// Write error with exception
        /// </summary>
        public static void WriteError(Exception exception, string message)
        {
            // Always write to Console for backward compatibility with tests
            Console.Error.WriteLine($"{message}: {exception.Message}");
            
            // Also log via structured logger if available
            _logger?.LogError(exception, message);
        }

        /// <summary>
        /// Write verbose message (only shown in verbose mode)
        /// </summary>
        public static void WriteVerbose(string message)
        {
            if (!_quietMode && _verboseMode)
            {
                // Always write to Console for backward compatibility with tests
                Console.WriteLine(message);
                
                // Also log via structured logger if available
                _logger?.LogDebug(message);
            }
        }

        /// <summary>
        /// Write structured verbose message
        /// </summary>
        public static void WriteVerbose(string message, params object[] args)
        {
            if (!_quietMode && _verboseMode)
            {
                // Always write to Console for backward compatibility with tests
                Console.WriteLine(message, args);
                
                // Also log via structured logger if available
                _logger?.LogDebug(message, args);
            }
        }

        /// <summary>
        /// Write warning message
        /// </summary>
        public static void WriteWarning(string message)
        {
            // Always write to Console for backward compatibility with tests
            Console.WriteLine($"WARNING: {message}");
            
            // Also log via structured logger if available
            _logger?.LogWarning(message);
        }

        /// <summary>
        /// Write structured warning message
        /// </summary>
        public static void WriteWarning(string message, params object[] args)
        {
            // Always write to Console for backward compatibility with tests
            Console.WriteLine($"WARNING: {string.Format(message, args)}");
            
            // Also log via structured logger if available
            _logger?.LogWarning(message, args);
        }

        /// <summary>
        /// Log performance metrics
        /// </summary>
        public static void LogPerformance(string operation, TimeSpan duration, object? additionalData = null)
        {
            if (_logger != null)
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
            else if (_verboseMode)
            {
                Console.WriteLine($"Performance: {operation} completed in {duration.TotalMilliseconds:F2}ms");
            }
        }

        /// <summary>
        /// Log audit events
        /// </summary>
        public static void LogAudit(string action, string policyName, object? details = null)
        {
            if (_logger != null)
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
            else if (_verboseMode)
            {
                Console.WriteLine($"Audit: {action} on policy {policyName}");
            }
        }

        /// <summary>
        /// Create a scoped logger with correlation ID
        /// </summary>
        public static IDisposable? BeginScope(string correlationId)
        {
            return _logger?.BeginScope("CorrelationId: {CorrelationId}", correlationId);
        }

        /// <summary>
        /// Check if logging level is enabled
        /// </summary>
        public static bool IsEnabled(LogLevel logLevel)
        {
            return _logger?.IsEnabled(logLevel) ?? false;
        }
    }
}




using Microsoft.Extensions.Logging;

namespace ConditionalAccessExporter.Utils
{
    public static class LoggerWithColors
    {
        public static void SetQuietMode(bool quietMode) => StructuredLoggerWithColors.SetQuietMode(quietMode);
        public static void SetVerboseMode(bool verboseMode) => StructuredLoggerWithColors.SetVerboseMode(verboseMode);
        public static void WriteInfo(string message) => StructuredLoggerWithColors.WriteInfo(message);
        public static void WriteError(string message) => StructuredLoggerWithColors.WriteError(message);
        public static void WriteVerbose(string message) => StructuredLoggerWithColors.WriteVerbose(message);

        // Additional structured logging methods
        public static void WriteWarning(string message) => StructuredLoggerWithColors.WriteWarning(message);
        public static void LogPerformance(string operation, TimeSpan duration, object? additionalData = null)
            => StructuredLoggerWithColors.LogPerformance(operation, duration, additionalData);
        public static void LogAudit(string action, string policyName, object? details = null)
            => StructuredLoggerWithColors.LogAudit(action, policyName, details);
        public static IDisposable? BeginScope(string correlationId) => StructuredLoggerWithColors.BeginScope(correlationId);
    }
}


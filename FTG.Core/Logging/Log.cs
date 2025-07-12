using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FTG.Core.Logging;

public static class GlobalLogger
    {
        private static ILogger? _logger;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger("GlobalLogger");
        }

        public static string? LogInfo(
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return LogMessage(LogLevel.Information, message, filePath, lineNumber);
        }

        public static string? LogWarning(
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return LogMessage(LogLevel.Warning, message, filePath, lineNumber);
        }

        public static string? LogError(
            string message,
            Exception? exception = null,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return LogMessage(LogLevel.Error, message, filePath, lineNumber, exception);
        }

        public static string? LogDebug(
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return LogMessage(LogLevel.Debug, message, filePath, lineNumber);
        }

        public static string? LogCritical(
            string message,
            Exception? exception = null,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return LogMessage(LogLevel.Critical, message, filePath, lineNumber, exception);
        }

        private static string? LogMessage(
            LogLevel level,
            string message,
            string filePath,
            int lineNumber,
            Exception? exception = null)
        {
            if (_logger == null)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [UNINITIALIZED] {GetFileName(filePath)}:{lineNumber} - {message}");
                return null;
            }

            var fileName = GetFileName(filePath);
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {fileName}:{lineNumber} - {message}";

            switch (level)
            {
                case LogLevel.Information:
                    _logger.LogInformation(logMessage);
                    return logMessage;
                case LogLevel.Warning:
                    _logger.LogWarning(logMessage);
                    return logMessage;
                case LogLevel.Error:
                    _logger.LogError(exception, logMessage);
                    return logMessage;
                case LogLevel.Debug:
                    _logger.LogDebug(logMessage);
                    return logMessage;
                case LogLevel.Critical:
                    _logger.LogCritical(exception, logMessage);
                    return logMessage;
            }

            return null;
        }

        private static string GetFileName(string filePath)
        {
            return Path.GetFileName(filePath);
        }
    }
    
public static class LoggerExtensions
{
    public static void LogHere(
        this object obj,
        string message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        GlobalLogger.LogInfo(message, filePath, lineNumber);
    }

    public static void LogErrorHere(
        this object obj,
        string message,
        Exception? exception = null,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        GlobalLogger.LogError(message, exception, filePath, lineNumber);
    }
}
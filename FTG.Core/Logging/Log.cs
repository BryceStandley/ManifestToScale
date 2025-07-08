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

        public static void LogInfo(
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogMessage(LogLevel.Information, message, filePath, lineNumber);
        }

        public static void LogWarning(
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogMessage(LogLevel.Warning, message, filePath, lineNumber);
        }

        public static void LogError(
            string message,
            Exception? exception = null,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogMessage(LogLevel.Error, message, filePath, lineNumber, exception);
        }

        public static void LogDebug(
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogMessage(LogLevel.Debug, message, filePath, lineNumber);
        }

        public static void LogCritical(
            string message,
            Exception? exception = null,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogMessage(LogLevel.Critical, message, filePath, lineNumber, exception);
        }

        private static void LogMessage(
            LogLevel level,
            string message,
            string filePath,
            int lineNumber,
            Exception? exception = null)
        {
            if (_logger == null)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [UNINITIALIZED] {GetFileName(filePath)}:{lineNumber} - {message}");
                return;
            }

            var fileName = GetFileName(filePath);
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {fileName}:{lineNumber} - {message}";

            switch (level)
            {
                case LogLevel.Information:
                    _logger.LogInformation(logMessage);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning(logMessage);
                    break;
                case LogLevel.Error:
                    _logger.LogError(exception, logMessage);
                    break;
                case LogLevel.Debug:
                    _logger.LogDebug(logMessage);
                    break;
                case LogLevel.Critical:
                    _logger.LogCritical(exception, logMessage);
                    break;
            }
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
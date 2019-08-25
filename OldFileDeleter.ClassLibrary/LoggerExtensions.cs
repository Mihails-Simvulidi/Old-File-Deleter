using Microsoft.Extensions.Logging;
using System;

namespace OldFileDeleter.ClassLibrary
{
    public static class LoggerExtensions
    {
        public static void LogCritical(this ILogger logger, string description, Exception exception)
        {
            logger.LogCritical($"{description}: {FormatException(exception)}");
        }

        public static void LogError(this ILogger logger, string description, Exception exception)
        {
            logger.LogError($"{description}: {FormatException(exception)}");
        }

        public static void LogWarning(this ILogger logger, string description, Exception exception)
        {
            logger.LogWarning($"{description}: {FormatException(exception)}");
        }

        private static string FormatException(Exception exception)
        {
            string text = exception is FileDeleterException ? exception.Message : $"{exception.GetType()}: {exception.Message}";

            if (exception.InnerException != null)
            {
                text += $"{Environment.NewLine}Inner exception: {FormatException(exception.InnerException)}";
            }

            return text;
        }
    }
}

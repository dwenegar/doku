// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using Microsoft.Extensions.Logging;

namespace Doku.Logging;

internal static class LoggerExtensions
{
    public static void LogInfo(this Logger logger, string message) =>
        logger.Log(LogLevel.Information, null, message);

    public static void LogWarning(this Logger logger, string message) =>
        logger.Log(LogLevel.Warning, null, message);

    public static void LogError(this Logger logger, string message) =>
        logger.Log(LogLevel.Error, null, message);

    public static void LogDebug(this Logger logger, string message) =>
        logger.Log(LogLevel.Debug, null, message);

    public static void LogTrace(this Logger logger, string message) =>
        logger.Log(LogLevel.Trace, null, message);
}

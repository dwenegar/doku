// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using Microsoft.Extensions.Logging;

namespace Doku.Logging;

internal static class LoggerExtensions
{
    public static void LogInfo(this Logger logger, string message) =>
        logger.Log(LogLevel.Information, null, message, false);

    public static void InfoMarkup(this Logger log, string message) =>
        log.Log(LogLevel.Information, null, message, true);

    public static void InfoMarkup(this Logger log, string message, params object?[] renderables) =>
        log.Log(LogLevel.Information, null, message, true, renderables);

    public static void LogWarning(this Logger logger, string message) =>
        logger.Log(LogLevel.Warning, null, message, false);

    public static void LogError(this Logger logger, string message) =>
        logger.Log(LogLevel.Error, null, message, false);

    public static void LogDebug(this Logger logger, string message) =>
        logger.Log(LogLevel.Debug, null, message, false);

    public static void LogTrace(this Logger logger, string message) =>
        logger.Log(LogLevel.Trace, null, message, false);
}

// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;

namespace Dwenegar.Doku.Logging
{
    public enum LogLevel
    {
        None,
        Verbose,
        Info,
        Warning,
        Error
    }

    internal static class LogLevelExtensions
    {
        public static ConsoleColor? ToConsoleColor(this LogLevel self) => self switch
        {
            LogLevel.Verbose => ConsoleColor.Gray,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => null
        };
    }
}

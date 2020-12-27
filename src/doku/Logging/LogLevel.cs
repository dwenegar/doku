// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;

namespace Dwenegar.Doku.Logging
{
    internal enum LogLevel
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

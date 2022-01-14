// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;

namespace Dwenegar.Doku.Logging.Handlers
{
    internal sealed class ConsoleLogHandler : LogHandlerBase
    {
        public override void Handle(ref LogRecord logRecord)
            => WriteLine(logRecord.Level, FormatRecord(ref logRecord));

        public override void Close()
            => Console.ResetColor();

        private static void WriteLine(LogLevel level, string line)
            => ConsoleUtils.WriteLine(line, level.ToConsoleColor());
    }
}

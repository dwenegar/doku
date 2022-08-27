// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;

namespace Doku.Logging.Handlers
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

// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Dwenegar.Doku.Logging.Handlers;

namespace Dwenegar.Doku.Logging
{
    internal sealed class LogAggregatorHandler : LogHandlerBase
    {
        private readonly ConsoleLogHandler _innerHandler;
        private readonly SortedList<LogLevel, List<LogRecord>> _recordsByLevel = new();

        public LogAggregatorHandler(LogLevel level)
        {
            _innerHandler = new ConsoleLogHandler();
            for (LogLevel l = level; l <= LogLevel.Error; l++)
            {
                _recordsByLevel.Add(l, new List<LogRecord>());
            }
        }

        public override void Handle(ref LogRecord logRecord)
        {
            if (_recordsByLevel.TryGetValue(logRecord.Level, out var records))
            {
                records.Add(logRecord);
            }
        }

        public override void Close()
        {
            LogLevel level = _recordsByLevel.LastOrDefault(x => x.Value.Count > 0).Key;

            WriteHeader(level);
            WriteRecords();
            WriteFooter(level);

            _innerHandler.Close();
        }

        private static void WriteHeader(LogLevel level)
        {
            string message = level switch
            {
                LogLevel.Error => "Build failed.",
                LogLevel.Warning => "Succeeded with warnings.",
                _ => "Build succeeded."
            };

            ConsoleUtils.WriteLine($"\n\n{message}", level.ToConsoleColor());
        }

        private void WriteRecords()
        {
            foreach (var (_, records) in _recordsByLevel)
            {
                for (var i = 0; i < records.Count; i++)
                {
                    LogRecord record = records[i];
                    _innerHandler.Handle(ref record);
                }
            }
        }

        private void WriteFooter(LogLevel level)
        {
            foreach ((LogLevel l, var records) in _recordsByLevel)
            {
                ConsoleUtils.WriteLine($"\t{records.Count} {l}(s)", level.ToConsoleColor());
            }
        }
    }
}

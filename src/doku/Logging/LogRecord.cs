// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;

namespace Dwenegar.Doku.Logging
{
    internal readonly struct LogRecord
    {
        public LogRecord(DateTime date, string? scope, LogLevel level, string message)
        {
            Date = date;
            Level = level;
            Message = message;
            Scope = scope;
        }

        public DateTime Date { get; }
        public string? Scope { get; }
        public LogLevel Level { get; }
        public string Message { get; }
    }
}

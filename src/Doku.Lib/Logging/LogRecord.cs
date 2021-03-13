// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

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

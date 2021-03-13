// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace Dwenegar.Doku.Logging.Handlers
{
    internal abstract class LogHandlerBase : ILogHandler
    {
        private static StringBuilder? s_stringBuilder;

        public abstract void Handle(ref LogRecord logRecord);

        public abstract void Close();

        protected static string FormatRecord(ref LogRecord logRecord)
        {
            Debug.Assert(logRecord.Level != LogLevel.None);

            StringBuilder sb = s_stringBuilder ??= new StringBuilder();

            sb.Append(logRecord.Date.ToString("[HH:mm:ss.fff]"));

            switch (logRecord.Level)
            {
                case LogLevel.Verbose:
                    sb.Append(" VERBOSE");
                    break;
                case LogLevel.Info:
                    sb.Append(" INFO   ");
                    break;
                case LogLevel.Warning:
                    sb.Append(" WARNING");
                    break;
                case LogLevel.Error:
                    sb.Append(" ERROR  ");
                    break;
            }

            if (logRecord.Scope != null)
            {
                sb.Append(" [").Append(logRecord.Scope).Append(']');
            }

            var result = sb.Append(' ').Append(logRecord.Message).ToString();
            sb.Clear();
            return result;
        }
    }
}

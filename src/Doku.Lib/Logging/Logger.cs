// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dwenegar.Doku.Logging.Handlers;

namespace Dwenegar.Doku.Logging
{
    public sealed class Logger
    {
        private static readonly Logger s_instance = new();

        private LogLevel _level;

        private ILogHandler[] _handlers = null!;

        private string? _scope;

        private Logger() => DoInitialize(LogLevel.Info, null);

        public static LogLevel Level => s_instance._level;

        public static void Initialize(LogLevel level, string? logFilePath)
        {
            s_instance.DoInitialize(level, logFilePath);
        }

        public static void Shutdown()
        {
            s_instance.CloseHandlers();
        }

        public static void LogVerbose(string message) => s_instance.Log(LogLevel.Verbose, message);

        public static void LogInfo(string message) => s_instance.Log(LogLevel.Info, message);

        public static void LogWarning(string message) => s_instance.Log(LogLevel.Warning, message);

        public static void LogError(string message) => s_instance.Log(LogLevel.Error, message);

        public static void LogVerbose(Exception exception) => s_instance.Log(LogLevel.Verbose, exception);

        public static void LogInfo(Exception exception) => s_instance.Log(LogLevel.Info, exception);

        public static void LogWarning(Exception exception) => s_instance.Log(LogLevel.Warning, exception);

        public static void LogError(Exception exception) => s_instance.Log(LogLevel.Error, exception);

        private void DoInitialize(LogLevel level, string? logFilePath)
        {
            var handlers = new List<ILogHandler>();
            handlers.Add(new ConsoleLogHandler());
            handlers.Add(new LogAggregatorHandler(LogLevel.Warning));
            if (logFilePath != null)
            {
                handlers.Add(new FileLogHandler(logFilePath));
            }

            _level = level;
            _handlers = handlers.ToArray();
        }

        private void CloseHandlers()
        {
            foreach (ILogHandler handler in _handlers)
            {
                handler.Close();
            }
        }

        private bool CanLog(LogLevel level)
        {
            return _level != LogLevel.None && level >= _level;
        }

        private void Log(LogLevel level, string message)
        {
            if (CanLog(level))
            {
                DoLog(level, message);
            }
        }

        private void DoLog(LogLevel level, string message)
        {
            DateTime date = DateTime.Now;
            var logRecord = new LogRecord(date, _scope, level, message);
            for (int i = _handlers.Length; --i >= 0;)
            {
                _handlers[i].Handle(ref logRecord);
            }
        }

        private void Log(LogLevel level, Exception exception)
        {
            if (CanLog(level))
            {
                DoLog(level, exception.ToString());
            }
        }

        public sealed class Scope : IDisposable
        {
            private readonly Stopwatch _stopwatch = new();
            private readonly string? _previousScope;

            public Scope(string scopeName)
            {
                _previousScope = s_instance._scope;
                s_instance._scope = _previousScope == null ? scopeName : $"{_previousScope}.{scopeName}";
                _stopwatch.Start();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                TimeSpan elapsed = _stopwatch.Elapsed;
                LogVerbose($"Completed in {elapsed.TotalMilliseconds} milliseconds.");
                s_instance._scope = _previousScope;
            }
        }
    }
}

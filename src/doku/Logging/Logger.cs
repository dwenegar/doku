// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.Threading;
using Doku.Utils;
using Lunet.Extensions.Logging.SpectreConsole;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Doku.Logging;

internal sealed class Logger
{
    private readonly ILogger _logger;
    private readonly bool _runningFromGitHubAction;
    private int _logId;

    public Logger(ILogger logger, LogLevel level)
    {
        _logger = logger;
        Level = level;
        _runningFromGitHubAction = GitHubActionHelpers.IsRunningOnGitHubAction;
    }

    public LogLevel Level { get; }

    public bool HasErrors { get; private set; }

    public IDisposable BeginGroup(string name) => new Scope(name, _runningFromGitHubAction);

    public void Log(LogLevel level, Exception? exception, string? message, bool markup, params object?[] args)
    {
        if (level == LogLevel.Error)
        {
            HasErrors = true;
        }

        int id = Interlocked.Increment(ref _logId);
        if (markup)
        {
            _logger.LogMarkup(level, new EventId(id), exception, message, args);
        }
        else
        {
            _logger.Log(level, new EventId(id), exception, message, args);
        }
    }

    private sealed class Scope : IDisposable
    {
        private readonly bool _isRunningFromGitHubAction;

        public Scope(string name, bool isRunningFromGitHubAction)
        {
            _isRunningFromGitHubAction = isRunningFromGitHubAction;
            if (isRunningFromGitHubAction)
            {
                AnsiConsole.WriteLine($"::group::{name}");
            }

            AnsiConsole.Write(new Rule(name) { Alignment = Justify.Left });
            Console.Out.Flush();
        }

        public void Dispose()
        {
            AnsiConsole.WriteLine();
            if (_isRunningFromGitHubAction)
            {
                AnsiConsole.WriteLine("::endgroup::");
            }

            Console.Out.Flush();
        }
    }
}

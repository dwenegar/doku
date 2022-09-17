// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using static Doku.Utils.GitHubActionHelpers;

namespace Doku.Logging;

internal sealed class Logger
{
    private readonly ILogger _logger;

    public Logger(ILogger logger, LogLevel level)
    {
        _logger = logger;
        Level = level;
    }

    public LogLevel Level { get; }

    public bool HasErrors { get; private set; }

    public IDisposable BeginGroup(string title) => new Group(title);

    public void Log(LogLevel level, Exception? exception, string? message, params object?[] args)
    {
        if (level == LogLevel.Error)
        {
            HasErrors = true;
        }

        _logger.Log(level, exception, message, args);
    }

    private sealed class Group : IDisposable
    {
        public Group(string title)
        {
            if (IsRunningOnGitHubAction)
            {
                // https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#grouping-log-lines
                AnsiConsole.WriteLine($"::group::{title}");
            }

            AnsiConsole.Write(new Rule(title) { Alignment = Justify.Left });
            Console.Out.Flush();
        }

        public void Dispose()
        {
            AnsiConsole.WriteLine();
            if (IsRunningOnGitHubAction)
            {
                AnsiConsole.WriteLine("::endgroup::");
            }

            Console.Out.Flush();
        }
    }
}

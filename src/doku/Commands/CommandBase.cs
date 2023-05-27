// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.Threading.Tasks;
using Doku.Logging;
using Doku.Utils;
using JetBrains.Annotations;
using Lunet.Extensions.Logging.SpectreConsole;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using static Doku.Utils.GitHubActionHelpers;

// ReSharper disable ReplaceAutoPropertyWithComputedProperty

namespace Doku.Commands;

internal abstract class CommandBase
{
    [Option("--log-level", Description = "The level at which to log the tool operations.")]
    private LogLevel LogLevel { get; } = LogLevel.Information;

    protected abstract Task ExecuteAsync(CommandLineApplication app, Logger logger);

    [UsedImplicitly]
    protected async Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        Logger logger = InitializeLogging();

        logger.LogInfo(Program.LongVersion);

        GitHubActionInfo? gitHubInfo = GetGitHubInfo();
        if (gitHubInfo is not null)
        {
            logger.LogInfo($"Running from GitHub: {gitHubInfo}");
        }

        await ExecuteAsync(app, logger);
        return logger.HasErrors ? 1 : 0;
    }

    private Logger InitializeLogging()
    {
        using ILoggerFactory? factory = LoggerFactory.Create(builder =>
        {
            IAnsiConsoleOutput consoleOutput = new AnsiConsoleOutput(Console.Out);
            if (IsRunningOnGitHubAction)
            {
                consoleOutput = new AnsiConsoleOutputOverride(consoleOutput)
                {
                    Width = 256,
                    Height = 128
                };
            }

            builder.SetMinimumLevel(LogLevel)
                   .AddSpectreConsole(new SpectreConsoleLoggerOptions
                   {
                       ConsoleSettings = new AnsiConsoleSettings
                       {
                           Ansi = IsRunningOnGitHubAction ? AnsiSupport.No : default,
                           Out = consoleOutput
                       },
                       IndentAfterNewLine = false,
                       IncludeTimestamp = true,
                       IncludeNewLineBeforeMessage = false
                   });
        });

        ILogger logger = factory.CreateLogger(Program.Name);
        return new Logger(logger, LogLevel);
    }
}

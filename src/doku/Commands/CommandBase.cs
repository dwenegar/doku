// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System.Threading.Tasks;
using Doku.Logging;
using McMaster.Extensions.CommandLineUtils;

namespace Doku.Commands;

internal abstract class CommandBase
{
    [Option("--log-level", Description = "The level at which to log the tool operations.")]
    protected LogLevel LogLevel { get; set; } = LogLevel.Info;

    [Option("--log", Description = "The path to save the log to.")]
    protected string? LogFilePath { get; set; }

    protected abstract Task<int> OnExecuteAsync(CommandLineApplication app);
}

// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using Doku.Logging;
using Microsoft.Extensions.Logging;

namespace Doku.Runners;

internal sealed class DocFx
{
    private readonly string _path;
    private readonly Logger _logger;

    public DocFx(string path, Logger logger)
    {
        _path = path;
        _logger = logger;
    }

    public string? WorkingDirectory { get; init; }

    public async Task<string> Run(string arguments)
    {
        string? logLevel = _logger.Level switch
        {
            LogLevel.Trace => "Verbose",
            LogLevel.Debug => "Verbose",
            LogLevel.Information => "Info",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Error",
            LogLevel.None => null,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (logLevel is not null)
        {
            arguments = string.IsNullOrEmpty(arguments)
                ? $"--logLevel {logLevel}"
                : $"--logLevel {logLevel} {arguments}";
        }

        var outputBuffer = new StringBuilder();
        var toOutputBuffer = PipeTarget.ToStringBuilder(outputBuffer);
        var toLogger = PipeTarget.ToDelegate(RedirectToLogger);

        CommandResult result = await Cli.Wrap(_path)
                                        .WithArguments(arguments)
                                        .WithWorkingDirectory(WorkingDirectory ?? Environment.CurrentDirectory)
                                        .WithStandardOutputPipe(PipeTarget.Merge(toOutputBuffer, toLogger))
                                        .ExecuteAsync();

        if (result.ExitCode != 0)
        {
            throw new Exception($"Failed to run {_path} {arguments}");
        }

        return outputBuffer.ToString();
    }

    private void RedirectToLogger(string line)
    {
        int levelBegin = line.IndexOf(']');
        if (levelBegin == -1)
        {
            return;
        }

        int levelEnd = line.IndexOf(':', levelBegin + 1);
        LogLevel level = line[(levelBegin + 1)..levelEnd] switch
        {
            "Info" => LogLevel.Information,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            _ => LogLevel.Debug
        };

        string message = line[(levelEnd + 1)..].Trim();
        _logger.Log(level, null, message);
    }
}

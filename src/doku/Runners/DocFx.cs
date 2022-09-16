// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
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

    public async Task Run(string arguments, string workingDirectory)
    {
        string? logLevel = _logger.Level switch
        {
            LogLevel.Trace => "Verbose",
            LogLevel.Information => "Info",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Debug => "Verbose",
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

        Command command = Cli.Wrap(_path)
                             .WithArguments(arguments)
                             .WithWorkingDirectory(workingDirectory);

        if (logLevel is not null)
        {
            command = command.WithStandardOutputPipe(PipeTarget.ToDelegate(OnProgramOutput));
        }

        CommandResult result = await command.ExecuteAsync();
        if (result.ExitCode != 0)
        {
            throw new Exception($"Failed to run {_path} {arguments}");
        }
    }

    private void OnProgramOutput(string line)
    {
        int levelBegin = line.IndexOf(']');
        if (levelBegin == -1)
        {
            return;
        }

        int levelEnd = line.IndexOf(':', levelBegin + 1);

        string level = line[(levelBegin + 1)..levelEnd];
        string message = line[(levelEnd + 1)..].Trim();

        Console.WriteLine(level);
        switch (level)
        {
            case "Verbose":
                _logger.LogTrace(message);
                break;
            case "Info":
                _logger.LogInfo(message);
                break;
            case "Warning":
                _logger.LogWarning(message);
                break;
            case "Error":
                _logger.LogError(message);
                break;
        }
    }

    private void OnProgramOutput(ReadOnlySpan<char> line)
    {
        int levelBegin = line.IndexOf(']');
        if (levelBegin == -1)
        {
            return;
        }

        line = line[(levelBegin + 1)..];
        int levelEnd = line.IndexOf(':');


        ReadOnlySpan<char> level = line[..levelEnd];
        Console.WriteLine($">{level}<");
        if (level == "Verbose")
        {
            _logger.LogTrace(line[(levelEnd + 1)..].Trim().ToString());
        }
        else if (level == "Info")
        {
            _logger.LogInfo(line[(levelEnd + 1)..].Trim().ToString());
        }
        else if (level == "Warning")
        {
            _logger.LogWarning(line[(levelEnd + 1)..].Trim().ToString());
        }
        else if (level == "Error")
        {
            _logger.LogError(line[(levelEnd + 1)..].Trim().ToString());
        }
    }
}

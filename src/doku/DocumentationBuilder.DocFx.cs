// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Doku.Logging;
using Doku.Utils;

namespace Doku
{
    public sealed partial class DocumentationBuilder
    {
        private void CheckDocFx()
        {
            string? docFxPath = DocFxPath ?? FindDocFxInPath();
            if (docFxPath == null)
            {
                throw new Exception("Could not find docfx.exe in the system path.");
            }

            if (!docFxPath.EndsWith("docfx.exe", StringComparison.OrdinalIgnoreCase))
            {
                docFxPath = Path.Combine(docFxPath, "docfx.exe");
            }

            var docFx = new DocFx(docFxPath);
            if (!docFx.Exists)
            {
                throw new Exception($"{docFxPath} is not a valid DocFx installation.");
            }

            _docFx = docFx;
            Logger.LogVerbose($"DocFx Path: {docFxPath}");
            Logger.LogVerbose($"DocFx Version: {docFx.Version}");

            static string? FindDocFxInPath()
            {
                string? envPath = Environment.GetEnvironmentVariable("PATH");
                return envPath?.Split(Path.PathSeparator).FirstOrDefault(ContainsDocFxExe);
            }

            static bool ContainsDocFxExe(string? directory)
            {
                return !string.IsNullOrEmpty(directory) && File.Exists(Path.Combine(directory, "docfx.exe"));
            }
        }

        private void RunDocFx()
        {
            using Logger.Scope scope = new("RunDocFx");
            Logger.LogVerbose("Running docfx");

            string docFxJsonFile = Path.Combine(_buildPath, "docfx.json");
            _docFx!.Run(docFxJsonFile, _buildPath, true);
        }

        private sealed class DocFx
        {
            private readonly string _path;

            private Version? _version;

            public DocFx(string path) => _path = path;

            public bool Exists => File.Exists(_path);

            public Version Version => _version ??= GetVersion();

            public string Run(string arguments, string workingDirectory, bool withLog)
            {
                Action<string>? outputCallback = null;
                if (withLog)
                {
                    string logLevel = Logger.Level switch
                    {
                        LogLevel.Verbose => "Verbose",
                        LogLevel.Info => "Info",
                        LogLevel.Warning => "Warning",
                        LogLevel.Error => "Error",
                        _ => string.Empty
                    };

                    arguments = string.IsNullOrEmpty(arguments)
                        ? $"--logLevel {logLevel}"
                        : $"--logLevel {logLevel} {arguments}";
                    outputCallback = OnProgramOutput;
                }


                var ci = new CommandInfo(_path)
                {
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    OutputCallback = outputCallback
                };

                return Command.Run(ci, "Failed to run DocFx");
            }

            private static void OnProgramOutput(string line)
            {
                if (line.Length == 0 || line[0] != '[')
                {
                    Logger.LogVerbose(line);
                    return;
                }

                int levelBegin = line.IndexOf(']');
                if (levelBegin == -1)
                {
                    Logger.LogVerbose(line);
                    return;
                }

                int levelEnd = line.IndexOf(':', levelBegin);
                string level = line.Substring(levelBegin + 1, levelEnd - levelBegin - 1);
                string message = line.Substring(levelEnd + 1).Trim();

                switch (level)
                {
                    case "Verbose":
                    case "Info":
                        Logger.LogVerbose(message);
                        break;
                    case "Warning":
                        Logger.LogWarning(message);
                        break;
                    case "Error":
                        Logger.LogError(message);
                        break;
                }
            }

            private Version GetVersion()
            {
                string docFxOutput = Run("--version", string.Empty, false);
                var regex = new Regex(@"docfx (\d+\.\d+\.\d+)", RegexOptions.Multiline);
                Match match = regex.Match(docFxOutput);
                return Version.Parse(match.Groups[1].Value);
            }
        }
    }
}

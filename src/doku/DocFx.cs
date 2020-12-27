// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text.RegularExpressions;
using Dwenegar.Doku.Logging;
using Dwenegar.Doku.Utils;

namespace Dwenegar.Doku
{
    internal sealed class DocFx
    {
        private readonly string _path;

        private Version? _version;

        public DocFx(string? path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public Version? Version => _version ??= GetVersion();

        public string GetPath() => _path;

        public void Run(string arguments, string workingDirectory)
        {
            Run(arguments, workingDirectory, true);
        }

        private static void OnProgramOutput(string line)
        {
            if (line.Length == 0 || line[0] != '[')
            {
                Logger.LogInfo(line);
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
                    Logger.LogVerbose(message);
                    break;
                case "Info":
                    Logger.LogInfo(message);
                    break;
                case "Warning":
                    Logger.LogWarning(message);
                    break;
                case "Error":
                    Logger.LogError(message);
                    break;
            }
        }

        private string Run(string arguments,
                           string workingDirectory,
                           bool withLog)
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


            var ci = new CommandInfo(Path.Combine(_path, "docfx.exe"))
            {
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                OutputCallback = outputCallback
            };

            return Command.Run(ci, "Failed to run DocFx");
        }

        private Version? GetVersion()
        {
            string docFxOutput = Run("--version", string.Empty, false);
            if (!string.IsNullOrEmpty(docFxOutput))
            {
                var regex = new Regex(@"docfx (\d+\.\d+\.\d+)", RegexOptions.Multiline);
                Match match = regex.Match(docFxOutput);
                if (match.Success && Version.TryParse(match.Groups[1].Value, out Version? version))
                {
                    return version;
                }
            }

            return null;
        }
    }
}

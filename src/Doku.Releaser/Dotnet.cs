// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text.RegularExpressions;
using Dwenegar.Doku.Logging;
using Dwenegar.Doku.Utils;

namespace Dwenegar.Doku.Releaser
{
    internal sealed class Dotnet
    {
        private readonly string _path;

        private Version? _version;

        public Dotnet(string? path)
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
            if (line.Contains("): error "))
            {
                Logger.LogError(line);
            }
            else if (line.Contains("): warning "))
            {
                Logger.LogWarning(line);
            }
            else
            {
                Logger.LogInfo(line);
            }
        }

        private string Run(string arguments,
                           string workingDirectory,
                           bool withLog)
        {
            Action<string>? outputCallback = null;
            if (withLog)
            {
                outputCallback = OnProgramOutput;
            }


            var ci = new CommandInfo(Path.Combine(_path, "dotnet.exe"))
            {
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                OutputCallback = outputCallback
            };

            return Command.Run(ci, "Failed to run dotnet");
        }

        private Version? GetVersion()
        {
            string output = Run("--version", string.Empty, false);
            if (!string.IsNullOrEmpty(output))
            {
                var regex = new Regex(@"(\d+\.\d+\.\d+)", RegexOptions.Multiline);
                Match match = regex.Match(output);
                if (match.Success && Version.TryParse(match.Groups[1].Value, out Version? version))
                {
                    return version;
                }
            }

            return null;
        }
    }
}

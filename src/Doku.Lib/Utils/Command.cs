// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Dwenegar.Doku.Logging;

namespace Dwenegar.Doku.Utils
{
    public sealed class CommandInfo
    {
        public CommandInfo(string fileName) => FileName = fileName;

        public string FileName { get; }
        public string Arguments { get; init; } = string.Empty;
        public string WorkingDirectory { get; init; } = string.Empty;
        public Action<string>? OutputCallback { get; init; }
    }

    public static class Command
    {
        public static string Run(CommandInfo ci, string errorMessage)
        {
            Logger.LogVerbose($"Running {ci.FileName} {ci.Arguments} in \"{ci.WorkingDirectory}\"");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = ci.Arguments,
                    CreateNoWindow = true,
                    FileName = ci.FileName,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = ci.WorkingDirectory
                }
            };

            process.Start();

            var stdout = new ProcessStreamReader(process.StandardOutput, ci.OutputCallback);

            if (!process.HasExited)
            {
                process.WaitForExit();
            }

            if (process.ExitCode != 0)
            {
                throw new Exception(errorMessage);
            }

            return stdout.GetString();
        }

        private sealed class ProcessStreamReader
        {
            private readonly StreamReader _stream;
            private readonly Action<string>? _lineRead;

            private readonly Thread _thread;
            private readonly List<string> _lines = new();

            public ProcessStreamReader(StreamReader stream, Action<string>? lineRead = null)
            {
                _stream = stream;
                _lineRead = lineRead;

                _thread = new Thread(ReadLines);
                _thread.Start();
            }

            internal string GetString()
            {
                if (_thread.IsAlive)
                {
                    _thread.Join();
                }

                var sb = new StringBuilder();
                foreach (string line in _lines)
                {
                    sb.AppendLine(line);
                }

                return sb.ToString();
            }

            private void ReadLines()
            {
                try
                {
                    string? line = _stream.ReadLine();
                    while (line != null)
                    {
                        _lineRead?.Invoke(line);
                        _lines.Add(line);

                        line = _stream.ReadLine();
                    }
                }
                catch (ObjectDisposedException)
                {
                    OnLineRead("ObjectDisposedException caught.");
                }
            }

            private void OnLineRead(string line)
            {
                _lineRead?.Invoke(line);
                _lines.Add(line);
            }
        }
    }
}

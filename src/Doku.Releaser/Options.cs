// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

#nullable disable

using CommandLine;
using Dwenegar.Doku.Logging;

namespace Dwenegar.Doku
{
    internal sealed class Options
    {
        [Option('o', "output", Default = "artifacts", HelpText = "Path to write the files to")]
        public string OutputPath { get; set; }

        [Option("token", HelpText = "GitHub API token")]
        public string GitHubToken { get; set; }

        [Option("repository", HelpText = "GitHub repository")]
        public string GitHubRepository { get; set; }

        [Option("log-level",
                Default = LogLevel.Info,
                HelpText = "Specifies the log level; acceptable values are: None, Verbose, Info, Warning, Error")]
        public LogLevel LogLevel { get; set; }

        [Option("build-only", HelpText = "Skip publishing to GitHub")]
        public bool BuildOnly { get; set; }

        [Option("log", HelpText = "Path to save the log to")]
        public string LogFilePath { get; set; }

        [Value(0, MetaName = "root", Hidden = true, Default = ".")]
        public string RootPath { get; set; }
    }
}

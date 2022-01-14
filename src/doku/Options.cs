// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using CommandLine;
using Dwenegar.Doku.Logging;

namespace Dwenegar.Doku
{
    internal sealed class Options
    {
        [Option('o', "output", Default = "docs", HelpText = "Path to write the files to")]
        public string OutputPath { get; set; } = null!;

        [Option('t', "template", HelpText = "Path to the template directory")]
        public string? TemplatePath { get; set; }

        [Option("with-docfx", HelpText = "Which DocFx installation to use")]
        public string? DocFxPath { get; set; }

        [Option("keep-build-folder", Default = false, HelpText = "Keeps the build folder (useful for debugging)")]
        public bool KeepBuildFolder { get; set; }

        [Option("build", HelpText = "Path to use for building the documentation.")]
        public string? BuildPath { get; set; }

        [Option("log-level",
                Default = LogLevel.Info,
                HelpText = "Specifies the log level; acceptable values are: None, Verbose, Info, Warning, Error")]
        public LogLevel LogLevel { get; set; }

        [Option("log", HelpText = "Path to save the log to")]
        public string? LogFilePath { get; set; }

        [Value(0, MetaName = "package", Hidden = true, Default = ".")]
        public string PackagePath { get; set; } = null!;
    }
}

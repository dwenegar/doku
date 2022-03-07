﻿// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using CommandLine;
using Dwenegar.Doku.Logging;

namespace Dwenegar.Doku
{
    internal sealed class Options
    {
        [Option('o', "output", Default = "docs", HelpText = "Path to write the files to")]
        public string OutputPath { get; set; } = "docs";

        [Option('S', "source", Default = "Documentation~", HelpText = "Path to the documentation source")]
        public string SourcePath { get; set; } = "Documentation~";

        [Option('t', "template", HelpText = "Path to the template directory")]
        public string? TemplatePath { get; set; }

        [Option('s', "style", HelpText = "Path to the custom stylesheet")]
        public string? StyleSheetPath { get; set; }

        [Option("with-docfx", HelpText = "Which DocFx installation to use")]
        public string? DocFxPath { get; set; }

        [Option("pdf", Default = false, HelpText = "Generates a pdf files of the documentation")]
        public bool GeneratePdf { get; set; }

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
        public string PackagePath { get; set; } = ".";
    }
}

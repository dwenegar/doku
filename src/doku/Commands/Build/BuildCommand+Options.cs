// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using McMaster.Extensions.CommandLineUtils;

// ReSharper disable ReplaceAutoPropertyWithComputedProperty

namespace Doku.Commands.Build;

internal sealed partial class BuildCommand
{
    [Option("-o|--output", Description = "The output folder.")]
    private string OutputPath { get; } = "docs";

    [DirectoryExists]
    [Option("--template", Description = "The custom template folder.")]
    private string? TemplatePath { get; } = default;

    [FileExists]
    [Option("--config", Description = "The path of the config file.")]
    private string? ConfigPath { get; } = default;

    [FileExists]
    [Option("--style", Description = "The path of the custom stylesheet.")]
    private string? StyleSheetPath { get; } = default;

    [FileExists]
    [Option("--with-docfx", Description = "The folder of the DocFx installation to use.")]
    private string? DocFxPath { get; } = default;

    [Option("--keep-build-folder", Description = "Keep the build folder.")]
    private bool KeepBuildFolder { get; } = default;

    [Option("--build", Description = "The folder used for building the documentation.")]
    private string? BuildPath { get; } = default;

    [Option("--modern", Description = "Use the modern DocFx template")]
    private bool UseModernTemplate { get; } = default;

    [DirectoryExists]
    [Argument(0, "package-path", Description = "Path to the folder containing the package.json file.")]
    private string PackagePath { get; } = Environment.CurrentDirectory;
}

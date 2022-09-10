// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.Threading.Tasks;
using Doku.Logging;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

// ReSharper disable ReplaceAutoPropertyWithComputedProperty

namespace Doku.Commands;

[Command("build", Description = "Build the documentation.")]
internal sealed class BuildCommand : CommandBase
{
    [Option("-o|--output", Description = "The output folder.")]
    private string OutputPath { get; } = "docs";

    [DirectoryExists]
    [Option("--template", Description = "The custom template folder.")]
    private string? TemplatePath { get; } = default;

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

    [DirectoryExists]
    [Argument(0, "package-path", Description = "Path to the folder containing the package.json file.")]
    private string PackagePath { get; } = Environment.CurrentDirectory;

    [UsedImplicitly]
    protected override Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        Logger.Initialize(LogLevel, LogFilePath);

        DocumentationBuilder builder = new(PackagePath, OutputPath, BuildPath)
        {
            DocFxPath = DocFxPath,
            TemplatePath = TemplatePath,
            StyleSheetPath = StyleSheetPath,
            KeepBuildFolder = KeepBuildFolder
        };

        builder.Build();
        Logger.Shutdown();
        return Task.FromResult(0);
    }
}

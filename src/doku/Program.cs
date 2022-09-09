// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Doku.Logging;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace Doku;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

[VersionOptionFromMember("-V|--version", MemberName = nameof(Version))]
internal sealed class Program
{
    [Option("-o|--output", Description = "The output folder.")]
    private string OutputPath { get; set; } = "docs";

    [DirectoryExists]
    [Option("-t|--template", Description = "The custom template folder.")]
    private string? TemplatePath { get; set; }

    [FileExists]
    [Option("-s|--style", Description = "The path of the custom stylesheet.")]
    private string? StyleSheetPath { get; set; }

    [FileExists]
    [Option("--with-docfx", Description = "The folder of the DocFx installation to use.")]
    private string? DocFxPath { get; set; }

    [Option("--keep-build-folder", Description = "Keep the build folder.")]
    private bool KeepBuildFolder { get; set; }

    [Option("--build", Description = "The folder used for building the documentation.")]
    private string? BuildPath { get; set; }

    [Option("--log-level", Description = "The log level at which to log.")]
    private LogLevel LogLevel { get; set; } = LogLevel.Info;

    [Option("--log", Description = "The path to save the log to.")]
    private string? LogFilePath { get; set; }

    [DirectoryExists]
    [Argument(0, "packagePath", Description = "Path to the folder containing the package.json file.")]
    private string PackagePath { get; set; } = Directory.GetCurrentDirectory();

    private string Version
    {
        get
        {
            Assembly assembly = GetType().Assembly;
            string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?.?";
            string appName = assembly.GetName().Name ?? "?";
            return $"{appName} {version}";
        }
    }

    private static async Task<int> Main(string[] args) => await CommandLineApplication.ExecuteAsync<Program>(args);

    [UsedImplicitly]
    private Task<int> OnExecuteAsync(CancellationToken cancellationToken)
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

// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System.Threading.Tasks;
using Doku.Logging;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

// ReSharper disable ReplaceAutoPropertyWithComputedProperty

namespace Doku.Commands.Build;

[Command("build", Description = "Build the documentation.")]
internal sealed partial class BuildCommand : CommandBase
{
    [UsedImplicitly]
    protected override async Task ExecuteAsync(CommandLineApplication app, Logger logger)
    {
        DocumentBuilder builder = new(PackagePath, OutputPath, BuildPath, logger)
        {
            DocFxPath = DocFxPath,
            UseModernTemplate = UseModernTemplate,
            TemplatePath = TemplatePath,
            StyleSheetPath = StyleSheetPath,
            ConfigPath = ConfigPath,
            KeepBuildFolder = KeepBuildFolder
        };
        await builder.Build();
    }
}

// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System.Threading.Tasks;
using Doku.Commands.Build;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

// ReSharper disable ReplaceAutoPropertyWithComputedProperty

namespace Doku.Commands;

[Command("build", Description = "Build the documentation.")]
internal sealed partial class BuildCommand : CommandBase
{
    [UsedImplicitly]
    protected override async Task ExecuteAsync(CommandLineApplication app)
    {
        DocumentBuilder builder = new(PackagePath, OutputPath, BuildPath, Logger!)
        {
            DocFxPath = DocFxPath,
            TemplatePath = TemplatePath,
            StyleSheetPath = StyleSheetPath,
            ConfigPath = ConfigPath,
            KeepBuildFolder = KeepBuildFolder
        };
        await builder.Build();
    }
}

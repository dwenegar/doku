// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Doku.Commands;
using Doku.Utils;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace Doku;

#pragma warning disable CA1822

[Subcommand(typeof(BuildCommand))]
[VersionOptionFromMember("-V|--version", MemberName = nameof(Version))]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class Program
{
    private string Version => AssemblyHelpers.GetInformationalVersion();

    private static async Task<int> Main(string[] args) => await CommandLineApplication.ExecuteAsync<Program>(args);

    [UsedImplicitly]
    private Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

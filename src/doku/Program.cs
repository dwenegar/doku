// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Doku.Commands.Build;
using Doku.Commands.Init;
using Doku.Utils;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace Doku;

#pragma warning disable CA1822

[Subcommand(typeof(BuildCommand), typeof(InitCommand))]
[VersionOptionFromMember("-V|--version", MemberName = nameof(LongVersion))]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class Program
{
    public static string Name => AssemblyHelpers.GetAssemblyName();
    public static string Version => AssemblyHelpers.GetInformationalVersion();
    public static string LongVersion => $"{Name} {Version} - {DateTime.Now.Year} Copyright Simone Livieri";

    private static async Task<int> Main(string[] args) => await CommandLineApplication.ExecuteAsync<Program>(args);

    [UsedImplicitly]
    private Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        Console.WriteLine("Specify a command");
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

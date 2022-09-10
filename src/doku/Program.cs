// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Doku.Commands;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace Doku;

#pragma warning disable CA1822

[Subcommand(typeof(BuildCommand))]
[VersionOptionFromMember("-V|--version", MemberName = nameof(Version))]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed class Program
{
    private string Version
    {
        get
        {
            Assembly assembly = GetType().Assembly;
            string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                             ?? "?.?";
            string appName = assembly.GetName().Name ?? "?";
            return $"{appName} {version}";
        }
    }

    private static async Task<int> Main(string[] args) => await CommandLineApplication.ExecuteAsync<Program>(args);

    [UsedImplicitly]
    private Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

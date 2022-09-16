// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Doku.Runners;

namespace Doku;

internal sealed partial class DocumentationBuilder
{
    private string? _docFxPath;

    private void CheckDocFx()
    {
        string? docFxPath = DocFxPath ?? FindDocFxInPath();
        if (docFxPath == null)
        {
            throw new Exception("Could not find docfx.exe in the system path.");
        }

        if (!docFxPath.EndsWith("docfx.exe", StringComparison.OrdinalIgnoreCase))
        {
            docFxPath = Path.Combine(docFxPath, "docfx.exe");
        }

        if (!File.Exists(docFxPath))
        {
            throw new Exception($"{docFxPath} is not a valid DocFx installation.");
        }

        Verbose($"DocFx Path: {docFxPath}");

        _docFxPath = docFxPath;

        static string? FindDocFxInPath()
        {
            string? envPath = Environment.GetEnvironmentVariable("PATH");
            return envPath?.Split(Path.PathSeparator).FirstOrDefault(ContainsDocFxExe);
        }

        static bool ContainsDocFxExe(string? directory)
        {
            return !string.IsNullOrEmpty(directory) && File.Exists(Path.Combine(directory, "docfx.exe"));
        }
    }

    private async Task RunDocFx()
    {
        Verbose("Running docfx");

        string docFxJsonFile = Path.Combine(_buildPath, "docfx.json");

        var docFx = new DocFx(_docFxPath!, _logger);
        await docFx.Run(docFxJsonFile, _buildPath);
    }
}

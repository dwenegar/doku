// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Doku.Runners;

namespace Doku.Commands.Build;

internal sealed partial class DocumentBuilder
{
    private async Task RunDocFx()
    {
        using IDisposable _ = _logger.BeginGroup("Running DocFX");

        var docFx = new DocFx(_docFxPath!, _logger)
        {
            WorkingDirectory = _buildPath
        };

        string docFxJsonFile = Path.Combine(_buildPath, "docfx.json");
        await docFx.Run(docFxJsonFile);
    }

    private async Task<Version> GetDocFxVersion(string docfxPath)
    {
        var docFx = new DocFx(docfxPath, _logger);
        string docFxOutput = await docFx.Run("--version");
        var regex = new Regex(@"(\d+\.\d+\.\d+)", RegexOptions.Multiline);
        Match match = regex.Match(docFxOutput);
        return Version.Parse(match.Groups[1].Value);
    }
}

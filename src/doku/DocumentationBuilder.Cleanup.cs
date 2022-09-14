// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using Doku.Utils;

namespace Doku;

internal sealed partial class DocumentationBuilder
{
    private void DeleteFolders()
    {
        Verbose($"Deleting `{_outputPath}`");
        Files.DeleteDirectory(_outputPath);

        Verbose($"Deleting `{_buildPath}`");
        Files.DeleteDirectory(_buildPath);
    }

    private void DeleteBuildFolder()
    {
        Verbose($"Deleting `{_buildPath}`");
        Files.DeleteDirectory(_buildPath);
    }
}

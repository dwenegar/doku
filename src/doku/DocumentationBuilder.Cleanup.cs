// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using Doku.Logging;
using Doku.Utils;

namespace Doku
{
    public sealed partial class DocumentationBuilder
    {
        private void DeleteFolders()
        {
            using Logger.Scope scope = new("DeleteFolders");

            Logger.LogVerbose($"Deleting `{_outputPath}`");
            Files.DeleteDirectory(_outputPath);

            Logger.LogVerbose($"Deleting `{_buildPath}`");
            Files.DeleteDirectory(_buildPath);
        }

        private void DeleteBuildFolder()
        {
            using Logger.Scope scope = new("DeleteBuildFolder");

            Logger.LogVerbose($"Deleting `{_buildPath}`");
            Files.DeleteDirectory(_buildPath);
        }
    }
}

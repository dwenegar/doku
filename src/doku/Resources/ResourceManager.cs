// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Reflection;
using Doku.Logging;
using Doku.Utils;

namespace Doku.Resources;

internal sealed class ResourceManager
{
    private readonly Logger _logger;

    public ResourceManager(Logger logger) => _logger = logger;

    public void ExportAssemblyResources(Assembly assembly, string archiveName, string outputDirectory)
    {
        var finder = new AssemblyResourceFinder(assembly);
        using ResourceReader? templateResource = finder.Find(archiveName);
        if (templateResource == null)
        {
            throw new Exception($"Could not find resource `{archiveName}`");
        }

        foreach ((string resourceName, Stream resourceStream) in templateResource.GetResourceStreams())
        {
            string targetPath = Path.Combine(outputDirectory, resourceName);
            Files.CreateDirectory(Path.GetDirectoryName(targetPath), _logger);

            using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            resourceStream.CopyTo(fs);

            _logger.LogDebug($"Exported resource `{resourceName}` to `{targetPath}`");
        }
    }
}

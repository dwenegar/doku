// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Reflection;
using Doku.Logging;
using Doku.Utils;

namespace Doku.Resources
{
    internal sealed class ResourceManager
    {
        private readonly Logger _logger;
        private readonly ResourceFinder _finder;

        public ResourceManager(Assembly assembly, string rootNamespace, Logger logger)
        {
            _finder = new ResourceFinder(assembly, rootNamespace);
            _logger = logger;
        }

        public void ExportResources(string archiveName, string outputDirectory)
        {
            using ResourceReader? templateResource = _finder.Find(archiveName);
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
}

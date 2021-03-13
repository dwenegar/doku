// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using Dwenegar.Doku.Logging;
using Dwenegar.Doku.Utils;

namespace Dwenegar.Doku.Resources
{
    internal sealed class ResourceManager
    {
        private readonly ResourceFinder _finder;

        public ResourceManager(Assembly assembly, string rootNamespace)
        {
            _finder = new ResourceFinder(assembly, rootNamespace);
        }

        public void ExportResources(string archiveName, string outputDirectory)
        {
            using Logger.Scope scope = new("ExportResources");

            using ResourceReader? templateResource = _finder.Find(archiveName);
            if (templateResource == null)
            {
                throw new Exception($"Could not find resource `{archiveName}`");
            }

            foreach (var (resourceName, resourceStream) in templateResource.GetResourceStreams())
            {
                string targetPath = Path.Combine(outputDirectory, resourceName);
                Files.CreateDirectory(Path.GetDirectoryName(targetPath));

                using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                resourceStream.CopyTo(fs);

                Logger.LogVerbose($"Exported resource `{resourceName}` to `{targetPath}`");
            }
        }
    }
}

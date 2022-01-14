// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dwenegar.Doku.Resources
{
    internal sealed class ResourceFinder
    {
        private readonly Assembly _assembly;
        private readonly string _resourcePrefix;
        private readonly string[] _manifestResourceNames;

        public ResourceFinder(Assembly assembly, string rootNamespace)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _resourcePrefix = $"Dwenegar.Doku.{rootNamespace}.";
            _manifestResourceNames = assembly.GetManifestResourceNames();
        }

        public ResourceReader? Find(string resourceName)
        {
            string? manifestResourceName = _manifestResourceNames.FirstOrDefault(x => IsMatch(x, resourceName));
            if (manifestResourceName != null)
            {
                Stream? stream = _assembly.GetManifestResourceStream(manifestResourceName);
                if (stream != null)
                {
                    return new ResourceReader(stream);
                }
            }

            return null;
        }

        private bool IsMatch(string embeddedResourceName, string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                return false;
            }

            string filename = Path.GetFileNameWithoutExtension(embeddedResourceName);
            return filename[_resourcePrefix.Length..].Equals(resourceName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

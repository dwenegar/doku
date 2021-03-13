// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dwenegar.Doku.Resources
{
    internal sealed class ResourceFinder
    {
        private readonly Assembly _assembly;
        private readonly string _resourcePrefix;
        private readonly IEnumerable<string> _manifestResourceNames;

        public ResourceFinder(Assembly assembly, string rootNamespace)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _resourcePrefix = $"{assembly.GetName().Name}.{rootNamespace}.";
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
            return !string.IsNullOrEmpty(resourceName)
                   && Path.GetFileNameWithoutExtension(embeddedResourceName)
                          .Substring(_resourcePrefix.Length)
                          .Equals(resourceName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

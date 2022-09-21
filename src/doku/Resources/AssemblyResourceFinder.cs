// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Doku.Resources;

internal sealed class AssemblyResourceFinder
{
    private readonly Assembly _assembly;
    private readonly string[] _manifestResourceNames;

    public AssemblyResourceFinder(Assembly assembly)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        _manifestResourceNames = assembly.GetManifestResourceNames();
    }

    public ResourceReader? Find(string resourceName)
    {
        string? manifestResourceName = _manifestResourceNames.FirstOrDefault(IsMatch);
        if (manifestResourceName != null)
        {
            Stream? stream = _assembly.GetManifestResourceStream(manifestResourceName);
            if (stream != null)
            {
                return new ResourceReader(stream);
            }
        }

        return null;

        bool IsMatch(string x)
        {
            return x.Equals(resourceName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

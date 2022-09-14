// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.Reflection;

namespace Doku.Utils;

internal static class AssemblyHelpers
{
    public static string GetAssemblyName()
    {
        Assembly assembly = typeof(AssemblyHelpers).Assembly;
        AssemblyName assemblyName = assembly.GetName();
        return assemblyName.Name ?? throw new InvalidOperationException("Invalid assembly name");
    }

    public static string GetInformationalVersion()
    {
        Assembly assembly = typeof(AssemblyHelpers).Assembly;
        AssemblyInformationalVersionAttribute? attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attr?.InformationalVersion ?? throw new InvalidOperationException($"Missing attribute {nameof(AssemblyInformationalVersionAttribute)}");
    }
}

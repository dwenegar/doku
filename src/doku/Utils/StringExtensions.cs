// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;

namespace Doku.Utils;

internal static class StringExtensions
{
    public static string NormalizeSeparators(this string path)
        => OperatingSystem.IsWindows() ? path.Replace('/', '\\') : path.Replace('\\', '/');
}

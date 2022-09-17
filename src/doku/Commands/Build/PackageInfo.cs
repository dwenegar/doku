// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;

namespace Doku.Commands.Build;

[Serializable]
internal sealed class PackageInfo
{
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    public override string ToString() => $"{DisplayName} version {Version}";
}

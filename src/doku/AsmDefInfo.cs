// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;

namespace Doku.Commands;

[Serializable]
internal sealed class AsmDefInfo
{
    public string Name { get; set; } = string.Empty;
}

// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;

namespace Doku.Commands;

internal enum TemplateType
{
    Full,
    Partial
}

[Serializable]
internal sealed class TemplateInfo
{
    public TemplateType Type { get; set; } = TemplateType.Partial;
}

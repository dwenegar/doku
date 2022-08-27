// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;

namespace Doku
{
    internal enum TemplateType
    {
        Full,
        Partial
    }

    [Serializable]
    internal sealed class TemplateInfo
    {
        public TemplateType Type { get; set; }
    }
}

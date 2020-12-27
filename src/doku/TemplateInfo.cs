// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;

namespace Dwenegar.Doku.Templates
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

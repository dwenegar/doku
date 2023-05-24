// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;

namespace Doku.Commands;

[Serializable]
internal sealed class DocumentationConfig
{
    public ProjectConfigExcludes Excludes { get; set; } = new();
    public string[] DefineConstants { get; set; } = Array.Empty<string>();
    public string[] Sources { get; set; } = { "**/Editor", "**/Runtime" };
}

[Serializable]
internal sealed class ProjectConfigExcludes
{
    public bool ApiDocs { get; set; }
    public bool Manual { get; set; }
    public bool License { get; set; }
    public bool Changelog { get; set; }
}

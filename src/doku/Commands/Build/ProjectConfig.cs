// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.Text.Json;

namespace Doku.Commands.Build;

[Serializable]
internal sealed class ProjectConfig
{
    public bool DisableDefaultFilter { get; set; }
    public bool EnableSearch { get; set; }
    public ProjectConfigExcludes Excludes { get; set; } = new();
    public string[] DefineConstants { get; set; } = Array.Empty<string>();
    public string[] Sources { get; set; } = { "Editor", "Runtime" };

    public override string ToString() => JsonSerializer.Serialize(this, SerializerContext.Default.ProjectConfig);
}

[Serializable]
internal sealed class ProjectConfigExcludes
{
    public bool ApiDocs { get; set; }
    public bool Manual { get; set; }
    public bool License { get; set; }
    public bool Changelog { get; set; }
}

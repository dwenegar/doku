// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Dwenegar.Doku
{
    [JsonSerializable(typeof(TemplateInfo))]
    [JsonSerializable(typeof(DocumentationBuilder.ProjectConfig))]
    [JsonSerializable(typeof(DocumentationBuilder.PackageInfo))]
    [JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    internal partial class SerializerContext : JsonSerializerContext { }
}

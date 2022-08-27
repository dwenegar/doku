// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System.Text.Json.Serialization;

namespace Doku
{
    [JsonSerializable(typeof(TemplateInfo))]
    [JsonSerializable(typeof(DocumentationBuilder.ProjectConfig))]
    [JsonSerializable(typeof(DocumentationBuilder.PackageInfo))]
    [JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    internal partial class SerializerContext : JsonSerializerContext { }
}

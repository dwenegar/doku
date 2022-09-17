// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System.Text.Json.Serialization;

namespace Doku.Commands.Build;

[JsonSerializable(typeof(TemplateInfo))]
[JsonSerializable(typeof(ProjectConfig))]
[JsonSerializable(typeof(PackageInfo))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
// ReSharper disable once PartialTypeWithSinglePart
internal partial class SerializerContext : JsonSerializerContext { }

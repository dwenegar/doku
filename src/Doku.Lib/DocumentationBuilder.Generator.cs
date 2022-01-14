// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dwenegar.Doku.Logging;
using Dwenegar.Doku.Utils;

namespace Dwenegar.Doku
{
    public sealed partial class DocumentationBuilder
    {
        private void GenerateCSharpProject()
        {
            using Logger.Scope scope = new("GenerateCSharpProject");
            Logger.LogVerbose("Generating the C# project.");

            const string projectTemplate =
                "<Project ToolsVersion=\"4.0\" DefaultTargets=\"FullPublish\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\n"
                + "  <PropertyGroup>\n"
                + "    <DefineConstants>{0}</DefineConstants>\n"
                + "    <TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>"
                + "  </PropertyGroup>\n"
                + "  <ItemGroup>\n"
                + "{1}"
                + "  </ItemGroup>\n"
                + "  <Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />\n"
                + "</Project>\n";

            string GetDefineConstants()
            {
                var sb = new StringBuilder();
                sb.Append(PackageDocsGenerationDefine);
                foreach (string defineConstant in _projectConfig.DefineConstants)
                {
                    sb.Append(';').Append(defineConstant);
                }

                return sb.ToString();
            }

            string GetCompileItems()
            {
                var sb = new StringBuilder();
                foreach (string cs in Directory.GetFiles(_buildSourcesPath, "*.cs", SearchOption.AllDirectories))
                {
                    sb.Append(@"    <Compile Include=""").Append(cs).AppendLine(@"""/>");
                }

                return sb.ToString();
            }

            Directory.CreateDirectory(_buildSourcesPath);
            var projectContent = string.Format(projectTemplate, GetDefineConstants(), GetCompileItems());
            Files.WriteText(Path.Combine(_buildSourcesPath, "doku.csproj"), projectContent);
        }

        private void GenerateDocFxJson()
        {
            using Logger.Scope scope = new("GenerateDocFxJson");
            Logger.LogVerbose("Generating docfx.json");

            var template = new StringBuilder();
            if (_templateInfo is not { Type: TemplateType.Full })
            {
                template.Append(@"""default""");
            }

            if (_templateInfo != null)
            {
                if (_templateInfo.Type == TemplateType.Partial)
                {
                    template.Append(", ");
                }

                template.Append('"').Append(TemplateFolder).Append('"');
            }

            string srcPath = Path.Combine(_buildPath, "docfx.json.in");
            string json = File.ReadAllText(srcPath);

            json = json.Replace("$DISABLE_DEFAULT_FILTER", _projectConfig.DisableDefaultFilter ? "true" : "false")
                       .Replace("$TEMPLATE", template.ToString());

            string dstPath = Path.Combine(_buildPath, "docfx.json");
            Files.WriteText(dstPath, json);
        }

        private void GenerateTableOfContents()
        {
            using Logger.Scope scope = new("GenerateTableOfContents");
            Logger.LogVerbose("Generating toc.yml");

            var toc = new StringBuilder();
            if (_manualHomePage != null)
            {
                toc.AppendLine("- name: Manual") //
                   .AppendLine("  href: manual/") //
                   .Append("  homepage: ")
                   .AppendLine(_manualHomePage);
            }

            if (_hasApiDocs)
            {
                toc.AppendLine("- name: API Documentation") //
                   .AppendLine("  href: api/") //
                   .AppendLine("  homepage: api/index.md");
            }

            if (_hasChangeLog)
            {
                toc.AppendLine("- name: Changes") //
                   .AppendLine("  href: changelog/");
            }

            if (_hasLicenses)
            {
                toc.AppendLine("- name: License") //
                   .AppendLine("  href: license/");
            }

            string dstPath = Path.Combine(_buildPath, "toc.yml");
            Files.WriteText(dstPath, toc.ToString());
        }

        private void GenerateGlobalMetadataJson()
        {
            using Logger.Scope scope = new("GenerateGlobalMetadataJson");
            Logger.LogVerbose("Generating globalMetadata.json");

            string sourceFile = Path.Combine(_buildPath, "globalMetadata.json.in");
            string source = File.ReadAllText(sourceFile);

            source = source.Replace("$APP_TITLE", _packageInfo!.DisplayName)
                           .Replace("$PACKAGE_VERSION", _packageInfo!.Version)
                           .Replace("$ENABLE_SEARCH", _projectConfig.EnableSearch ? "true" : "false");

            string destinationFile = Path.Combine(_buildPath, "globalMetadata.json");

            Files.WriteText(destinationFile, source);
        }

        private void GenerateManualTableOfContents(IEnumerable<string> manualFiles, string tocSourcePath)
        {
            using Logger.Scope scope = new("GenerateManualTableOfContents");
            Logger.LogVerbose("Generating manual's table of contents.");

            TocEntry root = new(null, null);

            foreach (string file in manualFiles)
            {
                string href = Path.GetRelativePath(_buildManualPath, file).Replace('\\', '/');
                string title = TocHelper.GetTitleForFile(file);
                root.AddEntry(title, href);
            }

            static void AppendTocSection(StringBuilder sb, TocEntry entry, int indent)
            {
                Logger.LogVerbose($"AppendTocSection entry={entry} indent={indent}");

                sb.Append(' ', indent).Append("- name: ").AppendLine(entry.Title);
                if (entry.Href != null)
                {
                    sb.Append(' ', indent).Append("  href: ").AppendLine(entry.Href);
                }

                if (entry.Entries.Any())
                {
                    sb.Append(' ', indent).AppendLine("  items:");
                    foreach (TocEntry tocEntry in entry.Entries)
                    {
                        AppendTocSection(sb, tocEntry, indent + 2);
                    }
                }
            }

            StringBuilder sb = new();
            foreach (TocEntry entry in root.Entries)
            {
                AppendTocSection(sb, entry, 0);
            }

            Files.WriteText(tocSourcePath, sb.ToString());
        }
    }
}

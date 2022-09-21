// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Doku.Resources;
using Doku.Utils;

namespace Doku.Commands.Build;

internal sealed partial class DocumentBuilder
{
    private async Task CreateProject()
    {
        using IDisposable _ = _logger.BeginGroup("Creating the DocFX project");

        await ExtractDocFxProject();
        await CopyResources();
        await CreateCSharpProject();
        await CreateGlobalMetadataJson();
        await CreateDocFxJson();
        await CreateTableOfContents();
    }

    private async Task ExtractDocFxProject()
    {
        Info("Extracting the DocFX base project");

        Assembly assembly = GetType().Assembly;
        var resourceManager = new ResourceManager(_logger);
        await resourceManager.ExportAssemblyResources(assembly, "doku.templates.project.zip", _buildPath);
    }

    private async Task CopyResources()
    {
        await CopyTemplateFiles();

        if (!_projectConfig!.Excludes.ApiDocs)
        {
            await CopySourceFiles();
        }

        if (!_projectConfig.Excludes.License)
        {
            await CopyLicenses();
        }

        if (!_projectConfig.Excludes.Changelog)
        {
            await CopyChangelog();
        }

        if (Directory.Exists(_packageDocumentationPath))
        {
            await Files.TryCopyFile("logo.svg", _packageDocumentationPath, _buildPath, _logger);
            await Files.TryCopyFile("favicon.ico", _packageDocumentationPath, _buildPath, _logger);

            if (!_projectConfig.Excludes.Manual)
            {
                await CopyManualFiles();
            }
        }
    }

    private async Task CreateCSharpProject()
    {
        Info("Creating the C# project");

        const string projectTemplate =
            "<Project ToolsVersion=\"4.0\" DefaultTargets=\"FullPublish\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\n"
            + "  <PropertyGroup>\n"
            + "    <DefineConstants>{0}</DefineConstants>\n"
            + "    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>"
            + "  </PropertyGroup>\n"
            + "  <ItemGroup>\n"
            + "{1}"
            + "  </ItemGroup>\n"
            + "  <Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />\n"
            + "</Project>\n";

        string GetDefineConstants()
        {
            var sb = new StringBuilder();
            sb.Append((string?)PackageDocsGenerationDefine);
            foreach (string defineConstant in _projectConfig!.DefineConstants)
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
        await Files.WriteText(Path.Combine(_buildSourcesPath, "doku.csproj"), projectContent, _logger);
    }

    private async Task CreateGlobalMetadataJson()
    {
        Info("Creating the globalMetadata.json file");

        string sourceFile = Path.Combine(_buildPath, "globalMetadata.json.in");
        string source = await Files.ReadText(sourceFile);

        source = source.Replace("$APP_TITLE", _packageInfo!.DisplayName)
                       .Replace("$PACKAGE_VERSION", _packageInfo.Version);

        string destinationFile = Path.Combine(_buildPath, "globalMetadata.json");

        await Files.WriteText(destinationFile, source, _logger);
    }

    private async Task CreateDocFxJson()
    {
        Info("Creating the docfx.json file");

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

            template.Append('"').Append((string?)TemplateFolder).Append('"');
        }

        string srcPath = Path.Combine(_buildPath, "docfx.json.in");
        string json = await Files.ReadText(srcPath);

        json = json.Replace("$UNITY_VERSION", _packageInfo!.Unity)
                   .Replace("$TEMPLATE", template.ToString());

        string dstPath = Path.Combine(_buildPath, "docfx.json");
        await Files.WriteText(dstPath, json, _logger);
    }

    private async Task CreateTableOfContents()
    {
        Info("Creating the main table of contents");

        var toc = new StringBuilder();
        if (_manualHomePage != null)
        {
            toc.AppendLine("- name: Manual")  //
               .AppendLine("  href: manual/") //
               .Append("  homepage: ")
               .AppendLine(_manualHomePage);
        }

        if (_hasApiDocs)
        {
            toc.AppendLine("- name: API Documentation") //
               .AppendLine("  href: api/")              //
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
        await Files.WriteText(dstPath, toc.ToString(), _logger);
    }

    private async Task CreateManualTableOfContents(IEnumerable<string> manualFiles, string tocSourcePath)
    {
        Info("Creating the manual's table of contents");

        TocEntry root = new(null, null);

        foreach (string file in manualFiles)
        {
            string href = Path.GetRelativePath(_buildManualPath, file).Replace('\\', '/');
            string title = TocHelper.GetTitleForFile(file);
            root.AddEntry(title, href);
        }

        void AppendTocSection(StringBuilder sb, TocEntry entry, int indent)
        {
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

        await Files.WriteText(tocSourcePath, sb.ToString(), _logger);
    }

    private async Task CopyTemplateFiles()
    {
        Info("Copying the template files");

        if (_templatePath is not null)
        {
            string dst = Path.GetFullPath(Path.Combine(_buildPath, TemplateFolder));
            await Files.CopyDirectory(_templatePath, dst, "*.*", _logger);
        }
    }

    private async Task CopySourceFiles()
    {
        Info("Copying the source code");

        var totalFileCount = 0;
        foreach (string source in _projectConfig!.Sources)
        {
            string src = Path.Combine(_packagePath, source);
            string dst = Path.Combine(_buildSourcesPath, source);
            int fileCount = await Files.CopyDirectory(src, dst, "*.cs", _logger);
            Info($"- {source} / {fileCount} files");

            totalFileCount += fileCount;
        }


        _hasApiDocs = totalFileCount > 0;
    }

    private async Task CopyManualFiles()
    {
        Info("Copying the manual files");

        await Files.CopyDirectory(_packageDocumentationPath, _buildManualPath, "*.*", _logger);

        if (!Directory.EnumerateFiles(_buildManualPath, "*.md", SearchOption.AllDirectories).Any())
        {
            return;
        }

        string homeSourcePath = Path.Combine(_buildManualPath, "home.md");
        string homeDestinationPath = Path.Combine(_buildPath, "index.md");

        Files.MoveFile(homeSourcePath, homeDestinationPath, _logger);
        Files.MoveFile("filter.yml", _buildManualPath, _buildPath, _logger);
        Files.MoveFile("projectMetadata.yml", _buildManualPath, _buildPath, _logger);

        var files = Directory.GetFiles(_buildManualPath, "*.md", SearchOption.AllDirectories)
                             .ToList();
        files.Sort();

        // move `index.md` to the top
        string manualIndexPath = Path.Combine(_buildManualPath, "index.md");
        if (files.Remove(manualIndexPath))
        {
            files.Insert(0, manualIndexPath);
        }

        _manualHomePage = Path.GetRelativePath(_buildPath, files[0]).Replace('\\', '/');

        string tocSourcePath = Path.Combine(_buildManualPath, "toc.yml");
        if (!File.Exists(tocSourcePath))
        {
            Warning("Missing `toc.yml` file; will create one.");
            await CreateManualTableOfContents(files, tocSourcePath);
        }
    }

    private async Task CopyLicenses()
    {
        Info("Copying licenses");

        var toc = new StringBuilder();

        string[] licenseFiles = { "LICENSE.md", "LICENSE.text" };
        foreach (string licenseFile in licenseFiles)
        {
            if (await TryCopyPackageFileToBuildFolder(licenseFile, "license/LICENSE.md"))
            {
                toc.AppendLine("- name: License") //
                   .AppendLine("  href: LICENSE.md");
            }

        }

        string[] thirdPartyLicenseFiles =
        {
            "Third Party Notices.md",
            "ThirdPartyNotices.md",
            "Third Party Notices.txt",
            "ThirdPartyNotices.txt"
        };

        foreach (string thirdPartyLicenseFile in thirdPartyLicenseFiles)
        {
            if (await TryCopyPackageFileToBuildFolder(thirdPartyLicenseFile, "license/ThirdPartyNotices.md"))
            {
                toc.AppendLine("- name: Third Party Notices") //
                   .AppendLine("  href: ThirdPartyNotices.md");
            }
        }

        if (toc.Length > 0)
        {
            _hasLicenses = true;

            string destinationFolder = Path.Combine(_buildPath, "license");
            Directory.CreateDirectory(destinationFolder);

            string indexFile = Path.Combine(destinationFolder, "index.md");
            await Files.WriteText(indexFile, "<script>window.location.replace('LICENSE.html')</script>", _logger);

            string tocFile = Path.Combine(destinationFolder, "toc.yml");
            await Files.WriteText(tocFile, toc.ToString(), _logger);
        }
    }

    private async Task CopyChangelog()
    {
        Info("Copying changelog");
        if (await TryCopyPackageFileToBuildFolder("CHANGELOG.md", "changelog/CHANGELOG.md"))
        {
            _hasChangeLog = true;

            var tocContent = new StringBuilder();
            tocContent.AppendLine("- name: Changelog") //
                      .AppendLine("  href: CHANGELOG.md");

            string destinationFolder = Path.Combine(_buildPath, "changelog");
            Directory.CreateDirectory(destinationFolder);

            string indexFile = Path.Combine(destinationFolder, "index.md");
            await Files.WriteText(indexFile, "<script>window.location.replace('CHANGELOG.html')</script>", _logger);

            string tocFile = Path.Combine(destinationFolder, "toc.yml");
            await Files.WriteText(tocFile, tocContent.ToString(), _logger);
        }
    }

    private static class TocHelper
    {
        private static readonly string[] s_shouldBeCapitalized =
        {
            "i", "we", "you", "he", "she", "our", "him", "her", "his"
        };

        private static readonly string[] s_shouldBeAllCaps =
        {
            "http", "url"
        };

        public static string GetTitleForFile(string path)
        {
            string? header = FindFirstHeader(path);
            if (header != null)
            {
                string title = header.TrimStart('#').TrimStart();
                if (!string.IsNullOrEmpty(title))
                {
                    return title;
                }
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            return ToTitleCase(fileName);
        }

        // Implement title case loosely based on the APA Style
        public static string ToTitleCase(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            char[] chars = s.ToCharArray();

            // first pass: convert all characters to lowercase
            ToTitleCase_ToLower(chars);

            // second pass: convert all single '-' to spaces
            ToTitleCase_SingleHyphenToSpace(chars);

            // third pass: convert consecutive '-' into a single '-'
            int n = ToTitleCase_CompressHyphens(chars);

            // change capitalization
            var sb = new StringBuilder();

            var title = new string(chars, 0, n);
            string[] parts = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return s;
            }

            AppendPart(sb, parts[0]);
            for (var i = 1; i < parts.Length; i++)
            {
                sb.Append(' ');
                AppendPart(sb, parts[i]);
            }

            return sb.ToString();
        }

        private static void AppendPart(StringBuilder sb, string s)
        {
            if (Array.IndexOf(s_shouldBeAllCaps, s) != -1)
            {
                sb.Append(s.ToUpper());
            }
            else if (Array.IndexOf(s_shouldBeCapitalized, s) == -1 && s.Length < 4)
            {
                sb.Append(s);
            }
            else
            {
                sb.Append(char.ToUpper(s[0]));
                if (s.Length > 1)
                {
                    sb.Append(s.Substring(1));
                }
            }
        }

        private static int ToTitleCase_CompressHyphens(char[] chars)
        {
            var n = 0;
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != '-' || i + 1 == chars.Length || chars[i + 1] != '-')
                {
                    chars[n++] = chars[i];
                }
            }

            return n;
        }

        private static void ToTitleCase_SingleHyphenToSpace(char[] chars)
        {
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] == '-'
                    && (i == 0 || chars[i - 1] != '-')
                    && (i + 1 == chars.Length || chars[i + 1] != '-'))
                {
                    chars[i] = ' ';
                }
            }
        }

        private static void ToTitleCase_ToLower(char[] chars)
        {
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = char.ToLower(chars[i]);
            }
        }

        private static string? FindFirstHeader(string path)
        {
            foreach (string line in File.ReadLines(path))
            {
                if (line.StartsWith('#'))
                {
                    return line;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    break;
                }
            }

            return null;
        }
    }

    private sealed class TocEntry
    {
        private static int s_nextId;

        private readonly int _id = s_nextId++;
        private readonly TocEntry? _parent;
        private readonly List<TocEntry> _entries = new();

        public TocEntry(TocEntry? parent, string? title)
        {
            Title = title;
            _parent = parent;
        }

        public string? Title { get; }
        public string? Href { get; private set; }

        public IEnumerable<TocEntry> Entries => _entries;

        public TocEntry AddEntry(string title, string? href)
        {
            string? parentName = Path.GetDirectoryName(href);
            TocEntry parent = string.IsNullOrEmpty(parentName) ? this : GetParentEntry(parentName);

            TocEntry? entry = parent.Entries.FirstOrDefault(x => x.Title == title);
            if (entry == null)
            {
                parent._entries.Add(entry = new TocEntry(parent, title));
            }

            if (href != null && href.EndsWith(".md"))
            {
                entry.Href = href.Replace('\\', '/');
            }

            return entry;
        }

        public override string ToString() => $"id={_id} href={Href} title={Title} parent={_parent?._id ?? -1}";

        private TocEntry GetParentEntry(string path)
        {
            string entryName = Path.GetFileName(path);
            string title = TocHelper.ToTitleCase(entryName);

            foreach (TocEntry tocEntry in Entries)
            {
                if (string.Equals(tocEntry.Title, title))
                {
                    return tocEntry;
                }
            }

            return AddEntry(entryName, path);
        }
    }
}

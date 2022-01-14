// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Dwenegar.Doku.Logging;
using Dwenegar.Doku.Resources;
using Dwenegar.Doku.Utils;

namespace Dwenegar.Doku
{
    internal delegate void DocumentationBuildCompletedHandler();

    public sealed class DocumentationBuilder
    {
        private const string TemplateFolder = "templates/custom";
        private const string PackageDocsGenerationDefine = "PACKAGE_DOCS_GENERATION";

        private readonly string _packagePath;
        private readonly string _outputPath;

        private readonly string _buildPath;
        private readonly string _buildManualPath;
        private readonly string _buildSourcesPath;

        private bool _hasApiDocs;
        private bool _hasChangeLog;
        private bool _hasLicenses;
        private bool _hasManual;
        private string? _manualHomePage;

        private TemplateInfo? _templateInfo;
        private string? _packageManualPath;
        private ProjectConfig _projectConfig = new();
        private PackageInfo? _packageInfo;

        public DocumentationBuilder(string packagePath, string outputPath, string? buildPath)
        {
            _packagePath = packagePath;
            _outputPath = outputPath;

            _buildPath = buildPath == null
                ? Path.Combine(Path.GetTempPath(), $"dg-{Path.GetRandomFileName()}")
                : Path.GetFullPath(buildPath);
            _buildManualPath = Path.Combine(_buildPath, "manual");
            _buildSourcesPath = Path.Combine(_buildPath, "src");
        }

        public string? DocFxPath { get; set; }

        public bool KeepBuildFolder { get; init; }

        public string? TemplatePath { get; init; }

        public void Build()
        {
            using Logger.Scope scope = new("Build");

            Logger.LogVerbose($"OutputPath: {_outputPath}");
            Logger.LogVerbose($"PackagePath: {_packagePath}");
            Logger.LogVerbose($"TemplateRoot: {TemplatePath}");

            if (KeepBuildFolder)
            {
                Logger.LogInfo($"BuildPath: {_buildPath}");
            }
            else
            {
                Logger.LogVerbose($"BuildPath: {_buildPath}");
            }

            try
            {
                LoadPackageJson();

                LocateDocFx();
                LocateManualFolder();

                LoadConfigJson();
                LoadTemplateJson();

                DeleteFolders();

                InitializeDocFxProject();
                CopyTemplateFiles();
                CopySourceFiles();
                CopyManualFiles();
                CopyLogo();
                CopyFavicon();
                CopyLicense();
                CopyChangelog();

                GenerateCSharpProject();
                GenerateGlobalMetadataJson();
                GenerateDocFxJson();
                GenerateTableOfContents();

                RunDocFx();

                CopyFilesToOutputFolder();
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
                Logger.LogVerbose(e);
            }
            finally
            {
                DeleteBuildFolder();
            }
        }

        private void LoadPackageJson()
        {
            Logger.LogVerbose("Loading package.json.");
            using Logger.Scope scope = new("LoadPackageInfo");

            string packageJsonPath = "package.json";
            if (!string.IsNullOrEmpty(_packagePath))
            {
                packageJsonPath = Path.Combine(_packagePath, packageJsonPath);
            }

            try
            {
                packageJsonPath = Path.GetFullPath(packageJsonPath);
                string packageJsonText = File.ReadAllText(packageJsonPath);
                _packageInfo = JsonSerializer.Deserialize(packageJsonText, SerializerContext.Default.PackageInfo);
                if (_packageInfo == null)
                {
                    throw new Exception("Invalid package.json.");
                }

                Logger.LogVerbose($"Package DisplayName: {_packageInfo.DisplayName}");
                Logger.LogVerbose($"Package Version: {_packageInfo.Version}");
            }
            catch (FileNotFoundException)
            {
                throw new Exception($"File {packageJsonPath} does not exist.");
            }
        }

        private void LocateDocFx()
        {
            Logger.LogVerbose("Locating docfx.exe.");
            using Logger.Scope scope = new("LocateDocFx");

            static bool ContainsDocFxExe(string? directory)
            {
                return !string.IsNullOrEmpty(directory) && File.Exists(Path.Combine(directory, "docfx.exe"));
            }

            if (DocFxPath == null)
            {
                string? envPath = Environment.GetEnvironmentVariable("PATH");
                DocFxPath = envPath?.Split(Path.PathSeparator).FirstOrDefault(ContainsDocFxExe);
                if (DocFxPath == null)
                {
                    throw new Exception("Could not find docfx.exe in the system path.");
                }
            }
            else if (!ContainsDocFxExe(DocFxPath))
            {
                throw new Exception($"{DocFxPath} is not a valid DocFx installation.");
            }

            Logger.LogVerbose($"DocFxPath: {DocFxPath}");

            var docfx = new DocFx(DocFxPath);
            Logger.LogVerbose($"Found DocFx version {docfx.Version}");
        }

        private void LocateManualFolder()
        {
            Logger.LogVerbose("Locating manual folder.");
            using Logger.Scope scope = new("LocateManualFolder");

            string path = Path.Combine(_packagePath, "Documentation~");
            if (Directory.Exists(path))
            {
                _packageManualPath = path;
                Logger.LogVerbose($"PackageManualPath: {_packageManualPath}");
            }
        }

        private void LoadConfigJson()
        {
            if (_packageManualPath == null)
            {
                _projectConfig = new ProjectConfig();
                return;
            }

            Logger.LogVerbose("Loading config.json.");
            using Logger.Scope scope = new("LoadConfigJson");

            string path = Path.Combine(_packageManualPath, "config.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                ProjectConfig? projectConfig = JsonSerializer.Deserialize(json, SerializerContext.Default.ProjectConfig);
                if (projectConfig != null)
                {
                    _projectConfig = projectConfig;
                    Logger.LogVerbose($"Config: {_projectConfig}");
                }
            }
        }

        private void LoadTemplateJson()
        {
            if (TemplatePath == null)
            {
                return;
            }

            Logger.LogVerbose("Loading template.json.");
            using Logger.Scope scope = new("LoadTemplateJson");

            string path = Path.Combine(TemplatePath, "template.json");
            if (!File.Exists(path))
            {
                throw new Exception($"Template at {TemplatePath} is missing file template.json.");
            }

            string json = File.ReadAllText(path, Encoding.UTF8);
            _templateInfo = JsonSerializer.Deserialize(json, SerializerContext.Default.TemplateInfo);
            if (_templateInfo == null)
            {
                throw new Exception($"Failed to load {path}.");
            }
        }

        private void DeleteFolders()
        {
            Logger.LogVerbose("Deleting folders.");

            Files.DeleteDirectory(_outputPath);
            Files.DeleteDirectory(_buildPath);
        }

        private void InitializeDocFxProject()
        {
            Logger.LogVerbose("Initializing the DocFx project.");
            using Logger.Scope scope = new("InitializeDocFxProject");

            Assembly assembly = typeof(DocumentationBuilder).Assembly;
            var resourceManager = new ResourceManager(assembly, "Templates");
            resourceManager.ExportResources("project", _buildPath);
        }

        private void CopyTemplateFiles()
        {
            if (TemplatePath == null)
            {
                return;
            }

            Logger.LogVerbose("Copying template files.");
            using Logger.Scope scope = new("CopyTemplateFiles");

            string dst = Path.GetFullPath(Path.Combine(_buildPath, TemplateFolder));
            Files.CopyDirectory(TemplatePath, dst);
        }

        private void CopySourceFiles()
        {
            if (_projectConfig.Excludes.ApiDocs)
            {
                Logger.LogInfo("Skipping copying source files.");
                return;
            }

            Logger.LogVerbose("Copying source files.");
            using Logger.Scope scope = new("CopySourceFiles");

            _hasApiDocs = true;
            foreach (var source in _projectConfig.Sources)
            {
                Files.CopyDirectory(Path.Combine(_packagePath, source),
                                    Path.Combine(_buildSourcesPath, source),
                                    "*.cs");
            }
        }

        private void CopyManualFiles()
        {
            if (_projectConfig.Excludes.Manual)
            {
                Logger.LogInfo("Skipping copying manual files.");
                return;
            }

            Logger.LogVerbose("Copying manual files.");
            using Logger.Scope scope = new("CopyManualFiles");

            Files.CopyDirectory(_packageManualPath, _buildManualPath);

            _hasManual = Directory.EnumerateFiles(_buildManualPath, "*.md", SearchOption.AllDirectories).Any();
            if (!_hasManual)
            {
                return;
            }

            string homeSourcePath = Path.Combine(_buildManualPath, "home.md");
            string homeDestinationPath = Path.Combine(_buildPath, "index.md");

            Files.MoveFile(homeSourcePath, homeDestinationPath);

            Files.MoveFile("filter.yml", _buildManualPath, _buildPath);
            Files.MoveFile("projectMetadata.yml", _buildManualPath, _buildPath);

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
                Logger.LogWarning("Missing `toc.yml` file; will create one.");
                GenerateManualTableOfContents(files, tocSourcePath);
            }
        }

        private void GenerateManualTableOfContents(IEnumerable<string> manualFiles, string tocSourcePath)
        {
            Logger.LogVerbose("Generating manual's table of contents.");
            using Logger.Scope scope = new("GenerateManualTableOfContents");

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
            foreach (var entry in root.Entries)
            {
                AppendTocSection(sb, entry, 0);
            }

            Files.WriteText(tocSourcePath, sb.ToString());
        }

        private void CopyLicense()
        {
            if (_projectConfig.Excludes.License)
            {
                Logger.LogInfo("Skipping copying license files.");
                return;
            }

            Logger.LogVerbose("Copying license files.");
            using Logger.Scope scope = new("CopyLicense");

            var toc = new StringBuilder();

            if (TryCopyPackageFileToBuildFolder("LICENSE.md", "license/LICENSE.md"))
            {
                _hasLicenses = true;
                toc.AppendLine("- name: License") //
                   .AppendLine("  href: LICENSE.md");
            }

            if (TryCopyPackageFileToBuildFolder("Third Party Notices.md", "license/ThirdPartyNotices.md")
                || TryCopyPackageFileToBuildFolder("ThirdPartyNotices.md", "license/ThirdPartyNotices.md"))
            {
                _hasLicenses = true;
                toc.AppendLine("- name: Third Party Notices") //
                   .AppendLine("  href: ThirdPartyNotices.md");
            }

            if (_hasLicenses)
            {
                string destinationFolder = Path.Combine(_buildPath, "license");
                Directory.CreateDirectory(destinationFolder);

                string indexFile = Path.Combine(destinationFolder, "index.md");
                Files.WriteText(indexFile, "<script>window.location.replace('LICENSE.html')</script>");

                string tocFile = Path.Combine(destinationFolder, "toc.yml");
                Files.WriteText(tocFile, toc.ToString());
            }
        }

        private void CopyLogo()
        {
            if (_packageManualPath == null)
            {
                return;
            }

            Logger.LogVerbose("Copying logo file.");
            using Logger.Scope scope = new("CopyLogo");

            Files.TryCopyFile("logo.svg", _packageManualPath, _buildPath);
        }

        private void CopyFavicon()
        {
            if (_packageManualPath == null)
            {
                return;
            }

            Logger.LogVerbose("Copying favicon file.");
            using Logger.Scope scope = new("CopyFavicon");

            Files.TryCopyFile("favicon.ico", _packageManualPath, _buildPath);
        }

        private void CopyChangelog()
        {
            if (_projectConfig.Excludes.Changelog)
            {
                Logger.LogInfo("Skipping changelog file.");
                return;
            }

            Logger.LogVerbose("Copying changelog file.");
            using Logger.Scope scope = new("CopyChangelog");

            if (TryCopyPackageFileToBuildFolder("CHANGELOG.md", "changelog/CHANGELOG.md"))
            {
                _hasChangeLog = true;

                var tocContent = new StringBuilder();
                tocContent.AppendLine("- name: Changelog") //
                          .AppendLine("  href: CHANGELOG.md");

                string destinationFolder = Path.Combine(_buildPath, "changelog");
                Directory.CreateDirectory(destinationFolder);

                string indexFile = Path.Combine(destinationFolder, "index.md");
                Files.WriteText(indexFile, "<script>window.location.replace('CHANGELOG.html')</script>");

                string tocFile = Path.Combine(destinationFolder, "toc.yml");
                Files.WriteText(tocFile, tocContent.ToString());
            }
        }

        private void GenerateCSharpProject()
        {
            Logger.LogVerbose("Generating the C# project.");
            using Logger.Scope scope = new("GenerateCSharpProject");

            const string projectTemplate =
                "<Project ToolsVersion=\"4.0\" DefaultTargets=\"FullPublish\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\n"
                + "  <PropertyGroup>\n"
                + "    <DefineConstants>{0}</DefineConstants>\n"
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

        private void GenerateGlobalMetadataJson()
        {
            Debug.Assert(_packageInfo != null, "_packageInfo != null");

            Logger.LogVerbose("Generating globalMetadata.json");
            using Logger.Scope scope = new("GenerateGlobalMetadataJson");

            string sourceFile = Path.Combine(_buildPath, "globalMetadata.json.in");
            string source = File.ReadAllText(sourceFile);

            source = source.Replace("$APP_TITLE", _packageInfo.DisplayName)
                           .Replace("$PACKAGE_VERSION", _packageInfo.Version)
                           .Replace("$ENABLE_SEARCH", _projectConfig.EnableSearch.ToString().ToLower());

            string destinationFile = Path.Combine(_buildPath, "globalMetadata.json");

            Files.WriteText(destinationFile, source);
        }

        private void GenerateDocFxJson()
        {
            Logger.LogVerbose("Generating docfx.json");
            using Logger.Scope scope = new("GenerateDocFxJson");

            var template = new StringBuilder();
            if (_templateInfo == null || _templateInfo.Type != TemplateType.Full)
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

            json = json.Replace("$DISABLE_DEFAULT_FILTER", _projectConfig.DisableDefaultFilter.ToString().ToLower())
                       .Replace("$TEMPLATE", template.ToString());

            string dstPath = Path.Combine(_buildPath, "docfx.json");
            Files.WriteText(dstPath, json);
        }

        private void GenerateTableOfContents()
        {
            Logger.LogVerbose("Generating toc.yml");
            using Logger.Scope scope = new("GenerateTableOfContent");

            var toc = new StringBuilder();
            if (_hasManual)
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

        private void RunDocFx()
        {
            Logger.LogVerbose("Running docfx.");
            using Logger.Scope scope = new("RunDocFx");

            var docFx = new DocFx(DocFxPath);
            string docFxJsonFile = Path.Combine(_buildPath, "docfx.json");
            docFx.Run(docFxJsonFile, _buildPath);
        }

        private void CopyFilesToOutputFolder()
        {
            Logger.LogVerbose("Copying files to output folder.");
            using Logger.Scope scope = new("CopyFilesToOutputFolder");

            string sourcePath = Path.Combine(_buildPath, "_site");
            Directory.CreateDirectory(_outputPath);
            Files.CopyDirectory(sourcePath, _outputPath);
        }

        private void DeleteBuildFolder()
        {
            if (KeepBuildFolder)
            {
                return;
            }

            Logger.LogVerbose("Deleting the build folder.");
            using Logger.Scope scope = new("DeleteBuildFolder");

            Files.DeleteDirectory(_buildPath);
        }

        private bool TryCopyPackageFileToBuildFolder(string fileName, string? destinationFileName = null)
        {
            string source = Path.Combine(_packagePath, fileName);
            string destination = Path.Combine(_buildPath, destinationFileName ?? fileName);
            return Files.TryCopyFile(source, destination);
        }

        [Serializable]
        internal sealed class PackageInfo
        {
            public string DisplayName { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
        }

        [Serializable]
        internal sealed class ProjectConfig
        {
            public bool DisableDefaultFilter { get; set; }
            public bool EnableSearch { get; set; }
            public ProjectConfigExcludes Excludes { get; set; } = new();
            public string[] DefineConstants { get; set; } = Array.Empty<string>();
            public string[] Sources { get; set; } = { "Editor", "Runtime" };

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append("DisableDefaultFilter = ").Append(DisableDefaultFilter).AppendLine();
                sb.Append("EnableSearch = ").Append(EnableSearch).AppendLine();
                sb.Append("DefineConstants = ").AppendLine(string.Join(',', DefineConstants));
                sb.AppendLine("Excludes = {");
                sb.Append("  ApiDocs = ").Append(Excludes.ApiDocs).AppendLine();
                sb.Append("  Manual = ").Append(Excludes.Manual).AppendLine();
                sb.Append("  License = ").Append(Excludes.License).AppendLine();
                sb.Append("  Changelog = ").Append(Excludes.Changelog).AppendLine();
                sb.AppendLine("}");
                return sb.ToString();
            }
        }

        [Serializable]
        internal sealed class ProjectConfigExcludes
        {
            public bool ApiDocs { get; set; } = false;
            public bool Manual { get; set; } = false;
            public bool License { get; set; } = false;
            public bool Changelog { get; set; } = false;
        }
    }
}

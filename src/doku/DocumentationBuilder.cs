// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Doku.Logging;
using Doku.Utils;

namespace Doku;

internal delegate void DocumentationBuildCompletedHandler();

internal sealed partial class DocumentationBuilder
{
    private const string TemplateFolder = "templates/custom";
    private const string PackageDocsGenerationDefine = "PACKAGE_DOCS_GENERATION";

    private readonly string _packagePath;
    private readonly string _outputPath;
    private readonly string _buildPath;
    private readonly Logger _logger;

    private readonly string _buildManualPath;
    private readonly string _buildSourcesPath;

    private bool _hasApiDocs;
    private bool _hasChangeLog;
    private bool _hasLicenses;
    private string? _manualHomePage;

    private PackageInfo? _packageInfo;
    private ProjectConfig _projectConfig = new();
    private TemplateInfo? _templateInfo;
    private string? _packageManualPath;

    public DocumentationBuilder(string packagePath, string outputPath, string? buildPath, Logger logger)
    {
        _logger = logger;
        _packagePath = Path.GetFullPath(packagePath);
        _outputPath = Path.GetFullPath(outputPath);
        _buildPath = buildPath == null
            ? Path.Combine(Path.GetTempPath(), $"dg-{Path.GetRandomFileName()}")
            : Path.GetFullPath(buildPath);

        _buildManualPath = Path.Combine(_buildPath, "manual");
        _buildSourcesPath = Path.Combine(_buildPath, "src");
    }

    public string? DocFxPath { get; init; }
    public bool KeepBuildFolder { get; init; }
    public string? TemplatePath { get; init; }
    public string? StyleSheetPath { get; init; }

    public async Task Build()
    {
        Verbose($"OutputPath: {_outputPath}");
        Verbose($"PackagePath: {_packagePath}");
        Verbose($"BuildPath: {_buildPath}");

        try
        {
            DeleteFolders();

            CheckDocFx();
            LoadPackageInfo();
            LocatePackageManualFolder();

            LoadProjectConfig();
            if (TryLoadTemplateInfo(out TemplateInfo? templateInfo))
            {
                _templateInfo = templateInfo!;
            }
            else if (TryCopyStyle())
            {
                _templateInfo = new TemplateInfo
                {
                    Type = TemplateType.Partial
                };
            }

            ExtractDocFxProject();

            CopyResources();

            GenerateCSharpProject();
            GenerateGlobalMetadataJson();
            GenerateDocFxJson();
            GenerateTableOfContents();
            GeneratePdfTableOfContents();

            await RunDocFx();

            CopyFilesToOutputFolder();
        }
        catch (Exception e)
        {
            Error(e.Message);
        }
        finally
        {
            if (!KeepBuildFolder)
            {
                DeleteBuildFolder();
            }
        }
    }

    private void CopyResources()
    {
        CopyTemplateFiles();
        CopySourceFiles();
        CopyManualFiles();
        CopyLogo();
        CopyFavicon();
        CopyLicenses();
        CopyChangelog();
    }

    private void CopyTemplateFiles()
    {
        if (TemplatePath != null)
        {
            Verbose("Copying template files.");
            string dst = Path.GetFullPath(Path.Combine(_buildPath, TemplateFolder));
            Files.CopyDirectory(TemplatePath, dst);
        }
    }

    private void CopySourceFiles()
    {
        if (_projectConfig.Excludes.ApiDocs)
        {
            Info("Skipping copying source files.");
            return;
        }

        Verbose("Copying source files.");

        int count = _projectConfig.Sources.Sum(x => CopyFiles(x));
        _hasApiDocs = count > 0;

        int CopyFiles(string source)
        {
            return Files.CopyDirectory(Path.Combine(_packagePath, source),
                                       Path.Combine(_buildSourcesPath, source),
                                       "*.cs");
        }
    }

    private void CopyManualFiles()
    {
        if (_projectConfig.Excludes.Manual || _packageManualPath == null)
        {
            return;
        }

        Verbose("Copying manual files.");

        Files.CopyDirectory(_packageManualPath, _buildManualPath);

        if (!Directory.EnumerateFiles(_buildManualPath, "*.md", SearchOption.AllDirectories).Any())
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
            Warning("Missing `toc.yml` file; will create one.");
            GenerateManualTableOfContents(files, tocSourcePath);
        }
    }

    private void CopyLicenses()
    {
        if (_projectConfig.Excludes.License)
        {
            return;
        }

        Verbose("Copying license files.");

        var toc = new StringBuilder();

        string[] licenseFiles = { "LICENSE.md", "LICENSE.text" };
        if (licenseFiles.Any(x => TryCopyPackageFileToBuildFolder(x, "license/LICENSE.md")))
        {
            toc.AppendLine("- name: License") //
               .AppendLine("  href: LICENSE.md");
        }

        string[] thirdPartyLicenseFiles =
        {
            "Third Party Notices.md",
            "ThirdPartyNotices.md",
            "Third Party Notices.txt",
            "ThirdPartyNotices.txt"
        };

        if (thirdPartyLicenseFiles.Any(x => TryCopyPackageFileToBuildFolder(x, "license/ThirdPartyNotices.md")))
        {
            toc.AppendLine("- name: Third Party Notices") //
               .AppendLine("  href: ThirdPartyNotices.md");
        }

        if (toc.Length > 0)
        {
            _hasLicenses = true;

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
        if (_packageManualPath != null)
        {
            Verbose("Copying logo file.");
            Files.TryCopyFile("logo.svg", _packageManualPath, _buildPath);
        }
    }

    private void CopyFavicon()
    {
        if (_packageManualPath != null)
        {
            Verbose("Copying favicon file.");
            Files.TryCopyFile("favicon.ico", _packageManualPath, _buildPath);
        }
    }

    private void CopyChangelog()
    {
        if (_projectConfig.Excludes.Changelog)
        {
            return;
        }

        Verbose("Copying changelog file.");

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

    private void CopyFilesToOutputFolder()
    {
        Verbose($"Copying files to {_outputPath}.");
        string sourcePath = Path.Combine(_buildPath, "_site");
        Files.CopyDirectory(sourcePath, _outputPath);

        var pdfFileName = $"{Path.GetFileName(_buildPath)}_pdf.pdf";

        string source = Path.Combine(_buildPath, "_site_pdf", pdfFileName);
        string destination = Path.Combine(_outputPath, $"{_packageInfo!.DisplayName}.pdf");
        Files.TryCopyFile(source, destination);
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
}

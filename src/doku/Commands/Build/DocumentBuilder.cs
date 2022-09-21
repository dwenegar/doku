// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Threading.Tasks;
using Doku.Logging;
using Doku.Utils;

namespace Doku.Commands.Build;

internal sealed partial class DocumentBuilder
{
    private const string TemplateFolder = "templates/custom";
    private const string PackageDocsGenerationDefine = "PACKAGE_DOCS_GENERATION";

    private readonly string _packagePath;
    private readonly string _outputPath;
    private readonly string _buildPath;
    private readonly Logger _logger;

    private readonly string _packageDocumentationPath;
    private readonly string _buildManualPath;
    private readonly string _buildSourcesPath;

    private bool _hasApiDocs;
    private bool _hasChangeLog;
    private bool _hasLicenses;
    private string? _manualHomePage;

    private PackageInfo? _packageInfo;
    private ProjectConfig _projectConfig = new();
    private TemplateInfo? _templateInfo;
    private string? _templatePath;

    private string? _docFxPath;

    public DocumentBuilder(string packagePath, string outputPath, string? buildPath, Logger logger)
    {
        _logger = logger;
        _packagePath = Path.GetFullPath(packagePath);
        _outputPath = Path.GetFullPath(outputPath);
        _buildPath = buildPath == null
            ? Path.Combine(Path.GetTempPath(), $"{AssemblyHelpers.GetAssemblyName()}-{Path.GetRandomFileName()}")
            : Path.GetFullPath(buildPath);

        _packageDocumentationPath = Path.Combine(_packagePath, "Documentation~");
        _buildManualPath = Path.Combine(_buildPath, "manual");
        _buildSourcesPath = Path.Combine(_buildPath, "src");
    }

    public string? DocFxPath { get; init; }
    public bool KeepBuildFolder { get; init; }
    public string? TemplatePath { get; init; }
    public string? StyleSheetPath { get; init; }

    public async Task Build()
    {
        try
        {
            Files.DeleteDirectory(_outputPath, _logger);
            Files.DeleteDirectory(_buildPath, _logger);

            await Configure();
            await CreateProject();
            await RunDocFx();
            await CopyFilesToOutputFolder();
        }
        catch (Exception e)
        {
            Error(e.Message);
        }
        finally
        {
            if (!KeepBuildFolder)
            {
                Files.DeleteDirectory(_buildPath, _logger);
            }
        }
    }

    private async Task CopyFilesToOutputFolder()
    {
        using IDisposable _ = _logger.BeginGroup("Copying files to output folder");

        string sourcePath = Path.Combine(_buildPath, "_site");
        await Files.CopyDirectory(sourcePath, _outputPath, "*.*", _logger);
    }

    private async Task<bool> TryCopyPackageFileToBuildFolder(string fileName, string? destinationFileName = null)
    {
        string source = Path.Combine(_packagePath, fileName);
        string destination = Path.Combine(_buildPath, destinationFileName ?? fileName);
        return await Files.TryCopyFile(source, destination, _logger);
    }
}

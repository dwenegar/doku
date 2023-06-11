// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Doku.Utils;

namespace Doku.Commands.Build;

internal sealed partial class DocumentBuilder
{
    private async Task Configure()
    {
        using IDisposable _ = BeginGroup("Configuring");

        Version docFxMinimumVersion = UseModernTemplate ? new Version(2, 63, 0) : new Version (2, 60, 0);
        (_docFxPath, Version docFxVersion) = await FindDocFx(docFxMinimumVersion);
        Info($"Using DocFx {docFxVersion} at {_docFxPath}");

        (_templateInfo, _templatePath) = await ConfigureTemplate();
        if (_templateInfo is not null && _templatePath is not null)
        {
            Info($"Using {(_templateInfo.Type == TemplateType.Full ? string.Empty : "partial ")} template at {_templatePath}");
        }
        else if (_templateInfo is not null)
        {
            Info($"Using {(_templateInfo.Type == TemplateType.Full ? string.Empty : "partial ")} template");
        }

        _projectConfig = await LoadConfiguration();
        Info($"Define constants: {string.Join(',', _projectConfig.DefineConstants)}");

        string[] sources = _projectConfig.Sources.Select(x => x.NormalizeSeparators()).ToArray();
        if (sources.Length < 3)
        {
            Info($"Sources: {string.Join(',', sources)}");
        }
        else
        {
            Info("Sources:");
            foreach (string source in sources)
            {
                Info($"- {source}");
            }
        }

        Info($"Exclude manual: {(_projectConfig.Excludes.Manual ? "YES" : "NO")}");
        Info($"Exclude API docs: {(_projectConfig.Excludes.ApiDocs ? "YES" : "NO")}");
        Info($"Exclude changelog: {(_projectConfig.Excludes.Changelog ? "YES" : "NO")}");
        Info($"Exclude license: {(_projectConfig.Excludes.License ? "YES" : "NO")}");
        Info($"Build directory: {_buildPath}");
        Info($"Output directory: {_outputPath}");
    }

    private async Task<(TemplateInfo?, string?)> ConfigureTemplate()
    {
        if (TemplatePath is not null)
        {
            string templatePath = Path.GetFullPath(TemplatePath);
            TemplateInfo templateInfo = await LoadTemplateInfo(templatePath);
            return (templateInfo, templatePath);
        }

        if (StyleSheetPath is null)
        {
            return (null, null);
        }

        string stylesheetPath = Path.GetFullPath(StyleSheetPath);
        if (await TryCopyStyle(stylesheetPath))
        {
            Info($"Using stylesheet at {stylesheetPath}");
            return (new TemplateInfo(), null);
        }

        string defaultStylesheetPath = Path.Combine(_packageDocumentationPath, "style.css");
        if (await TryCopyStyle(defaultStylesheetPath))
        {
            Info($"Using stylesheet at {defaultStylesheetPath}");
            return (new TemplateInfo(), null);
        }

        static async Task<TemplateInfo> LoadTemplateInfo(string templatePath)
        {
            if (!Directory.Exists(templatePath))
            {
                throw new Exception($"{templatePath} does not exists.");
            }

            string path = Path.Combine(templatePath, "template.json");
            string json = await Files.ReadText(path);
            TemplateInfo? templateInfo = JsonSerializer.Deserialize(json, SerializerContext.Default.TemplateInfo);
            return templateInfo ?? throw new Exception($"Failed to load {path}.");
        }

        return (null, null);
    }

    private async Task<(string, Version)> FindDocFx(Version minimumVersion)
    {
        string? docFoxInstallPath = DocFxPath ?? FindInPath("docfx.exe") ?? FindInPath("docfx");
        if (docFoxInstallPath == null)
        {
            throw new Exception("Could not find docfx or docfx.exe in the system path.");
        }

        return await ContinueFindDocFx(minimumVersion, docFoxInstallPath);

        static string? FindInPath(string fileName)
        {
            string? envPath = Environment.GetEnvironmentVariable("PATH");
            return envPath?.Split(Path.PathSeparator).FirstOrDefault(x => ContainsDocFxExe(x, fileName));
        }

        static bool ContainsDocFxExe(string? directory, string docFxFile)
        {
            return !string.IsNullOrEmpty(directory) && File.Exists(Path.Combine(directory, docFxFile));
        }
    }

    private async Task<(string, Version)> ContinueFindDocFx(Version minimumVersion, string docFxInstallPath)
    {
        string? docFxPath = ResolveDocFxPath(docFxInstallPath);
        if (docFxPath == null)
        {
            throw new Exception($"{docFxInstallPath} is not a valid DocFx installation.");
        }

        Version version = await GetDocFxVersion(docFxPath);
        if (version < minimumVersion)
        {
            throw new Exception($"{Program.Name} required DocFx version {minimumVersion} or greater, got {version}");
        }

        return (docFxPath, version);

        static string? ResolveDocFxPath(string docFxInstallPath)
        {
            if (Directory.Exists(docFxInstallPath))
            {
                string path = Path.Combine(docFxInstallPath, "docfx.exe");
                if (!File.Exists(path))
                {
                    path = Path.Combine(docFxInstallPath, "docfx");
                }

                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }
    }

    private async Task<PackageInfo> LoadPackageInfo()
    {
        string packageJsonPath = Path.Combine(_packagePath, "package.json");
        try
        {
            string packageJsonText = await Files.ReadText(packageJsonPath);
            PackageInfo? packageInfo = JsonSerializer.Deserialize(packageJsonText, SerializerContext.Default.PackageInfo);
            if (packageInfo?.IsValid != true)
            {
                throw new Exception("Invalid package.json.");
            }

            return packageInfo;
        }
        catch (FileNotFoundException)
        {
            throw new Exception($"File {packageJsonPath} does not exist.");
        }
    }

    private async Task<bool> TryCopyStyle(string stylesheetPath)
    {
        string dst = Path.Combine(_buildPath, TemplateFolder, "styles/main.css");
        return await Files.TryCopyFile(stylesheetPath, dst, _logger);
    }

    private async Task<DocumentationConfig> LoadConfiguration()
    {
        string path = ConfigPath is null
            ? Path.Combine(_packageDocumentationPath, "config.json")
            : Path.GetFullPath(ConfigPath);

        Info($"Loading {path}");
        DocumentationConfig? projectConfig = null;
        if (File.Exists(path))
        {
            string json = await Files.ReadText(path);
            projectConfig = JsonSerializer.Deserialize(json, SerializerContext.Default.DocumentationConfig);
        }

        return projectConfig ?? new DocumentationConfig();
    }
}

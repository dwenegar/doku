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

        GitHubActionInfo? gitHubInfo = GitHubActionHelpers.GetInfo();
        if (gitHubInfo is not null)
        {
            Info($"Running from GitHub: {gitHubInfo}");
        }

        _packageInfo = await LoadPackageInfo();
        Info($"Building documentation for package {_packageInfo.DisplayName} {_packageInfo.Version} targeting {_packageInfo.Unity}");

        _docFxPath = FindDocFx();
        Info($"Using DocFx at {_docFxPath}, version {await GetDocFxVersion()}");
        Info($"Writing files to {_outputPath}");
        Info($"The build directory is {_buildPath}");

        (_templateInfo, _templatePath) = await ConfigureTemplate();

        _projectConfig = await LoadConfiguration();
        Info("Configuration");
        Info($"  Disable default filter: {(_projectConfig.DisableDefaultFilter ? "YES" : "NO")}");
        Info($"  Enable search: {(_projectConfig.EnableSearch ? "YES" : "NO")}");
        Info("  Define constants:");
        foreach (string defineConstant in _projectConfig.DefineConstants)
        {
            Info($"  - {defineConstant}");
        }

        Info("  Sources:");
        foreach (string configSource in _projectConfig.Sources)
        {
            Info($"  - {configSource.NormalizeSeparators()}");
        }

        Info($"  Exclude manual: {(_projectConfig.Excludes.Manual ? "YES" : "NO")}");
        Info($"  Exclude API docs: {(_projectConfig.Excludes.ApiDocs ? "YES" : "NO")}");
        Info($"  Exclude changelog: {(_projectConfig.Excludes.Changelog ? "YES" : "NO")}");
        Info($"  Exclude license: {(_projectConfig.Excludes.License ? "YES" : "NO")}");
    }

    private async Task<(TemplateInfo?, string?)> ConfigureTemplate()
    {
        if (TemplatePath is not null)
        {
            string templatePath = Path.GetFullPath(TemplatePath);
            TemplateInfo templateInfo = await LoadTemplateInfo(templatePath);
            Info($"Using {(templateInfo.Type == TemplateType.Full ? string.Empty : "partial ")} template at {templatePath}");
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

    private string FindDocFx()
    {
        string? docFxPath = DocFxPath ?? FindDocFxInPath();
        if (docFxPath == null)
        {
            throw new Exception("Could not find docfx.exe in the system path.");
        }

        if (!docFxPath.EndsWith("docfx.exe", StringComparison.OrdinalIgnoreCase))
        {
            docFxPath = Path.Combine(docFxPath, "docfx.exe");
        }

        if (!File.Exists(docFxPath))
        {
            throw new Exception($"{docFxPath} is not a valid DocFx installation.");
        }

        return docFxPath;

        static string? FindDocFxInPath()
        {
            string? envPath = Environment.GetEnvironmentVariable("PATH");
            return envPath?.Split(Path.PathSeparator).FirstOrDefault(ContainsDocFxExe);
        }

        static bool ContainsDocFxExe(string? directory)
        {
            return !string.IsNullOrEmpty(directory) && File.Exists(Path.Combine(directory, "docfx.exe"));
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

    private async Task<ProjectConfig> LoadConfiguration()
    {
        string path = Path.Combine(_packageDocumentationPath, "config.json");
        ProjectConfig? projectConfig = null;
        if (File.Exists(path))
        {
            string json = await Files.ReadText(path);
            projectConfig = JsonSerializer.Deserialize(json, SerializerContext.Default.ProjectConfig);
        }

        return projectConfig ?? new ProjectConfig();
    }
}

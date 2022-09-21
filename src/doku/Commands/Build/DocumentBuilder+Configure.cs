// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Linq;
using System.Text;
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

        LoadPackageInfo();
        Info($"Building documentation for package {_packageInfo!.DisplayName} {_packageInfo.Version} targeting {_packageInfo.Unity}");

        FindDocFx();
        Info($"Using DocFx at {_docFxPath}, version {await GetDocFxVersion()}");
        Info($"Writing files to {_outputPath}");
        Info($"The build directory is {_buildPath}");

        if (TryLoadTemplateInfo(out TemplateInfo? templateInfo, out string? templatePath))
        {
            Info($"Using {(templateInfo!.Type == TemplateType.Full ? string.Empty : "partial ")} template at {templatePath!}");
            _templateInfo = templateInfo;
            _templatePath = templatePath;
        }
        else if (TryCopyStyle(out string? stylesheetPath))
        {
            Info($"Using stylesheet at {stylesheetPath!}");
            _templateInfo = new TemplateInfo
            {
                Type = TemplateType.Partial
            };
        }

        LoadConfiguration();
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

    private void FindDocFx()
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

        _docFxPath = docFxPath;

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

    private void LoadPackageInfo()
    {
        string packageJsonPath = Path.Combine(_packagePath, "package.json");
        try
        {
            string packageJsonText = Files.ReadText(packageJsonPath);
            _packageInfo = JsonSerializer.Deserialize(packageJsonText, SerializerContext.Default.PackageInfo);
            if (_packageInfo?.IsValid != true)
            {
                throw new Exception("Invalid package.json.");
            }
        }
        catch (FileNotFoundException)
        {
            throw new Exception($"File {packageJsonPath} does not exist.");
        }
    }

    private bool TryLoadTemplateInfo(out TemplateInfo? templateInfo, out string? templatePath)
    {
        templateInfo = null;
        templatePath = null;
        if (TemplatePath == null)
        {
            return false;
        }

        templatePath = Path.GetFullPath(TemplatePath);
        if (!Directory.Exists(templatePath))
        {
            throw new Exception($"{templatePath} does not exists.");
        }

        string path = Path.Combine(templatePath, "template.json");
        string json = File.ReadAllText(path, Encoding.UTF8);
        templateInfo = JsonSerializer.Deserialize(json, SerializerContext.Default.TemplateInfo);
        if (templateInfo == null)
        {
            throw new Exception($"Failed to load {path}.");
        }

        return true;
    }

    private bool TryCopyStyle(out string? stylesheetPath)
    {
        stylesheetPath = null;
        if (StyleSheetPath != null)
        {
            stylesheetPath = Path.GetFullPath(StyleSheetPath);
        }
        else if (Directory.Exists(_packageDocumentationPath))
        {
            stylesheetPath = Path.Combine(_packageDocumentationPath, "style.css");
        }

        if (stylesheetPath != null)
        {
            string dst = Path.Combine(_buildPath, TemplateFolder, "styles/main.css");
            return Files.TryCopyFile(stylesheetPath, dst, _logger);
        }

        return false;
    }

    private void LoadConfiguration()
    {
        string path = Path.Combine(_packageDocumentationPath, "config.json");
        ProjectConfig? projectConfig = null;
        if (File.Exists(path))
        {
            string json = Files.ReadText(path);
            projectConfig = JsonSerializer.Deserialize(json, SerializerContext.Default.ProjectConfig);
        }

        _projectConfig = projectConfig ?? _projectConfig;
    }
}

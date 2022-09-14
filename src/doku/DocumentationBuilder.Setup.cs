// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Doku.Resources;
using Doku.Utils;

namespace Doku;

internal sealed partial class DocumentationBuilder
{
    private void LoadPackageInfo()
    {
        Verbose("Loading package.json");

        string packageJsonPath = Path.Combine(_packagePath, "package.json");
        try
        {
            string packageJsonText = File.ReadAllText(packageJsonPath);
            _packageInfo = JsonSerializer.Deserialize(packageJsonText, SerializerContext.Default.PackageInfo);
            if (_packageInfo == null)
            {
                throw new Exception("Invalid package.json.");
            }

            Verbose($"  Package DisplayName: {_packageInfo.DisplayName}");
            Verbose($"  Package Version: {_packageInfo.Version}");
        }
        catch (FileNotFoundException)
        {
            throw new Exception($"File {packageJsonPath} does not exist.");
        }
    }

    private void LoadProjectConfig()
    {
        if (_packageManualPath == null)
        {
            return;
        }

        Verbose("Loading config.json.");

        string path = Path.Combine(_packageManualPath, "config.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            ProjectConfig? projectConfig = JsonSerializer.Deserialize(json, SerializerContext.Default.ProjectConfig);
            if (projectConfig != null)
            {
                _projectConfig = projectConfig;
                Verbose($"  ProjectConfig: {projectConfig}");
            }
        }
    }

    private bool TryCopyStyle()
    {
        string? stylesheetPath = null;
        if (StyleSheetPath != null)
        {
            stylesheetPath = Path.GetFullPath(StyleSheetPath);
        }
        else if (_packageManualPath != null)
        {
            stylesheetPath = Path.Combine(_packageManualPath, "style.css");
        }

        if (stylesheetPath != null)
        {
            Verbose("Copying custom stylesheet.");
            string dst = Path.Combine(_buildPath, TemplateFolder, "styles/main.css");
            return Files.TryCopyFile(stylesheetPath, dst);
        }

        return false;
    }

    private bool TryLoadTemplateInfo(out TemplateInfo? templateInfo)
    {
        templateInfo = null;
        if (TemplatePath == null)
        {
            return false;
        }

        Verbose("Loading template.json");

        string templatePath = Path.GetFullPath(TemplatePath);
        if (!Directory.Exists(templatePath))
        {
            throw new Exception($"{TemplatePath} does not exists.");
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

    private void LocatePackageManualFolder()
    {
        Verbose("Locating manual folder.");

        string path = Path.Combine(_packagePath, "Documentation~");
        if (Directory.Exists(path))
        {
            _packageManualPath = path;
            Verbose($"  Package Manual Path: {_packageManualPath}");
        }
    }

    private void ExtractDocFxProject()
    {
        Verbose("Initializing DocFx project.");
        Assembly assembly = typeof(DocumentationBuilder).Assembly;
        var resourceManager = new ResourceManager(assembly, "Templates", _logger);
        resourceManager.ExportResources("project", _buildPath);
    }
}

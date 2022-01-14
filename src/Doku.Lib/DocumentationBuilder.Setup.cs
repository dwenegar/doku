// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Dwenegar.Doku.Logging;
using Dwenegar.Doku.Resources;

namespace Dwenegar.Doku
{
    public sealed partial class DocumentationBuilder
    {
        private void LoadPackageInfo()
        {
            Logger.LogVerbose("Loading package.json");

            string packageJsonPath = Path.Combine(_packagePath, "package.json");
            try
            {
                string packageJsonText = File.ReadAllText(packageJsonPath);
                _packageInfo = JsonSerializer.Deserialize(packageJsonText, SerializerContext.Default.PackageInfo);
                if (_packageInfo == null)
                {
                    throw new Exception("Invalid package.json.");
                }

                Logger.LogVerbose($"  Package DisplayName: {_packageInfo.DisplayName}");
                Logger.LogVerbose($"  Package Version: {_packageInfo.Version}");
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

            Logger.LogVerbose("Loading config.json.");

            string path = Path.Combine(_packageManualPath, "config.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                ProjectConfig? projectConfig = JsonSerializer.Deserialize(json, SerializerContext.Default.ProjectConfig);
                if (projectConfig != null)
                {
                    _projectConfig = projectConfig;
                    Logger.LogVerbose($"  ProjectConfig: {projectConfig}");
                }
            }
        }

        private bool TryLoadTemplateInfo(out TemplateInfo? templateInfo)
        {
            templateInfo = null;
            if (TemplatePath == null)
            {
                return false;
            }

            Logger.LogVerbose("Loading template.json.");

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
            Logger.LogVerbose("Locating manual folder.");

            string path = Path.Combine(_packagePath, "Documentation~");
            if (Directory.Exists(path))
            {
                _packageManualPath = path;
                Logger.LogVerbose($"  Package Manual Path: {_packageManualPath}");
            }
        }

        private void ExtractDocFxProject()
        {
            using Logger.Scope scope = new("InitializeDocFxProject");

            Logger.LogVerbose("Initializing DocFx project.");
            Assembly assembly = typeof(DocumentationBuilder).Assembly;
            var resourceManager = new ResourceManager(assembly, "Templates");
            resourceManager.ExportResources("project", _buildPath);
        }
    }
}

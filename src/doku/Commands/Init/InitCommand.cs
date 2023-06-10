// Copyright Simone Livieri. All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// For terms of use, see LICENSE.txt

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Doku.Logging;
using Doku.Utils;
using McMaster.Extensions.CommandLineUtils;

namespace Doku.Commands.Init;

internal sealed class InitCommand : CommandBase
{
    [Option("--force", Description = "Overwrite any existing file.")]
    private bool Force { get; } = default;

    [DirectoryExists]
    [Argument(0, "package-path", Description = "Path to the folder containing the package.json file.")]
    private string PackagePath { get; } = Environment.CurrentDirectory;

    protected override async Task ExecuteAsync(CommandLineApplication app, Logger logger)
    {
        var initializer = new DocumentationInitializer(PackagePath, logger)
        {
            Force = Force
        };

        await initializer.Initialize();
    }
}

internal sealed class DocumentationInitializer
{
    private readonly string _packagePath;
    private readonly Logger _logger;

    public DocumentationInitializer(string packagePath, Logger logger)
    {
        _packagePath = Path.GetFullPath(packagePath);
        _logger = logger;
    }

    public bool Force { get; init; }

    public async Task Initialize()
    {
        try
        {
            PackageInfo packageInfo = await LoadPackageInfo();
            _logger.LogInfo($"Initializing documentation for package {packageInfo.DisplayName} {packageInfo.Version} targeting {packageInfo.Unity}");

            string documentationPath = Path.Combine(_packagePath, "Documentation~");
            if (Directory.Exists(documentationPath) && !Force)
            {
                _logger.LogWarning($"Directory {documentationPath} exists; use --force to overwrite it.");
            }
            else
            {
                Files.DeleteDirectory(documentationPath, _logger);
                Files.CreateDirectory(documentationPath, _logger);

                string configPath = Path.Combine(documentationPath, "config.json");
                string configText = JsonSerializer.Serialize(new DocumentationConfig(), SerializerContext.Default.DocumentationConfig);
                await Files.WriteText(configPath, configText, _logger);

                string indexPath = Path.Combine(documentationPath, "index.md");
                string indexText = $@"#{packageInfo.DisplayName} {packageInfo.Version}

This is the documentation for the package {packageInfo.DisplayName}.
";
                await Files.WriteText(indexPath, indexText, _logger);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
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
}

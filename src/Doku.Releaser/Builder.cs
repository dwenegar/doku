// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Dwenegar.Doku.Logging;
using Dwenegar.Doku.Utils;

namespace Dwenegar.Doku.Releaser
{
    internal record BuildResult(string Version, string Changelog, IEnumerable<ReleaseAssetInfo> Assets);

    internal sealed class Builder
    {
        private readonly string _rootPath;
        private readonly string _outputPath;

        private readonly string _srcPath;
        private readonly string _dokuPath;
        private readonly string _dokuReleasePath;

        public Builder(string rootPath, string outputPath)
        {
            _rootPath = rootPath;
            _outputPath = outputPath;

            _srcPath = Path.Combine(rootPath, "src");
            _dokuPath = Path.Combine(_srcPath, "doku");
            _dokuReleasePath = Path.Combine(_dokuPath, "bin/Release/net6.0");
        }

        public BuildResult Build()
        {
            using Logger.Scope scope = new("Build");

            Logger.LogVerbose($"OutputPath: {_outputPath}");
            Logger.LogVerbose($"RootPath: {_rootPath}");

            string dotnetPath = LocateDotnet();
            string version = LoadVersion();
            string changelog = LoadVersionChangeLog(version);

            DeleteFolders();
            var entries = new List<ReleaseAssetInfo>
            {
                PackPlatform(dotnetPath, "win-x64", version, ReleaseAssetKind.Zip),
                PackPlatform(dotnetPath, "linux-x64", version, ReleaseAssetKind.Deb),
                PackPlatform(dotnetPath, "linux-x64", version, ReleaseAssetKind.Rpm),
                PackPlatform(dotnetPath, "linux-x64", version, ReleaseAssetKind.TarBall),
                PackPlatform(dotnetPath, "osx-x64", version, ReleaseAssetKind.TarBall)
            };
            CopyFilesToOutputFolder(entries);
            return new BuildResult(version, changelog, entries);
        }

        private static PackageMeta GetPackageMeta(ReleaseAssetKind kind)
        {
            switch (kind)
            {
                case ReleaseAssetKind.Deb:
                    return new PackageMeta("CreateDeb", "deb", "application/vnd.debian.binary-package");
                case ReleaseAssetKind.Rpm:
                    return new PackageMeta("CreateRpm", "rpm", "application/x-rpm");
                case ReleaseAssetKind.Zip:
                    return new PackageMeta("CreateZip", "zip", "application/zip");
                case ReleaseAssetKind.TarBall:
                    return new PackageMeta("CreateTarball", "tar.gz", "application/gzip");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static string LocateDotnet()
        {
            Logger.LogVerbose("Locating dotnet.exe.");

            static bool ContainsDotnet(string? directory)
            {
                return !string.IsNullOrEmpty(directory) && File.Exists(Path.Combine(directory, "dotnet.exe"));
            }

            string? envPath = Environment.GetEnvironmentVariable("PATH");
            string? dotnetPath = envPath?.Split(Path.PathSeparator).FirstOrDefault(ContainsDotnet);
            if (dotnetPath == null)
            {
                throw new Exception("Could not find docfx.exe in the system path.");
            }

            Logger.LogVerbose($"Dotnet path: {dotnetPath}");

            var dotnet = new Dotnet(dotnetPath);
            Logger.LogVerbose($"Using dotnet version {dotnet.Version}");

            return dotnetPath;
        }

        private string LoadVersion()
        {
            Logger.LogVerbose("Loading version");

            using var scope = new Logger.Scope("LoadVersion");

            string path = Path.Combine(_srcPath, "Version.props");
            var doc = XElement.Load(path);

            string? versionPrefix = doc.Descendants("VersionPrefix").FirstOrDefault()?.Value;
            string? versionSuffix = doc.Descendants("VersionSuffix").FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(versionPrefix))
            {
                throw new Exception("Invalid Version.props");
            }

            var version = $"{versionPrefix}";
            if (!string.IsNullOrEmpty(versionSuffix))
            {
                version = $"{version}-{versionSuffix}";
            }

            return version;
        }

        private string LoadVersionChangeLog(string version)
        {
            string changelogPath = Path.Combine(_rootPath, "CHANGELOG.md");

            var versionHeader = $"## {version}";

            var sb = new StringBuilder();

            var versionFound = false;
            foreach (var line in File.ReadLines(changelogPath, Encoding.UTF8))
            {
                // copy only the lines between the current version header...
                if (line.StartsWith(versionHeader))
                {
                    versionFound = true;
                    continue;
                }

                if (versionFound)
                {
                    // ... and the previous one
                    if (line.StartsWith("## "))
                    {
                        break;
                    }

                    sb.AppendLine(line.TrimEnd());
                }
            }

            if (!versionFound)
            {
                throw new Exception($"Could not find version {version} in CHANGELOG.md");
            }

            return sb.ToString();
        }

        private void DeleteFolders()
        {
            Logger.LogVerbose("Deleting folders.");
            Files.DeleteDirectory(_outputPath);
        }

        private ReleaseAssetInfo PackPlatform(string dotnetPath, string platform, string version, ReleaseAssetKind kind)
        {
            Logger.LogVerbose($"Packing platform {platform}.");
            using Logger.Scope scope = new("PackPlatform");

            var (target, extension, mimeType) = GetPackageMeta(kind);

            var fileName = $"doku.{version}.{platform}.{extension}";
            string sourcePath = Path.Combine(_dokuReleasePath, platform, fileName);

            Logger.LogVerbose("Building file");

            var entry = new ReleaseAssetInfo(kind, sourcePath, mimeType);

            StringBuilder arguments = new("publish");
            arguments.Append($" -c Release -r {platform}")
                     .Append($" --self-contained")
                     .Append($" -t:{target}")
                     .Append(" -p:PublishTrimmed=true")
                     .Append(" -p:TrimMode=Link")
                     .Append(" -p:PublishSingleFile=true")
                     .Append(" -p:CopyOutputSymbolsToPublishDirectory=false")
                     .Append(" -p:SkipCopyingSymbolsToOutputDirectory=true");

            var dotnet = new Dotnet(dotnetPath);
            dotnet.Run(arguments.ToString(), _dokuPath);

            if (!File.Exists(entry.Path))
            {
                throw new Exception($"Cannot find the generated file: {entry.Path}");
            }

            return entry;
        }

        private void CopyFilesToOutputFolder(IEnumerable<ReleaseAssetInfo> entries)
        {
            Logger.LogVerbose("Copying files to output folder.");
            using Logger.Scope scope = new("CopyFilesToOutputFolder");

            Directory.CreateDirectory(_outputPath);

            foreach (ReleaseAssetInfo entry in entries)
            {
                string buildEntryPath = Path.Combine(_outputPath, Path.GetFileName(entry.Path));
                if (buildEntryPath != entry.Path)
                {
                    Files.TryCopyFile(entry.Path, buildEntryPath);
                }
            }
        }

        private record PackageMeta(string Target, string Extension, string MimeType);
    }
}

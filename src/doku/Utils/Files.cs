// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Dwenegar.Doku.Logging;

namespace Dwenegar.Doku.Utils
{
    internal static class Files
    {
        public static void WriteText(string path, string text)
        {
            File.WriteAllText(path, text, Encoding.UTF8);
            Logger.LogVerbose($"Wrote {path}");
        }

        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Logger.LogVerbose($"Deleted directory {path}");
            }
        }

        public static void MoveFile(string src, string dst)
        {
            if (File.Exists(src))
            {
                if (File.Exists(dst))
                {
                    File.Delete(dst);
                }

                File.Move(src, dst);
                Logger.LogVerbose($"Moved file {src} to {dst}");
            }
        }

        public static void MoveFile(string filename, string srcDir, string dstDir)
        {
            MoveFile(Path.Combine(srcDir, filename), Path.Combine(dstDir, filename));
        }

        public static bool TryCopyFile(string src, string dst)
        {
            if (File.Exists(src))
            {
                CreateDirectory(Path.GetDirectoryName(dst));
                File.Copy(src, dst, true);
                Logger.LogVerbose($"Copied file {src} to {dst}");
                return true;
            }

            return false;
        }

        public static void CopyDirectory(string? src, string dst, string filter = "*.*")
        {
            using Logger.Scope scope = new("CopyDirectory");

            if (src != null && Directory.Exists(src))
            {
                CreateDirectory(dst);
                foreach (string path in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
                {
                    CreateDirectory(path.Replace(src, dst));
                }

                string[] files = Directory.GetFiles(src, filter, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    TryCopyFile(file, file.Replace(src, dst));
                }
            }
        }

        public static void RemoveIgnoredPaths(string rootPath)
        {
            foreach (string path in Directory.GetDirectories(rootPath, ".*", SearchOption.AllDirectories))
            {
                DeleteDirectory(path);
            }

            foreach (string path in Directory.GetDirectories(rootPath, "*~", SearchOption.AllDirectories))
            {
                DeleteDirectory(path);
            }
        }

        public static void CreateDirectory(string? name)
        {
            if (name != null && !Directory.Exists(name))
            {
                Directory.CreateDirectory(name);
                Logger.LogVerbose($"Created directory {name}");
            }
        }
    }
}

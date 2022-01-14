// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System.IO;
using System.Linq;
using System.Text;
using Dwenegar.Doku.Logging;

namespace Dwenegar.Doku.Utils
{
    public static class Files
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
            => MoveFile(Path.Combine(srcDir, filename), Path.Combine(dstDir, filename));

        public static void TryCopyFile(string filename, string srcDir, string dstDir)
            => TryCopyFile(Path.Combine(srcDir, filename), Path.Combine(dstDir, filename));

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

        public static int CopyDirectory(string? src, string dst, string filter = "*.*")
        {
            var count = 0;
            if (src != null && Directory.Exists(src))
            {
                CreateDirectory(dst);
                foreach (string path in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
                {
                    CreateDirectory(path.Replace(src, dst));
                }

                count += Directory.GetFiles(src, filter, SearchOption.AllDirectories)
                                  .Count(x => TryCopyFile(x, x.Replace(src, dst)));
            }

            return count;
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

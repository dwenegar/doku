// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System.IO;
using System.Linq;
using System.Text;
using Doku.Logging;

namespace Doku.Utils;

internal static class Files
{
    public static void WriteText(string path, string text, Logger logger)
    {
        File.WriteAllText(path, text, Encoding.UTF8);
        logger.LogDebug($"Written {path}");
    }

    public static string ReadText(string path)
        => File.ReadAllText(path, Encoding.UTF8);

    public static void DeleteDirectory(string path, Logger logger)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            logger.LogDebug($"Deleted directory {path}");
        }
    }

    public static void MoveFile(string filename, string srcDir, string dstDir, Logger logger)
        => MoveFile(Path.Combine(srcDir, filename), Path.Combine(dstDir, filename), logger);

    public static void MoveFile(string src, string dst, Logger logger)
    {
        DeleteFile(dst, logger);
        if (File.Exists(src))
        {
            File.Move(src, dst);
            logger.LogDebug($"Moved {src} to {dst}");
        }
    }

    private static void DeleteFile(string path, Logger logger)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            logger.LogDebug($"Deleted file {path}");
        }
    }

    public static void TryCopyFile(string filename, string srcDir, string dstDir, Logger logger)
        => TryCopyFile(Path.Combine(srcDir, filename), Path.Combine(dstDir, filename), logger);

    public static bool TryCopyFile(string src, string dst, Logger logger)
    {
        if (File.Exists(src))
        {
            CreateDirectory(Path.GetDirectoryName(dst), logger);
            File.Copy(src, dst, true);
            logger.LogDebug($"Copied {src} to {dst}");
            return true;
        }

        return false;
    }

    public static int CopyDirectory(string src, string dst, string filter, Logger logger)
    {
        var count = 0;
        if (Directory.Exists(src))
        {
            CreateDirectory(dst, logger);
            foreach (string path in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            {
                CreateDirectory(path.Replace(src, dst), logger);
            }

            count += Directory.GetFiles(src, filter, SearchOption.AllDirectories)
                              .Count(x => TryCopyFile(x, x.Replace(src, dst), logger));
        }

        return count;
    }

    public static void RemoveIgnoredPaths(string rootPath, Logger logger)
    {
        foreach (string path in Directory.GetDirectories(rootPath, ".*", SearchOption.AllDirectories))
        {
            DeleteDirectory(path, logger);
        }

        foreach (string path in Directory.GetDirectories(rootPath, "*~", SearchOption.AllDirectories))
        {
            DeleteDirectory(path, logger);
        }
    }

    public static void CreateDirectory(string? path, Logger logger)
    {
        if (path != null && !Directory.Exists(path))
        {
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(path);
            logger.LogDebug($"Created directory {path}");
        }
    }
}

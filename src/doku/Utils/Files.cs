// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Doku.Logging;

namespace Doku.Utils;

internal static class Files
{
    public static async Task WriteText(string path, string text, Logger logger)
    {
        await File.WriteAllTextAsync(path, text, Encoding.UTF8);
        logger.LogDebug($"Written {path}");
    }

    public static async Task<string> ReadText(string path)
        => await File.ReadAllTextAsync(path, Encoding.UTF8);

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

        static void DeleteFile(string path, Logger logger)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                logger.LogDebug($"Deleted file {path}");
            }
        }
    }

    public static async Task TryCopyFile(string filename, string srcDir, string dstDir, Logger logger)
        => await TryCopyFile(Path.Combine(srcDir, filename), Path.Combine(dstDir, filename), logger);

    public static async Task<bool> TryCopyFile(string src, string dst, Logger logger)
    {
        if (File.Exists(src))
        {
            CreateDirectory(Path.GetDirectoryName(dst), logger);
            await using FileStream srcStream = File.OpenRead(src);
            await using FileStream dstStream = File.OpenWrite(dst);
            await srcStream.CopyToAsync(dstStream);
            logger.LogDebug($"Copied {src} to {dst}");
            return true;
        }

        return false;
    }

    public static async Task<int> CopyDirectory(string src, string dst, string filter, Logger logger)
    {
        Debug.Assert(Path.IsPathRooted(src));
        Debug.Assert(Path.IsPathRooted(dst));

        var count = 0;
        if (Directory.Exists(src))
        {
            CreateDirectory(dst, logger);
            foreach (string path in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            {
                CreateDirectory(path.Replace(src, dst), logger);
            }

            foreach (string x in Directory.GetFiles(src, filter, SearchOption.AllDirectories))
            {
                if (await TryCopyFile(x, x.Replace(src, dst), logger))
                {
                    count++;
                }
            }
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

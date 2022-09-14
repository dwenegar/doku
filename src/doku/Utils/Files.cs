// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System.IO;
using System.Linq;
using System.Text;

namespace Doku.Utils;

public static class Files
{
    public static void WriteText(string path, string text)
        => File.WriteAllText(path, text, Encoding.UTF8);

    public static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    public static void MoveFile(string filename, string srcDir, string dstDir)
        => MoveFile(Path.Combine(srcDir, filename), Path.Combine(dstDir, filename));

    public static void MoveFile(string src, string dst)
    {
        if (File.Exists(dst))
        {
            File.Delete(dst);
        }

        if (File.Exists(src))
        {
            File.Move(src, dst);
        }
    }

    public static void TryCopyFile(string filename, string srcDir, string dstDir)
        => TryCopyFile(Path.Combine(srcDir, filename), Path.Combine(dstDir, filename));

    public static bool TryCopyFile(string src, string dst)
    {
        if (File.Exists(src))
        {
            CreateDirectory(Path.GetDirectoryName(dst));
            File.Copy(src, dst, true);
            return true;
        }

        return false;
    }

    public static int CopyDirectory(string src, string dst, string filter = "*.*")
    {
        var count = 0;
        if (Directory.Exists(src))
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
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlyatOS;

public class PathHelpers
{
    public static string TrimPath(string full)
    {
        if (string.IsNullOrEmpty(full)) return full;
        var sep = full.TrimEnd('\\').LastIndexOf('\\');
        if (sep >= 0 && sep < full.Length - 1)
            return full.TrimEnd('\\').Substring(sep + 1);
        return full.TrimEnd('\\');
    }

    public static bool IsAbsolute(string path) => path.Contains(":\\");

    public static bool IsRoot(string path, string rootPath) => string.Equals(path, rootPath, StringComparison.Ordinal);

    public static string EnsureTrailingSlash(string path)
    {
        if (!path.EndsWith("\\"))
            return path + "\\";
        return path;
    }

    public static string PathCombine(string baseDir, string child)
    {
        // Basis immer mit Backslash
        baseDir = EnsureTrailingSlash(baseDir);
        // Path.Combine entfernt doppelte Backslashes korrekt
        var combined = System.IO.Path.Combine(baseDir, child);
        return EnsureTrailingSlash(combined);
    }

    public static string GetParent(string path, string rootPath)
    {
        path = EnsureTrailingSlash(path);
        if (IsRoot(path, rootPath)) return path;
        // entfernt letzten Segment-Backslash
        var trimmed = path.TrimEnd('\\');
        var idx = trimmed.LastIndexOf('\\');
        if (idx <= 2) // z.B. "0:\" -> Index 2
            return rootPath;
        var parent = trimmed.Substring(0, idx + 1);
        return EnsureTrailingSlash(parent);
    }
}

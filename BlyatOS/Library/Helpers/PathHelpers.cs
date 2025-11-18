using System;
using System.Drawing;
using System.IO;
using BlyatOS.Library.Helpers;

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

    public static void SearchFileRecursive(string currentPath, string fileName)
    {
        try
        {
            // Check file in current directory
            string testFile = Path.Combine(currentPath, fileName);
            if (File.Exists(testFile))
            {
                FileInfo fi = new FileInfo(testFile);
                ConsoleHelpers.WriteLine($"      FOUND: {testFile} ({fi.Length} bytes)", Color.Green);
            }
            else
            {
                ConsoleHelpers.WriteLine("      Not found in: " + currentPath, Color.Red);
            }

            // Traverse directories
            var dirs = Directory.GetDirectories(currentPath);
            foreach (var dir in dirs)
            {
                // WICHTIG: vollständigen Pfad zusammensetzen
                string fullDirPath = Path.Combine(currentPath, dir);

                SearchFileRecursive(fullDirPath, fileName);
            }
        }
        catch (Exception ex)
        {
            ConsoleHelpers.WriteLine($"  [WARN] Could not access: {currentPath} ({ex.Message})", Color.Orange);
        }
    }

}

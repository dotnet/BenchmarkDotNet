using System;
using System.Collections.Generic;
using System.IO;

namespace BenchmarkDotNet.Helpers;

#nullable enable

internal static class PathHelper
{
    public static string GetRelativePath(string relativeTo, string path)
    {
#if !NETSTANDARD2_0
        return Path.GetRelativePath(relativeTo, path);
#else
        return GetRelativePathCompat(relativeTo, path);
#endif
    }

#if NETSTANDARD2_0
    private static string GetRelativePathCompat(string relativeTo, string path)
    {
        // Get absolute full paths
        string basePath = Path.GetFullPath(relativeTo);
        string targetPath = Path.GetFullPath(path);

        // Normalize base to directory (Path.GetRelativePath treats base as directory always)
        if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            basePath += Path.DirectorySeparatorChar;

        // If roots differ, return the absolute target
        string baseRoot = Path.GetPathRoot(basePath)!;
        string targetRoot = Path.GetPathRoot(targetPath)!;
        if (!string.Equals(baseRoot, targetRoot, StringComparison.OrdinalIgnoreCase))
            return targetPath;

        // Break into segments
        var baseSegments = SplitPath(basePath);
        var targetSegments = SplitPath(targetPath);

        // Find common prefix
        int i = 0;
        while (i < baseSegments.Count && i < targetSegments.Count && string.Equals(baseSegments[i], targetSegments[i], StringComparison.OrdinalIgnoreCase))
        {
            i++;
        }

        // Build relative parts
        var relativeParts = new List<string>();

        // For each remaining segment in base -> go up one level
        for (int j = i; j < baseSegments.Count; j++)
            relativeParts.Add("..");

        // For each remaining in target -> add those segments
        for (int j = i; j < targetSegments.Count; j++)
            relativeParts.Add(targetSegments[j]);

        // If nothing added, it is the same directory
        if (relativeParts.Count == 0)
            return ".";

        // Join with separator and return
        return string.Join(Path.DirectorySeparatorChar.ToString(), relativeParts);
    }

    private static List<string> SplitPath(string path)
    {
        var segments = new List<string>();
        string[] raw = path.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

        foreach (var seg in raw)
        {
            // Skip root parts like "C:\"
            if (seg.EndsWith(":"))
                continue;
            segments.Add(seg);
        }

        return segments;
    }
#endif
}

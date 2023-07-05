using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cake.Common.Tools.DotNet;

namespace BenchmarkDotNet.Build.Helpers;

public static class Utils
{
    public static string GetOs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macos";
        return "unknown";
    }

    public static DotNetVerbosity? ParseVerbosity(string verbosity)
    {
        var lookup = new Dictionary<string, DotNetVerbosity>(StringComparer.OrdinalIgnoreCase)
        {
            { "q", DotNetVerbosity.Quiet },
            { "quiet", DotNetVerbosity.Quiet },
            { "m", DotNetVerbosity.Minimal },
            { "minimal", DotNetVerbosity.Minimal },
            { "n", DotNetVerbosity.Normal },
            { "normal", DotNetVerbosity.Normal },
            { "d", DotNetVerbosity.Detailed },
            { "detailed", DotNetVerbosity.Detailed },
            { "diag", DotNetVerbosity.Diagnostic },
            { "diagnostic", DotNetVerbosity.Diagnostic }
        };
        return lookup.TryGetValue(verbosity, out var value) ? value : null;
    }
}
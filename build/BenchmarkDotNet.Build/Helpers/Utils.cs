using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Build.Options;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Octokit;

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

    public static GitHubClient CreateGitHubClient()
    {
        EnvVar.GitHubToken.AssertHasValue();

        var client = new GitHubClient(new ProductHeaderValue("BenchmarkDotNet"));
        var tokenAuth = new Credentials(EnvVar.GitHubToken.GetValue());
        client.Credentials = tokenAuth;
        return client;
    }
}
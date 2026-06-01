using Cake.Common;
using Cake.Common.Tools.DotNet;
using Octokit;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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

    public static string ApplyRegex(string content, string pattern, string newValue)
    {
        var regex = new Regex(pattern);
        var match = regex.Match(content);
        if (!match.Success)
            throw new Exception("Failed to apply regex");

        var oldValue = match.Groups[1].Value;
        return content.Replace(oldValue, newValue);
    }

    public static string[] GetTargetFrameworks(BuildContext context)
    {
        var jobName = context.Environment.GetEnvironmentVariable("GITHUB_JOB");

        switch (jobName)
        {
            case "test-windows-core":
                return ["net8.0"];

            case "test-windows-full":
                return ["net472"];

            case "test-linux":
            case "test-macos":
                return ["net8.0"];

            default:
                return context.IsRunningOnWindows()
                    ? ["net472", "net8.0"]
                    : ["net8.0"];
        }
    }
}
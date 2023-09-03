using BenchmarkDotNet.Toolchains.Results;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    internal static class MsBuildErrorMapper
    {
        private static readonly (Regex regex, Func<Match, string> translation)[] Rules = new (Regex rule, Func<Match, string> translation)[]
        {
            (
                new Regex("warning NU1702: ProjectReference '(.*)' was resolved using '(.*)' instead of the project target framework '(.*)'. This project may not be fully compatible with your project.",
                    RegexOptions.CultureInvariant | RegexOptions.Compiled),
                match => $@"The project which defines benchmarks does not target '{Map(match.Groups[3])}'." + Environment.NewLine +
                    $"You need to add '{Map(match.Groups[3])}' to <TargetFrameworks> in your project file ('{match.Groups[1]}')." + Environment.NewLine +
                    $"Example: <TargetFrameworks>{Map(match.Groups[2])};{Map(match.Groups[3])}</TargetFrameworks>"
            ),
            (
                new Regex("error NU1201: Project (.*) is not compatible with (.*) ((.*)) / (.*). Project (.*) supports: (.*) ((.*))",
                    RegexOptions.CultureInvariant | RegexOptions.Compiled),
                match => $@"The project which defines benchmarks does not target '{Map(match.Groups[2])}'." + Environment.NewLine +
                    $"You need to add '{Map(match.Groups[2])}' to <TargetFrameworks> in your project file ('{match.Groups[1]}')." + Environment.NewLine +
                    $"Example: <TargetFrameworks>{Map(match.Groups[7])};{Map(match.Groups[2])}</TargetFrameworks>"
            ),
            (
                new Regex("error NETSDK1045: The current .NET SDK does not support targeting (.*).  Either target (.*) or lower, or use a version of the .NET SDK that supports (.*).",
                    RegexOptions.CultureInvariant | RegexOptions.Compiled),
                match => $"The current .NET SDK does not support targeting {match.Groups[1]}. You need to install it or pass the path to dotnet cli via the `--cli` console line argument."
            ),
        };

        internal static bool TryToExplainFailureReason(BuildResult buildResult, out string? reason)
        {
            reason = null;

            if (buildResult.IsBuildSuccess || string.IsNullOrEmpty(buildResult.ErrorMessage))
            {
                return false;
            }

            foreach (var errorLine in buildResult.ErrorMessage.Split('\r', '\n').Where(line => !string.IsNullOrEmpty(line)))
            foreach (var rule in Rules)
            {
                var match = rule.regex.Match(errorLine);
                if (match.Success)
                {
                    reason = rule.translation(match);
                    return true;
                }
            }

            return false;
        }

        private static string Map(Capture capture)
        {
            switch (capture.Value)
            {
                case ".NETFramework,Version=v4.6.1":
                    return "net461";
                case ".NETFramework,Version=v4.6.2":
                    return "net462";
                case ".NETFramework,Version=v4.7":
                    return "net47";
                case ".NETFramework,Version=v4.7.1":
                    return "net471";
                case ".NETFramework,Version=v4.7.2":
                    return "net472";
                case ".NETFramework,Version=v4.8":
                    return "net48";
                case ".NETCoreApp,Version=v2.0":
                    return "netcoreapp2.0";
                case ".NETCoreApp,Version=v2.1":
                    return "netcoreapp2.1";
                case ".NETCoreApp,Version=v2.2":
                    return "netcoreapp2.2";
                case ".NETCoreApp,Version=v3.0":
                    return "netcoreapp3.0";
                case ".NETCoreApp,Version=v3.1":
                    return "netcoreapp3.1";
                case ".NETCoreApp,Version=v5.0":
                    return "net5.0";
                case ".NETCoreApp,Version=v6.0":
                    return "net6.0";
                default:
                    return capture.Value; // we don't want to throw for future versions of .NET
            }
        }
    }
}

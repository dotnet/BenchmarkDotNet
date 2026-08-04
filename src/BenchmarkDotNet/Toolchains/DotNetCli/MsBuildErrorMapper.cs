using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Toolchains.Results;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    internal static class MsBuildErrorMapper
    {
        private static readonly (Regex regex, Func<Match, string> translation)[] Rules =
        [
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
        ];

        // CS0234 (doesn't exist in namespace) / CS0246 (could not be found) / CS0400 (could not be found in the global namespace).
        private static readonly Regex MissingTypeRegex = new(
            @"error CS(?:0234|0246|0400): The type or namespace name '(?<name>[^']+)'(?: does not exist in the namespace '(?<namespace>[^']+)'| could not be found)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        internal static bool TryToExplainFailureReason(BuildResult buildResult, IReadOnlyList<Type> inProcessDiagnoserHandlerTypes, [NotNullWhen(true)] out string? reason)
        {
            reason = null;

            if (buildResult.IsBuildSuccess || buildResult.ErrorMessage.IsBlank())
            {
                return false;
            }

            var errorLines = buildResult.ErrorMessage.Split('\r', '\n').Where(line => line.IsNotBlank()).ToArray();

            // The generated benchmark references the project that defines the benchmarks; anything else it is compiled
            // against (BenchmarkDotNet itself, in-process diagnoser handlers) must be reachable from that project. When
            // it isn't, the build fails with a cryptic missing-type error - translate it into an actionable one. See #3218.
            if (TryToExplainMissingReference(errorLines, inProcessDiagnoserHandlerTypes, out reason))
            {
                return true;
            }

            foreach (var errorLine in errorLines)
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

        private static bool TryToExplainMissingReference(string[] errorLines, IReadOnlyList<Type> handlerTypes, [NotNullWhen(true)] out string? reason)
        {
            reason = null;

            foreach (var errorLine in errorLines)
            {
                var match = MissingTypeRegex.Match(errorLine);
                if (!match.Success)
                {
                    continue;
                }

                // The compiler reports the first unresolved segment of the fully-qualified name, e.g.
                // "'SharedDiagnosers' does not exist in the namespace 'BenchmarkDotNet.IntegrationTests'".
                string missingQualifiedName = match.Groups["namespace"].Success
                    ? $"{match.Groups["namespace"].Value}.{match.Groups["name"].Value}"
                    : match.Groups["name"].Value;

                // The compiler reports generic arity for open/constructed generics (e.g. 'ValueTask<>'); strip it so the
                // name matches the (non-generic) full names collected above.
                int genericMarker = missingQualifiedName.IndexOf('<');
                if (genericMarker >= 0)
                {
                    missingQualifiedName = missingQualifiedName.Substring(0, genericMarker);
                }

                // 1. An in-process diagnoser handler that lives in an assembly the benchmark project doesn't reference.
                foreach (var handlerType in handlerTypes)
                {
                    string? handlerFullName = handlerType.FullName?.Replace('+', '.');
                    if (handlerFullName is not null
                        && (handlerFullName == missingQualifiedName || handlerFullName.StartsWith(missingQualifiedName + ".", StringComparison.Ordinal)))
                    {
                        string assemblyName = handlerType.Assembly.GetName().Name!;
                        reason = $"The in-process diagnoser handler '{handlerType.Name}' (from assembly '{assemblyName}') could not be found while building the benchmark." + Environment.NewLine +
                            "In-process diagnoser handlers are compiled into the benchmark, so the project that defines your benchmarks must reference the assembly that contains the handler." + Environment.NewLine +
                            $"Add a reference to '{assemblyName}' in your benchmark project.";
                        return true;
                    }
                }

                // 2. A type from BenchmarkDotNet or one of its dependencies (e.g. Perfolizer) - the benchmark project
                //    must reference BenchmarkDotNet itself (a transitive reference that doesn't flow, e.g. one with
                //    PrivateAssets, is not enough).
                if (BenchmarkDotNetEcosystemNames.Value.Contains(missingQualifiedName))
                {
                    reason = $"A BenchmarkDotNet (or BenchmarkDotNet dependency) type could not be found while building the benchmark: '{missingQualifiedName}'." + Environment.NewLine +
                        "The project that defines your benchmarks must reference BenchmarkDotNet.";
                    return true;
                }
            }

            return false;
        }

        // Full names and (ancestor) namespaces of every public type in the BenchmarkDotNet libraries the generated
        // benchmark is compiled against (see BenchmarkDotNetReferences - shared with the Roslyn toolchain so they stay in
        // sync). Used to recognize when a missing type reported by the compiler means the benchmark project didn't
        // reference BenchmarkDotNet. Built once, only used on failure.
        private static readonly Lazy<HashSet<string>> BenchmarkDotNetEcosystemNames = new(() =>
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assembly in BenchmarkDotNetReferences.Assemblies)
                foreach (var type in assembly.GetExportedTypes())
                {
                    if (type.FullName is not null)
                    {
                        names.Add(type.FullName.Replace('+', '.'));
                    }

                    for (string? ns = type.Namespace; !string.IsNullOrEmpty(ns);)
                    {
                        names.Add(ns!);
                        int lastDot = ns!.LastIndexOf('.');
                        ns = lastDot < 0 ? null : ns.Substring(0, lastDot);
                    }
                }

            // Types that come from a package on .NET Framework (ValueTask). Add them by full name only - NOT their namespace,
            // which is a framework namespace we must not treat as part of the BenchmarkDotNet closure.
            foreach (var type in BenchmarkDotNetReferences.Types)
            {
                string? fullName = type.FullName?.Replace('+', '.');
                if (fullName is null)
                {
                    continue;
                }

                int backtick = fullName.IndexOf('`');
                names.Add(backtick < 0 ? fullName : fullName.Substring(0, backtick));
            }

            return names;
        });

        // e.g. ".NETFramework,Version=v4.7.2" or ".NETCoreApp,Version=v8.0"
        private static readonly Regex FrameworkMonikerRegex = new(
            @"^\.NET(?<framework>Framework|CoreApp),Version=v(?<version>\d+(?:\.\d+)+)$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // Parses a long framework name into its short moniker (net472 / net8.0). The version is parsed rather than mapped
        // via a lookup so future .NET versions are handled automatically. Anything that isn't a long framework name
        // (an already-short moniker, an unexpected format) is returned unchanged - we don't want to throw.
        private static string Map(Capture capture)
        {
            var match = FrameworkMonikerRegex.Match(capture.Value);
            if (!match.Success)
            {
                return capture.Value;
            }

            string version = match.Groups["version"].Value;
            if (match.Groups["framework"].Value == "Framework")
            {
                // .NET Framework: v4.7.2 -> net472, v4.8 -> net48, v4.8.1 -> net481
                return "net" + version.Replace(".", "");
            }

            // .NETCoreApp: net5.0 and later use the "netX.Y" moniker, earlier versions use "netcoreappX.Y".
            int major = int.Parse(version.Substring(0, version.IndexOf('.')), CultureInfo.InvariantCulture);
            return major >= 5 ? "net" + version : "netcoreapp" + version;
        }
    }
}

using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BenchmarkDotNet.TestAdapter
{
    /// <summary>
    /// A class used for enumerating all the benchmarks in an assembly.
    /// </summary>
    internal static class BenchmarkEnumerator
    {
        /// <summary>
        /// Returns all the BenchmarkRunInfo objects from a given assembly.
        /// </summary>
        /// <param name="assemblyPath">The dll or exe of the benchmark project.</param>
        /// <returns>The benchmarks inside the assembly.</returns>
        public static BenchmarkRunInfo[] GetBenchmarksFromAssemblyPath(string assemblyPath)
        {
#if NET462_OR_GREATER
            // Temporary workaround for BenchmarkDotNet assembly loading issue that occurred under the following conditions:
            //   1. Run BenchmarkDotNet.Samples project with following command.
            //     > dotnet test -c Release --list-tests --framework net462 -tl:off
            //   2. When using `BenchmarkDotNet.TestAdapter` package and targeting .NET Framework.
            string[] assemblyNames =
            [
                "BenchmarkDotNet",
                "System.Collections.Immutable",
                "System.Memory",
                "System.Threading.Tasks.Extensions",
            ];

            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
            {
                var targetAssemblyName = assemblyNames.FirstOrDefault(assemblyName => eventArgs.Name.StartsWith($"{assemblyName}, Version="));
                if (targetAssemblyName != null)
                {
                    var baseDir = Path.GetDirectoryName(assemblyPath);
                    var path = Path.Combine(baseDir, $"{targetAssemblyName}.dll");
                    if (File.Exists(path))
                        return Assembly.LoadFrom(path);
                }

                // Fallback to default assembly resolver
                return null;
            };
#endif

            var assembly = Assembly.LoadFrom(assemblyPath);

            var isDebugAssembly = assembly.IsJitOptimizationDisabled() ?? false;

            return GenericBenchmarksBuilder.GetRunnableBenchmarks(assembly.GetRunnableBenchmarks())
                .Select(type =>
                {
                    var benchmarkRunInfo = BenchmarkConverter.TypeToBenchmarks(type);
                    if (isDebugAssembly)
                    {
                        // If the assembly is a debug assembly, then only display them if they will run in-process
                        // This will allow people to debug their benchmarks using VSTest if they wish.
                        benchmarkRunInfo = new BenchmarkRunInfo(
                            benchmarkRunInfo.BenchmarksCases.Where(c => c.GetToolchain().IsInProcess).ToArray(),
                            benchmarkRunInfo.Type,
                            benchmarkRunInfo.Config,
                            benchmarkRunInfo.CompositeInProcessDiagnoser);
                    }

                    return benchmarkRunInfo;
                })
                .Where(runInfo => runInfo.BenchmarksCases.Length > 0)
                .ToArray();
        }
    }
}

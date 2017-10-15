using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Running
{
    public static class BenchmarkRunner
    {
        public static Summary Run<T>(IConfig config = null) =>
            BenchmarkRunnerCore.Run(BenchmarkConverter.TypeToBenchmarks(typeof(T), config), ToolchainExtensions.GetToolchain);

        public static Summary Run(Type type, IConfig config = null) =>
            BenchmarkRunnerCore.Run(BenchmarkConverter.TypeToBenchmarks(type, config), ToolchainExtensions.GetToolchain);

        public static Summary Run(Type type, MethodInfo[] methods, IConfig config = null) =>
            BenchmarkRunnerCore.Run(BenchmarkConverter.MethodsToBenchmarks(type, methods, config), ToolchainExtensions.GetToolchain);

        public static Summary Run(Benchmark[] benchmarks, IConfig config)
        {
            var targetType = benchmarks?.FirstOrDefault()?.Target.Type;
            return BenchmarkRunnerCore.Run(
                new BenchmarkRunInfo(benchmarks, targetType, BenchmarkConverter.GetFullConfig(targetType, config)),
                ToolchainExtensions.GetToolchain);
        }

        public static Summary Run(BenchmarkRunInfo benchmarks) => BenchmarkRunnerCore.Run(benchmarks, ToolchainExtensions.GetToolchain);

        public static Summary Run(BenchmarkRunInfo[] benchmarks) => BenchmarkRunnerCore.Run(benchmarks, ToolchainExtensions.GetToolchain);

        public static Summary RunUrl(string url, IConfig config = null)
        {
#if CLASSIC
            return BenchmarkRunnerCore.Run(BenchmarkConverter.UrlToBenchmarks(url, config), ToolchainExtensions.GetToolchain);
#else
            throw new NotSupportedException();
#endif
        }

        public static Summary RunSource(string source, IConfig config = null)
        {
#if CLASSIC
            return BenchmarkRunnerCore.Run(BenchmarkConverter.SourceToBenchmarks(source, config), ToolchainExtensions.GetToolchain);
#else
            throw new NotSupportedException();
#endif
        }
    }
}
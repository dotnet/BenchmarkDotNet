using System;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Running
{
    public static class BenchmarkRunner
    {
#if !UAP
        public static Summary Run<T>(IConfig config = null) =>
            BenchmarkRunnerCore.Run(BenchmarkConverter.TypeToBenchmarks(typeof(T), config), config, ToolchainExtensions.GetToolchain);

        public static Summary Run(Type type, IConfig config = null) =>
            BenchmarkRunnerCore.Run(BenchmarkConverter.TypeToBenchmarks(type, config), config, ToolchainExtensions.GetToolchain);

        public static Summary Run(Type type, MethodInfo[] methods, IConfig config = null) =>
            BenchmarkRunnerCore.Run(BenchmarkConverter.MethodsToBenchmarks(type, methods, config), config, ToolchainExtensions.GetToolchain);

        public static Summary Run(Benchmark[] benchmarks, IConfig config) =>
            BenchmarkRunnerCore.Run(benchmarks, config, ToolchainExtensions.GetToolchain);
#endif

        public static Summary RunUrl(string url, IConfig config = null)
        {
#if CLASSIC
            return BenchmarkRunnerCore.Run(BenchmarkConverter.UrlToBenchmarks(url, config), config, ToolchainExtensions.GetToolchain);
#else
            throw new NotSupportedException();
#endif
        }

        public static Summary RunSource(string source, IConfig config = null)
        {
#if CLASSIC
            return BenchmarkRunnerCore.Run(BenchmarkConverter.SourceToBenchmarks(source,  config), config, ToolchainExtensions.GetToolchain);
#else
            throw new NotSupportedException();
#endif
        }
    }
}
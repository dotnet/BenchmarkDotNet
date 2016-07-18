using System;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;

namespace BenchmarkDotNet.Running
{
    public static class BenchmarkRunner
    {
        public static Summary Run<T>(IConfig config = null) =>
            BenchmarkRunnerCore.Run(BenchmarkConverter.TypeToBenchmarks(typeof(T), config), config, ToolchainExtensions.GetToolchain);

        public static Summary Run(Type type, IConfig config = null) =>
            BenchmarkRunnerCore.Run(BenchmarkConverter.TypeToBenchmarks(type, config), config, ToolchainExtensions.GetToolchain);

        public static Summary Run(Type type, MethodInfo[] methods, IConfig config = null) =>
            BenchmarkRunnerCore.Run(BenchmarkConverter.MethodsToBenchmarks(type, methods, config), config, ToolchainExtensions.GetToolchain);

        public static Summary Run(Benchmark[] benchmarks, IConfig config) => 
            BenchmarkRunnerCore.Run(benchmarks, config, ToolchainExtensions.GetToolchain);

#if CLASSIC
        public static Summary RunUrl(string url, IConfig config = null) =>
            BenchmarkRunnerCore.Run(BenchmarkConverter.UrlToBenchmarks(url, config), config, ToolchainExtensions.GetToolchain);

        public static Summary RunSource(string source, IConfig config = null) =>
            BenchmarkRunnerCore.Run(BenchmarkConverter.SourceToBenchmarks(source, config), config, ToolchainExtensions.GetToolchain);
#endif
    }
}
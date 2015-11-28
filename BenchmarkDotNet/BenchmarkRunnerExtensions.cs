using System;
using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet
{
    public static class BenchmarkRunnerExtensions
    {
        public static IEnumerable<BenchmarkReport> Run<T>(this BenchmarkRunner runner)
        {
            return runner.Run(BenchmarkConverter.TypeToBenchmarks(typeof(T)).ToSortedList());
        }

        public static IEnumerable<BenchmarkReport> Run(this BenchmarkRunner runner, Type type)
        {
            return runner.Run(BenchmarkConverter.TypeToBenchmarks(type).ToSortedList());
        }

        public static IEnumerable<BenchmarkReport> RunUrl(this BenchmarkRunner runner, string url)
        {
            return runner.Run(BenchmarkConverter.UrlToBenchmarks(url).ToSortedList());
        }

        public static IEnumerable<BenchmarkReport> RunSource(this BenchmarkRunner runner, string source)
        {
            return runner.Run(BenchmarkConverter.SourceToBenchmarks(source).ToSortedList());
        }

        public static BenchmarkRunner AddLoggers(this BenchmarkRunner runner, params IBenchmarkLogger[] loggers)
        {
            runner.Plugins.AddLoggers(loggers);
            return runner;
        }

        public static BenchmarkRunner AddExporters(this BenchmarkRunner runner, params IBenchmarkExporter[] exporters)
        {
            runner.Plugins.AddExporters(exporters);
            return runner;
        }
    }
}
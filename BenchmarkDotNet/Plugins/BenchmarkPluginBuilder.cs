using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Plugins.Analyzers;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.Toolchains;

namespace BenchmarkDotNet.Plugins
{
    public class BenchmarkPluginBuilder : IBenchmarkPluginBuilder
    {
        private readonly List<IBenchmarkLogger> loggers = new List<IBenchmarkLogger>();
        private readonly List<IBenchmarkExporter> exporters = new List<IBenchmarkExporter>();
        private readonly List<IBenchmarkDiagnoser> diagnosers = new List<IBenchmarkDiagnoser>();
        private readonly List<IBenchmarkToolchainBuilder> toolchains = new List<IBenchmarkToolchainBuilder>();
        private readonly List<IBenchmarkAnalyser> analysers = new List<IBenchmarkAnalyser>();

        private BenchmarkPluginBuilder()
        {
        }

        public IBenchmarkPluginBuilder AddLogger(IBenchmarkLogger logger)
        {
            loggers.Add(logger);
            return this;
        }

        public IBenchmarkPluginBuilder AddExporter(IBenchmarkExporter exporter)
        {
            exporters.Add(exporter);
            return this;
        }

        public IBenchmarkPluginBuilder AddDiagnoser(IBenchmarkDiagnoser diagnoser)
        {
            diagnosers.Add(diagnoser);
            return this;
        }

        public IBenchmarkPluginBuilder AddToolchain(IBenchmarkToolchainBuilder toolchain)
        {
            toolchains.Add(toolchain);
            return this;
        }

        public IBenchmarkPluginBuilder AddAnalyser(IBenchmarkAnalyser analyser)
        {
            analysers.Add(analyser);
            return this;
        }

        public IBenchmarkLogger CompositeLogger => new BenchmarkCompositeLogger(loggers.ToArray());
        public IBenchmarkExporter CompositeExporter => new BenchmarkCompositeExporter(exporters.ToArray());
        public IBenchmarkDiagnoser CompositeDiagnoser => new BenchmarkCompositeDiagnoser(diagnosers.ToArray());
        public IBenchmarkAnalyser CompositeAnalyser => new BenchmarkCompositeAnalyser(analysers.ToArray());

        public IBenchmarkToolchainFacade CreateToolchain(Benchmark benchmark, IBenchmarkLogger logger)
        {
            var toolchain = benchmark.Task.Configuration.Toolchain;
            var targetToolchainBuilder = toolchains.FirstOrDefault(t => t.TargetToolchain == toolchain);
            if (targetToolchainBuilder != null)
                return targetToolchainBuilder.Build(benchmark, logger);
            throw new NotSupportedException($"There are no toolchain implementations for the '{toolchain}' toolchain");
        }

        public IBenchmarkPlugins Build() => this;

        public static IBenchmarkPluginBuilder CreateEmpty()
        {
            return new BenchmarkPluginBuilder();
        }

        public static IBenchmarkPluginBuilder CreateDefault()
        {
            return BuildFromArgs(new string[0]);
        }

        public static IBenchmarkPluginBuilder BuildFromArgs(string[] args)
        {
            var requestedDiagnosers = Parse(args, "d");
            var requestedLoggers = Parse(args, "l");
            var requestedExprters = Parse(args, "e");

            return new BenchmarkPluginBuilder().
                AddDiagnosers(GetMatched(BenchmarkDefaultPlugins.Diagnosers, requestedDiagnosers, false)).
                AddLoggers(GetMatched(BenchmarkDefaultPlugins.Loggers, requestedLoggers, true)).
                AddExporters(GetMatched(BenchmarkDefaultPlugins.Exporters, requestedExprters, true)).
                AddToolchains(BenchmarkDefaultPlugins.Toolchains).
                AddAnalysers(BenchmarkDefaultPlugins.Analysers);
        }

        private static T[] GetMatched<T>(T[] items, string[] requestedNames, bool takeByDefault) where T : IPlugin
        {
            if (requestedNames.Length == 0 && takeByDefault)
                return items;
            return items.Where(item => requestedNames.Contains(item.Name)).ToArray();
        }

        private static string[] Parse(string[] args, string keyword)
        {
            var prefix1 = $"-{keyword}=";
            var prefix2 = $"-{keyword}:";
            var arg = args.FirstOrDefault(a => a.StartsWith(prefix1) || a.StartsWith(prefix2));
            if (arg == null)
                return new string[0];
            var content = arg.Substring(prefix1.Length);
            return content.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
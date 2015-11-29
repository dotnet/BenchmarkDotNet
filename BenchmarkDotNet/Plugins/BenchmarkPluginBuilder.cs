using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;

namespace BenchmarkDotNet.Plugins
{
    public class BenchmarkPluginBuilder : IBenchmarkPluginBuilder
    {
        private readonly List<IBenchmarkLogger> loggers = new List<IBenchmarkLogger>();
        private readonly List<IBenchmarkExporter> exporters = new List<IBenchmarkExporter>();
        private readonly List<IBenchmarkDiagnoser> diagnosers = new List<IBenchmarkDiagnoser>();

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

        public IBenchmarkLogger CompositeLogger => new BenchmarkCompositeLogger(loggers.ToArray());
        public IBenchmarkExporter CompositeExporter => new BenchmarkCompositeExporter(exporters.ToArray());
        public IBenchmarkDiagnoser CompositeDiagnoser => new BenchmarkCompositeDiagnoser(diagnosers.ToArray());

        public IBenchmarkPlugins Build() => this;

        public static IBenchmarkPluginBuilder BuildFromArgs(string[] args)
        {
            var requestedDiagnosers = Parse(args, "d");
            var requestedLoggers = Parse(args, "l");
            var requestedExprters = Parse(args, "e");

            return new BenchmarkPluginBuilder().
                AddDiagnosers(GetMathced(BenchmarkDefaultPlugins.Diagnosers, requestedDiagnosers, false)).
                AddLoggers(GetMathced(BenchmarkDefaultPlugins.Loggers, requestedLoggers, true)).
                AddExporters(GetMathced(BenchmarkDefaultPlugins.Exporters, requestedExprters, true));
        }

        private static T[] GetMathced<T>(T[] items, string[] requestedNames, bool takeByDefault) where T : IPlugin
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
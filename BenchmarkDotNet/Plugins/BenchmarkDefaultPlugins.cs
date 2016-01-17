using System;
using System.IO;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Plugins.Analyzers;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Plugins.ResultExtenders;
using BenchmarkDotNet.Plugins.Toolchains;
using BenchmarkDotNet.Plugins.Toolchains.Classic;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Plugins
{
    public static class BenchmarkDefaultPlugins
    {
        public static readonly IBenchmarkLogger[] Loggers = { BenchmarkConsoleLogger.Default };
        public static readonly IBenchmarkExporter[] Exporters =
        {
            BenchmarkCsvExporter.Default,
            BenchmarkMarkdownExporter.StackOverflow,
            BenchmarkMarkdownExporter.Default,
            BenchmarkMarkdownExporter.GitHub,
            BenchmarkPlainExporter.Default,
            BenchmarkCsvRunsExporter.Default,
            BenchmarkRPlotExporter.Default
        };
        // Make the Diagnosers lazy-loaded, so they are only instantiated if needed
        public static readonly Lazy<IBenchmarkDiagnoser[]> Diagnosers =
            new Lazy<IBenchmarkDiagnoser[]>(LoadDiagnoser, LazyThreadSafetyMode.ExecutionAndPublication);
        public static readonly IBenchmarkToolchainBuilder[] Toolchains = CreateToolchainBuilders();
        public static readonly IBenchmarkAnalyser[] Analysers = { BenchmarkEnvironmentAnalyser.Default };

        public static readonly IBenchmarkResultExtender[] ResultExtenders =
        {
            BenchmarkStatResultExtender.AvrTime,
            BenchmarkStatResultExtender.Error
        };

        private static IBenchmarkDiagnoser[] LoadDiagnoser()
        {
            var diagnosticAssembly = "BenchmarkDotNet.Diagnostics.dll";
            try
            {
                var loadedAssembly = Assembly.LoadFrom(diagnosticAssembly);
                var thisAssembly = Assembly.GetAssembly(typeof(BenchmarkPluginBuilder));
                if (loadedAssembly.GetName().Version != thisAssembly.GetName().Version)
                {
                    var errorMsg =
                        $"Unable to load: {diagnosticAssembly} version {loadedAssembly.GetName().Version}" +
                        Environment.NewLine +
                        $"Does not match: {Path.GetFileName(thisAssembly.Location)} version {thisAssembly.GetName().Version}";
                    BenchmarkConsoleLogger.Default.WriteLineError(errorMsg);
                }
                else
                {
                    var runtimeDiagnoserType = loadedAssembly.GetType("BenchmarkDotNet.Diagnostics.BenchmarkRuntimeDiagnoser");
                    var runtimeDiagnoser = (IBenchmarkDiagnoser)Activator.CreateInstance(runtimeDiagnoserType);
                    var sourceDiagnoserType = loadedAssembly.GetType("BenchmarkDotNet.Diagnostics.BenchmarkSourceDiagnoser");
                    var sourceDiagnoser = (IBenchmarkDiagnoser)Activator.CreateInstance(sourceDiagnoserType);
                    return new[] { runtimeDiagnoser, sourceDiagnoser };
                }
            }
            catch (Exception ex) // we're loading a plug-in, better to be safe rather than sorry
            {
                BenchmarkConsoleLogger.Default.WriteLineError($"Error loading {diagnosticAssembly}: {ex.GetType().Name} - {ex.Message}");
            }
            return new IBenchmarkDiagnoser[0];
        }

        private static IBenchmarkToolchainBuilder[] CreateToolchainBuilders()
        {
            return new IBenchmarkToolchainBuilder[]
            {
                new BenchmarkToolchainBuilder(
                    BenchmarkToolchain.Classic,
                    (benchmark, logger) => new BenchmarkClassicGenerator(logger),
                    (benchmark, logger) => new BenchmarkClassicBuilder(logger),
                    (benchmark, logger) => new BenchmarkClassicExecutor(benchmark, logger)),
            };
        }
    }
}
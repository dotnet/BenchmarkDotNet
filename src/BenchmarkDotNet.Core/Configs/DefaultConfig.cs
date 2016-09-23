using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Configs
{
    public class DefaultConfig : IConfig
    {
        public static readonly IConfig Instance = new DefaultConfig();

        private DefaultConfig()
        {
        }

        public IEnumerable<IColumnProvider> GetColumnProviders() => DefaultColumnProviders.Instance;

        public IEnumerable<IExporter> GetExporters()
        {
            // Now that we can specify exporters on the cmd line (e.g. "exporters=html,stackoverflow"), 
            // we should have less enabled by default and then users can turn on the ones they want
            yield return CsvExporter.Default;
            yield return MarkdownExporter.GitHub;
            yield return HtmlExporter.Default;
        }

        public IEnumerable<ILogger> GetLoggers()
        {
            yield return ConsoleLogger.Default;
        }

        public IEnumerable<IAnalyser> GetAnalysers()
        {
            yield return EnvironmentAnalyser.Default;
        }

        public IEnumerable<IValidator> GetValidators()
        {
            yield return BaselineValidator.FailOnError;
            yield return JitOptimizationsValidator.DontFailOnError;
            yield return UnrollFactorValidator.Default;
        }

        public IEnumerable<Job> GetJobs() => Enumerable.Empty<Job>();

        public IOrderProvider GetOrderProvider() => null;

        public ConfigUnionRule UnionRule => ConfigUnionRule.Union;

        public bool KeepBenchmarkFiles => false;

        public IEnumerable<IDiagnoser> GetDiagnosers() => Enumerable.Empty<IDiagnoser>();

        // Make the Diagnosers lazy-loaded, so they are only instantiated if neededs
        public static readonly Lazy<IDiagnoser[]> LazyLoadedDiagnosers =
            new Lazy<IDiagnoser[]>(LoadDiagnosers, LazyThreadSafetyMode.ExecutionAndPublication);

        private static IDiagnoser[] LoadDiagnosers()
        {
#if !CORE
            var diagnosticAssembly = "BenchmarkDotNet.Diagnostics.Windows.dll";
            try
            {
                var loadedAssembly = Assembly.LoadFrom(diagnosticAssembly);
                var thisAssembly = typeof(DefaultConfig).GetTypeInfo().Assembly;
                if (loadedAssembly.GetName().Version != thisAssembly.GetName().Version)
                {
                    var errorMsg =
                        $"Unable to load: {diagnosticAssembly} version {loadedAssembly.GetName().Version}" +
                        System.Environment.NewLine +
                        $"Does not match: {System.IO.Path.GetFileName(thisAssembly.Location)} version {thisAssembly.GetName().Version}";
                    ConsoleLogger.Default.WriteLineError(errorMsg);
                }
                else
                {
                    return new[]
                    {
                        GetDiagnoser(loadedAssembly, "BenchmarkDotNet.Diagnostics.Windows.MemoryDiagnoser"),
                        GetDiagnoser(loadedAssembly, "BenchmarkDotNet.Diagnostics.Windows.InliningDiagnoser"),
                    };
                }
            }
            catch (Exception ex) // we're loading a plug-in, better to be safe rather than sorry
            {
                ConsoleLogger.Default.WriteLineError($"Error loading {diagnosticAssembly}: {ex.GetType().Name} - {ex.Message}");
            }
#endif
            return new IDiagnoser[0];
        }

        private static IDiagnoser GetDiagnoser(Assembly loadedAssembly, string typeName)
        {
            var diagnoserType = loadedAssembly.GetType(typeName);
            return (IDiagnoser) Activator.CreateInstance(diagnoserType);
        }
    }
}
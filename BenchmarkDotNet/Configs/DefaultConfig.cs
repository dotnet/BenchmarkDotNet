using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Analyzers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Configs
{
    public class DefaultConfig : IConfig
    {
        public static readonly IConfig Instance = new DefaultConfig();

        private DefaultConfig()
        {
        }

        public IEnumerable<IColumn> GetColumns()
        {
            yield return PropertyColumn.Type;
            yield return PropertyColumn.Method;
            yield return PropertyColumn.Mode;
            yield return PropertyColumn.Platform;
            yield return PropertyColumn.Jit;
            yield return PropertyColumn.Framework;
            yield return PropertyColumn.Toolchain;
            yield return PropertyColumn.Runtime;
            yield return PropertyColumn.ProcessCount;
            yield return PropertyColumn.WarmupCount;
            yield return PropertyColumn.TargetCount;
            yield return PropertyColumn.Affinity;

            yield return StatisticColumn.Time;
            yield return StatisticColumn.Error;

            yield return BaselineDeltaColumn.Default;
        }

        public IEnumerable<IExporter> GetExporters()
        {
            yield return CsvExporter.Default;
            yield return MarkdownExporter.StackOverflow;
            yield return MarkdownExporter.Default;
            yield return MarkdownExporter.GitHub;
            yield return PlainExporter.Default;
            yield return CsvRunsExporter.Default;
            yield return RPlotExporter.Default;
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

        public IEnumerable<IJob> GetJobs() => EnumerableHelper.Empty<IJob>();

        // TODO use LoadDiagnoser
        public IEnumerable<IDiagnoser> GetDiagnosers() => EnumerableHelper.Empty<IDiagnoser>();

        // Make the Diagnosers lazy-loaded, so they are only instantiated if neededs
        private static readonly Lazy<IDiagnoser[]> Diagnosers =
            new Lazy<IDiagnoser[]>(LoadDiagnoser, LazyThreadSafetyMode.ExecutionAndPublication);

        private static IDiagnoser[] LoadDiagnoser()
        {
            var diagnosticAssembly = "BenchmarkDotNet.Diagnostics.dll";
            try
            {
                var loadedAssembly = Assembly.LoadFrom(diagnosticAssembly);
                var thisAssembly = Assembly.GetAssembly(typeof(DefaultConfig));
                if (loadedAssembly.GetName().Version != thisAssembly.GetName().Version)
                {
                    var errorMsg =
                        $"Unable to load: {diagnosticAssembly} version {loadedAssembly.GetName().Version}" +
                        Environment.NewLine +
                        $"Does not match: {Path.GetFileName(thisAssembly.Location)} version {thisAssembly.GetName().Version}";
                    ConsoleLogger.Default.WriteLineError(errorMsg);
                }
                else
                {
                    var runtimeDiagnoserType = loadedAssembly.GetType("BenchmarkDotNet.Diagnostics.RuntimeDiagnoser");
                    var runtimeDiagnoser = (IDiagnoser)Activator.CreateInstance(runtimeDiagnoserType);
                    var sourceDiagnoserType = loadedAssembly.GetType("BenchmarkDotNet.Diagnostics.SourceDiagnoser");
                    var sourceDiagnoser = (IDiagnoser)Activator.CreateInstance(sourceDiagnoserType);
                    return new[] { runtimeDiagnoser, sourceDiagnoser };
                }
            }
            catch (Exception ex) // we're loading a plug-in, better to be safe rather than sorry
            {
                ConsoleLogger.Default.WriteLineError($"Error loading {diagnosticAssembly}: {ex.GetType().Name} - {ex.Message}");
            }
            return new IDiagnoser[0];
        }

    }
}
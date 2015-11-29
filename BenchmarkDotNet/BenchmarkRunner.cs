using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Plugins.Exporters;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tasks;
using BenchmarkDotNet.Toolchain;
using BenchmarkDotNet.Toolchain.Results;

namespace BenchmarkDotNet
{
    public class BenchmarkRunner
    {
        public BenchmarkRunner(IBenchmarkPlugins plugins = null)
        {            
            if (plugins == null)
            {
                var builder = new BenchmarkPluginBuilder();
                builder.AddLogger(BenchmarkConsoleLogger.Default);
                builder.AddExporter(BenchmarkMarkdownExporter.Default);
                plugins = builder.Build();
            }
            Plugins = plugins;
        }

        public IBenchmarkPlugins Plugins { get; }
        public IBenchmarkLogger Logger => Plugins.CompositeLogger;
        public IBenchmarkExporter Exporter => Plugins.CompositeExporter;
        public IBenchmarkDiagnoser Diagnoser => Plugins.CompositeDiagnoser;

        internal IEnumerable<BenchmarkReport> Run(List<Benchmark> benchmarks)
        {
            Logger.WriteLineHeader("// ***** BenchmarkRunner: Start   *****");
            Logger.WriteLineInfo("// Found benchmarks:");
            foreach (var benchmark in benchmarks)
                Logger.WriteLineInfo($"//   {benchmark.Description}");
            Logger.NewLine();

            var importantPropertyNames = benchmarks.Select(b => b.Properties).GetImportantNames();

            var reports = new List<BenchmarkReport>();
            foreach (var benchmark in benchmarks)
            {
                if (benchmark.Task.ParametersSets.IsEmpty())
                {
                    var report = Run(benchmark, importantPropertyNames);
                    reports.Add(report);
                    if (report.Runs.Count > 0)
                    {
                        var stat = new BenchmarkRunReportsStatistic("Target", report.Runs);
                        Logger.WriteLineResult($"AverageTime (ns/op): {stat.AverageTime}");
                        Logger.WriteLineResult($"OperationsPerSecond: {stat.OperationsPerSeconds}");
                    }
                }
                else
                {
                    var parametersSets = benchmark.Task.ParametersSets;
                    foreach (var parameters in parametersSets.ToParameters())
                    {
                        var report = Run(benchmark, importantPropertyNames, parameters);
                        reports.Add(report);
                        if (report.Runs.Count > 0)
                        {
                            var stat = new BenchmarkRunReportsStatistic("Target", report.Runs);
                            Logger.WriteLineResult($"AverageTime (ns/op): {stat.AverageTime}");
                            Logger.WriteLineResult($"OperationsPerSecond: {stat.OperationsPerSeconds}");
                        }
                    }
                }
                Logger.NewLine();
            }
            Logger.WriteLineHeader("// ***** BenchmarkRunner: Finish  *****");
            Logger.NewLine();

            Exporter.Export(reports, Logger);

            Logger.NewLine();
            Logger.WriteLineHeader("// ***** BenchmarkRunner: End *****");
            return reports;
        }

        private BenchmarkReport Run(Benchmark benchmark, IList<string> importantPropertyNames, BenchmarkParameters parameters = null)
        {
            var toolchain = BenchmarkToolchainFacade.CreateToolchain(benchmark, Logger);

            Logger.WriteLineHeader("// **************************");
            Logger.WriteLineHeader("// Benchmark: " + benchmark.Description);

            var generateResult = Generate(toolchain);
            if (!generateResult.IsGenerateSuccess)
                return BenchmarkReport.CreateEmpty(benchmark, parameters);

            var buildResult = Build(toolchain, generateResult);
            if (!buildResult.IsBuildSuccess)
                return BenchmarkReport.CreateEmpty(benchmark, parameters);

            var runReports = Exec(benchmark, importantPropertyNames, parameters, toolchain, buildResult);
            return new BenchmarkReport(benchmark, runReports, parameters);
        }

        private BenchmarkGenerateResult Generate(IBenchmarkToolchainFacade toolchain)
        {
            Logger.WriteLineInfo("// *** Generate *** ");
            var generateResult = toolchain.Generate();
            if (generateResult.IsGenerateSuccess)
            {
                Logger.WriteLineInfo("// Result = Success");
                Logger.WriteLineInfo($"// {nameof(generateResult.DirectoryPath)} = {generateResult.DirectoryPath}");
            }
            else
            {
                Logger.WriteLineError("// Result = Failure");
                if (generateResult.GenerateException != null)
                    Logger.WriteLineError($"// Exception: {generateResult.GenerateException.Message}");
            }
            Logger.NewLine();
            return generateResult;
        }

        private BenchmarkBuildResult Build(IBenchmarkToolchainFacade toolchain, BenchmarkGenerateResult generateResult)
        {
            Logger.WriteLineInfo("// *** Build ***");
            var buildResult = toolchain.Build(generateResult);
            if (buildResult.IsBuildSuccess)
            {
                Logger.WriteLineInfo("// Result = Success");
            }
            else
            {
                Logger.WriteLineError("// Result = Failure");
                if (buildResult.BuildException != null)
                    Logger.WriteLineError($"// Exception: {buildResult.BuildException.Message}");
            }
            Logger.NewLine();
            return buildResult;
        }

        private List<BenchmarkRunReport> Exec(Benchmark benchmark, IList<string> importantPropertyNames, BenchmarkParameters parameters, IBenchmarkToolchainFacade toolchain, BenchmarkBuildResult buildResult)
        {
            Logger.WriteLineInfo("// *** Exec ***");
            var processCount = Math.Max(1, benchmark.Task.ProcessCount);
            var runReports = new List<BenchmarkRunReport>();

            for (int processNumber = 0; processNumber < processCount; processNumber++)
            {
                Logger.WriteLineInfo($"// Run, Process: {processNumber + 1} / {processCount}");
                if (parameters != null)
                    Logger.WriteLineInfo($"// {parameters.ToInfo()}");
                if (importantPropertyNames.Any())
                {
                    Logger.WriteInfo("// ");
                    foreach (var name in importantPropertyNames)
                        Logger.WriteInfo($"{name}={benchmark.Properties.GetValue(name)} ");
                    Logger.NewLine();
                }

                var execResult = toolchain.Exec(buildResult, parameters);

                if (execResult.FoundExecutable)
                {
                    var iterRunReports = execResult.Data.Select(line => BenchmarkRunReport.Parse(Logger, line)).Where(r => r != null).ToList();
                    runReports.AddRange(iterRunReports);
                }
                else
                {
                    Logger.WriteLineError("Executable not found");
                }
            }
            Logger.NewLine();
            return runReports;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Export;
using BenchmarkDotNet.Flow;
using BenchmarkDotNet.Flow.Results;
using BenchmarkDotNet.Logging;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet
{
    public class BenchmarkRunner
    {
        public BenchmarkRunner(IEnumerable<IBenchmarkLogger> loggers)
        {
            Logger = new BenchmarkCompositeLogger(loggers.ToArray());
            ReportExporter = MarkdownReportExporter.Default;
        }

        public BenchmarkRunner() : this(new[] { new BenchmarkConsoleLogger() })
        {
        }

        public IBenchmarkLogger Logger { get; }
        public IReportExporter ReportExporter { get; }

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

            ReportExporter.Export(reports, Logger);

            Logger.NewLine();
            Logger.WriteLineHeader("// ***** BenchmarkRunner: End *****");
            return reports;
        }

        private BenchmarkReport Run(Benchmark benchmark, IList<string> importantPropertyNames, BenchmarkParameters parameters = null)
        {
            var flow = BenchmarkFlowFactory.CreateFlow(benchmark, Logger);

            Logger.WriteLineHeader("// **************************");
            Logger.WriteLineHeader("// Benchmark: " + benchmark.Description);

            var generateResult = Generate(flow);
            if (!generateResult.IsGenerateSuccess)
                return BenchmarkReport.CreateEmpty(benchmark, parameters);

            var buildResult = Build(flow, generateResult);
            if (!buildResult.IsBuildSuccess)
                return BenchmarkReport.CreateEmpty(benchmark, parameters);

            var runReports = Exec(benchmark, importantPropertyNames, parameters, flow, buildResult);
            return new BenchmarkReport(benchmark, runReports, parameters);
        }

        private BenchmarkGenerateResult Generate(IBenchmarkFlow flow)
        {
            Logger.WriteLineInfo("// *** Generate *** ");
            var generateResult = flow.Generate();
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

        private BenchmarkBuildResult Build(IBenchmarkFlow flow, BenchmarkGenerateResult generateResult)
        {
            Logger.WriteLineInfo("// *** Build ***");
            var buildResult = flow.Build(generateResult);
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

        private List<BenchmarkRunReport> Exec(Benchmark benchmark, IList<string> importantPropertyNames, BenchmarkParameters parameters, IBenchmarkFlow flow, BenchmarkBuildResult buildResult)
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

                var execResult = flow.Exec(buildResult, parameters);

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
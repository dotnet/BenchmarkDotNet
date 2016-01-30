using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Running
{
    public static class BenchmarkRunner
    {
        private static int benchmarkRunIndex;

        public static Summary Run<T>(IConfig config = null) =>
            Run(BenchmarkConverter.TypeToBenchmarks(typeof(T), config), config);

        public static Summary Run(Type type, IConfig config = null) =>
            Run(BenchmarkConverter.TypeToBenchmarks(type, config), config);

        public static Summary RunUrl(string url, IConfig config = null) =>
            Run(BenchmarkConverter.UrlToBenchmarks(url, config), config);

        public static Summary RunSource(string source, IConfig config = null) =>
            Run(BenchmarkConverter.SourceToBenchmarks(source, config), config);

        internal static Summary Run(IList<Benchmark> benchmarks, IConfig config)
        {
            config = BenchmarkConverter.GetFullConfig(benchmarks.FirstOrDefault()?.Target.Type, config);

            var title = GetTitle(benchmarks);
            EnsureNoMoreOneBaseline(benchmarks, title);

            using (var logStreamWriter = new StreamWriter(title + ".log"))
            {
                var logger = new CompositeLogger(config.GetCompositeLogger(), new StreamLogger(logStreamWriter));
                var summary = Run(benchmarks, logger, title, config);
                config.GetCompositeExporter().ExportToFiles(summary);
                return summary;
            }
        }

        private static string GetTitle(IList<Benchmark> benchmarks)
        {
            var types = benchmarks.Select(b => b.Target.Type.Name).Distinct().ToArray();
            if (types.Length == 1)
                return types[0];
            benchmarkRunIndex++;
            return $"BenchmarkRun-{benchmarkRunIndex:##000}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}";
        }

        private static Summary Run(IList<Benchmark> benchmarks, ILogger logger, string title, IConfig config)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            logger.WriteLineHeader("// ***** BenchmarkRunner: Start   *****");
            logger.WriteLineInfo("// Found benchmarks:");
            foreach (var benchmark in benchmarks)
                logger.WriteLineInfo($"//   {benchmark.ShortInfo}");
            logger.NewLine();

            var globalStopwatch = Stopwatch.StartNew();
            var reports = new List<BenchmarkReport>();
            foreach (var benchmark in benchmarks)
            {
                var report = Run(benchmark, logger, config);
                reports.Add(report);
                if (report.GetResultRuns().Any())
                    logger.WriteLineStatistic(report.GetResultRuns().GetStatistics().ToTimeStr());

                logger.NewLine();
            }
            globalStopwatch.Stop();

            var summary = new Summary(title, reports, EnvironmentHelper.GetCurrentInfo(), config, currentDirectory, globalStopwatch.Elapsed);

            logger.WriteLineHeader("// ***** BenchmarkRunner: Finish  *****");
            logger.NewLine();

            logger.WriteLineHeader("// * Export *");
            var files = config.GetCompositeExporter().ExportToFiles(summary);
            foreach (var file in files)
            {
                var printedFile = file.StartsWith(currentDirectory) ? file.Substring(currentDirectory.Length).Trim('/', '\\') : file;
                logger.WriteLineInfo($"  {printedFile}");
            }
            logger.NewLine();

            logger.WriteLineHeader("// * Detailed results *");

            // TODO: make exporter
            foreach (var report in reports)
            {
                logger.WriteLineInfo(report.Benchmark.ShortInfo);
                logger.WriteLineStatistic(report.GetResultRuns().GetStatistics().ToTimeStr());
                logger.NewLine();
            }

            LogTotalTime(logger, globalStopwatch.Elapsed);
            logger.NewLine();

            logger.WriteLineHeader("// * Summary *");
            MarkdownExporter.Default.ExportToLog(summary, logger);

            // TODO: make exporter
            var warnings = config.GetCompositeAnalyser().Analyze(summary).ToList();
            if (warnings.Count > 0)
            {
                logger.NewLine();
                logger.WriteLineError("// * Warnings * ");
                foreach (var warning in warnings)
                    logger.WriteLineError($"{warning.Message}");
            }

            logger.NewLine();
            logger.WriteLineHeader("// ***** BenchmarkRunner: End *****");
            return summary;
        }

        private static void LogTotalTime(ILogger logger, TimeSpan time)
        {
            var hhMmSs = $"{time.TotalHours:00}:{time:mm\\:ss}";
            var totalSecs = $"{time.TotalSeconds.ToStr()} sec";
            logger.WriteLineStatistic($"Total time: {hhMmSs} ({totalSecs})");
        }

        private static BenchmarkReport Run(Benchmark benchmark, ILogger logger, IConfig config)
        {
            var toolchain = benchmark.Job.Toolchain;

            logger.WriteLineHeader("// **************************");
            logger.WriteLineHeader("// Benchmark: " + benchmark.ShortInfo);

            var generateResult = Generate(logger, toolchain, benchmark);
            if (!generateResult.IsGenerateSuccess)
                return new BenchmarkReport(benchmark, generateResult, null, null, null);

            var buildResult = Build(logger, toolchain, generateResult);
            if (!buildResult.IsBuildSuccess)
                return new BenchmarkReport(benchmark, generateResult, buildResult, null, null);

            var executeResults = Execute(logger, benchmark, toolchain, buildResult, config);

            var runs = new List<Measurement>();
            for (int index = 0; index < executeResults.Count; index++)
            {
                var executeResult = executeResults[index];
                runs.AddRange(executeResult.Data.Select(line => Measurement.Parse(logger, line, index + 1)).Where(r => r != null));
            }

            return new BenchmarkReport(benchmark, generateResult, buildResult, executeResults, runs);
        }

        private static GenerateResult Generate(ILogger logger, IToolchain toolchain, Benchmark benchmark)
        {
            logger.WriteLineInfo("// *** Generate *** ");
            var generateResult = toolchain.Generator.GenerateProject(benchmark, logger);
            if (generateResult.IsGenerateSuccess)
            {
                logger.WriteLineInfo("// Result = Success");
                logger.WriteLineInfo($"// {nameof(generateResult.DirectoryPath)} = {generateResult.DirectoryPath}");
            }
            else
            {
                logger.WriteLineError("// Result = Failure");
                if (generateResult.GenerateException != null)
                    logger.WriteLineError($"// Exception: {generateResult.GenerateException.Message}");
            }
            logger.NewLine();
            return generateResult;
        }

        private static BuildResult Build(ILogger logger, IToolchain toolchain, GenerateResult generateResult)
        {
            logger.WriteLineInfo("// *** Build ***");
            var buildResult = toolchain.Builder.Build(generateResult, logger);
            if (buildResult.IsBuildSuccess)
            {
                logger.WriteLineInfo("// Result = Success");
            }
            else
            {
                logger.WriteLineError("// Result = Failure");
                if (buildResult.BuildException != null)
                    logger.WriteLineError($"// Exception: {buildResult.BuildException.Message}");
            }
            logger.NewLine();
            return buildResult;
        }

        private static List<ExecuteResult> Execute(ILogger logger, Benchmark benchmark, IToolchain toolchain, BuildResult buildResult, IConfig config)
        {
            var executeResults = new List<ExecuteResult>();


            logger.WriteLineInfo("// *** Execute ***");
            var launchCount = Math.Max(1, benchmark.Job.LaunchCount.IsAuto ? 2 : benchmark.Job.LaunchCount.Value);

            for (int processNumber = 0; processNumber < launchCount; processNumber++)
            {
                var printedProcessNumber = (benchmark.Job.LaunchCount.IsAuto && processNumber < 2) ? "?" : launchCount.ToString();
                logger.WriteLineInfo($"// Run, Process: {processNumber + 1} / {printedProcessNumber}");

                var executeResult = toolchain.Executor.Execute(buildResult, config.GetCompositeDiagnoser(), benchmark, logger);

                if (!executeResult.FoundExecutable)
                    logger.WriteLineError("Executable not found");
                executeResults.Add(executeResult);

                if (benchmark.Job.LaunchCount.IsAuto && processNumber == 1)
                {
                    var measurements = executeResults.
                        SelectMany(r => r.Data).
                        Select(line => Measurement.Parse(logger, line, 0)).
                        Where(r => r != null).
                        ToArray();
                    var idleApprox = new Statistics(measurements.Where(m => m.IterationMode == IterationMode.IdleTarget).Select(m => m.Nanoseconds)).Median;
                    var mainApprox = new Statistics(measurements.Where(m => m.IterationMode == IterationMode.MainTarget).Select(m => m.Nanoseconds)).Median;
                    var percent = idleApprox/mainApprox*100;
                    launchCount = (int) Math.Round(Math.Max(2, 2 + (percent - 1)/3)); // an empirical formula
                }
            }
            logger.NewLine();
            return executeResults;
        }

        /// <summary>
        /// This method is ONLY for wiring up extensions that can be detected/inferred from the list of Benchmarks.
        /// Any extensions that are wired-up via command-line parameters are handled elsewhere
        /// </summary>
        private static void EnsureNoMoreOneBaseline(IList<Benchmark> benchmarks, string benchmarkName)
        {
            var baselineCount = benchmarks.Select(b => b.Target).Distinct().Count(target => target.Baseline);
            if (baselineCount > 1)
                throw new InvalidOperationException($"Only 1 [Benchmark] in a class can have \"Baseline = true\" applied to it, {benchmarkName} has {baselineCount}");
        }
    }
}
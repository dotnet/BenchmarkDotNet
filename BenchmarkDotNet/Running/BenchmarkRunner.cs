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
            config = config ?? DefaultConfig.Instance;

            benchmarkRunIndex++;
            var title = $"BenchmarkRun-{benchmarkRunIndex:##000}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}"; // TODO
            EnsureNoMoreOneBaseline(benchmarks, title);

            using (var logStreamWriter = new StreamWriter(title + ".log"))
            {
                var logger = new CompositeLogger(config.GetCompositeLogger(), new StreamLogger(logStreamWriter));
                var summary = Run(benchmarks, logger, title, config);
                config.GetCompositeExporter().ExportToFiles(summary);
                return summary;
            }
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
                if (report.GetTargetRuns().Any())
                    logger.WriteLineStatistic(report.GetTargetRuns().GetStats().ToTimeStr());

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
                logger.WriteLineStatistic(report.GetTargetRuns().GetStats().ToTimeStr());
                logger.NewLine();
            }

            logger.WriteLineStatistic($"Total time: {globalStopwatch.Elapsed.TotalHours:00}:{globalStopwatch.Elapsed:mm\\:ss}");
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

            var runs = new List<BenchmarkRunReport>();
            for (int index = 0; index < executeResults.Count; index++)
            {
                var executeResult = executeResults[index];
                runs.AddRange(executeResult.Data.Select(line => BenchmarkRunReport.Parse(logger, line, index + 1)).Where(r => r != null));
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
            var processCount = Math.Max(1, benchmark.Job.ProcessCount.IsAuto ? 3 : benchmark.Job.ProcessCount.Value); // TODO

            for (int processNumber = 0; processNumber < processCount; processNumber++)
            {
                logger.WriteLineInfo($"// Run, Process: {processNumber + 1} / {processCount}");

                var executeResult = toolchain.Executor.Execute(buildResult, config.GetCompositeDiagnoser(), benchmark, logger);

                if (!executeResult.FoundExecutable)
                    logger.WriteLineError("Executable not found");
                executeResults.Add(executeResult);
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
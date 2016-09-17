using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Running
{
    internal static class BenchmarkRunnerCore
    {
        private static int benchmarkRunIndex;

        internal static readonly IResolver DefaultResolver = new CompositeResolver(EnvResolver.Instance, InfraResolver.Instance);

        internal static Summary Run(Benchmark[] benchmarks, IConfig config, Func<Job, IToolchain> toolchainProvider)
        {
            var resolver = DefaultResolver;
            config = BenchmarkConverter.GetFullConfig(benchmarks.FirstOrDefault()?.Target.Type, config);

            var title = GetTitle(benchmarks);
            var rootArtifactsFolderPath = GetRootArtifactsFolderPath();

            using (var logStreamWriter = Portability.StreamWriter.FromPath(Path.Combine(rootArtifactsFolderPath, title + ".log")))
            {
                var logger = new CompositeLogger(config.GetCompositeLogger(), new StreamLogger(logStreamWriter));
                benchmarks = GetSupportedBenchmarks(benchmarks, logger, toolchainProvider, resolver);

                var summary = Run(benchmarks, logger, title, config, rootArtifactsFolderPath, toolchainProvider, resolver);
                if (!summary.HasCriticalValidationErrors)
                {
                    config.GetCompositeExporter().ExportToFiles(summary).ToArray();
                }
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

        private static Summary Run(Benchmark[] benchmarks, ILogger logger, string title, IConfig config, string rootArtifactsFolderPath, Func<Job, IToolchain> toolchainProvider, IResolver resolver)
        {
            logger.WriteLineHeader("// ***** BenchmarkRunner: Start   *****");
            logger.WriteLineInfo("// Found benchmarks:");
            foreach (var benchmark in benchmarks)
                logger.WriteLineInfo($"//   {benchmark.DisplayInfo}");
            logger.WriteLine();

            var validationErrors = Validate(benchmarks, logger, config);
            if (validationErrors.Any(validationError => validationError.IsCritical))
            {
                return Summary.CreateFailed(benchmarks, title, HostEnvironmentInfo.GetCurrent(), config, GetResultsFolderPath(rootArtifactsFolderPath), validationErrors);
            }

            var globalChronometer = Chronometer.Start();
            var reports = new List<BenchmarkReport>();
            foreach (var benchmark in benchmarks)
            {
                var report = Run(benchmark, logger, config, rootArtifactsFolderPath, toolchainProvider, resolver);
                reports.Add(report);
                if (report.GetResultRuns().Any())
                    logger.WriteLineStatistic(report.GetResultRuns().GetStatistics().ToTimeStr());

                logger.WriteLine();
            }
            var clockSpan = globalChronometer.Stop();

            var summary = new Summary(title, reports, HostEnvironmentInfo.GetCurrent(), config, GetResultsFolderPath(rootArtifactsFolderPath), clockSpan.GetTimeSpan(), validationErrors);

            logger.WriteLineHeader("// ***** BenchmarkRunner: Finish  *****");
            logger.WriteLine();

            logger.WriteLineHeader("// * Export *");
            var currentDirectory = Directory.GetCurrentDirectory();
            foreach (var file in config.GetCompositeExporter().ExportToFiles(summary))
            {
                logger.WriteLineInfo($"  {file.Replace(currentDirectory, string.Empty).Trim('/', '\\')}");
            }
            logger.WriteLine();

            logger.WriteLineHeader("// * Detailed results *");

            // TODO: make exporter
            foreach (var report in reports)
            {
                logger.WriteLineInfo(report.Benchmark.DisplayInfo);
                logger.WriteLineStatistic(report.GetResultRuns().GetStatistics().ToTimeStr());
                logger.WriteLine();
            }

            LogTotalTime(logger, clockSpan.GetTimeSpan());
            logger.WriteLine();

            logger.WriteLineHeader("// * Summary *");
            MarkdownExporter.Console.ExportToLog(summary, logger);

            // TODO: make exporter
            var warnings = config.GetCompositeAnalyser().Analyse(summary).ToList();
            if (warnings.Count > 0)
            {
                logger.WriteLine();
                logger.WriteLineError("// * Warnings * ");
                foreach (var warning in warnings)
                    logger.WriteLineError($"{warning.Message}");
            }

            if (config.GetDiagnosers().Count() > 0)
            {
                logger.WriteLine();
                config.GetCompositeDiagnoser().DisplayResults(logger);
            }

            logger.WriteLine();
            logger.WriteLineHeader("// ***** BenchmarkRunner: End *****");
            return summary;
        }

        private static ValidationError[] Validate(IList<Benchmark> benchmarks, ILogger logger, IConfig config)
        {
            logger.WriteLineInfo("// Validating benchmarks:");
            var validationErrors = config.GetCompositeValidator().Validate(benchmarks).ToArray();
            foreach (var validationError in validationErrors)
            {
                logger.WriteLineError(validationError.Message);
            }
            return validationErrors;
        }

        internal static void LogTotalTime(ILogger logger, TimeSpan time, string message = "Total time")
        {
            logger.WriteLineStatistic($"{message}: {time.ToFormattedTotalTime()}");
        }

        private static BenchmarkReport Run(Benchmark benchmark, ILogger logger, IConfig config, string rootArtifactsFolderPath, Func<Job, IToolchain> toolchainProvider, IResolver resolver)
        {
            var toolchain = toolchainProvider(benchmark.Job);

            logger.WriteLineHeader("// **************************");
            logger.WriteLineHeader("// Benchmark: " + benchmark.DisplayInfo);

            var generateResult = Generate(logger, toolchain, benchmark, rootArtifactsFolderPath, config, resolver);
            
            try
            {
                if (!generateResult.IsGenerateSuccess)
                    return new BenchmarkReport(benchmark, generateResult, null, null, null);

                var buildResult = Build(logger, toolchain, generateResult, benchmark, resolver);
                if (!buildResult.IsBuildSuccess)
                    return new BenchmarkReport(benchmark, generateResult, buildResult, null, null);

                List<ExecuteResult> executeResults = Execute(logger, benchmark, toolchain, buildResult, config, resolver);

                var runs = new List<Measurement>();
                for (int index = 0; index < executeResults.Count; index++)
                {
                    var executeResult = executeResults[index];
                    runs.AddRange(executeResult.Data.Select(line => Measurement.Parse(logger, line, index + 1)).Where(r => r.IterationMode != IterationMode.Unknown));
                }

                return new BenchmarkReport(benchmark, generateResult, buildResult, executeResults, runs);
            }
            finally
            {
                if (!config.KeepBenchmarkFiles)
                {
                    generateResult.ArtifactsPaths?.RemoveBenchmarkFiles();
                }
            }
        }

        private static GenerateResult Generate(ILogger logger, IToolchain toolchain, Benchmark benchmark, string rootArtifactsFolderPath, IConfig config, IResolver resolver)
        {
            logger.WriteLineInfo("// *** Generate *** ");
            var generateResult = toolchain.Generator.GenerateProject(benchmark, logger, rootArtifactsFolderPath, config, resolver);
            if (generateResult.IsGenerateSuccess)
            {
                logger.WriteLineInfo("// Result = Success");
                logger.WriteLineInfo($"// {nameof(generateResult.ArtifactsPaths.BinariesDirectoryPath)} = {generateResult.ArtifactsPaths?.BinariesDirectoryPath}");
            }
            else
            {
                logger.WriteLineError("// Result = Failure");
                if (generateResult.GenerateException != null)
                    logger.WriteLineError($"// Exception: {generateResult.GenerateException.Message}");
            }
            logger.WriteLine();
            return generateResult;
        }

        private static BuildResult Build(ILogger logger, IToolchain toolchain, GenerateResult generateResult, Benchmark benchmark, IResolver resolver)
        {
            logger.WriteLineInfo("// *** Build ***");
            var buildResult = toolchain.Builder.Build(generateResult, logger, benchmark, resolver);
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
            logger.WriteLine();
            return buildResult;
        }

        private static List<ExecuteResult> Execute(ILogger logger, Benchmark benchmark, IToolchain toolchain, BuildResult buildResult, IConfig config, IResolver resolver)
        {
            var executeResults = new List<ExecuteResult>();

            logger.WriteLineInfo("// *** Execute ***");
            var launchCount = Math.Max(1, benchmark.Job.Run.LaunchCount.IsDefault ? 2 : benchmark.Job.Run.LaunchCount.SpecifiedValue);

            for (int processNumber = 0; processNumber < launchCount; processNumber++)
            {
                var printedProcessNumber = (benchmark.Job.Run.LaunchCount.IsDefault && processNumber < 2) ? "" : " / " + launchCount.ToString();
                logger.WriteLineInfo($"// Launch: {processNumber + 1}{printedProcessNumber}");

                var executeResult = toolchain.Executor.Execute(buildResult, benchmark, logger, resolver);

                if (!executeResult.FoundExecutable)
                    logger.WriteLineError("Executable not found");
                if (executeResult.ExitCode != 0)
                    logger.WriteLineError("ExitCode != 0");
                executeResults.Add(executeResult);

                var measurements = executeResults
                        .SelectMany(r => r.Data)
                        .Select(line => Measurement.Parse(logger, line, 0))
                        .Where(r => r.IterationMode != IterationMode.Unknown).
                        ToArray();

                if (!measurements.Any())
                {
                    // Something went wrong during the benchmark, don't bother doing more runs
                    logger.WriteLineError($"No more Benchmark runs will be launched as NO measurements were obtained from the previous run!");
                    break;
                }

                if (benchmark.Job.Run.LaunchCount.IsDefault && processNumber == 1)
                {
                    var idleApprox = new Statistics(measurements.Where(m => m.IterationMode == IterationMode.IdleTarget).Select(m => m.Nanoseconds)).Median;
                    var mainApprox = new Statistics(measurements.Where(m => m.IterationMode == IterationMode.MainTarget).Select(m => m.Nanoseconds)).Median;
                    var percent = idleApprox / mainApprox * 100;
                    launchCount = (int)Math.Round(Math.Max(2, 2 + (percent - 1) / 3)); // an empirical formula
                }
            }
            logger.WriteLine();

            // Do a "Diagnostic" run, but DISCARD the results, so that the overhead of Diagnostics doesn't skew the overall results
            if (config.GetDiagnosers().Count() > 0)
            {
                logger.WriteLineInfo($"// Run, Diagnostic");
                config.GetCompositeDiagnoser().Start(benchmark);
                var executeResult = toolchain.Executor.Execute(buildResult, benchmark, logger, resolver, config.GetCompositeDiagnoser());
                var allRuns = executeResult.Data.Select(line => Measurement.Parse(logger, line, 0)).Where(r => r.IterationMode != IterationMode.Unknown).ToList();
                var report = new BenchmarkReport(benchmark, null, null, new[] { executeResult }, allRuns);
                config.GetCompositeDiagnoser().Stop(benchmark, report);

                if (!executeResult.FoundExecutable)
                    logger.WriteLineError("Executable not found");
                logger.WriteLine();
            }

            return executeResults;
        }

        private static Benchmark[] GetSupportedBenchmarks(IList<Benchmark> benchmarks, CompositeLogger logger, Func<Job, IToolchain> toolchainProvider, IResolver resolver)
        {
            return benchmarks.Where(benchmark => toolchainProvider(benchmark.Job).IsSupported(benchmark, logger, resolver)).ToArray();
        }

        private static string GetRootArtifactsFolderPath() => CombineAndCreate(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts");

        private static string GetResultsFolderPath(string rootArtifactsFolderPath) => CombineAndCreate(rootArtifactsFolderPath, "results");

        private static string CombineAndCreate(string rootFolderPath, string childFolderName)
        {
            var path = Path.Combine(rootFolderPath, childFolderName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }
}
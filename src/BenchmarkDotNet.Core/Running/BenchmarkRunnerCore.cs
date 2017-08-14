using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Running
{
    // TODO: Find a better name
    public static class BenchmarkRunnerCore
    {
        private static int benchmarkRunIndex;

        internal static readonly IResolver DefaultResolver = new CompositeResolver(EnvResolver.Instance, InfrastructureResolver.Instance);

        public static Summary Run(Benchmark[] benchmarks, IConfig config, Func<Job, IToolchain> toolchainProvider)
        {
            var resolver = DefaultResolver;
            config = BenchmarkConverter.GetFullConfig(benchmarks.FirstOrDefault()?.Target.Type, config);

            var title = GetTitle(benchmarks);
            var rootArtifactsFolderPath = GetRootArtifactsFolderPath();

            using (var logStreamWriter = Portability.StreamWriter.FromPath(Path.Combine(rootArtifactsFolderPath, title + ".log")))
            {
                var logger = new CompositeLogger(config.GetCompositeLogger(), new StreamLogger(logStreamWriter));
                benchmarks = GetSupportedBenchmarks(benchmarks, logger, toolchainProvider, resolver);
                var artifactsToCleanup = new List<string>();

                try
                {
                    return Run(benchmarks, logger, title, config, rootArtifactsFolderPath, toolchainProvider, resolver, artifactsToCleanup);
                }
                finally
                {
                    logger.WriteLineHeader("// * Artifacts cleanup *");
                    Cleanup(artifactsToCleanup);
                }
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

        public static Summary Run(Benchmark[] benchmarks, ILogger logger, string title, IConfig config, string rootArtifactsFolderPath, Func<Job, IToolchain> toolchainProvider, IResolver resolver, List<string> artifactsToCleanup)
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
                var report = Run(benchmark, logger, config, rootArtifactsFolderPath, toolchainProvider, resolver, artifactsToCleanup);
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
            foreach (var file in config.GetCompositeExporter().ExportToFiles(summary, logger))
            {
                logger.WriteLineInfo($"  {file.Replace(currentDirectory, string.Empty).Trim('/', '\\')}");
            }
            logger.WriteLine();

            logger.WriteLineHeader("// * Detailed results *");

            // TODO: make exporter
            foreach (var report in reports)
            {
                logger.WriteLineInfo(report.Benchmark.DisplayInfo);
                logger.WriteLineStatistic($"Runtime = {report.GetRuntimeInfo()}; GC = {report.GetGcInfo()}");
                var resultRuns = report.GetResultRuns();
                if (resultRuns.IsEmpty())
                    logger.WriteLineError("There are no any results runs");
                else
                    logger.WriteLineStatistic(resultRuns.GetStatistics().ToTimeStr());
                logger.WriteLine();
            }

            LogTotalTime(logger, clockSpan.GetTimeSpan());
            logger.WriteLine();

            logger.WriteLineHeader("// * Summary *");
            MarkdownExporter.Console.ExportToLog(summary, logger);

            // TODO: make exporter
            ConclusionHelper.Print(logger, config.GetCompositeAnalyser().Analyse(summary).ToList());

            // TODO: move to conclusions
            var columnWithLegends = summary.Table.Columns.Select(c => c.OriginalColumn).Where(c => !string.IsNullOrEmpty(c.Legend)).ToList();
            var effectiveTimeUnit = summary.Table.EffectiveSummaryStyle.TimeUnit;
            if (columnWithLegends.Any() || effectiveTimeUnit != null)
            {
                logger.WriteLine();
                logger.WriteLineHeader("// * Legends *");
                int maxNameWidth = 0;
                if (columnWithLegends.Any())
                    maxNameWidth = Math.Max(maxNameWidth, columnWithLegends.Select(c => c.ColumnName.Length).Max());
                if (effectiveTimeUnit != null)
                    maxNameWidth = Math.Max(maxNameWidth, effectiveTimeUnit.Name.Length + 2);

                foreach (var column in columnWithLegends)
                    logger.WriteLineHint($"  {column.ColumnName.PadRight(maxNameWidth, ' ')} : {column.Legend}");

                if (effectiveTimeUnit != null)
                    logger.WriteLineHint($"  {("1 " + effectiveTimeUnit.Name).PadRight(maxNameWidth, ' ')} :" +
                                         $" 1 {effectiveTimeUnit.Description} ({TimeUnit.Convert(1, effectiveTimeUnit, TimeUnit.Second).ToStr("0.#########")} sec)");
            }

            if (config.GetDiagnosers().Any())
            {
                logger.WriteLine();
                config.GetCompositeDiagnoser().DisplayResults(logger);
            }

            logger.WriteLine();
            logger.WriteLineHeader("// ***** BenchmarkRunner: End *****");
            return summary;
        }

        private static ValidationError[] Validate(IReadOnlyList<Benchmark> benchmarks, ILogger logger, IConfig config)
        {
            logger.WriteLineInfo("// Validating benchmarks:");
            var validationErrors = config.GetCompositeValidator().Validate(new ValidationParameters(benchmarks, config)).ToArray();
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

        public static BenchmarkReport Run(Benchmark benchmark, ILogger logger, IConfig config, string rootArtifactsFolderPath, Func<Job, IToolchain> toolchainProvider, IResolver resolver, List<string> artifactsToCleanup)
        {
            var toolchain = toolchainProvider(benchmark.Job);

            logger.WriteLineHeader("// **************************");
            logger.WriteLineHeader("// Benchmark: " + benchmark.DisplayInfo);

            var assemblyResolveHelper = GetAssemblyResolveHelper(toolchain, logger);
            var generateResult = Generate(logger, toolchain, benchmark, rootArtifactsFolderPath, config, resolver);

            try
            {
                if (!generateResult.IsGenerateSuccess)
                    return new BenchmarkReport(benchmark, generateResult, null, null, null, default(GcStats));

                var buildResult = Build(logger, toolchain, generateResult, benchmark, resolver);
                if (!buildResult.IsBuildSuccess)
                    return new BenchmarkReport(benchmark, generateResult, buildResult, null, null, default(GcStats));

                var executeResults = Execute(logger, benchmark, toolchain, buildResult, config, resolver, out GcStats gcStats);

                var runs = new List<Measurement>();

                for (int index = 0; index < executeResults.Count; index++)
                {
                    var executeResult = executeResults[index];
                    runs.AddRange(executeResult.Data.Select(line => Measurement.Parse(logger, line, index + 1)).Where(r => r.IterationMode != IterationMode.Unknown));
                }

                return new BenchmarkReport(benchmark, generateResult, buildResult, executeResults, runs, gcStats);
            }
            catch (Exception e)
            {
                logger.WriteLineError("// Exception: " + e);
                return new BenchmarkReport(benchmark, generateResult, BuildResult.Failure(generateResult, e), Array.Empty<ExecuteResult>(), Array.Empty<Measurement>(), GcStats.Empty);
            }
            finally
            {
                if (!config.KeepBenchmarkFiles)
                {
                    artifactsToCleanup.AddRange(generateResult.ArtifactsToCleanup);
                }

                assemblyResolveHelper?.Dispose();
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
                    logger.WriteLineError($"// Exception: {generateResult.GenerateException}");
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

        private static List<ExecuteResult> Execute(ILogger logger, Benchmark benchmark, IToolchain toolchain, BuildResult buildResult, IConfig config, IResolver resolver, out GcStats gcStats)
        {
            var executeResults = new List<ExecuteResult>();
            gcStats = default(GcStats);

            logger.WriteLineInfo("// *** Execute ***");
            bool analyzeRunToRunVariance = benchmark.Job.ResolveValue(AccuracyMode.AnalyzeLaunchVarianceCharacteristic, resolver);
            bool autoLaunchCount = !benchmark.Job.HasValue(RunMode.LaunchCountCharacteristic);
            int defaultValue = analyzeRunToRunVariance ? 2 : 1;
            int launchCount = Math.Max(
                1,
                autoLaunchCount ? defaultValue : benchmark.Job.Run.LaunchCount);

            for (int launchIndex = 0; launchIndex < launchCount; launchIndex++)
            {
                string printedLaunchCount = (analyzeRunToRunVariance &&
                    autoLaunchCount &&
                    launchIndex < 2)
                    ? ""
                    : " / " + launchCount;
                logger.WriteLineInfo($"// Launch: {launchIndex + 1}{printedLaunchCount}");

                var executeResult = toolchain.Executor.Execute(
                    new ExecuteParameters(buildResult, benchmark, logger, resolver, config));

                if (!executeResult.FoundExecutable)
                    logger.WriteLineError($"Executable {buildResult.ArtifactsPaths.ExecutablePath} not found");
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
                    logger.WriteLineError("No more Benchmark runs will be launched as NO measurements were obtained from the previous run!");
                    break;
                }

                if (autoLaunchCount && launchIndex == 1 && analyzeRunToRunVariance)
                {
                    // TODO: improve this logic
                    var idleApprox = new Statistics(measurements.Where(m => m.IterationMode == IterationMode.IdleTarget).Select(m => m.Nanoseconds)).Median;
                    var mainApprox = new Statistics(measurements.Where(m => m.IterationMode == IterationMode.MainTarget).Select(m => m.Nanoseconds)).Median;
                    var percent = idleApprox / mainApprox * 100;
                    launchCount = (int)Math.Round(Math.Max(2, 2 + (percent - 1) / 3)); // an empirical formula
                }
            }
            logger.WriteLine();

            // Do a "Diagnostic" run, but DISCARD the results, so that the overhead of Diagnostics doesn't skew the overall results
            if (config.GetDiagnosers().Any(diagnoser => diagnoser.GetRunMode(benchmark) == Diagnosers.RunMode.ExtraRun))
            {
                logger.WriteLineInfo("// Run, Diagnostic");
                var compositeDiagnoser = config.GetCompositeDiagnoser(benchmark, Diagnosers.RunMode.ExtraRun);

                var executeResult = toolchain.Executor.Execute(
                    new ExecuteParameters(buildResult, benchmark, logger, resolver, config, compositeDiagnoser));

                var allRuns = executeResult.Data.Select(line => Measurement.Parse(logger, line, 0)).Where(r => r.IterationMode != IterationMode.Unknown).ToList();
                gcStats = GcStats.Parse(executeResult.Data.Last());
                var report = new BenchmarkReport(benchmark, null, null, new[] { executeResult }, allRuns, gcStats);
                compositeDiagnoser.ProcessResults(benchmark, report);

                if (!executeResult.FoundExecutable)
                    logger.WriteLineError("Executable not found");
                logger.WriteLine();
            }

            foreach (var diagnoser in config.GetDiagnosers().Where(diagnoser => diagnoser.GetRunMode(benchmark) == Diagnosers.RunMode.SeparateLogic))
            {
                logger.WriteLineInfo("// Run, Diagnostic [SeparateLogic]");

                diagnoser.ProcessResults(benchmark, null);
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

        private static IDisposable GetAssemblyResolveHelper(IToolchain toolchain, ILogger logger)
        {
#if CLASSIC
            if (!(toolchain is InProcessToolchain) // we don't want to mess with assembly loading when running benchmarks in the same process (could produce wrong results)
                && !RuntimeInformation.IsMono()) // so far it was never an issue for Mono
            {
                return Helpers.DirtyAssemblyResolveHelper.Create(logger);
            }
#endif
            return null;
        }

        private static void Cleanup(List<string> artifactsToCleanup)
        {
            foreach (string path in artifactsToCleanup)
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
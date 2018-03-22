using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
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
using RunMode = BenchmarkDotNet.Jobs.RunMode;

namespace BenchmarkDotNet.Running
{
    // TODO: Find a better name
    public static class BenchmarkRunnerCore
    {
        private static int benchmarkRunIndex;

        internal static readonly IResolver DefaultResolver = new CompositeResolver(EnvResolver.Instance, InfrastructureResolver.Instance);

        public static Summary Run(BenchmarkRunInfo[] benchmarkRunInfos, Func<Job, IToolchain> toolchainProvider)
        {
            var temp = benchmarkRunInfos.FirstOrDefault();
            var benchmarkRunInfo = new BenchmarkRunInfo(
                benchmarkRunInfos.SelectMany(i => i.Benchmarks).ToArray(),
                temp?.Type,
                temp?.Config);
            return Run(benchmarkRunInfo, toolchainProvider);
        }

        public static Summary Run(BenchmarkRunInfo benchmarkRunInfo, Func<Job, IToolchain> toolchainProvider)
        {
            var resolver = DefaultResolver;
            var benchmarks = benchmarkRunInfo.Benchmarks;
            var config = benchmarkRunInfo.Config;

            var title = GetTitle(benchmarks);
            var rootArtifactsFolderPath = (config?.ArtifactsPath ?? DefaultConfig.Instance.ArtifactsPath).CreateIfNotExists();

            using (var logStreamWriter = Portability.StreamWriter.FromPath(Path.Combine(rootArtifactsFolderPath, title + ".log")))
            {
                var logger = new CompositeLogger(config.GetCompositeLogger(), new StreamLogger(logStreamWriter));
                benchmarks = GetSupportedBenchmarks(benchmarks, logger, toolchainProvider, resolver);
                var artifactsToCleanup = new List<string>();

                try
                {
                    var runInfo = new BenchmarkRunInfo(benchmarks, benchmarkRunInfo.Type, config);
                    return Run(runInfo, logger, title, rootArtifactsFolderPath, toolchainProvider, resolver, artifactsToCleanup);
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
            // few types might have the same name: A.Name and B.Name will both report "Name"
            // in that case, we can not use the type name as file name because they would be getting overwritten #529
            var typeNames = benchmarks.Select(b => b.Target.Type).Distinct().GroupBy(type => type.Name);

            if (typeNames.Count() == 1 && typeNames.Single().Count() == 1)
                return FolderNameHelper.ToFolderName(benchmarks.Select(b => b.Target.Type).First());

            benchmarkRunIndex++;
            return $"BenchmarkRun-{benchmarkRunIndex:##000}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}";
        }

        public static Summary Run(BenchmarkRunInfo benchmarkRunInfo, ILogger logger, string title, string rootArtifactsFolderPath, Func<Job, IToolchain> toolchainProvider, IResolver resolver, List<string> artifactsToCleanup)
        {
            var benchmarks = benchmarkRunInfo.Benchmarks;
            var config = benchmarkRunInfo.Config;

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

            var buildResults = BuildInParallel(logger, rootArtifactsFolderPath, toolchainProvider, resolver, benchmarks, config, ref globalChronometer);

            foreach (var benchmark in benchmarks)
            {
                var buildResult = buildResults[benchmark];

                if (!config.KeepBenchmarkFiles)
                    artifactsToCleanup.AddRange(buildResult.ArtifactsToCleanup);

                if (buildResult.IsBuildSuccess)
                {
                    var report = RunCore(benchmark, logger, config, rootArtifactsFolderPath, toolchainProvider, resolver, buildResult);
                    if (report.AllMeasurements.Any(m => m.Operations == 0))
                        throw new InvalidOperationException("An iteration with 'Operations == 0' detected");
                    reports.Add(report);
                    if (report.GetResultRuns().Any())
                        logger.WriteLineStatistic(report.GetResultRuns().GetStatistics().ToTimeStr());
                }
                else
                {
                    reports.Add(new BenchmarkReport(benchmark, buildResult, buildResult, null, null, default));

                    if (buildResult.GenerateException != null)
                        logger.WriteLineError($"// Generate Exception: {buildResult.GenerateException.Message}");
                    if (buildResult.BuildException != null)
                        logger.WriteLineError($"// Build Exception: {buildResult.BuildException.Message}");
                }

                logger.WriteLine();
            }
            var clockSpan = globalChronometer.GetElapsed();

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
                    logger.WriteLineError("There are not any results runs");
                else
                    logger.WriteLineStatistic(resultRuns.GetStatistics().ToTimeStr(calcHistogram: true));
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

        private static Dictionary<Benchmark, BuildResult> BuildInParallel(ILogger logger, string rootArtifactsFolderPath, Func<Job, IToolchain> toolchainProvider, IResolver resolver, Benchmark[] benchmarks, ReadOnlyConfig config, ref StartedClock globalChronometer)
        {
            using (benchmarks.Select(benchmark => GetAssemblyResolveHelper(toolchainProvider(benchmark.Job), logger)).FirstOrDefault(helper => helper != null))
            {
                logger.WriteLineHeader($"// ***** Building {benchmarks.Length} benchmark(s) in Parallel: Start   *****");

                var buildResults = benchmarks
                    .AsParallel()
                    .Select(benchmark => { return (benchmark, buildResult: Build(benchmark, config, rootArtifactsFolderPath, toolchainProvider, resolver)); })
                    .ToDictionary(result => result.benchmark, result => result.buildResult);

                logger.WriteLineHeader($"// ***** Done, took {globalChronometer.GetElapsed().GetTimeSpan().ToFormattedTotalTime()}   *****");

                return buildResults;
            }
        }

        private static BuildResult Build(Benchmark benchmark, ReadOnlyConfig config, string rootArtifactsFolderPath, Func<Job, IToolchain> toolchainProvider, IResolver resolver)
        {
            var toolchain = toolchainProvider(benchmark.Job);

            var generateResult = toolchain.Generator.GenerateProject(benchmark, NullLogger.Instance, rootArtifactsFolderPath, config, resolver);

            try
            {
                if (!generateResult.IsGenerateSuccess)
                    return BuildResult.Failure(generateResult);

                return toolchain.Builder.Build(generateResult, NullLogger.Instance, benchmark, resolver);

            }
            catch (Exception e)
            {
                return BuildResult.Failure(generateResult, e);
            }
        }

        private static BenchmarkReport RunCore(Benchmark benchmark, ILogger logger, ReadOnlyConfig config, string rootArtifactsFolderPath, Func<Job, IToolchain> toolchainProvider, IResolver resolver, BuildResult buildResult)
        {
            var toolchain = toolchainProvider(benchmark.Job);

            logger.WriteLineHeader("// **************************");
            logger.WriteLineHeader("// Benchmark: " + benchmark.DisplayInfo);

            var (executeResults, gcStats) = Execute(logger, benchmark, toolchain, buildResult, config, resolver);

            var runs = new List<Measurement>();

            for (int index = 0; index < executeResults.Count; index++)
            {
                var executeResult = executeResults[index];
                runs.AddRange(executeResult.Data.Select(line => Measurement.Parse(logger, line, index + 1)).Where(r => r.IterationMode != IterationMode.Unknown));
            }

            return new BenchmarkReport(benchmark, buildResult, buildResult, executeResults, runs, gcStats);
        }

        private static (List<ExecuteResult> executeResults, GcStats gcStats) Execute(
            ILogger logger, Benchmark benchmark, IToolchain toolchain, BuildResult buildResult, IConfig config, IResolver resolver)
        {
            var executeResults = new List<ExecuteResult>();
            var gcStats = default(GcStats);

            logger.WriteLineInfo("// *** Execute ***");
            bool analyzeRunToRunVariance = benchmark.Job.ResolveValue(AccuracyMode.AnalyzeLaunchVarianceCharacteristic, resolver);
            bool autoLaunchCount = !benchmark.Job.HasValue(RunMode.LaunchCountCharacteristic);
            int defaultValue = analyzeRunToRunVariance ? 2 : 1;
            int launchCount = Math.Max(
                1,
                autoLaunchCount ? defaultValue : benchmark.Job.Run.LaunchCount);

            var noOverheadCompositeDiagnoser = config.GetCompositeDiagnoser(benchmark, Diagnosers.RunMode.NoOverhead);

            for (int launchIndex = 1; launchIndex <= launchCount; launchIndex++)
            {
                string printedLaunchCount = (analyzeRunToRunVariance && autoLaunchCount && launchIndex <= 2)
                    ? ""
                    : " / " + launchCount;
                logger.WriteLineInfo($"// Launch: {launchIndex}{printedLaunchCount}");

                // use diagnoser only for the last run (we need single result, not many)
                bool useDiagnoser = launchIndex == launchCount && noOverheadCompositeDiagnoser != null;

                var executeResult = toolchain.Executor.Execute(
                    new ExecuteParameters(
                        buildResult,
                        benchmark,
                        logger,
                        resolver,
                        config,
                        useDiagnoser ? noOverheadCompositeDiagnoser : null));

                if (!executeResult.FoundExecutable)
                    logger.WriteLineError($"Executable {buildResult.ArtifactsPaths.ExecutablePath} not found");
                if (executeResult.ExitCode != 0)
                    logger.WriteLineError("ExitCode != 0");

                executeResults.Add(executeResult);

                var measurements = executeResults
                    .SelectMany(r => r.Data)
                    .Select(line => Measurement.Parse(logger, line, 0))
                    .Where(r => r.IterationMode != IterationMode.Unknown)
                    .ToArray();

                if (!measurements.Any())
                {
                    // Something went wrong during the benchmark, don't bother doing more runs
                    logger.WriteLineError("No more Benchmark runs will be launched as NO measurements were obtained from the previous run!");
                    break;
                }

                if (useDiagnoser)
                {
                    if (config.HasMemoryDiagnoser())
                        gcStats = GcStats.Parse(executeResult.Data.Last());

                    noOverheadCompositeDiagnoser.ProcessResults(
                        new DiagnoserResults(benchmark, measurements.Where(measurement => !measurement.IterationMode.IsIdle()).Sum(m => m.Operations), gcStats));
                }

                if (autoLaunchCount && launchIndex == 2 && analyzeRunToRunVariance)
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
            var extraRunCompositeDiagnoser = config.GetCompositeDiagnoser(benchmark, Diagnosers.RunMode.ExtraRun);
            if (extraRunCompositeDiagnoser != null)
            {
                logger.WriteLineInfo("// Run, Diagnostic");

                var executeResult = toolchain.Executor.Execute(
                    new ExecuteParameters(buildResult, benchmark, logger, resolver, config, extraRunCompositeDiagnoser));

                var allRuns = executeResult.Data.Select(line => Measurement.Parse(logger, line, 0)).Where(r => r.IterationMode != IterationMode.Unknown).ToList();

                extraRunCompositeDiagnoser.ProcessResults(
                    new DiagnoserResults(benchmark, allRuns.Where(measurement => !measurement.IterationMode.IsIdle()).Sum(m => m.Operations), gcStats));

                if (!executeResult.FoundExecutable)
                    logger.WriteLineError("Executable not found");
                logger.WriteLine();
            }

            var separateLogicCompositeDiagnoser = config.GetCompositeDiagnoser(benchmark, Diagnosers.RunMode.SeparateLogic);
            if (separateLogicCompositeDiagnoser != null)
            {
                logger.WriteLineInfo("// Run, Diagnostic [SeparateLogic]");

                separateLogicCompositeDiagnoser.Handle(HostSignal.SeparateLogic, new DiagnoserActionParameters(null, benchmark, config));
            }

            return (executeResults, gcStats);
        }

        internal static void LogTotalTime(ILogger logger, TimeSpan time, string message = "Total time") => logger.WriteLineStatistic($"{message}: {time.ToFormattedTotalTime()}");

        private static Benchmark[] GetSupportedBenchmarks(IList<Benchmark> benchmarks, CompositeLogger logger, Func<Job, IToolchain> toolchainProvider, IResolver resolver)
            => benchmarks.Where(benchmark => toolchainProvider(benchmark.Job).IsSupported(benchmark, logger, resolver)).ToArray();

        private static string GetResultsFolderPath(string rootArtifactsFolderPath) => Path.Combine(rootArtifactsFolderPath, "results").CreateIfNotExists();

        private static IDisposable GetAssemblyResolveHelper(IToolchain toolchain, ILogger logger)
        {
            if (RuntimeInformation.IsFullFramework 
                && !(toolchain is InProcessToolchain) // we don't want to mess with assembly loading when running benchmarks in the same process (could produce wrong results)
                && !RuntimeInformation.IsMono) // so far it was never an issue for Mono
            {
                return DirtyAssemblyResolveHelper.Create(logger);
            }

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

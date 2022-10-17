using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using Perfolizer.Horology;
using RunMode = BenchmarkDotNet.Jobs.RunMode;

namespace BenchmarkDotNet.Running
{
    internal static class BenchmarkRunnerClean
    {
        internal const string DateTimeFormat = "yyyyMMdd-HHmmss";

        internal static readonly IResolver DefaultResolver = new CompositeResolver(EnvironmentResolver.Instance, InfrastructureResolver.Instance);

        internal static Summary[] Run(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            var resolver = DefaultResolver;
            var artifactsToCleanup = new List<string>();

            var title = GetTitle(benchmarkRunInfos);
            var rootArtifactsFolderPath = GetRootArtifactsFolderPath(benchmarkRunInfos);
            var resultsFolderPath = GetResultsFolderPath(rootArtifactsFolderPath, benchmarkRunInfos);
            var logFilePath = Path.Combine(rootArtifactsFolderPath, title + ".log");

            using (var streamLogger = new StreamLogger(GetLogFileStreamWriter(benchmarkRunInfos, logFilePath)))
            {
                var compositeLogger = CreateCompositeLogger(benchmarkRunInfos, streamLogger);

                compositeLogger.WriteLineInfo("// Validating benchmarks:");

                var (supportedBenchmarks, validationErrors) = GetSupportedBenchmarks(benchmarkRunInfos, compositeLogger, resolver);

                validationErrors.AddRange(Validate(supportedBenchmarks));

                PrintValidationErrors(compositeLogger, validationErrors);

                if (validationErrors.Any(validationError => validationError.IsCritical))
                    return new[] { Summary.ValidationFailed(title, resultsFolderPath, logFilePath, validationErrors.ToImmutableArray()) };

                if (!supportedBenchmarks.Any(benchmarks => benchmarks.BenchmarksCases.Any()))
                    return new[] { Summary.ValidationFailed(title, resultsFolderPath, logFilePath) };

                int totalBenchmarkCount = supportedBenchmarks.Sum(benchmarkInfo => benchmarkInfo.BenchmarksCases.Length);
                int benchmarksToRunCount = totalBenchmarkCount;
                compositeLogger.WriteLineHeader("// ***** BenchmarkRunner: Start   *****");
                compositeLogger.WriteLineHeader($"// ***** Found {totalBenchmarkCount} benchmark(s) in total *****");
                var globalChronometer = Chronometer.Start();

                var buildPartitions = BenchmarkPartitioner.CreateForBuild(supportedBenchmarks, resolver);
                var buildResults = BuildInParallel(compositeLogger, rootArtifactsFolderPath, buildPartitions, in globalChronometer);
                var allBuildsHaveFailed = buildResults.Values.All(buildResult => !buildResult.IsBuildSuccess);

                try
                {
                    var results = new List<Summary>();

                    var benchmarkToBuildResult = buildResults
                        .SelectMany(buildResult => buildResult.Key.Benchmarks.Select(buildInfo => (buildInfo.BenchmarkCase, buildInfo.Id, buildResult.Value)))
                        .ToDictionary(info => info.BenchmarkCase, info => (info.Id, info.Value));

                    // used to estimate finish time, in contrary to globalChronometer it does not include build time
                    var runsChronometer = Chronometer.Start();

                    foreach (var benchmarkRunInfo in supportedBenchmarks) // we run them in the old order now using the new build artifacts
                    {
                        var summary = Run(benchmarkRunInfo, benchmarkToBuildResult, resolver, compositeLogger, artifactsToCleanup,
                            resultsFolderPath, logFilePath, totalBenchmarkCount, in runsChronometer, ref benchmarksToRunCount);

                        if (!benchmarkRunInfo.Config.Options.IsSet(ConfigOptions.JoinSummary))
                            PrintSummary(compositeLogger, benchmarkRunInfo.Config, summary);

                        LogTotalTime(compositeLogger, summary.TotalTime, summary.GetNumberOfExecutedBenchmarks(), message: "Run time");
                        compositeLogger.WriteLine();

                        results.Add(summary);

                        if ((benchmarkRunInfo.Config.Options.IsSet(ConfigOptions.StopOnFirstError) && summary.Reports.Any(report => !report.Success)) || allBuildsHaveFailed)
                            break;
                    }

                    if (supportedBenchmarks.Any(b => b.Config.Options.IsSet(ConfigOptions.JoinSummary)))
                    {
                        var joinedSummary = Summary.Join(results, runsChronometer.GetElapsed());

                        PrintSummary(compositeLogger, supportedBenchmarks.First(b => b.Config.Options.IsSet(ConfigOptions.JoinSummary)).Config, joinedSummary);

                        results.Clear();
                        results.Add(joinedSummary);
                    }

                    var totalTime = globalChronometer.GetElapsed().GetTimeSpan();
                    int totalNumberOfExecutedBenchmarks = results.Sum(summary => summary.GetNumberOfExecutedBenchmarks());
                    LogTotalTime(compositeLogger, totalTime, totalNumberOfExecutedBenchmarks, "Global total time");

                    return results.ToArray();
                }
                finally
                {
                    // some benchmarks might be using parameters that have locking finalizers
                    // so we need to dispose them after we are done running the benchmarks
                    // see https://github.com/dotnet/BenchmarkDotNet/issues/1383 and https://github.com/dotnet/runtime/issues/314 for more
                    foreach (var benchmarkInfo in benchmarkRunInfos)
                    {
                        benchmarkInfo.Dispose();
                    }

                    compositeLogger.WriteLineHeader("// * Artifacts cleanup *");
                    Cleanup(new HashSet<string>(artifactsToCleanup.Distinct()));
                    compositeLogger.Flush();
                }
            }
        }

        private static Summary Run(BenchmarkRunInfo benchmarkRunInfo,
                                   Dictionary<BenchmarkCase, (BenchmarkId benchmarkId, BuildResult buildResult)> buildResults,
                                   IResolver resolver,
                                   ILogger logger,
                                   List<string> artifactsToCleanup,
                                   string resultsFolderPath,
                                   string logFilePath,
                                   int totalBenchmarkCount,
                                   in StartedClock runsChronometer,
                                   ref int benchmarksToRunCount)
        {
            var runStart = runsChronometer.GetElapsed();

            var benchmarks = benchmarkRunInfo.BenchmarksCases;
            var allBuildsHaveFailed = benchmarks.All(benchmark => !buildResults[benchmark].buildResult.IsBuildSuccess);
            var config = benchmarkRunInfo.Config;
            var cultureInfo = config.CultureInfo ?? DefaultCultureInfo.Instance;
            var reports = new List<BenchmarkReport>();
            string title = GetTitle(new[] { benchmarkRunInfo });
            var consoleTitle = RuntimeInformation.IsWindows() ? Console.Title : string.Empty;

            logger.WriteLineInfo($"// Found {benchmarks.Length} benchmarks:");
            foreach (var benchmark in benchmarks)
                logger.WriteLineInfo($"//   {benchmark.DisplayInfo}");
            logger.WriteLine();

            UpdateTitle(totalBenchmarkCount, benchmarksToRunCount);

            using (var powerManagementApplier = new PowerManagementApplier(logger))
            {
                bool stop = false;

                for (int i = 0; i < benchmarks.Length && !stop; i++)
                {
                    var benchmark = benchmarks[i];

                    powerManagementApplier.ApplyPerformancePlan(benchmark.Job.Environment.PowerPlanMode
                        ?? benchmark.Job.ResolveValue(EnvironmentMode.PowerPlanModeCharacteristic, EnvironmentResolver.Instance).GetValueOrDefault());

                    var info = buildResults[benchmark];
                    var buildResult = info.buildResult;

                    if (buildResult.IsBuildSuccess)
                    {
                        if (!config.Options.IsSet(ConfigOptions.KeepBenchmarkFiles))
                            artifactsToCleanup.AddRange(buildResult.ArtifactsToCleanup);

                        var report = RunCore(benchmark, info.benchmarkId, logger, resolver, buildResult);
                        if (report.AllMeasurements.Any(m => m.Operations == 0))
                            throw new InvalidOperationException("An iteration with 'Operations == 0' detected");
                        reports.Add(report);
                        if (report.GetResultRuns().Any())
                        {
                            var statistics = report.GetResultRuns().GetStatistics();
                            var formatter = statistics.CreateNanosecondFormatter(cultureInfo);
                            logger.WriteLineStatistic(statistics.ToString(cultureInfo, formatter));
                        }

                        if (!report.Success && config.Options.IsSet(ConfigOptions.StopOnFirstError))
                        {
                            stop = true;
                        }
                    }
                    else
                    {
                        reports.Add(new BenchmarkReport(false, benchmark, buildResult, buildResult, default, default));

                        if (buildResult.GenerateException != null)
                            logger.WriteLineError($"// Generate Exception: {buildResult.GenerateException.Message}");
                        else if (!buildResult.IsBuildSuccess && buildResult.TryToExplainFailureReason(out string reason))
                            logger.WriteLineError($"// Build Error: {reason}");
                        else if (buildResult.ErrorMessage != null)
                            logger.WriteLineError($"// Build Error: {buildResult.ErrorMessage}");

                        if (!benchmark.Job.GetToolchain().IsInProcess)
                        {
                            logger.WriteLine();
                            logger.WriteLineError("// BenchmarkDotNet has failed to build the auto-generated boilerplate code.");
                            logger.WriteLineError($"// It can be found in {buildResult.ArtifactsPaths.BuildArtifactsDirectoryPath}");
                            logger.WriteLineError("// Please follow the troubleshooting guide: https://benchmarkdotnet.org/articles/guides/troubleshooting.html");
                        }

                        if (config.Options.IsSet(ConfigOptions.StopOnFirstError) || allBuildsHaveFailed)
                        {
                            stop = true;
                        }
                    }

                    logger.WriteLine();

                    benchmarksToRunCount -= stop ? benchmarks.Length - i : 1;

                    LogProgress(logger, in runsChronometer, totalBenchmarkCount, benchmarksToRunCount);
                }
            }

            if (RuntimeInformation.IsWindows())
            {
                Console.Title = consoleTitle;
            }

            var runEnd = runsChronometer.GetElapsed();

            return new Summary(title,
                reports.ToImmutableArray(),
                HostEnvironmentInfo.GetCurrent(),
                resultsFolderPath,
                logFilePath,
                runEnd.GetTimeSpan() - runStart.GetTimeSpan(),
                cultureInfo,
                Validate(benchmarkRunInfo), // validate them once again, but don't print the output
                config.GetColumnHidingRules().ToImmutableArray());
        }

        private static void PrintSummary(ILogger logger, ImmutableConfig config, Summary summary)
        {
            var cultureInfo = config.CultureInfo ?? DefaultCultureInfo.Instance;

            logger.WriteLineHeader("// ***** BenchmarkRunner: Finish  *****");
            logger.WriteLine();

            logger.WriteLineHeader("// * Export *");
            string currentDirectory = Directory.GetCurrentDirectory();
            foreach (string file in config.GetCompositeExporter().ExportToFiles(summary, logger))
            {
                logger.WriteLineInfo($"  {file.GetBaseName(currentDirectory)}");
            }

            logger.WriteLine();

            logger.WriteLineHeader("// * Detailed results *");

            BenchmarkReportExporter.Default.ExportToLog(summary, logger);

            logger.WriteLineHeader("// * Summary *");
            MarkdownExporter.Console.ExportToLog(summary, logger);

            // TODO: make exporter
            ConclusionHelper.Print(logger, config.GetCompositeAnalyser().Analyse(summary).Distinct().ToList());

            if (config.ConfigAnalysisConclusion.Any())
            {
                logger.WriteLineHeader("// * Config Issues *");
                ConclusionHelper.Print(logger, config.ConfigAnalysisConclusion);
            }

            // TODO: move to conclusions
            var columnWithLegends = summary.Table.Columns.Where(c => c.NeedToShow && !string.IsNullOrEmpty(c.OriginalColumn.Legend)).Select(c => c.OriginalColumn).ToArray();

            bool needToShowTimeLegend = summary.Table.Columns.Any(c => c.NeedToShow && c.OriginalColumn.UnitType == UnitType.Time);
            var effectiveTimeUnit = needToShowTimeLegend ? summary.Table.EffectiveSummaryStyle.TimeUnit : null;

            if (columnWithLegends.Any() || effectiveTimeUnit != null)
            {
                logger.WriteLine();
                logger.WriteLineHeader("// * Legends *");
                int maxNameWidth = 0;
                if (columnWithLegends.Any())
                    maxNameWidth = Math.Max(maxNameWidth, columnWithLegends.Select(c => c.ColumnName.Length).Max());
                if (effectiveTimeUnit != null)
                    maxNameWidth = Math.Max(maxNameWidth, effectiveTimeUnit.Name.ToString(cultureInfo).Length + 2);

                foreach (var column in columnWithLegends)
                    logger.WriteLineHint($"  {column.ColumnName.PadRight(maxNameWidth, ' ')} : {column.Legend}");

                if (effectiveTimeUnit != null)
                    logger.WriteLineHint($"  {("1 " + effectiveTimeUnit.Name).PadRight(maxNameWidth, ' ')} :" +
                                         $" 1 {effectiveTimeUnit.Description} ({TimeUnit.Convert(1, effectiveTimeUnit, TimeUnit.Second).ToString("0.#########", summary.GetCultureInfo())} sec)");
            }

            if (config.GetDiagnosers().Any())
            {
                logger.WriteLine();
                config.GetCompositeDiagnoser().DisplayResults(logger);
            }

            logger.WriteLine();
            logger.WriteLineHeader("// ***** BenchmarkRunner: End *****");
        }

        private static ImmutableArray<ValidationError> Validate(params BenchmarkRunInfo[] benchmarks)
        {
            var validationErrors = new List<ValidationError>();

            if (benchmarks.Any(b => b.Config.Options.IsSet(ConfigOptions.JoinSummary)))
            {
                var joinedCases = benchmarks.SelectMany(b => b.BenchmarksCases).ToArray();

                validationErrors.AddRange(
                    ConfigCompatibilityValidator
                        .FailOnError
                        .Validate(new ValidationParameters(joinedCases, null))
                    );
            }

            foreach (var benchmarkRunInfo in benchmarks)
                validationErrors.AddRange(benchmarkRunInfo.Config.GetCompositeValidator().Validate(new ValidationParameters(benchmarkRunInfo.BenchmarksCases, benchmarkRunInfo.Config)));

            return validationErrors.ToImmutableArray();
        }

        private static Dictionary<BuildPartition, BuildResult> BuildInParallel(ILogger logger, string rootArtifactsFolderPath, BuildPartition[] buildPartitions, in StartedClock globalChronometer)
        {
            logger.WriteLineHeader($"// ***** Building {buildPartitions.Length} exe(s) in Parallel: Start   *****");

            var buildLogger = buildPartitions.Length == 1 ? logger : NullLogger.Instance; // when we have just one partition we can print to std out

            var beforeParallelBuild = globalChronometer.GetElapsed();

            var buildResults = buildPartitions
                .AsParallel()
                .Select(buildPartition => (buildPartition, buildResult: Build(buildPartition, rootArtifactsFolderPath, buildLogger)))
                .ToDictionary(result => result.buildPartition, result => result.buildResult);

            var afterParallelBuild = globalChronometer.GetElapsed();

            logger.WriteLineHeader($"// ***** Done, took {GetFormattedDifference(beforeParallelBuild, afterParallelBuild)}   *****");

            if (buildPartitions.Length <= 1 || !buildResults.Values.Any(result => result.IsGenerateSuccess && !result.IsBuildSuccess))
                return buildResults;

            logger.WriteLineHeader("// ***** Failed to build in Parallel, switching to sequential build   *****");

            foreach (var buildPartition in buildPartitions)
                if (buildResults[buildPartition].IsGenerateSuccess && !buildResults[buildPartition].IsBuildSuccess && !buildResults[buildPartition].TryToExplainFailureReason(out string _))
                    buildResults[buildPartition] = Build(buildPartition, rootArtifactsFolderPath, buildLogger);

            var afterSequentialBuild = globalChronometer.GetElapsed();

            logger.WriteLineHeader($"// ***** Done, took {GetFormattedDifference(afterParallelBuild, afterSequentialBuild)}   *****");

            return buildResults;

            static string GetFormattedDifference(ClockSpan before, ClockSpan after)
                => (after.GetTimeSpan() - before.GetTimeSpan()).ToFormattedTotalTime(DefaultCultureInfo.Instance);
        }

        private static BuildResult Build(BuildPartition buildPartition, string rootArtifactsFolderPath, ILogger buildLogger)
        {
            var toolchain = buildPartition.RepresentativeBenchmarkCase.GetToolchain(); // it's guaranteed that all the benchmarks in single partition have same toolchain

            var generateResult = toolchain.Generator.GenerateProject(buildPartition, buildLogger, rootArtifactsFolderPath);

            try
            {
                if (!generateResult.IsGenerateSuccess)
                    return BuildResult.Failure(generateResult, generateResult.GenerateException);

                return toolchain.Builder.Build(generateResult, buildPartition, buildLogger);
            }
            catch (Exception e)
            {
                return BuildResult.Failure(generateResult, e);
            }
        }

        private static BenchmarkReport RunCore(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, IResolver resolver, BuildResult buildResult)
        {
            var toolchain = benchmarkCase.GetToolchain();

            logger.WriteLineHeader("// **************************");
            logger.WriteLineHeader("// Benchmark: " + benchmarkCase.DisplayInfo);

            var (success, executeResults, metrics) = Execute(logger, benchmarkCase, benchmarkId, toolchain, buildResult, resolver);

            return new BenchmarkReport(success, benchmarkCase, buildResult, buildResult, executeResults, metrics);
        }

        private static (bool success, List<ExecuteResult> executeResults, List<Metric> metrics) Execute(
            ILogger logger, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, IToolchain toolchain, BuildResult buildResult, IResolver resolver)
        {
            var executeResults = new List<ExecuteResult>();
            var metrics = new List<Metric>();

            logger.WriteLineInfo("// *** Execute ***");
            bool analyzeRunToRunVariance = benchmarkCase.Job.ResolveValue(AccuracyMode.AnalyzeLaunchVarianceCharacteristic, resolver);
            bool autoLaunchCount = !benchmarkCase.Job.HasValue(RunMode.LaunchCountCharacteristic);
            int defaultValue = analyzeRunToRunVariance ? 2 : 1;
            int launchCount = Math.Max(
                1,
                autoLaunchCount ? defaultValue : benchmarkCase.Job.Run.LaunchCount);

            var noOverheadCompositeDiagnoser = benchmarkCase.Config.GetCompositeDiagnoser(benchmarkCase, Diagnosers.RunMode.NoOverhead);

            for (int launchIndex = 1; launchIndex <= launchCount; launchIndex++)
            {
                string printedLaunchCount = analyzeRunToRunVariance && autoLaunchCount && launchIndex <= 2
                    ? ""
                    : " / " + launchCount;
                logger.WriteLineInfo($"// Launch: {launchIndex}{printedLaunchCount}");

                // use diagnoser only for the last run (we need single result, not many)
                bool useDiagnoser = launchIndex == launchCount && noOverheadCompositeDiagnoser != null;

                var executeResult = RunExecute(
                    logger,
                    benchmarkCase,
                    benchmarkId,
                    toolchain,
                    buildResult,
                    resolver,
                    useDiagnoser ? noOverheadCompositeDiagnoser : null,
                    launchIndex);

                executeResults.Add(executeResult);

                if (!executeResult.IsSuccess)
                {
                    return (false, executeResults, metrics);
                }

                var measurements = executeResult.Measurements;

                if (useDiagnoser)
                {
                    metrics.AddRange(noOverheadCompositeDiagnoser.ProcessResults(new DiagnoserResults(benchmarkCase, executeResult, buildResult)));
                }

                if (autoLaunchCount && launchIndex == 2 && analyzeRunToRunVariance)
                {
                    // TODO: improve this logic
                    double overheadApprox = new Statistics(measurements.Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual)).Select(m => m.Nanoseconds)).Median;
                    double workloadApprox = new Statistics(measurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).Select(m => m.Nanoseconds)).Median;
                    double percent = overheadApprox / workloadApprox * 100;
                    launchCount = (int)Math.Round(Math.Max(2, 2 + (percent - 1) / 3)); // an empirical formula
                }
            }
            logger.WriteLine();

            // Do a "Diagnostic" run, but DISCARD the results, so that the overhead of Diagnostics doesn't skew the overall results
            var extraRunCompositeDiagnoser = benchmarkCase.Config.GetCompositeDiagnoser(benchmarkCase, Diagnosers.RunMode.ExtraRun);
            if (extraRunCompositeDiagnoser != null)
            {
                logger.WriteLineInfo("// Run, Diagnostic");

                var executeResult = RunExecute(
                    logger,
                    benchmarkCase,
                    benchmarkId,
                    toolchain,
                    buildResult,
                    resolver,
                    extraRunCompositeDiagnoser,
                    launchCount + 1);

                if (executeResult.IsSuccess)
                {
                    metrics.AddRange(extraRunCompositeDiagnoser.ProcessResults(new DiagnoserResults(benchmarkCase, executeResult, buildResult)));
                }

                logger.WriteLine();
            }

            var separateLogicCompositeDiagnoser = benchmarkCase.Config.GetCompositeDiagnoser(benchmarkCase, Diagnosers.RunMode.SeparateLogic);
            if (separateLogicCompositeDiagnoser != null)
            {
                logger.WriteLineInfo("// Run, Diagnostic [SeparateLogic]");

                separateLogicCompositeDiagnoser.Handle(HostSignal.SeparateLogic, new DiagnoserActionParameters(null, benchmarkCase, benchmarkId));
            }

            return (true, executeResults, metrics);
        }

        private static ExecuteResult RunExecute(ILogger logger, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, IToolchain toolchain,
            BuildResult buildResult, IResolver resolver, IDiagnoser diagnoser, int launchIndex)
        {
            var executeResult = toolchain.Executor.Execute(
                new ExecuteParameters(
                    buildResult,
                    benchmarkCase,
                    benchmarkId,
                    logger,
                    resolver,
                    launchIndex,
                    diagnoser));

            if (!executeResult.IsSuccess)
            {
                executeResult.LogIssues(logger, buildResult);
            }

            if (executeResult.ProcessId.HasValue)
            {
                if (executeResult.ExitCode is int exitCode)
                {
                    logger.WriteLineInfo($"// Benchmark Process {executeResult.ProcessId} has exited with code {exitCode}.");
                }
                else
                {
                    logger.WriteLineInfo($"// Benchmark Process {executeResult.ProcessId} failed to exit.");
                }
            }

            return executeResult;
        }

        private static void LogTotalTime(ILogger logger, TimeSpan time, int executedBenchmarksCount, string message = "Total time")
            => logger.WriteLineStatistic($"{message}: {time.ToFormattedTotalTime(DefaultCultureInfo.Instance)}, executed benchmarks: {executedBenchmarksCount}");

        private static (BenchmarkRunInfo[], List<ValidationError>) GetSupportedBenchmarks(BenchmarkRunInfo[] benchmarkRunInfos, ILogger logger, IResolver resolver)
        {
            List<ValidationError> validationErrors = new ();

            var benchmarksRunInfo = benchmarkRunInfos.Select(info => new BenchmarkRunInfo(
                    info.BenchmarksCases.Where(benchmark =>
                    {
                        var errors = benchmark.GetToolchain().Validate(benchmark, resolver).ToArray();
                        validationErrors.AddRange(errors);
                        return !errors.Any();
                    }).ToArray(),
                    info.Type,
                    info.Config))
                .Where(infos => infos.BenchmarksCases.Any())
                .ToArray();

            return (benchmarkRunInfos, validationErrors);
        }

        private static string GetRootArtifactsFolderPath(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            var defaultPath = DefaultConfig.Instance.ArtifactsPath;

            var customPath = benchmarkRunInfos
                .Where(benchmark => !string.IsNullOrEmpty(benchmark.Config.ArtifactsPath) && benchmark.Config.ArtifactsPath != defaultPath)
                .Select(benchmark => benchmark.Config.ArtifactsPath)
                .Distinct()
                .SingleOrDefault();

            return customPath != default ? customPath.CreateIfNotExists() : defaultPath;
        }

        private static string GetTitle(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            // few types might have the same name: A.Name and B.Name will both report "Name"
            // in that case, we can not use the type name as file name because they would be getting overwritten #529
            var uniqueTargetTypes = benchmarkRunInfos.SelectMany(info => info.BenchmarksCases.Select(benchmark => benchmark.Descriptor.Type)).Distinct().ToArray();

            var fileNamePrefix = (uniqueTargetTypes.Length == 1)
                ? FolderNameHelper.ToFolderName(uniqueTargetTypes[0])
                : "BenchmarkRun";

            return $"{fileNamePrefix}-{DateTime.Now.ToString(DateTimeFormat)}";
        }

        private static string GetResultsFolderPath(string rootArtifactsFolderPath, BenchmarkRunInfo[] benchmarkRunInfos)
        {
            if (benchmarkRunInfos.Any(info => info.Config.Options.IsSet(ConfigOptions.DontOverwriteResults)))
                return Path.Combine(rootArtifactsFolderPath, DateTime.Now.ToString(DateTimeFormat)).CreateIfNotExists();

            return Path.Combine(rootArtifactsFolderPath, "results").CreateIfNotExists();
        }

        private static StreamWriter GetLogFileStreamWriter(BenchmarkRunInfo[] benchmarkRunInfos, string logFilePath)
        {
            if (benchmarkRunInfos.Any(info => info.Config.Options.IsSet(ConfigOptions.DisableLogFile)))
                return StreamWriter.Null;

            return new StreamWriter(logFilePath, append: false);
        }

        private static ILogger CreateCompositeLogger(BenchmarkRunInfo[] benchmarkRunInfos, StreamLogger streamLogger)
        {
            var loggers = new Dictionary<string, ILogger>();

            void AddLogger(ILogger logger)
            {
                if (!loggers.ContainsKey(logger.Id) || loggers[logger.Id].Priority < logger.Priority)
                    loggers[logger.Id] = logger;
            }

            foreach (var benchmarkRunInfo in benchmarkRunInfos)
                foreach (var logger in benchmarkRunInfo.Config.GetLoggers())
                    AddLogger(logger);

            AddLogger(streamLogger);

            return new CompositeLogger(loggers.Values.ToImmutableHashSet());
        }

        private static void Cleanup(HashSet<string> artifactsToCleanup)
        {
            foreach (string path in artifactsToCleanup)
            {
                try
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
                catch
                {
                    // sth is locking our auto-generated files
                    // there is very little we can do about it
                }
            }
        }

        private static void UpdateTitle(int totalBenchmarkCount, int benchmarksToRunCount)
        {
            if (!Console.IsOutputRedirected && (RuntimeInformation.IsWindows() || RuntimeInformation.IsLinux() || RuntimeInformation.IsMacOSX()))
            {
                Console.Title = $"{benchmarksToRunCount}/{totalBenchmarkCount} Remaining";
            }
        }

        private static void LogProgress(ILogger logger, in StartedClock runsChronometer, int totalBenchmarkCount, int benchmarksToRunCount)
        {
            int executedBenchmarkCount = totalBenchmarkCount - benchmarksToRunCount;
            TimeSpan fromNow = GetEstimatedFinishTime(runsChronometer, benchmarksToRunCount, executedBenchmarkCount);
            DateTime estimatedEnd = DateTime.Now.Add(fromNow);
            string message = $"// ** Remained {benchmarksToRunCount} ({(double)benchmarksToRunCount / totalBenchmarkCount:P1}) benchmark(s) to run." +
                $" Estimated finish {estimatedEnd:yyyy-MM-dd H:mm} ({(int)fromNow.TotalHours}h {fromNow.Minutes}m from now) **";
            logger.WriteLineHeader(message);

            if (!Console.IsOutputRedirected && (RuntimeInformation.IsWindows() || RuntimeInformation.IsLinux() || RuntimeInformation.IsMacOSX()))
            {
                Console.Title = $"{benchmarksToRunCount}/{totalBenchmarkCount} Remaining - {(int)fromNow.TotalHours}h {fromNow.Minutes}m to finish";
            }
        }

        private static TimeSpan GetEstimatedFinishTime(in StartedClock runsChronometer, int benchmarksToRunCount, int executedBenchmarkCount)
        {
            double avgSecondsPerBenchmark = executedBenchmarkCount > 0 ? runsChronometer.GetElapsed().GetTimeSpan().TotalSeconds / executedBenchmarkCount : 0;
            TimeSpan fromNow = TimeSpan.FromSeconds(avgSecondsPerBenchmark * benchmarksToRunCount);
            return fromNow;
        }

        private static void PrintValidationErrors(ILogger logger, IEnumerable<ValidationError> validationErrors)
        {
            foreach (var validationError in validationErrors.Distinct())
            {
                if (validationError.BenchmarkCase != null)
                {
                    logger.WriteLineInfo($"// Benchmark {validationError.BenchmarkCase.DisplayInfo}");
                }

                logger.WriteLineError($"//    * {validationError.Message}");
                logger.WriteLine();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Characteristics;
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

                var supportedBenchmarks = GetSupportedBenchmarks(benchmarkRunInfos, compositeLogger, resolver);
                if (!supportedBenchmarks.Any(benchmarks => benchmarks.BenchmarksCases.Any()))
                    return new[] { Summary.NothingToRun(title, resultsFolderPath, logFilePath) };

                var validationErrors = Validate(supportedBenchmarks, compositeLogger);
                if (validationErrors.Any(validationError => validationError.IsCritical))
                    return new[] { Summary.ValidationFailed(title, resultsFolderPath, logFilePath, validationErrors) };

                var benchmarksToRunCount = supportedBenchmarks.Sum(benchmarkInfo => benchmarkInfo.BenchmarksCases.Length);
                compositeLogger.WriteLineHeader("// ***** BenchmarkRunner: Start   *****");
                compositeLogger.WriteLineHeader($"// ***** Found {benchmarksToRunCount} benchmark(s) in total *****");
                var globalChronometer = Chronometer.Start();

                var buildPartitions = BenchmarkPartitioner.CreateForBuild(supportedBenchmarks, resolver);
                var buildResults = BuildInParallel(compositeLogger, rootArtifactsFolderPath, buildPartitions, ref globalChronometer);
                var allBuildsHaveFailed = buildResults.Values.All(buildResult => !buildResult.IsBuildSuccess);

                try
                {
                    var results = new List<Summary>();

                    var benchmarkToBuildResult = buildResults
                        .SelectMany(buildResult => buildResult.Key.Benchmarks.Select(buildInfo => (buildInfo.BenchmarkCase, buildInfo.Id, buildResult.Value)))
                        .ToDictionary(info => info.BenchmarkCase, info => (info.Id, info.Value));

                    foreach (var benchmarkRunInfo in supportedBenchmarks) // we run them in the old order now using the new build artifacts
                    {
                        var runChronometer = Chronometer.Start();

                        var summary = Run(benchmarkRunInfo, benchmarkToBuildResult, resolver, compositeLogger, artifactsToCleanup, resultsFolderPath, logFilePath, ref runChronometer);

                        if (!benchmarkRunInfo.Config.Options.IsSet(ConfigOptions.JoinSummary))
                            PrintSummary(compositeLogger, benchmarkRunInfo.Config, summary);

                        benchmarksToRunCount -= benchmarkRunInfo.BenchmarksCases.Length;
                        compositeLogger.WriteLineHeader($"// ** Remained {benchmarksToRunCount} benchmark(s) to run **");
                        LogTotalTime(compositeLogger, runChronometer.GetElapsed().GetTimeSpan(), summary.GetNumberOfExecutedBenchmarks(), message: "Run time");
                        compositeLogger.WriteLine();

                        results.Add(summary);

                        if ((benchmarkRunInfo.Config.Options.IsSet(ConfigOptions.StopOnFirstError) && summary.Reports.Any(report => !report.Success)) || allBuildsHaveFailed)
                            break;
                    }

                    if (supportedBenchmarks.Any(b => b.Config.Options.IsSet(ConfigOptions.JoinSummary)))
                    {
                        var joinedSummary = Summary.Join(results, globalChronometer.GetElapsed());

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
                                   ref StartedClock runChronometer)
        {
            var benchmarks = benchmarkRunInfo.BenchmarksCases;
            var allBuildsHaveFailed = benchmarks.All(benchmark => !buildResults[benchmark].buildResult.IsBuildSuccess);
            var config = benchmarkRunInfo.Config;
            var cultureInfo = config.CultureInfo ?? DefaultCultureInfo.Instance;
            var reports = new List<BenchmarkReport>();
            string title = GetTitle(new[] { benchmarkRunInfo });

            logger.WriteLineInfo($"// Found {benchmarks.Length} benchmarks:");
            foreach (var benchmark in benchmarks)
                logger.WriteLineInfo($"//   {benchmark.DisplayInfo}");
            logger.WriteLine();

            using (var powerManagementApplier = new PowerManagementApplier(logger))
            {
                foreach (var benchmark in benchmarks)
                {
                    powerManagementApplier.ApplyPerformancePlan(benchmark.Job.Environment.PowerPlanMode);

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
                            break;
                    }
                    else
                    {
                        reports.Add(new BenchmarkReport(false, benchmark, buildResult, buildResult, default, default, default, default));

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
                            break;
                    }

                    logger.WriteLine();
                }
            }

            var clockSpan = runChronometer.GetElapsed();

            return new Summary(title,
                reports.ToImmutableArray(),
                HostEnvironmentInfo.GetCurrent(),
                resultsFolderPath,
                logFilePath,
                clockSpan.GetTimeSpan(),
                cultureInfo,
                Validate(new[] {benchmarkRunInfo }, NullLogger.Instance)); // validate them once again, but don't print the output
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

        private static ImmutableArray<ValidationError> Validate(BenchmarkRunInfo[] benchmarks, ILogger logger)
        {
            logger.WriteLineInfo("// Validating benchmarks:");

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

            foreach (var validationError in validationErrors.Distinct())
                logger.WriteLineError(validationError.Message);

            return validationErrors.ToImmutableArray();
        }

        private static Dictionary<BuildPartition, BuildResult> BuildInParallel(ILogger logger, string rootArtifactsFolderPath, BuildPartition[] buildPartitions, ref StartedClock globalChronometer)
        {
            logger.WriteLineHeader($"// ***** Building {buildPartitions.Length} exe(s) in Parallel: Start   *****");

            var buildLogger = buildPartitions.Length == 1 ? logger : NullLogger.Instance; // when we have just one partition we can print to std out

            var buildResults = buildPartitions
                .AsParallel()
                .Select(buildPartition => (buildPartition, buildResult: Build(buildPartition, rootArtifactsFolderPath, buildLogger)))
                .ToDictionary(result => result.buildPartition, result => result.buildResult);

            logger.WriteLineHeader($"// ***** Done, took {globalChronometer.GetElapsed().GetTimeSpan().ToFormattedTotalTime(DefaultCultureInfo.Instance)}   *****");

            if (buildPartitions.Length <= 1 || !buildResults.Values.Any(result => result.IsGenerateSuccess && !result.IsBuildSuccess))
                return buildResults;

            logger.WriteLineHeader("// ***** Failed to build in Parallel, switching to sequential build   *****");

            foreach (var buildPartition in buildPartitions)
                if (buildResults[buildPartition].IsGenerateSuccess && !buildResults[buildPartition].IsBuildSuccess && !buildResults[buildPartition].TryToExplainFailureReason(out string _))
                    buildResults[buildPartition] = Build(buildPartition, rootArtifactsFolderPath, buildLogger);

            logger.WriteLineHeader($"// ***** Done, took {globalChronometer.GetElapsed().GetTimeSpan().ToFormattedTotalTime(DefaultCultureInfo.Instance)}   *****");

            return buildResults;
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

            var (success, executeResults, gcStats, metrics) = Execute(logger, benchmarkCase, benchmarkId, toolchain, buildResult, resolver);

            var runs = new List<Measurement>();

            for (int index = 0; index < executeResults.Count; index++)
            {
                int currentIndex = index;
                var executeResult = executeResults[index];
                runs.AddRange(executeResult.Data.Where(line => !string.IsNullOrEmpty(line)).Select(line => Measurement.Parse(logger, line, currentIndex + 1)).Where(r => r.IterationMode != IterationMode.Unknown));
            }

            return new BenchmarkReport(success, benchmarkCase, buildResult, buildResult, executeResults, runs, gcStats, metrics);
        }

        private static (bool success, List<ExecuteResult> executeResults, GcStats gcStats, List<Metric> metrics) Execute(ILogger logger, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, IToolchain toolchain,
            BuildResult buildResult, IResolver resolver)
        {
            var success = true;
            var executeResults = new List<ExecuteResult>();
            var gcStats = default(GcStats);
            var threadingStats = default(ThreadingStats);
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
                    ref success);

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

                executeResults.Add(executeResult);

                var errors = executeResults.SelectMany(r => r.Data)
                    .Union(executeResults.SelectMany(r => r.ExtraOutput))
                    .Where(line => line.StartsWith(ValidationErrorReporter.ConsoleErrorPrefix))
                    .Select(line => line.Substring(ValidationErrorReporter.ConsoleErrorPrefix.Length).Trim())
                    .ToArray();

                if (errors.Any())
                {
                    success = false;
                    foreach (string error in errors)
                        logger.WriteLineError(error);
                    break;
                }

                var measurements = executeResults
                    .SelectMany(r => r.Data)
                    .Where(line => !string.IsNullOrEmpty(line))
                    .Select(line => Measurement.Parse(logger, line, 0))
                    .Where(r => r.IterationMode != IterationMode.Unknown)
                    .ToArray();

                if (!measurements.Any())
                {
                    // Something went wrong during the benchmark, don't bother doing more runs
                    logger.WriteLineError("No more Benchmark runs will be launched as NO measurements were obtained from the previous run!");
                    success = false;
                    break;
                }

                if (useDiagnoser)
                {
                    if (benchmarkCase.Config.HasMemoryDiagnoser())
                        gcStats = GcStats.Parse(executeResult.Data.Last(line => !string.IsNullOrEmpty(line) && line.StartsWith(GcStats.ResultsLinePrefix)));

                    if (benchmarkCase.Config.HasThreadingDiagnoser())
                        threadingStats = ThreadingStats.Parse(executeResult.Data.Last(line => !string.IsNullOrEmpty(line) && line.StartsWith(ThreadingStats.ResultsLinePrefix)));

                    metrics.AddRange(
                        noOverheadCompositeDiagnoser.ProcessResults(
                            new DiagnoserResults(benchmarkCase, measurements.Where(measurement => measurement.IsWorkload()).Sum(m => m.Operations), gcStats, threadingStats, buildResult)));
                }

                if (autoLaunchCount && launchIndex == 2 && analyzeRunToRunVariance)
                {
                    // TODO: improve this logic
                    double overheadApprox = new Statistics(measurements.Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual)).Select(m => m.Nanoseconds)).Median;
                    double workloadApprox = new Statistics(measurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).Select(m => m.Nanoseconds)).Median;
                    double percent = overheadApprox / workloadApprox * 100;
                    launchCount = (int)Math.Round(Math.Max(2, 2 + (percent - 1) / 3)); // an empirical formula
                }

                if (!success && benchmarkCase.Config.Options.IsSet(ConfigOptions.StopOnFirstError))
                {
                    break;
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
                    ref success);

                var allRuns = executeResult.Data.Where(line => !string.IsNullOrEmpty(line)).Select(line => Measurement.Parse(logger, line, 0)).Where(r => r.IterationMode != IterationMode.Unknown).ToList();

                metrics.AddRange(
                    extraRunCompositeDiagnoser.ProcessResults(
                        new DiagnoserResults(benchmarkCase, allRuns.Where(measurement => measurement.IsWorkload()).Sum(m => m.Operations), gcStats, threadingStats, buildResult)));

                logger.WriteLine();
            }

            var separateLogicCompositeDiagnoser = benchmarkCase.Config.GetCompositeDiagnoser(benchmarkCase, Diagnosers.RunMode.SeparateLogic);
            if (separateLogicCompositeDiagnoser != null)
            {
                logger.WriteLineInfo("// Run, Diagnostic [SeparateLogic]");

                separateLogicCompositeDiagnoser.Handle(HostSignal.SeparateLogic, new DiagnoserActionParameters(null, benchmarkCase, benchmarkId));
            }

            return (success, executeResults, gcStats, metrics);
        }

        private static ExecuteResult RunExecute(ILogger logger, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, IToolchain toolchain, BuildResult buildResult,
            IResolver resolver, IDiagnoser diagnoser, ref bool success)
        {
            var executeResult = toolchain.Executor.Execute(
                new ExecuteParameters(
                    buildResult,
                    benchmarkCase,
                    benchmarkId,
                    logger,
                    resolver,
                    diagnoser));

            if (!executeResult.FoundExecutable)
            {
                success = false;
                logger.WriteLineError($"Executable {buildResult.ArtifactsPaths.ExecutablePath} not found");
            }

            // exit code can be different than 0 if the process has hanged at the end
            // so we check if some results were reported, if not then it was a failure
            if (executeResult.ExitCode != 0 && executeResult.Data.IsEmpty())
            {
                success = false;
                logger.WriteLineError("ExitCode != 0 and no results reported");
            }

            return executeResult;
        }

        private static void LogTotalTime(ILogger logger, TimeSpan time, int executedBenchmarksCount, string message = "Total time")
            => logger.WriteLineStatistic($"{message}: {time.ToFormattedTotalTime(DefaultCultureInfo.Instance)}, executed benchmarks: {executedBenchmarksCount}");

        private static BenchmarkRunInfo[] GetSupportedBenchmarks(BenchmarkRunInfo[] benchmarkRunInfos, ILogger logger, IResolver resolver)
            => benchmarkRunInfos.Select(info => new BenchmarkRunInfo(
                    info.BenchmarksCases.Where(benchmark => benchmark.GetToolchain().IsSupported(benchmark, logger, resolver)).ToArray(),
                    info.Type,
                    info.Config))
                .Where(infos => infos.BenchmarksCases.Any())
                .ToArray();

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
    }
}

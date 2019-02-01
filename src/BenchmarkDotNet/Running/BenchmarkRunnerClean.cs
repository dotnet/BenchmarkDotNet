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
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using RunMode = BenchmarkDotNet.Jobs.RunMode;

namespace BenchmarkDotNet.Running
{
    internal static class BenchmarkRunnerClean
    {
        private static int benchmarkRunIndex;

        internal static readonly IResolver DefaultResolver = new CompositeResolver(EnvironmentResolver.Instance, InfrastructureResolver.Instance);

        internal static Summary[] Run(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            var resolver = DefaultResolver;
            var artifactsToCleanup = new List<string>();
            
            var title = GetTitle(benchmarkRunInfos);
            var rootArtifactsFolderPath = GetRootArtifactsFolderPath(benchmarkRunInfos);
            var resultsFolderPath = GetResultsFolderPath(rootArtifactsFolderPath);

            using (var streamLogger = new StreamLogger(new StreamWriter(Path.Combine(rootArtifactsFolderPath, title + ".log"), append: false)))
            {
                var compositeLogger = CreateCompositeLogger(benchmarkRunInfos, streamLogger);
                
                var supportedBenchmarks = GetSupportedBenchmarks(benchmarkRunInfos, compositeLogger, resolver);
                if (!supportedBenchmarks.Any(benchmarks => benchmarks.BenchmarksCases.Any()))
                    return new [] { Summary.NothingToRun(title, resultsFolderPath) };

                var validationErrors = Validate(supportedBenchmarks, compositeLogger);
                if (validationErrors.Any(validationError => validationError.IsCritical))
                    return new [] { Summary.ValidationFailed(title, resultsFolderPath, validationErrors) };

                compositeLogger.WriteLineHeader("// ***** BenchmarkRunner: Start   *****");
                var globalChronometer = Chronometer.Start();

                var buildPartitions = BenchmarkPartitioner.CreateForBuild(supportedBenchmarks, resolver);
                var buildResults = BuildInParallel(compositeLogger, rootArtifactsFolderPath, buildPartitions, ref globalChronometer);

                try
                {
                    var results = new List<Summary>();

                    var benchmarkToBuildResult = buildResults
                        .SelectMany(buildResult => buildResult.Key.Benchmarks.Select(buildInfo => (buildInfo.BenchmarkCase, buildInfo.Id, buildResult.Value)))
                        .ToDictionary(info => info.BenchmarkCase, info => (info.Id, info.Value));

                    foreach (var benchmarkRunInfo in supportedBenchmarks) // we run them in the old order now using the new build artifacts
                    {
                        var runChronometer = Chronometer.Start();
                        
                        var summary = Run(benchmarkRunInfo, benchmarkToBuildResult, resolver, compositeLogger, artifactsToCleanup, rootArtifactsFolderPath, ref runChronometer);
                        
                        if (benchmarkRunInfo.Config.SummaryPerType)
                            PrintSummary(compositeLogger, benchmarkRunInfo.Config, summary);
                        
                        LogTotalTime(compositeLogger, runChronometer.GetElapsed().GetTimeSpan(), summary.GetNumberOfExecutedBenchmarks(), message: "Run time");
                        compositeLogger.WriteLine();
                        
                        results.Add(summary);
                    }

                    if (supportedBenchmarks.Any(b => !b.Config.SummaryPerType))
                    {
                        var joinedSummary = Summary.Join(results, globalChronometer.GetElapsed());
                        
                        PrintSummary(compositeLogger, supportedBenchmarks.First(b => !b.Config.SummaryPerType).Config, joinedSummary);
                        
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
                    compositeLogger.WriteLineHeader("// * Artifacts cleanup *");
                    Cleanup(new HashSet<string>(artifactsToCleanup.Distinct()));
                }
            }
        }

        private static string GetTitle(BenchmarkRunInfo[] benchmarkRunInfos)
        {
            // few types might have the same name: A.Name and B.Name will both report "Name"
            // in that case, we can not use the type name as file name because they would be getting overwritten #529
            var uniqueTargetTypes = benchmarkRunInfos.SelectMany(info => info.BenchmarksCases.Select(benchmark => benchmark.Descriptor.Type)).Distinct().ToArray();

            if (uniqueTargetTypes.Length == 1)
                return FolderNameHelper.ToFolderName(uniqueTargetTypes[0]);

            benchmarkRunIndex++;

            return $"BenchmarkRun-{benchmarkRunIndex:##000}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}";
        }

        private static Summary Run(BenchmarkRunInfo benchmarkRunInfo, 
                                   Dictionary<BenchmarkCase, (BenchmarkId benchmarkId, BuildResult buildResult)> buildResults, 
                                   IResolver resolver,
                                   ILogger logger, 
                                   List<string> artifactsToCleanup, 
                                   string rootArtifactsFolderPath,
                                   ref StartedClock runChronometer)
        {
            var benchmarks = benchmarkRunInfo.BenchmarksCases;
            var config = benchmarkRunInfo.Config;
            var reports = new List<BenchmarkReport>();
            string title = GetTitle(new[] { benchmarkRunInfo });

            logger.WriteLineInfo("// Found benchmarks:");
            foreach (var benchmark in benchmarks)
                logger.WriteLineInfo($"//   {benchmark.DisplayInfo}");
            logger.WriteLine();
            foreach (var benchmark in benchmarks)
            {
                var info = buildResults[benchmark];
                var buildResult = info.buildResult;

                if (!config.KeepBenchmarkFiles)
                    artifactsToCleanup.AddRange(buildResult.ArtifactsToCleanup);

                bool success;
                if (buildResult.IsBuildSuccess)
                {
                    var report = RunCore(benchmark, info.benchmarkId, logger, resolver, buildResult);
                    if (report.AllMeasurements.Any(m => m.Operations == 0))
                        throw new InvalidOperationException("An iteration with 'Operations == 0' detected");
                    reports.Add(report);
                    if (report.GetResultRuns().Any())
                        logger.WriteLineStatistic(report.GetResultRuns().GetStatistics().ToTimeStr(config.Encoding));

                    success = report.Success;
                }
                else
                {
                    reports.Add(new BenchmarkReport(false, benchmark, buildResult, buildResult, default, default, default, default));

                    if (buildResult.GenerateException != null)
                        logger.WriteLineError($"// Generate Exception: {buildResult.GenerateException.Message}");
                    if (buildResult.ErrorMessage != null)
                        logger.WriteLineError($"// Build Error: {buildResult.ErrorMessage}");

                    success = true;
                }

                logger.WriteLine();

                if (!success && config.StopOnFirstError)
                {
                    break;
                }
            }
            
            var clockSpan = runChronometer.GetElapsed();

            return new Summary(title,
                reports.ToImmutableArray(),
                HostEnvironmentInfo.GetCurrent(),
                GetResultsFolderPath(rootArtifactsFolderPath),
                clockSpan.GetTimeSpan(), 
                Validate(new[] {benchmarkRunInfo }, NullLogger.Instance)); // validate them once again, but don't print the output
        }

        private static void PrintSummary(ILogger logger, ImmutableConfig config, Summary summary)
        {
            logger.WriteLineHeader("// ***** BenchmarkRunner: Finish  *****");
            logger.WriteLine();

            logger.WriteLineHeader("// * Export *");
            string currentDirectory = Directory.GetCurrentDirectory();
            foreach (string file in config.GetCompositeExporter().ExportToFiles(summary, logger))
            {
                logger.WriteLineInfo($"  {file.Replace(currentDirectory, string.Empty).Trim('/', '\\')}");
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
                    maxNameWidth = Math.Max(maxNameWidth, effectiveTimeUnit.Name.ToString(config.Encoding).Length + 2);

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
        }

        private static ImmutableArray<ValidationError> Validate(BenchmarkRunInfo[] benchmarks, ILogger logger)
        {
            logger.WriteLineInfo("// Validating benchmarks:");

            var validationErrors = new List<ValidationError>();

            foreach (var benchmarkRunInfo in benchmarks)
                validationErrors.AddRange(benchmarkRunInfo.Config.GetCompositeValidator().Validate(new ValidationParameters(benchmarkRunInfo.BenchmarksCases, benchmarkRunInfo.Config)));

            foreach (var validationError in validationErrors)
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

            logger.WriteLineHeader($"// ***** Done, took {globalChronometer.GetElapsed().GetTimeSpan().ToFormattedTotalTime()}   *****");

            if (buildPartitions.Length <= 1 || !buildResults.Values.Any(result => result.IsGenerateSuccess && !result.IsBuildSuccess))
                return buildResults;

            logger.WriteLineHeader("// ***** Failed to build in Parallel, switching to sequential build..   *****");

            foreach (var buildPartition in buildPartitions)
                if(buildResults[buildPartition].IsGenerateSuccess && !buildResults[buildPartition].IsBuildSuccess)
                    buildResults[buildPartition] = Build(buildPartition, rootArtifactsFolderPath, buildLogger);

            logger.WriteLineHeader($"// ***** Done, took {globalChronometer.GetElapsed().GetTimeSpan().ToFormattedTotalTime()}   *****");

            return buildResults;
        }

        private static BuildResult Build(BuildPartition buildPartition, string rootArtifactsFolderPath, ILogger buildLogger)
        {
            var toolchain = buildPartition.RepresentativeBenchmarkCase.Job.GetToolchain(); // it's guaranteed that all the benchmarks in single partition have same toolchain

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
            var toolchain = benchmarkCase.Job.GetToolchain();

            logger.WriteLineHeader("// **************************");
            logger.WriteLineHeader("// Benchmark: " + benchmarkCase.DisplayInfo);

            var (success, executeResults, gcStats, metrics) = Execute(logger, benchmarkCase, benchmarkId, toolchain, buildResult, resolver);

            var runs = new List<Measurement>();

            for (int index = 0; index < executeResults.Count; index++)
            {
                int currentIndex = index;
                var executeResult = executeResults[index];
                runs.AddRange(executeResult.Data.Select(line => Measurement.Parse(logger, line, currentIndex + 1)).Where(r => r.IterationMode != IterationMode.Unknown));
            }

            return new BenchmarkReport(success, benchmarkCase, buildResult, buildResult, executeResults, runs, gcStats, metrics);
        }

        private static (bool success, List<ExecuteResult> executeResults, GcStats gcStats, List<Metric> metrics) Execute(ILogger logger, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, IToolchain toolchain,
            BuildResult buildResult, IResolver resolver)
        {
            var success = true;
            var executeResults = new List<ExecuteResult>();
            var gcStats = default(GcStats);
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
                        gcStats = GcStats.Parse(executeResult.Data.Last(line => !string.IsNullOrEmpty(line)));
                    
                    metrics.AddRange(
                        noOverheadCompositeDiagnoser.ProcessResults(
                            new DiagnoserResults(benchmarkCase, measurements.Where(measurement => measurement.IsWorkload()).Sum(m => m.Operations), gcStats)));
                }

                if (autoLaunchCount && launchIndex == 2 && analyzeRunToRunVariance)
                {
                    // TODO: improve this logic
                    double overheadApprox = new Statistics(measurements.Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual)).Select(m => m.Nanoseconds)).Median;
                    double workloadApprox = new Statistics(measurements.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual)).Select(m => m.Nanoseconds)).Median;
                    double percent = overheadApprox / workloadApprox * 100;
                    launchCount = (int)Math.Round(Math.Max(2, 2 + (percent - 1) / 3)); // an empirical formula
                }

                if (!success && benchmarkCase.Config.StopOnFirstError)
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

                var allRuns = executeResult.Data.Select(line => Measurement.Parse(logger, line, 0)).Where(r => r.IterationMode != IterationMode.Unknown).ToList();

                metrics.AddRange(
                    extraRunCompositeDiagnoser.ProcessResults(
                        new DiagnoserResults(benchmarkCase, allRuns.Where(measurement => measurement.IsWorkload()).Sum(m => m.Operations), gcStats)));

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

            if (executeResult.ExitCode != 0)
            {
                success = false;
                logger.WriteLineError("ExitCode != 0");
            }

            return executeResult;
        }

        private static void LogTotalTime(ILogger logger, TimeSpan time, int executedBenchmarksCount, string message = "Total time")
            => logger.WriteLineStatistic($"{message}: {time.ToFormattedTotalTime()}, executed benchmarks: {executedBenchmarksCount}");

        private static BenchmarkRunInfo[] GetSupportedBenchmarks(BenchmarkRunInfo[] benchmarkRunInfos, ILogger logger, IResolver resolver)
            => benchmarkRunInfos.Select(info => new BenchmarkRunInfo(
                    info.BenchmarksCases.Where(benchmark => benchmark.Job.GetToolchain().IsSupported(benchmark, logger, resolver)).ToArray(),
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

        private static string GetResultsFolderPath(string rootArtifactsFolderPath) => Path.Combine(rootArtifactsFolderPath, "results").CreateIfNotExists();

        private static ILogger CreateCompositeLogger(BenchmarkRunInfo[] benchmarkRunInfos, StreamLogger streamLogger)
        {
            var builder = ImmutableHashSet.CreateBuilder<ILogger>();

            foreach (var benchmarkRunInfo in benchmarkRunInfos)
                foreach (var logger in benchmarkRunInfo.Config.GetLoggers())
                    if (!builder.Contains(logger))
                        builder.Add(logger);

            builder.Add(streamLogger);
            
            return new CompositeLogger(builder.ToImmutable());
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

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using JetBrains.Annotations;
using RunMode = BenchmarkDotNet.Jobs.RunMode;
using StreamWriter = BenchmarkDotNet.Portability.StreamWriter;

namespace BenchmarkDotNet.Running
{
    public static class BenchmarkRunner
    {
        private static int benchmarkRunIndex;

        internal static readonly IResolver DefaultResolver = new CompositeResolver(EnvironmentResolver.Instance, InfrastructureResolver.Instance);

        [PublicAPI] public static Summary Run<T>(IConfig config = null) => Run(BenchmarkConverter.TypeToBenchmarks(typeof(T), config));

        [PublicAPI] public static Summary Run(Type type, IConfig config = null) => Run(BenchmarkConverter.TypeToBenchmarks(type, config));

        [PublicAPI] public static Summary Run(Type type, MethodInfo[] methods, IConfig config = null) => Run(BenchmarkConverter.MethodsToBenchmarks(type, methods, config));

        [PublicAPI] public static Summary[] Run(Assembly assembly, IConfig config = null) 
            => Run(assembly.GetRunnableBenchmarks().Select(type => BenchmarkConverter.TypeToBenchmarks(type, config)).ToArray(), config);

        [PublicAPI] public static Summary RunUrl(string url, IConfig config = null)
        {
#if CLASSIC
            return Run(BenchmarkConverter.UrlToBenchmarks(url, config), config).Single();
#else
            throw new NotSupportedException();
#endif
        }

        [PublicAPI] public static Summary RunSource(string source, IConfig config = null)
        {
#if CLASSIC
            return Run(BenchmarkConverter.SourceToBenchmarks(source, config), config).Single();
#else
            throw new NotSupportedException();
#endif
        }

        [PublicAPI] public static Summary Run(BenchmarkRunInfo benchmarkRunInfo) => Run(new[] { benchmarkRunInfo }, benchmarkRunInfo.Config).Single();

        [PublicAPI] public static Summary[] Run(BenchmarkRunInfo[] benchmarkRunInfos, [CanBeNull] IConfig commonSettingsConfig)
        {
            if (commonSettingsConfig == null)
                commonSettingsConfig = DefaultConfig.Instance;
            var resolver = DefaultResolver;
            var artifactsToCleanup = new List<string>();
            string title = GetTitle(benchmarkRunInfos);

            string rootArtifactsFolderPath = (commonSettingsConfig?.ArtifactsPath ?? DefaultConfig.Instance.ArtifactsPath).CreateIfNotExists();

            using (var logStreamWriter = StreamWriter.FromPath(Path.Combine(rootArtifactsFolderPath, title + ".log")))
            {
                var logger = new CompositeLogger(commonSettingsConfig.GetCompositeLogger(), new StreamLogger(logStreamWriter));

                var supportedBenchmarks = GetSupportedBenchmarks(benchmarkRunInfos, logger, resolver);

                if (!supportedBenchmarks.Any(benchmarks => benchmarks.BenchmarksCases.Any()))
                    return  new [] { Summary.CreateFailed(
                        supportedBenchmarks.SelectMany(b => b.BenchmarksCases).ToArray(), 
                        title, HostEnvironmentInfo.GetCurrent(), commonSettingsConfig, GetResultsFolderPath(rootArtifactsFolderPath), Array.Empty<ValidationError>()) };

                var validationErrors = Validate(supportedBenchmarks, logger);
                if (validationErrors.Any(validationError => validationError.IsCritical))
                    return  new [] { Summary.CreateFailed(
                        supportedBenchmarks.SelectMany(b => b.BenchmarksCases).ToArray(), 
                        title, HostEnvironmentInfo.GetCurrent(), commonSettingsConfig, GetResultsFolderPath(rootArtifactsFolderPath), validationErrors) };

                var buildPartitions = BenchmarkPartitioner.CreateForBuild(supportedBenchmarks, resolver);

                logger.WriteLineHeader("// ***** BenchmarkRunner: Start   *****");
                var globalChronometer = Chronometer.Start();

                var buildResults = BuildInParallel(logger, rootArtifactsFolderPath, buildPartitions, ref globalChronometer);

                try
                {
                    var results = new List<Summary>();

                    var benchmarkToBuildResult = buildResults
                        .SelectMany(buildResult => buildResult.Key.Benchmarks.Select(buildInfo => (buildInfo.BenchmarkCase, buildInfo.Id, buildResult.Value)))
                        .ToDictionary(info => info.BenchmarkCase, info => (info.Id, info.Value));

                    foreach (var benchmarkRunInfo in supportedBenchmarks) // we run them in the old order now using the new build artifacts
                    {
                        var runChronometer = Chronometer.Start();
                        
                        var summary = Run(benchmarkRunInfo, benchmarkToBuildResult, resolver, logger, artifactsToCleanup, rootArtifactsFolderPath, ref runChronometer);
                        
                        if (commonSettingsConfig == null || commonSettingsConfig.SummaryPerType)
                            PrintSummary(logger, benchmarkRunInfo.Config, summary);
                        
                        LogTotalTime(logger, runChronometer.GetElapsed().GetTimeSpan(), summary.GetNumberOfExecutedBenchmarks(), message: "Run time");
                        logger.WriteLine();
                        
                        results.Add(summary);
                    }

                    if (commonSettingsConfig != null && !commonSettingsConfig.SummaryPerType)
                    {
                        var joinedSummary = Summary.Join(results, commonSettingsConfig, globalChronometer.GetElapsed());
                        
                        PrintSummary(logger, commonSettingsConfig, joinedSummary);
                        
                        results.Clear();
                        results.Add(joinedSummary);
                    }

                    return results.ToArray();
                }
                finally
                {
                    logger.WriteLineHeader("// * Artifacts cleanup *");
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
                    var report = RunCore(benchmark, info.benchmarkId, logger, config, resolver, buildResult);
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
                reports,
                HostEnvironmentInfo.GetCurrent(),
                config,
                GetResultsFolderPath(rootArtifactsFolderPath),
                clockSpan.GetTimeSpan(), 
                Validate(new[] {benchmarkRunInfo }, NullLogger.Instance)); // validate them once again, but don't print the output
        }

        private static void PrintSummary(ILogger logger, IConfig config, Summary summary)
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

        private static ValidationError[] Validate(BenchmarkRunInfo[] benchmarks, ILogger logger)
        {
            logger.WriteLineInfo("// Validating benchmarks:");

            var validationErrors = new List<ValidationError>();

            foreach (var benchmarkRunInfo in benchmarks)
                validationErrors.AddRange(benchmarkRunInfo.Config.GetCompositeValidator().Validate(new ValidationParameters(benchmarkRunInfo.BenchmarksCases, benchmarkRunInfo.Config)));

            foreach (var validationError in validationErrors)
                logger.WriteLineError(validationError.Message);

            return validationErrors.ToArray();
        }

        private static Dictionary<BuildPartition, BuildResult> BuildInParallel(ILogger logger, string rootArtifactsFolderPath, BuildPartition[] buildPartitions, ref StartedClock globalChronometer)
        {
            using (buildPartitions.Select(partition=> GetAssemblyResolveHelper(partition.RepresentativeBenchmarkCase.Job.GetToolchain(), logger))
                                  .FirstOrDefault(helper => helper != null))
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

        private static BenchmarkReport RunCore(BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, ILogger logger, ReadOnlyConfig config, IResolver resolver, BuildResult buildResult)
        {
            var toolchain = benchmarkCase.Job.GetToolchain();

            logger.WriteLineHeader("// **************************");
            logger.WriteLineHeader("// Benchmark: " + benchmarkCase.DisplayInfo);

            var (success, executeResults, gcStats, metrics) = Execute(logger, benchmarkCase, benchmarkId, toolchain, buildResult, config, resolver);

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
            BuildResult buildResult, IConfig config, IResolver resolver)
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

            var noOverheadCompositeDiagnoser = config.GetCompositeDiagnoser(benchmarkCase, Diagnosers.RunMode.NoOverhead);

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
                    config,
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
                    if (config.HasMemoryDiagnoser())
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

                if (!success && config.StopOnFirstError)
                {
                    break;
                }
            }
            logger.WriteLine();

            // Do a "Diagnostic" run, but DISCARD the results, so that the overhead of Diagnostics doesn't skew the overall results
            var extraRunCompositeDiagnoser = config.GetCompositeDiagnoser(benchmarkCase, Diagnosers.RunMode.ExtraRun);
            if (extraRunCompositeDiagnoser != null)
            {
                logger.WriteLineInfo("// Run, Diagnostic");

                var executeResult = RunExecute(
                    logger,
                    benchmarkCase,
                    benchmarkId,
                    toolchain,
                    buildResult,
                    config,
                    resolver,
                    extraRunCompositeDiagnoser,
                    ref success);

                var allRuns = executeResult.Data.Select(line => Measurement.Parse(logger, line, 0)).Where(r => r.IterationMode != IterationMode.Unknown).ToList();

                metrics.AddRange(
                    extraRunCompositeDiagnoser.ProcessResults(
                        new DiagnoserResults(benchmarkCase, allRuns.Where(measurement => measurement.IsWorkload()).Sum(m => m.Operations), gcStats)));

                logger.WriteLine();
            }

            var separateLogicCompositeDiagnoser = config.GetCompositeDiagnoser(benchmarkCase, Diagnosers.RunMode.SeparateLogic);
            if (separateLogicCompositeDiagnoser != null)
            {
                logger.WriteLineInfo("// Run, Diagnostic [SeparateLogic]");

                separateLogicCompositeDiagnoser.Handle(HostSignal.SeparateLogic, new DiagnoserActionParameters(null, benchmarkCase, benchmarkId, config));
            }

            return (success, executeResults, gcStats, metrics);
        }

        private static ExecuteResult RunExecute(ILogger logger, BenchmarkCase benchmarkCase, BenchmarkId benchmarkId, IToolchain toolchain, BuildResult buildResult,
            IConfig config, IResolver resolver, IDiagnoser diagnoser, ref bool success)
        {
            var executeResult = toolchain.Executor.Execute(
                new ExecuteParameters(
                    buildResult,
                    benchmarkCase,
                    benchmarkId,
                    logger,
                    resolver,
                    config,
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

        internal static void LogTotalTime(ILogger logger, TimeSpan time, int executedBenchmarksCount, string message = "Total time")
            => logger.WriteLineStatistic($"{message}: {time.ToFormattedTotalTime()}, executed benchmarks: {executedBenchmarksCount}");

        private static BenchmarkRunInfo[] GetSupportedBenchmarks(BenchmarkRunInfo[] benchmarkRunInfos, CompositeLogger logger, IResolver resolver)
            => benchmarkRunInfos.Select(info => new BenchmarkRunInfo(
                    info.BenchmarksCases.Where(benchmark => benchmark.Job.GetToolchain().IsSupported(benchmark, logger, resolver)).ToArray(),
                    info.Type,
                    info.Config))
                .Where(infos => infos.BenchmarksCases.Any())
                .ToArray();

        private static string GetResultsFolderPath(string rootArtifactsFolderPath) => Path.Combine(rootArtifactsFolderPath, "results").CreateIfNotExists();

        private static IDisposable GetAssemblyResolveHelper(IToolchain toolchain, ILogger logger)
        {
            if (RuntimeInformation.IsFullFramework 
                // we don't want to mess with assembly loading when running benchmarks in the same process (could produce wrong results)
                && !(toolchain is InProcessToolchain) 
                // so far it was never an issue for Mono
                && !RuntimeInformation.IsMono) 
            {
                return DirtyAssemblyResolveHelper.Create(logger);
            }

            return null;
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

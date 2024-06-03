using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.JetBrains;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using RunMode = BenchmarkDotNet.Diagnosers.RunMode;

namespace BenchmarkDotNet.Diagnostics.dotMemory
{
    public class DotMemoryDiagnoser(Uri? nugetUrl = null, string? toolsDownloadFolder = null) : IProfiler
    {
        private DotMemoryTool? tool;

        public IEnumerable<string> Ids => new[] { "DotMemory" };
        public string ShortName => "dotMemory";

        public RunMode GetRunMode(BenchmarkCase benchmarkCase)
        {
            return IsSupported(benchmarkCase.Job.Environment.GetRuntime().RuntimeMoniker) ? RunMode.ExtraRun : RunMode.None;
        }

        private readonly List<string> snapshotFilePaths = new ();

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            var logger = parameters.Config.GetCompositeLogger();
            var job = parameters.BenchmarkCase.Job;

            var runtimeMoniker = job.Environment.GetRuntime().RuntimeMoniker;
            if (!IsSupported(runtimeMoniker))
            {
                logger.WriteLineError($"Runtime '{runtimeMoniker}' is not supported by dotMemory");
                return;
            }

            switch (signal)
            {
                case HostSignal.BeforeAnythingElse:
                    if (tool is not null)
                        throw new InvalidOperationException("DotMemory tool is already initialized");
                    tool = new DotMemoryTool(logger, nugetUrl, downloadTo: toolsDownloadFolder);
                    tool.Init();
                    break;
                case HostSignal.BeforeActualRun:
                    if (tool is null)
                        throw new InvalidOperationException("DotMemory tool is not initialized");
                    snapshotFilePaths.Add(tool.Start(parameters));
                    break;
                case HostSignal.AfterActualRun:
                    if (tool is null)
                        throw new InvalidOperationException("DotMemory tool is not initialized");
                    tool.Stop();
                    tool = null;
                    break;
            }
        }

        public IEnumerable<IExporter> Exporters => Enumerable.Empty<IExporter>();
        public IEnumerable<IAnalyser> Analysers => Enumerable.Empty<IAnalyser>();

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var runtimeMonikers = validationParameters.Benchmarks.Select(b => b.Job.Environment.GetRuntime().RuntimeMoniker).Distinct();
            foreach (var runtimeMoniker in runtimeMonikers)
            {
                if (!IsSupported(runtimeMoniker))
                    yield return new ValidationError(true, $"Runtime '{runtimeMoniker}' is not supported by dotMemory");
            }
        }

        internal static bool IsSupported(RuntimeMoniker runtimeMoniker) => Helper.IsSupported(runtimeMoniker);

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => ImmutableArray<Metric>.Empty;

        public void DisplayResults(ILogger logger)
        {
            if (snapshotFilePaths.Any())
            {
                logger.WriteLineInfo("The following dotMemory snapshots were generated:");
                foreach (string snapshotFilePath in snapshotFilePaths)
                    logger.WriteLineInfo($"* {snapshotFilePath}");
            }
        }
    }
}
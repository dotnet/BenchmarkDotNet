using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Diagnosers;

public abstract class SnapshotProfilerBase : IProfiler
{
    public abstract string ShortName { get; }

    protected abstract void InitTool(Progress progress);
    protected abstract void AttachToCurrentProcess(string snapshotFile);
    protected abstract void AttachToProcessByPid(int pid, string snapshotFile);
    protected abstract void TakeSnapshot();
    protected abstract void Detach();

    protected abstract string CreateSnapshotFilePath(DiagnoserActionParameters parameters);
    protected abstract string GetRunnerPath();
    internal abstract bool IsSupported(RuntimeMoniker runtimeMoniker);

    private readonly List<string> snapshotFilePaths = [];

    public IEnumerable<string> Ids => [ShortName];
    public IEnumerable<IExporter> Exporters => [];
    public IEnumerable<IAnalyser> Analysers => [];

    public RunMode GetRunMode(BenchmarkCase benchmarkCase) =>
        IsSupported(benchmarkCase.Job.Environment.GetRuntime().RuntimeMoniker) ? RunMode.ExtraRun : RunMode.None;

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
                Init(logger);
                break;
            case HostSignal.BeforeActualRun:
                string snapshotFilePath = Start(logger, parameters);
                snapshotFilePaths.Add(snapshotFilePath);
                break;
            case HostSignal.AfterActualRun:
                Stop(logger);
                break;
        }
    }

    public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
    {
        var runtimeMonikers = validationParameters.Benchmarks.Select(b => b.Job.Environment.GetRuntime().RuntimeMoniker).Distinct();
        foreach (var runtimeMoniker in runtimeMonikers)
            if (!IsSupported(runtimeMoniker))
                yield return new ValidationError(true, $"Runtime '{runtimeMoniker}' is not supported by dotMemory");
    }

    public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => ImmutableArray<Metric>.Empty;

    public void DisplayResults(ILogger logger)
    {
        if (snapshotFilePaths.Count != 0)
        {
            logger.WriteLineInfo($"The following {ShortName} snapshots were generated:");
            foreach (string snapshotFilePath in snapshotFilePaths)
                logger.WriteLineInfo($"* {snapshotFilePath}");
        }
    }

    private void Init(ILogger logger)
    {
        try
        {
            logger.WriteLineInfo($"Ensuring that {ShortName} prerequisite is installed...");
            var progress = new Progress(logger, $"Installing {ShortName}");
            InitTool(progress);
            logger.WriteLineInfo($"{ShortName} prerequisite is installed");
            logger.WriteLineInfo($"{ShortName} runner path: {GetRunnerPath()}");
        }
        catch (Exception e)
        {
            logger.WriteLineError(e.ToString());
        }
    }

    private string Start(ILogger logger, DiagnoserActionParameters parameters)
    {
        string snapshotFilePath = CreateSnapshotFilePath(parameters);
        string? snapshotDirectory = Path.GetDirectoryName(snapshotFilePath);
        logger.WriteLineInfo($"Target snapshot file: {snapshotFilePath}");
        if (!Directory.Exists(snapshotDirectory) && snapshotDirectory != null)
        {
            try
            {
                Directory.CreateDirectory(snapshotDirectory);
            }
            catch (Exception e)
            {
                logger.WriteLineError($"Failed to create directory: {snapshotDirectory}");
                logger.WriteLineError(e.ToString());
            }
        }

        try
        {
            logger.WriteLineInfo($"Attaching {ShortName} to the process...");
            Attach(parameters, snapshotFilePath);
            logger.WriteLineInfo($"{ShortName} is successfully attached");
        }
        catch (Exception e)
        {
            logger.WriteLineError(e.ToString());
            return snapshotFilePath;
        }

        return snapshotFilePath;
    }

    private void Stop(ILogger logger)
    {
        try
        {
            logger.WriteLineInfo($"Taking {ShortName} snapshot...");
            TakeSnapshot();
            logger.WriteLineInfo($"{ShortName} snapshot is successfully taken");
        }
        catch (Exception e)
        {
            logger.WriteLineError(e.ToString());
        }

        try
        {
            logger.WriteLineInfo($"Detaching {ShortName} from the process...");
            Detach();
            logger.WriteLineInfo($"{ShortName} is successfully detached");
        }
        catch (Exception e)
        {
            logger.WriteLineError(e.ToString());
        }
    }


    private void Attach(DiagnoserActionParameters parameters, string snapshotFile)
    {
        int pid = parameters.Process.Id;
        int currentPid = Process.GetCurrentProcess().Id;
        if (pid != currentPid)
            AttachToProcessByPid(pid, snapshotFile);
        else
            AttachToCurrentProcess(snapshotFile);
    }

    protected class Progress(ILogger logger, string title) : IProgress<double>
    {
        private static readonly TimeSpan ReportInterval = TimeSpan.FromSeconds(0.1);

        private int lastProgress;
        private Stopwatch? stopwatch;

        public void Report(double value)
        {
            int progress = (int)Math.Floor(value);
            bool needToReport = stopwatch == null ||
                                (stopwatch != null && stopwatch?.Elapsed > ReportInterval) ||
                                progress == 100;

            if (lastProgress != progress && needToReport)
            {
                logger.WriteLineInfo($"{title}: {progress}%");
                lastProgress = progress;
                stopwatch = Stopwatch.StartNew();
            }
        }
    }
}
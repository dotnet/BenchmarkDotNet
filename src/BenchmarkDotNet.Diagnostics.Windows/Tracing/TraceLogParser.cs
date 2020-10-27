using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace BenchmarkDotNet.Diagnostics.Windows.Tracing
{
    public class TraceLogParser
    {
        private readonly Dictionary<int, ProcessMetrics> processIdToData = new Dictionary<int, ProcessMetrics>();
        private readonly Dictionary<int, int> profileSourceIdToInterval = new Dictionary<int, int>();

        public static IEnumerable<Metric> Parse(string etlFilePath, PreciseMachineCounter[] counters)
        {
            var etlxFilePath = TraceLog.CreateFromEventTraceLogFile(etlFilePath);

            try
            {
                using (var traceLog = new TraceLog(etlxFilePath))
                {
                    var traceLogEventSource = traceLog.Events.GetSource();

                    return new TraceLogParser().Parse(traceLogEventSource, counters);
                }
            }
            finally
            {
                etlxFilePath.DeleteFileIfExists();
            }
        }

        private IEnumerable<Metric> Parse(TraceLogEventSource traceLogEventSource, PreciseMachineCounter[] counters)
        {
            var bdnEventsParser = new EngineEventLogParser(traceLogEventSource);
            var kernelEventsParser = new KernelTraceEventParser(traceLogEventSource);

            bdnEventsParser.OverheadActualStart += OnOverheadActualStart;
            bdnEventsParser.OverheadActualStop += OnOverheadActualStop;

            bdnEventsParser.WorkloadActualStart += OnWorkloadActualStart;
            bdnEventsParser.WorkloadActualStop += OnWorkloadActualStop;

            kernelEventsParser.PerfInfoCollectionStart += OnPmcIntervalChange;
            kernelEventsParser.PerfInfoPMCSample += OnPmcEvent;

            traceLogEventSource.Process();

            var benchmarkedProcessData = processIdToData.Values.Single(x => x.HasBenchmarkEvents);

            return benchmarkedProcessData.CalculateMetrics(profileSourceIdToInterval, counters);
        }

        private void OnOverheadActualStart(IterationEvent obj) => HandleIterationEvent(obj.ProcessID, obj.TimeStampRelativeMSec, IterationMode.Overhead, obj.TotalOperations);

        private void OnOverheadActualStop(IterationEvent obj) => HandleIterationEvent(obj.ProcessID, obj.TimeStampRelativeMSec, IterationMode.Overhead, obj.TotalOperations);

        private void OnWorkloadActualStart(IterationEvent obj) => HandleIterationEvent(obj.ProcessID, obj.TimeStampRelativeMSec, IterationMode.Workload, obj.TotalOperations);

        private void OnWorkloadActualStop(IterationEvent obj) => HandleIterationEvent(obj.ProcessID, obj.TimeStampRelativeMSec, IterationMode.Workload, obj.TotalOperations);

        private void HandleIterationEvent(int processId, double timeStampRelative, IterationMode iterationMode, long totalOperations)
        {
            // if given process emits Benchmarking events it's the process that we care about
            if (!processIdToData.ContainsKey(processId))
                processIdToData.Add(processId, new ProcessMetrics());

            processIdToData[processId].HandleIterationEvent(timeStampRelative, iterationMode, totalOperations);
        }

        private void OnPmcIntervalChange(SampledProfileIntervalTraceData data)
        {
            if (profileSourceIdToInterval.TryGetValue(data.SampleSource, out int storedInterval) && storedInterval != data.NewInterval)
                throw new NotSupportedException("Sampling interval change is not supported!");

            profileSourceIdToInterval[data.SampleSource] = data.NewInterval;
        }

        private void OnPmcEvent(PMCCounterProfTraceData data)
        {
            // if given process did not emit Benchmarking events before, we don't care about it
            if (!processIdToData.ContainsKey(data.ProcessID))
                return;

            processIdToData[data.ProcessID].HandleNewSample(data.TimeStampRelativeMSec, data.InstructionPointer, data.ProfileSource);
        }
    }

    public class ProcessMetrics
    {
        private readonly List<double> overheadTimestamps = new List<double>(20);
        private readonly List<double> workloadTimestamps = new List<double>(20);
        private long? totalOperationsPerIteration;

        private readonly List<(double timeStamp, ulong instructionPointer, int profileSource)> samples = new List<(double timeStamp, ulong instructionPointer, int profileSource)>();

        public bool HasBenchmarkEvents => overheadTimestamps.Any() || workloadTimestamps.Any();

        public void HandleIterationEvent(double timeStamp, IterationMode iterationMode, long totalOperations)
        {
            if (iterationMode == IterationMode.Overhead)
            {
                overheadTimestamps.Add(timeStamp);
            }
            else if (iterationMode == IterationMode.Workload)
            {
                if (!totalOperationsPerIteration.HasValue)
                    totalOperationsPerIteration = totalOperations;
                else if (totalOperationsPerIteration.Value != totalOperations)
                    throw new InvalidOperationException($"TotalOperations count can't change during the benchmark run! Invalid trace!");

                workloadTimestamps.Add(timeStamp);
            }
        }

        public void HandleNewSample(double timeStamp, ulong instructionPointer, int profileSourceId)
            => samples.Add((timeStamp, instructionPointer, profileSourceId));

        public IEnumerable<Metric> CalculateMetrics(Dictionary<int, int> profileSourceIdToInterval, PreciseMachineCounter[] counters)
        {
            if (overheadTimestamps.Count % 2 != 0)
                throw new InvalidOperationException("One overhead iteration stop event is missing, unable to calculate stats");
            if (workloadTimestamps.Count % 2 != 0)
                throw new InvalidOperationException("One workload iteration stop event is missing, unable to calculate stats");
            if (!totalOperationsPerIteration.HasValue)
                throw new InvalidOperationException("TotalOperations is missing, unable to calculate stats");

            var overheadIterations = CreateIterationData(overheadTimestamps);
            var workloadIterations = CreateIterationData(workloadTimestamps);

            SumCountersPerIterations(profileSourceIdToInterval, workloadIterations, overheadIterations, counters);

            var workloadTotalPerCounter = Sum(workloadIterations);
            var overheadTotalPerCounter = Sum(overheadIterations);

            return workloadTotalPerCounter.Select(perCounter =>
            {
                var pmc = counters.Single(counter => counter.ProfileSourceId == perCounter.Key);

                overheadTotalPerCounter.TryGetValue(perCounter.Key, out var overhead);

                // result = (avg(workload) - avg(overhead))/op
                double result = perCounter.Value / (double)workloadIterations.Length;

                if (overheadIterations.Length > 0) // we skip the overhead phase for long-running benchmarks
                {
                    result -= (overhead / (double)overheadIterations.Length);
                }

                result /= totalOperationsPerIteration.Value;

                return new Metric(new PmcMetricDescriptor(pmc), result);
            });
        }

        private IterationData[] CreateIterationData(List<double> startStopTimeStamps)
        {
            // collection contains mixted .Start and .Stop intervals, if we sort it we know that n is Start and n + 1 is Stop
            startStopTimeStamps.Sort();

            var iterations = new IterationData[startStopTimeStamps.Count / 2];
            for (int i = 0; i < iterations.Length; i++)
            {
                iterations[i] = new IterationData(startStopTimeStamps[i * 2], startStopTimeStamps[(i * 2) + 1]);
            }

            return iterations;
        }

        private void SumCountersPerIterations(Dictionary<int, int> profileSourceIdToInterval, IterationData[] workloadIterations, IterationData[] overheadIterations,
            PreciseMachineCounter[] counters)
        {
            var profileSourceIdToCounter = counters.ToDictionary(counter => counter.ProfileSourceId);

            foreach (var sample in samples)
            {
                var interval = profileSourceIdToInterval[sample.profileSource];

                foreach (var workloadIteration in workloadIterations)
                    if (workloadIteration.TryHandle(sample.timeStamp, sample.profileSource, interval))
                    {
                        profileSourceIdToCounter[sample.profileSource].OnSample(sample.instructionPointer);

                        goto next;
                    }

                foreach (var overheadIteration in overheadIterations)
                    if (overheadIteration.TryHandle(sample.timeStamp, sample.profileSource, interval))
                        goto next;

                next:
                    continue;
            }
        }

        private static Dictionary<int, ulong> Sum(IterationData[] iterations)
        {
            var totalPerCounter = new Dictionary<int, ulong>();

            foreach (var iteration in iterations)
            {
                foreach (var idToCount in iteration.ProfileSourceIdToCount)
                {
                    checked
                    {
                        totalPerCounter.TryGetValue(idToCount.Key, out ulong existing);
                        totalPerCounter[idToCount.Key] = existing + idToCount.Value;
                    }
                }
            }

            return totalPerCounter;
        }
    }

    public class IterationData
    {
        public Dictionary<int, ulong> ProfileSourceIdToCount { get; }
        private double StartTimestamp { get; }
        private double StopTimestamp { get; }

        public IterationData(double startTimestamp, double stopTimestamp)
        {
            StartTimestamp = startTimestamp;
            StopTimestamp = stopTimestamp;
            ProfileSourceIdToCount = new Dictionary<int, ulong>();
        }

        public bool TryHandle(double timeStamp, int profileSource, int interval)
        {
            if (!(StartTimestamp <= timeStamp && timeStamp <= StopTimestamp))
                return false;

            checked
            {
                ProfileSourceIdToCount.TryGetValue(profileSource, out ulong existing);
                ProfileSourceIdToCount[profileSource] = existing + (ulong)interval;
            }

            return true;
        }
    }
}
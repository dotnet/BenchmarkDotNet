using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class TraceLogParser
    {
        private readonly Dictionary<int, ProcessMetrics> processIdToData = new Dictionary<int, ProcessMetrics>();
        private readonly Dictionary<int, int> profileSourceIdToInterval = new Dictionary<int, int>();
        
        public static ImmutableArray<Metric> Parse(string etlFilePath)
        {
            using (var traceLog = new TraceLog(TraceLog.CreateFromEventTraceLogFile(etlFilePath)))
            {
                var traceLogEventSource = traceLog.Events.GetSource();
                
                return new TraceLogParser().Parse(traceLogEventSource);
            }
        }

        private ImmutableArray<Metric> Parse(TraceLogEventSource traceLogEventSource)
        {
            var bdnEventsParser = new EngineEventLogParser(traceLogEventSource);
            var kernelEventsParser = new KernelTraceEventParser(traceLogEventSource);

            bdnEventsParser.BenchmarkIterationStart += HandleIterationEvent;
            bdnEventsParser.BenchmarkIterationStop += HandleIterationEvent;
            
            kernelEventsParser.PerfInfoCollectionStart += OnPmcIntervalChange;
            kernelEventsParser.PerfInfoPMCSample += OnPmcEvent;

            traceLogEventSource.Process();

            var benchmarkedProcessData = processIdToData.Values.Single(x => x.HasBenchmarkEvents);
            
            return benchmarkedProcessData.CalculateMetrics(profileSourceIdToInterval);
        }

        private void HandleIterationEvent(IterationEvent data)
        {
            // we are interested only in the actual runs (not pilot, not warmup)
            if (data.IterationStage != IterationStage.Actual)
                return;

            // if given process emits Benchmarking events it's the process that we care about
            if (!processIdToData.ContainsKey(data.ProcessID))
                processIdToData.Add(data.ProcessID, new ProcessMetrics());

            processIdToData[data.ProcessID].HandleIterationEvent(data.TimeStampRelativeMSec, data.IterationMode);
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
        
        private readonly List<(double timeStamp, ulong instructionPointer, int profileSource)> samples = new List<(double timeStamp, ulong instructionPointer, int profileSource)>();

        public bool HasBenchmarkEvents => overheadTimestamps.Any() || workloadTimestamps.Any();

        public void HandleIterationEvent(double timeStamp, IterationMode iterationMode)
        {
            if (iterationMode == IterationMode.Workload)
                workloadTimestamps.Add(timeStamp);
            else if (iterationMode == IterationMode.Overhead)
                overheadTimestamps.Add(timeStamp);
        }

        public void HandleNewSample(double timeStamp, ulong instructionPointer, int profileSourceId)
            => samples.Add((timeStamp, instructionPointer, profileSourceId));

        public ImmutableArray<Metric> CalculateMetrics(Dictionary<int, int> profileSourceIdToInterval)
        {
            if (overheadTimestamps.Count % 2 != 0)
                throw new InvalidOperationException("One overhead iteration stop event is missing, unable to calculate stats");
            if (workloadTimestamps.Count % 2 != 0)
                throw new InvalidOperationException("One workload iteration stop event is missing, unable to calculate stats");

            var overheadIterations = ToIterationData(overheadTimestamps);
            var workloadIterations = ToIterationData(workloadTimestamps);

            foreach (var sample in samples)
            {
                var interval = profileSourceIdToInterval[sample.profileSource];

                foreach (var workloadIteration in workloadIterations)
                    if (workloadIteration.TryHandle(sample.timeStamp, sample.profileSource, interval))
                        goto next;
                
                foreach (var overheadIteration in overheadIterations)
                    if (overheadIteration.TryHandle(sample.timeStamp, sample.profileSource, interval))
                        goto next;

                next:
                    continue;
            }

            var overheadTotalPerCounter = new Dictionary<int, ulong>();
            foreach (var overheadIteration in overheadIterations)
            {
                foreach (var idToCount in overheadIteration.ProfileSourceIdToCount)
                {
                    checked
                    {
                        overheadTotalPerCounter.TryGetValue(idToCount.Key, out ulong existing);
                        overheadTotalPerCounter[idToCount.Key] = existing + idToCount.Value;
                    }
                }
            }

            var overheadAveragePerCounter = overheadTotalPerCounter.ToDictionary(item => item.Key, item => item.Value / (ulong)overheadIterations.Length);

            return workloadIterations.SelectMany(iteration => iteration.ProfileSourceIdToCount
                    .Select(counterData => CreateMetric(counterData.Key, counterData.Value - overheadAveragePerCounter[counterData.Key]))) // result = workload - avg(overhead)
                    .ToImmutableArray();
        }

        private IterationData[] ToIterationData(List<double> startStopTimeStamps)
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

        private Metric CreateMetric(int counterDataKey, ulong @ulong)
        {
            throw new NotImplementedException();
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
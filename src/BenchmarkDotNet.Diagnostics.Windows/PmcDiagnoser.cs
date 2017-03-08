using System;
using System.Diagnostics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System.Linq;
using System.Collections.Generic;
using BenchmarkDotNet.Engines;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class PreciseMachineCounter
    {
        public int ProfileSourceId { get; }
        public string Name { get; }
        public int Interval { get; }

        public ulong Count { get; private set; }

        private PreciseMachineCounter(int profileSourceId, string name, int interval)
        {
            ProfileSourceId = profileSourceId;
            Name = name;
            Interval = interval;
        }

        public static PreciseMachineCounter FromName(string counterName)
        {
            var availableCpuCounters = TraceEventProfileSources.GetInfo();
            if (!availableCpuCounters.TryGetValue(counterName, out var profileSource))
            {
                throw new InvalidOperationException($"The counter {counterName} is not available. Please make sure you are Windows 8+ without Hyper-V");
            }

            return new PreciseMachineCounter(profileSource.ID, profileSource.Name,
                profileSource.MinInterval); // we want the smallest interval to have best possible precision
        }

        public void OnSample()
        {
            checked // if we ever overflow ulong we need to throw!
            {
                Count += (ulong)Interval;
            }
        }
    }

    public class PmcStats
    {
        public long TotalOperations { get; set; }

        public PreciseMachineCounter BranchInstructions { get; private set; }
        public PreciseMachineCounter CacheMisses { get; private set; }
        public PreciseMachineCounter BranchMispredictions { get; private set; }
        public PreciseMachineCounter InstructionRetired { get; private set; }

        public void Initialize()
        {
            // never change the names! use TraceEventProfileSources.GetInfo() to get tha available ones!
            BranchInstructions = PreciseMachineCounter.FromName("BranchInstructions");
            CacheMisses = PreciseMachineCounter.FromName("CacheMisses");
            BranchMispredictions = PreciseMachineCounter.FromName("BranchMispredictions");
            InstructionRetired = PreciseMachineCounter.FromName("InstructionRetired");
        }

        public PreciseMachineCounter[] GetCounters()
            => new[] { BranchInstructions, CacheMisses, BranchMispredictions, InstructionRetired };

        public void Handle(int profileSourceId)
        {
            if (profileSourceId == BranchInstructions.ProfileSourceId)
                BranchInstructions.OnSample();
            else if (profileSourceId == CacheMisses.ProfileSourceId)
                CacheMisses.OnSample();
            else if (profileSourceId == BranchMispredictions.ProfileSourceId)
                BranchMispredictions.OnSample();
            else if (profileSourceId == InstructionRetired.ProfileSourceId)
                InstructionRetired.OnSample();
        }
    }

    public class PmcDiagnoser : EtwDiagnoser<PmcStats>, IDiagnoser
    {
        private readonly Dictionary<Benchmark, PmcStats> results = new Dictionary<Benchmark, PmcStats>();

        protected override ulong EventType
            => unchecked((ulong)(KernelTraceEventParser.Keywords.PMCProfile | KernelTraceEventParser.Keywords.Profile));

        protected override string SessionNamePrefix
        {
            get { throw new NotImplementedException("Not needed for Kernel sessions (can be only one at a time"); }
        }

        public IColumnProvider GetColumnProvider()
            => new SimpleColumnProvider(
                new PmcColumn(results, "Instr Retired/Op", pmc => (pmc.InstructionRetired.Count / (ulong)pmc.TotalOperations).ToString()),
                new PmcColumn(results, "Cache Misses/Op", pmc => (pmc.CacheMisses.Count / (ulong)pmc.TotalOperations).ToString()),
                new PmcColumn(results, "Misspredict rate", pmc => (pmc.BranchMispredictions.Count / (double)pmc.BranchInstructions.Count).ToString("P2")));

        public void BeforeAnythingElse(Process process, Benchmark benchmark)
        {
            if (TraceEventSession.IsElevated() != true)
            {
                throw new InvalidOperationException("Must be elevated (Admin) to run this program.");
            }
        }

        public void AfterSetup(Process process, Benchmark benchmark) { }

        public void BeforeMainRun(Process process, Benchmark benchmark) => Start(process, benchmark);

        public void BeforeCleanup() => Stop();

        public void ProcessResults(Benchmark benchmark, BenchmarkReport report)
        {
            var processId = BenchmarkToProcess[benchmark];
            var stats = StatsPerProcess[processId];
            stats.TotalOperations = report.AllMeasurements.Where(measurement => !measurement.IterationMode.IsIdle()).Sum(m => m.Operations);
            results.Add(benchmark, stats);
        }

        public void DisplayResults(ILogger logger) { }

        protected override PmcStats GetInitializedStats()
        {
            var stats = new PmcStats();
            stats.Initialize();

            var counters = stats.GetCounters();

            TraceEventProfileSources.Set( // it's a must have to get the events enabled!!
                counters.Select(counter => counter.ProfileSourceId).ToArray(),
                counters.Select(counter => counter.Interval).ToArray());

            return stats;
        }

        protected override TraceEventSession CreateSession(Benchmark benchmark)
            => new TraceEventSession(KernelTraceEventParser.KernelSessionName);

        protected override void EnableProvider()
            => Session.EnableKernelProvider((KernelTraceEventParser.Keywords)EventType);

        protected override void AttachToEvents(TraceEventSession traceEventSession, Benchmark benchmark)
        {
            traceEventSession.Source.Kernel.PerfInfoCollectionStart += _ => { }; // we must subscribe to this event, otherwise the PerfInfoPMCSample is not raised ;)
            traceEventSession.Source.Kernel.PerfInfoPMCSample += OnPerfInfoPmcSample;
        }

        private void OnPerfInfoPmcSample(PMCCounterProfTraceData obj)
        {
            if (StatsPerProcess.TryGetValue(obj.ProcessID, out var stats))
                stats.Handle(obj.ProfileSource);
        }

        public class PmcColumn : IColumn
        {
            public PmcColumn(Dictionary<Benchmark, PmcStats> results, string columnName, Func<PmcStats, string> toDisplayValue)
            {
                Results = results;
                ColumnName = columnName;
                ToDisplayValue = toDisplayValue;
            }

            public string ColumnName { get; }
            public string Id => nameof(PmcColumn) + ColumnName;
            public bool IsDefault(Summary summary, Benchmark benchmark) => false;

            public bool IsAvailable(Summary summary) => true;
            public bool AlwaysShow => true;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;

            private Dictionary<Benchmark, PmcStats> Results { get; }
            private Func<PmcStats, string> ToDisplayValue { get; }

            public string GetValue(Summary summary, Benchmark benchmark)
            {
                if (Results.ContainsKey(benchmark) && Results[benchmark] != null)
                {
                    return ToDisplayValue(Results[benchmark]);
                }

                return "N/A";
            }
        }
    }
}

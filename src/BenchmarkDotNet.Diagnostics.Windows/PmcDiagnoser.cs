using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class PreciseMachineCounter
    {
        public int ProfileSourceId { get; }
        public string Name { get; }
        public HardwareCounter Counter { get; }
        public int Interval { get; }

        public ulong Count { get; private set; }

        private PreciseMachineCounter(int profileSourceId, string name, HardwareCounter counter, int interval)
        {
            ProfileSourceId = profileSourceId;
            Name = name;
            Counter = counter;
            Interval = interval;
        }

        public static PreciseMachineCounter FromCounter(HardwareCounter counter)
        {
            var profileSource = TraceEventProfileSources.GetInfo()[PmcDiagnoser.EtwTranslations[counter]]; // it can't fail, diagnoser validates that first

            return new PreciseMachineCounter(profileSource.ID, profileSource.Name, counter,
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
        public IReadOnlyDictionary<HardwareCounter, PreciseMachineCounter> Counters { get; }
        private IReadOnlyDictionary<int, PreciseMachineCounter> CountersByProfileSourceId { get; }

        public PmcStats() { throw new InvalidOperationException("should never be used"); }

        public PmcStats(IReadOnlyCollection<HardwareCounter> hardwareCounters)
        {
            CountersByProfileSourceId = hardwareCounters
                .Select(PreciseMachineCounter.FromCounter)
                .ToDictionary
                (
                    counter => counter.ProfileSourceId,
                    coounter => coounter
                );
            Counters = CountersByProfileSourceId.ToDictionary(c => c.Value.Counter, c => c.Value);
        }

        public void Handle(int profileSourceId)
        {
            if (CountersByProfileSourceId.TryGetValue(profileSourceId, out var counter))
                counter.OnSample();
        }
    }

    public class PmcDiagnoser : EtwDiagnoser<PmcStats>, IHardwareCountersDiagnoser
    {
        internal static readonly Dictionary<HardwareCounter, string> EtwTranslations
            = new Dictionary<HardwareCounter, string>
            {
                { HardwareCounter.Timer, "Timer" },
                { HardwareCounter.TotalIssues, "TotalIssues" },
                { HardwareCounter.BranchInstructions, "BranchInstructions" },
                { HardwareCounter.CacheMisses, "CacheMisses" },
                { HardwareCounter.BranchMispredictions, "BranchMispredictions" },
                { HardwareCounter.TotalCycles, "TotalCycles" },
                { HardwareCounter.UnhaltedCoreCycles, "UnhaltedCoreCycles" },
                { HardwareCounter.InstructionRetired, "InstructionRetired" },
                { HardwareCounter.UnhaltedReferenceCycles, "UnhaltedReferenceCycles" },
                { HardwareCounter.LlcReference, "LLCReference" },
                { HardwareCounter.LlcMisses, "LLCMisses" },
                { HardwareCounter.BranchInstructionRetired, "BranchInstructionRetired" },
                { HardwareCounter.BranchMispredictsRetired, "BranchMispredictsRetired" }
            };

        private readonly Dictionary<Benchmark, PmcStats> results = new Dictionary<Benchmark, PmcStats>();

        protected override ulong EventType
            => unchecked((ulong)(KernelTraceEventParser.Keywords.PMCProfile | KernelTraceEventParser.Keywords.Profile));

        protected override string SessionNamePrefix
        {
            get { throw new NotImplementedException("Not needed for Kernel sessions (can be only one at a time"); }
        }

        public void BeforeAnythingElse(Process process, Benchmark benchmark) { }
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

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            if (TraceEventSession.IsElevated() != true)
                yield return new ValidationError(true, "Must be elevated (Admin) to use Hardware Counters to use ETW Kernel Session.");

            var availableCpuCounters = TraceEventProfileSources.GetInfo();
            foreach (var benchmark in validationParameters.Benchmarks
                .Where(benchmark => !benchmark.Job.Diagnoser.HardwareCounters.IsNullOrEmpty()))
            {
                foreach (var hardwareCounter in benchmark.Job.Diagnoser.HardwareCounters)
                {
                    if (!EtwTranslations.TryGetValue(hardwareCounter, out var counterName))
                        yield return new ValidationError(true, $"Counter {hardwareCounter} not recognized. Please make sure that you are using counter supported on Windows", benchmark);

                    if (!availableCpuCounters.ContainsKey(counterName))
                        yield return new ValidationError(true, $"The counter {counterName} is not available. Please make sure you are Windows 8+ without Hyper-V", benchmark);
                }
            }
        }

        protected override PmcStats GetInitializedStats(Benchmark benchmark)
        {
            var stats = new PmcStats(benchmark.Job.Diagnoser.HardwareCounters);

            var counters = stats.Counters.Values;

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

        public IColumnProvider GetColumnProvider()
           => new SimpleColumnProvider(
               Enum.GetValues(typeof(HardwareCounter))
                   .OfType<HardwareCounter>()
                   .Where(counter => counter != HardwareCounter.NotSet)
                   .Select(counter => new PmcColumn(results, counter))
                   .Union(new IColumn[] 
                   {
                       new MisspredictRateColumn(results)
                   })
                   .ToArray());

        public class PmcColumn : IColumn
        {
            public PmcColumn(Dictionary<Benchmark, PmcStats> results, HardwareCounter hardwareCounter)
            {
                Results = results;
                Counter = hardwareCounter;
                ColumnName = $"{hardwareCounter}/Op";
            }

            public string ColumnName { get; }
            public string Id => nameof(PmcColumn) + ColumnName;
            public bool IsDefault(Summary summary, Benchmark benchmark) => false;
            public bool AlwaysShow => false;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0;

            private Dictionary<Benchmark, PmcStats> Results { get; }
            private HardwareCounter Counter { get; }

            public bool IsAvailable(Summary summary)
                => summary.Benchmarks.Any(
                    benchmark =>
                        !benchmark.Job.Diagnoser.HardwareCounters.IsNullOrEmpty()
                        && benchmark.Job.Diagnoser.HardwareCounters.Contains(Counter));

            public string GetValue(Summary summary, Benchmark benchmark)
                => Results.TryGetValue(benchmark, out var stats) && stats.Counters.ContainsKey(Counter)
                    ? (stats.Counters[Counter].Count / (ulong)stats.TotalOperations).ToString() 
                    : "-";
        }

        public class MisspredictRateColumn : IColumn
        {
            public MisspredictRateColumn(Dictionary<Benchmark, PmcStats> results)
            {
                Results = results;
            }

            public string ColumnName => "Misspredict rate";
            public string Id => "MisspredictRate";
            public bool IsDefault(Summary summary, Benchmark benchmark) => false;
            public bool AlwaysShow => false;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 1;

            private Dictionary<Benchmark, PmcStats> Results { get; }

            public bool IsAvailable(Summary summary)
                => summary.Benchmarks.Any(
                    benchmark =>
                        !benchmark.Job.Diagnoser.HardwareCounters.IsNullOrEmpty()
                        && benchmark.Job.Diagnoser.HardwareCounters.Contains(HardwareCounter.BranchInstructions)
                        && benchmark.Job.Diagnoser.HardwareCounters.Contains(HardwareCounter.BranchMispredictions));

            public string GetValue(Summary summary, Benchmark benchmark)
                => Results.TryGetValue(benchmark, out var stats) && stats.Counters.ContainsKey(HardwareCounter.BranchMispredictions) && stats.Counters.ContainsKey(HardwareCounter.BranchInstructions)
                    ? (stats.Counters[HardwareCounter.BranchMispredictions].Count / (double)stats.Counters[HardwareCounter.BranchInstructions].Count).ToString("P2")
                    : "-";
        }
    }
}
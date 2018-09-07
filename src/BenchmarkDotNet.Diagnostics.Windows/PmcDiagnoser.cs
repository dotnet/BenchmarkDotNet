using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class PmcDiagnoser : EtwDiagnoser<PmcStats>, IHardwareCountersDiagnoser
    {
        private readonly Dictionary<BenchmarkCase, PmcStats> results = new Dictionary<BenchmarkCase, PmcStats>();

        // ReSharper disable once EmptyConstructor parameterless ctor is mandatory for DiagnosersLoader.CreateDiagnoser
        public PmcDiagnoser() { }

        public IReadOnlyDictionary<BenchmarkCase, PmcStats> Results => results;

        public const string DiagnoserId = nameof(InliningDiagnoser);

        public IEnumerable<string> Ids => new[] { DiagnoserId };

        protected override ulong EventType
            => unchecked((ulong)(KernelTraceEventParser.Keywords.PMCProfile | KernelTraceEventParser.Keywords.Profile));

        protected override string SessionNamePrefix
            => throw new NotImplementedException("Not needed for Kernel sessions (can be only one at a time");

        public void Handle(HostSignal signal, DiagnoserActionParameters parameters)
        {
            if (signal == HostSignal.BeforeActualRun)
                Start(parameters);
            else if (signal == HostSignal.AfterActualRun)
                Stop();
        }

        public IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            var processId = BenchmarkToProcess[results.BenchmarkCase];
            var stats = StatsPerProcess[processId];
            stats.TotalOperations = results.TotalOperations;
            this.results.Add(results.BenchmarkCase, stats);

            foreach (var pmc in stats.Counters.Values)
                yield return new Metric($"{pmc.Name}/Op", (double)pmc.Count / results.TotalOperations, theGreaterTheBetter: pmc.Counter.TheGreaterTheBetter());
        }

        public void DisplayResults(ILogger logger) { }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => HardwareCounters.Validate(validationParameters, mandatory: true);

        protected override PmcStats GetInitializedStats(DiagnoserActionParameters parameters)
        {
            var stats = new PmcStats(
                parameters.Config.GetHardwareCounters().ToArray(), 
                counter => HardwareCounters.FromCounter(counter, info => info.MinInterval )); // for this diagnoser we want the smallest interval to have best possible precision

            HardwareCounters.Enable(stats.Counters.Values);

            return stats;
        }

        protected override TraceEventSession CreateSession(BenchmarkCase benchmarkCase)
            => new TraceEventSession(KernelTraceEventParser.KernelSessionName);

        protected override void EnableProvider()
            => Session.EnableKernelProvider((KernelTraceEventParser.Keywords)EventType);

        protected override void AttachToEvents(TraceEventSession traceEventSession, BenchmarkCase benchmarkCase)
        {
            traceEventSession.Source.Kernel.PerfInfoCollectionStart += _ => { }; // we must subscribe to this event, otherwise the PerfInfoPMCSample is not raised ;)
            traceEventSession.Source.Kernel.PerfInfoPMCSample += OnPerfInfoPmcSample;
        }

        private void OnPerfInfoPmcSample(PMCCounterProfTraceData obj)
        {
            if (StatsPerProcess.TryGetValue(obj.ProcessID, out var stats))
                stats.Handle(obj.ProfileSource, obj.InstructionPointer);
        }

        public IColumnProvider GetColumnProvider()
           => new SimpleColumnProvider(
               Enum.GetValues(typeof(HardwareCounter))
                   .OfType<HardwareCounter>()
                   .Where(counter => counter != HardwareCounter.NotSet)
                   .Select(counter => new PmcColumn(results, counter))
                   .Union(new IColumn[]
                   {
                       new MispredictRateColumn(results),
                       new InstructionRetiredPerCycleColumn(results)
                   })
                   .ToArray());

        public class PmcColumn : IColumn
        {
            public PmcColumn(Dictionary<BenchmarkCase, PmcStats> results, HardwareCounter hardwareCounter)
            {
                Results = results;
                Counter = hardwareCounter;
                ColumnName = $"{hardwareCounter}/Op";
            }

            public string ColumnName { get; }
            public string Id => nameof(PmcColumn) + ColumnName;
            public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
            public bool AlwaysShow => false;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 1;
            public bool IsNumeric => true;
            public UnitType UnitType => UnitType.Dimensionless;
            public string Legend => $"Hardware counter '{Counter}' per operation";
            public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style) => GetValue(summary, benchmarkCase);

            private Dictionary<BenchmarkCase, PmcStats> Results { get; }
            private HardwareCounter Counter { get; }

            public bool IsAvailable(Summary summary)
                => summary.Config.GetHardwareCounters().Contains(Counter);

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
                => Results.TryGetValue(benchmarkCase, out var stats) && stats.Counters.ContainsKey(Counter)
                    ? (stats.Counters[Counter].Count / (ulong)stats.TotalOperations).ToString()
                    : "-";
        }

        public class MispredictRateColumn : IColumn
        {
            public MispredictRateColumn(Dictionary<BenchmarkCase, PmcStats> results)
            {
                Results = results;
            }

            public string ColumnName => "Mispredict rate";
            public string Id => "MispredictRate";
            public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
            public bool AlwaysShow => false;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0; // if present should be displayed as the first column (we sort in ascending way)
            public bool IsNumeric => true;
            public UnitType UnitType => UnitType.Dimensionless;
            public string Legend => $"Mispredict rate per operation";
            public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

            private Dictionary<BenchmarkCase, PmcStats> Results { get; }

            public bool IsAvailable(Summary summary)
                => summary.Config.GetHardwareCounters().Any()
                        && summary.Config.GetHardwareCounters().Contains(HardwareCounter.BranchInstructions)
                        && summary.Config.GetHardwareCounters().Contains(HardwareCounter.BranchMispredictions);

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style)
                => Results.TryGetValue(benchmarkCase, out var stats) && stats.Counters.ContainsKey(HardwareCounter.BranchMispredictions) && stats.Counters.ContainsKey(HardwareCounter.BranchInstructions)
                    ? (stats.Counters[HardwareCounter.BranchMispredictions].Count / (double)stats.Counters[HardwareCounter.BranchInstructions].Count).ToString(style.PrintUnitsInContent ? "P2" : String.Empty)
                    : "-";
        }

        public class InstructionRetiredPerCycleColumn : IColumn
        {
            public InstructionRetiredPerCycleColumn(Dictionary<BenchmarkCase, PmcStats> results)
            {
                Results = results;
            }

            public string ColumnName => "IPC";
            public string Id => "IPC";
            public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
            public bool AlwaysShow => false;
            public ColumnCategory Category => ColumnCategory.Diagnoser;
            public int PriorityInCategory => 0; // if present should be displayed as the first column (we sort in ascending way)
            public bool IsNumeric => true;
            public UnitType UnitType => UnitType.Dimensionless;
            public string Legend => $"Instruction Retired per Cycle";
            public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

            private Dictionary<BenchmarkCase, PmcStats> Results { get; }

            public bool IsAvailable(Summary summary)
                => summary.Config.GetHardwareCounters().Any()
                    && summary.Config.GetHardwareCounters().Contains(HardwareCounter.InstructionRetired)
                    && summary.Config.GetHardwareCounters().Contains(HardwareCounter.TotalCycles);

            public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style)
                => Results.TryGetValue(benchmarkCase, out var stats) && stats.Counters.ContainsKey(HardwareCounter.InstructionRetired) && stats.Counters.ContainsKey(HardwareCounter.TotalCycles)
                    ? (stats.Counters[HardwareCounter.InstructionRetired].Count / (double)stats.Counters[HardwareCounter.TotalCycles].Count).ToString("N2")
                    : "-";
        }
    }
}
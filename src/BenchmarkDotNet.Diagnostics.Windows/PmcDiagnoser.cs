using System;
using System.Collections.Generic;
using System.Linq;
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

        public const string DiagnoserId = nameof(PmcDiagnoser);

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
                yield return new Metric(new PmcMetricDescriptor(pmc), (double)pmc.Count / results.TotalOperations);
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
    }
}
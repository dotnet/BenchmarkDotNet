using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace BenchmarkDotNet.Diagnostics.Windows
{
    public class JitStatsDiagnoser : JitDiagnoser<JitStats>, IProfiler
    {
        public override IEnumerable<string> Ids => new[] { nameof(JitStatsDiagnoser) };

        public string ShortName => "jit";

        protected override ulong EventType => (ulong)(ClrTraceEventParser.Keywords.Jit | ClrTraceEventParser.Keywords.Compilation);

        public override IEnumerable<Metric> ProcessResults(DiagnoserResults results)
        {
            if (BenchmarkToProcess.TryGetValue(results.BenchmarkCase, out int pid))
            {
                if (StatsPerProcess.TryGetValue(pid, out JitStats jitStats))
                {
                    yield return new Metric(MethodsJittedDescriptor.Instance, jitStats.MethodsCompiled);
                    yield return new Metric(MethodsTieredDescriptor.Instance, jitStats.MethodsTiered);
                    yield return new Metric(JitAllocatedMemoryDescriptor.Instance, jitStats.MemoryAllocated);
                }
            }
        }

        protected override void AttachToEvents(TraceEventSession session, BenchmarkCase benchmarkCase)
        {
            session.Source.Clr.MethodJittingStarted += methodData =>
            {
                if (StatsPerProcess.TryGetValue(methodData.ProcessID, out JitStats jitStats))
                {
                    Interlocked.Increment(ref jitStats.MethodsCompiled);
                }
            };

            session.Source.Clr.MethodMemoryAllocatedForJitCode += memoryAllocated =>
            {
                if (StatsPerProcess.TryGetValue(memoryAllocated.ProcessID, out JitStats jitStats))
                {
                    Interlocked.Add(ref jitStats.MemoryAllocated, memoryAllocated.AllocatedSizeForJitCode);
                }
            };

            session.Source.Clr.TieredCompilationBackgroundJitStop += tieredData =>
            {
                if (StatsPerProcess.TryGetValue(tieredData.ProcessID, out JitStats jitStats))
                {
                    Interlocked.Add(ref jitStats.MethodsTiered, tieredData.JittedMethodCount);
                }
            };
        }

        private sealed class MethodsJittedDescriptor : IMetricDescriptor
        {
            internal static readonly MethodsJittedDescriptor Instance = new ();

            public string Id => nameof(MethodsJittedDescriptor);
            public string DisplayName => "Methods JITted";
            public string Legend => "Total number of methods JITted during entire benchmark execution (including warmup).";
            public bool TheGreaterTheBetter => false;
            public string NumberFormat => "N0";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric) => true;
        }

        private sealed class MethodsTieredDescriptor : IMetricDescriptor
        {
            internal static readonly MethodsTieredDescriptor Instance = new ();

            public string Id => nameof(MethodsTieredDescriptor);
            public string DisplayName => "Methods Tiered";
            public string Legend => "Total number of methods re-compiled by Tiered JIT during entire benchmark execution (including warmup).";
            public bool TheGreaterTheBetter => false;
            public string NumberFormat => "N0";
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => "Count";
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric) => true;
        }

        private sealed class JitAllocatedMemoryDescriptor : IMetricDescriptor
        {
            internal static readonly JitAllocatedMemoryDescriptor Instance = new ();

            public string Id => nameof(JitAllocatedMemoryDescriptor);
            public string DisplayName => "JIT allocated memory";
            public string Legend => "Total memory allocated by the JIT during entire benchmark execution (including warmup).";
            public bool TheGreaterTheBetter => false;
            public string NumberFormat => "N0";
            public UnitType UnitType => UnitType.Size;
            public string Unit => SizeUnit.B.Name;
            public int PriorityInCategory => 0;
            public bool GetIsAvailable(Metric metric) => true;
        }
    }

    public sealed class JitStats
    {
        public long MethodsCompiled;
        public long MethodsTiered;
        public long MemoryAllocated;
    }
}

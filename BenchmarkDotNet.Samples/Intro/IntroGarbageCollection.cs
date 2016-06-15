using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples.Intro
{
    [Config(typeof(Config))]
    public class IntroGarbageCollection
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Dry.With(Mode.SingleRun).WithTargetCount(1).With(new GC { Server = true, Force = true }));
                Add(Job.Dry.With(Mode.SingleRun).WithTargetCount(1).With(new GC { Server = true, Force = false }));
                Add(Job.Dry.With(Mode.SingleRun).WithTargetCount(1).With(new GC { Server = false, Force = true }));
                Add(Job.Dry.With(Mode.SingleRun).WithTargetCount(1).With(new GC { Server = false, Force = false }));

                Add(MarkdownExporter.GitHub);

                Set(new FastestToSlowestOrderProvider());
            }

            private class FastestToSlowestOrderProvider : IOrderProvider
            {
                public IEnumerable<Benchmark> GetExecutionOrder(Benchmark[] benchmarks) => benchmarks;

                public IEnumerable<Benchmark> GetSummaryOrder(Benchmark[] benchmarks, Summary summary) =>
                    from benchmark in benchmarks
                    orderby summary[benchmark].ResultStatistics.Median
                    select benchmark;

                public string GetGroupKey(Benchmark benchmark, Summary summary) => null;
            }
        }

        [Benchmark(Description = "new byte[10KB]")]
        public byte[] Allocate()
        {
            return new byte[10000];
        }

        [Benchmark(Description = "stackalloc byte[10KB]")]
        public unsafe void AllocateWithStackalloc()
        {
            var array = stackalloc byte[10000];
            Blackhole(array);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Blackhole<T>(T input)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void Blackhole(byte* input)
        {
        }
    }
}
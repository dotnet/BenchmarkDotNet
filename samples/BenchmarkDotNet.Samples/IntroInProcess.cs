using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class IntroInProcess
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.MediumRun
                    .WithLaunchCount(1)
                    .WithId("OutOfProc"));

                Add(Job.MediumRun
                    .WithLaunchCount(1)
                    .With(InProcessEmitToolchain.Instance)
                    .WithId("InProcess"));
            }
        }

        [Benchmark(Description = "new byte[10kB]")]
        public byte[] Allocate()
        {
            return new byte[10000];
        }

        [Benchmark(Description = "stackalloc byte[10kB]")]
        public unsafe void AllocateWithStackalloc()
        {
            var array = stackalloc byte[10000];
            Consume(array);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void Consume(byte* input)
        {
        }
    }
}
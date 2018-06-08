using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    [OrderProvider(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class IntroGcMode
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.MediumRun.WithGcServer(true).WithGcForce(true).WithId("ServerForce"));
                Add(Job.MediumRun.WithGcServer(true).WithGcForce(false).WithId("Server"));
                Add(Job.MediumRun.WithGcServer(false).WithGcForce(true).WithId("Workstation"));
                Add(Job.MediumRun.WithGcServer(false).WithGcForce(false).WithId("WorkstationForce"));
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
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace BenchmarkDotNet.Samples.Intro
{
    [Config(typeof(Config))]
    [OrderProvider(SummaryOrderPolicy.FastestToSlowest)]
    public class IntroGcMode
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(new Job("ServerForce")
                {
                    Env = { Gc = { Server = true, Force = true } }
                });
                Add(new Job("Server")
                {
                    Env = { Gc = { Server = true, Force = false } }
                });
                Add(new Job("Workstation")
                {
                    Env = { Gc = { Server = false, Force = false } }
                });
                Add(new Job("WorkstationForce")
                {
                    Env = { Gc = { Server = false, Force = true } }
                });
#if !CORE
                Add(new Diagnostics.Windows.MemoryDiagnoser());
#endif
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
            Consume(array);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void Consume(byte* input)
        {
        }
    }
}
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.InProcess;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    public class IntroInProcessWrongEnv
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                var wrongPlatform = RuntimeInformation.Is64BitPlatform()
                    ? Platform.X64
                    : Platform.X86;

                Add(Job.MediumRun
                    .WithLaunchCount(1)
                    .With(wrongPlatform)
                    .With(InProcessToolchain.Instance)
                    .WithId("InProcess"));

                Add(InProcessValidator.DontFailOnError);
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
using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Toolchains.InProcess;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

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
                var wrongPlatform = Environment.Is64BitProcess
                    ? Platform.X64
                    : Platform.X86;

                AddJob(Job.MediumRun
                    .WithLaunchCount(1)
                    .WithPlatform(wrongPlatform)
                    .WithToolchain(InProcessEmitToolchain.Instance)
                    .WithId("InProcess"));

                AddValidator(InProcessValidator.DontFailOnError);
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
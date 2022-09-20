using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Samples
{
    public class Algo_Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] data;

        private readonly MD5 md5 = MD5.Create();
        private readonly SHA256 sha256 = SHA256.Create();

        public Algo_Md5VsSha256()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark(Baseline = true)]
        public byte[] Md5() => md5.ComputeHash(data);

        [Benchmark]
        public byte[] Sha256() => sha256.ComputeHash(data);
    }

    public class IntroFluentConfigBuilder
    {
        public static void Run()
        {
            BenchmarkRunner
                .Run<Algo_Md5VsSha256>(
                    DefaultConfig.Instance
                        .AddJob(Job.Default.WithRuntime(ClrRuntime.Net462))
                        .AddJob(Job.Default.WithRuntime(CoreRuntime.Core21))
                        .AddValidator(ExecutionValidator.FailOnError));
        }
    }
}
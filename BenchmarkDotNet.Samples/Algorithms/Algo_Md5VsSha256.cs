using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Algorithms
{
    internal class AllWindowsRuntimesConfig : ManualConfig
    {
        public AllWindowsRuntimesConfig()
        {
            Add(Job.Default.With(Runtime.Clr));
            Add(Job.Default.With(Runtime.Mono));
            Add(Job.Default.With(Runtime.Core));
        }
    }

    [Config(typeof(AllWindowsRuntimesConfig))]
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
        public byte[] Md5()
        {
            return md5.ComputeHash(data);
        }

        [Benchmark]
        public byte[] Sha256()
        {
            return sha256.ComputeHash(data);
        }
    }
}
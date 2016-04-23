using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Algorithms
{
    // you can target all runtimes that you support with single config
    internal class AllWindowsRuntimesConfig : ManualConfig
    {
        public AllWindowsRuntimesConfig()
        {
            Add(Job.Default.With(Runtime.Clr).With(Jit.RyuJit).With(Jobs.Framework.V40));
            Add(Job.Default.With(Runtime.Dnx).With(Jit.RyuJit));
            Add(Job.Default.With(Runtime.Core).With(Jit.RyuJit));
        }
    }

    [Config(typeof(AllWindowsRuntimesConfig))]
    public class Algo_Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] data;

        private readonly SHA256 sha256 = SHA256.Create();
        private readonly MD5 md5 = MD5.Create();

        public Algo_Md5VsSha256()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public byte[] Sha256()
        {
            return sha256.ComputeHash(data);
        }

        [Benchmark]
        public byte[] Md5()
        {
            return md5.ComputeHash(data);
        }
    }
}
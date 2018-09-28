using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [MediumRunJob, SkewnessColumn, KurtosisColumn]
    public class IntroStatisticsColumns
    {
        private const int N = 10000;
        private readonly byte[] data;

        private readonly MD5 md5 = MD5.Create();
        private readonly SHA256 sha256 = SHA256.Create();

        public IntroStatisticsColumns()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark(Baseline = true)]
        public byte[] Md5A() => md5.ComputeHash(data);

        [Benchmark]
        public byte[] Md5B() => md5.ComputeHash(data);

        [Benchmark]
        public byte[] Sha256() => sha256.ComputeHash(data);
    }
}
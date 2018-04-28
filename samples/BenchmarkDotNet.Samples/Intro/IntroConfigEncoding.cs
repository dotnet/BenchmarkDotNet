using System;
using System.Linq;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Samples.Algorithms;

namespace BenchmarkDotNet.Samples.Intro
{
    public class IntroConfigEncoding
    {
        private const int N = 1002;
        private readonly ulong[] numbers;
        private readonly Random random = new Random(42);
        
        public IntroConfigEncoding()
        {
            numbers = new ulong[N];
            for (int i = 0; i < N; i++)
                numbers[i] = NextUInt64();
        }
        
        public ulong NextUInt64()
        {
            var buffer = new byte[sizeof(long)];
            random.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }

        [Benchmark]
        public double Foo()
        {
            int counter = 0;
            for (int i = 0; i < N / 2; i++)
                counter += BitCountHelper.PopCountParallel2(numbers[i],numbers[i+1]);
            return counter;
        }
    }
}
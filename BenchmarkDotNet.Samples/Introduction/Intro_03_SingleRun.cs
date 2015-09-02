using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.Introduction
{
    public class Intro_03_SingleRun
    {
        private const int N = 128 * 1024 * 1024;
        private int[] x = new int[N];

        // The SingleRun mode is useful, if you want to measure the cold start of your application
        [Benchmark]
        [BenchmarkTask(5, mode: BenchmarkMode.SingleRun, platform: BenchmarkPlatform.X86, warmupIterationCount: 0, targetIterationCount: 1)]
        [OperationsPerInvoke(N / 16)] // The OperationsPerInvoke help you to specify amount of basic operation inside the target method
        public void ColdStart()
        {
            for (int i = 0; i < x.Length; i += 16)
                x[i]++;
        }

        [Benchmark]
        [BenchmarkTask(5, mode: BenchmarkMode.Throughput, platform: BenchmarkPlatform.X86, warmupIterationCount: 5, targetIterationCount: 10)]
        [OperationsPerInvoke(N / 16)]
        public void WarmStart()
        {
            for (int i = 0; i < x.Length; i += 16)
                x[i]++;
        }

        // See also: https://msdn.microsoft.com/en-us/library/cc656914.aspx
        // See also: http://en.wikipedia.org/wiki/CPU_cache
    }
}
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [Diagnostics.Windows.Configs.JitStatsDiagnoser]
    public class IntroJitStatsDiagnoser
    {
        [Benchmark]
        public void Sleep() => Thread.Sleep(10);
    }
}
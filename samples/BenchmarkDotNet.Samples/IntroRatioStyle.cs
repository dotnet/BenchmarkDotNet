using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Samples
{
    [ShortRunJob, Config(typeof(Config))]
    public class IntroRatioStyle
    {
        [Benchmark(Baseline = true)]
        public void Baseline() => Thread.Sleep(1000);

        [Benchmark]
        public void Bar() => Thread.Sleep(150);

        [Benchmark]
        public void Foo() => Thread.Sleep(1150);

        private class Config : ManualConfig
        {
            public Config()
            {
                SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
            }
        }
    }
}
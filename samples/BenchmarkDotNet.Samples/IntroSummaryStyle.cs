using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;
using Perfolizer.Metrology;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    public class IntroSummaryStyle
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                // Configure the summary style here
                var summaryStyle = new SummaryStyle
                (
                    cultureInfo: CultureInfo.InvariantCulture,
                    printUnitsInHeader: true,
                    printUnitsInContent: false,
                    sizeUnit: SizeUnit.KB,
                    timeUnit: TimeUnit.Nanosecond,
                    maxParameterColumnWidth: 20

                );

                WithSummaryStyle(summaryStyle);
            }
        }

        [Params(10, 100)]
        public int N;

        [Benchmark]
        public void Sleep() => System.Threading.Thread.Sleep(N);
    }
}
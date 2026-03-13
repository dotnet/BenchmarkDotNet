using System.Globalization;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    [ShortRunJob]
    [UseLocalJobOnly]
    public class IntroCultureInfo
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                CultureInfo = (CultureInfo) CultureInfo.InvariantCulture.Clone();
                CultureInfo.NumberFormat.NumberDecimalSeparator = "@";
            }
        }

        [Benchmark]
        public void Foo() => Thread.Sleep(100);
    }
}
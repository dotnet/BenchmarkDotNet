using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.Globalization;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    [ShortRunJob]
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
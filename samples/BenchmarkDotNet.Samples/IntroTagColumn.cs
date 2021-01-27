using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    // You can add custom tags per each method using Columns
    [Config(typeof(Config))]
    public class IntroTagColumn
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.Dry);
                AddColumn(new TagColumn("Kind", name => name.Substring(0, 3)));
                AddColumn(new TagColumn("Number", name => name.Substring(3)));
            }
        }

        [Benchmark]
        public void Foo1() => Thread.Sleep(10);

        [Benchmark]
        public void Foo12() => Thread.Sleep(10);

        [Benchmark]
        public void Bar3() => Thread.Sleep(10);

        [Benchmark]
        public void Bar34() => Thread.Sleep(10);
    }
}

using System.Threading;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples.Intro
{
    // You can add custom tags per each method using Columns
    [Config(typeof(Config))]
    public class IntroTags
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Dry);
                Add(new TagColumn("Foo or Bar", name => name.Substring(0, 3)));
                Add(new TagColumn("Number", name => name.Substring(3)));
            }
        }

        [Benchmark]
        public void Foo1()
        {
            Thread.Sleep(10);
        }

        [Benchmark]
        public void Foo12()
        {
            Thread.Sleep(10);
        }

        [Benchmark]
        public void Bar3()
        {
            Thread.Sleep(10);
        }

        [Benchmark]
        public void Bar34()
        {
            Thread.Sleep(10);
        }
    }
}
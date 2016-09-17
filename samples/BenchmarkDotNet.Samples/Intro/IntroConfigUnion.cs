using System.Threading;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Samples.Intro
{
    [Config(typeof(Config))]
    public class IntroConfigUnion
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Dry);
                Add(ConsoleLogger.Default);
                Add(TargetMethodColumn.Method, StatisticColumn.Max);
                Add(RPlotExporter.Default, CsvExporter.Default);
                Add(EnvironmentAnalyser.Default);
                UnionRule = ConfigUnionRule.AlwaysUseLocal;
            }
        }

        [Benchmark]
        public void Foo()
        {
            Thread.Sleep(10);
        }
    }
}
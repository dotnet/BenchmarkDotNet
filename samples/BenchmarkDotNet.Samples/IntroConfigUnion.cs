using System.Threading;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    public class IntroConfigUnion
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.Dry);
                AddLogger(ConsoleLogger.Default);
                AddColumn(TargetMethodColumn.Method, StatisticColumn.Max);
                AddExporter(RPlotExporter.Default, CsvExporter.Default);
                AddAnalyser(EnvironmentAnalyser.Default);
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
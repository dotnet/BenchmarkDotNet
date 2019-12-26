using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace _BenchmarkProjectName_
{
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            // Configure your benchmarks, see for more details: https://benchmarkdotnet.org/articles/configs/configs.html.
            //Add(Job.Dry);
            //Add(ConsoleLogger.Default);
            //Add(TargetMethodColumn.Method, StatisticColumn.Max);
            //Add(RPlotExporter.Default, CsvExporter.Default);
            //Add(EnvironmentAnalyser.Default);
        }
    }
}
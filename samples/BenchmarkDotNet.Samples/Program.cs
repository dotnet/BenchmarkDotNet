using System.Reflection;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Csv;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance);
            config.Add(CsvMeasurementsExporter.Default);
            config.Set(new Reports.SummaryStyle(printUnitsInHeader: true, printUnitsInContent: false, timeUnit: Horology.TimeUnit.Second));
            BenchmarkRunner.Run<Intro.IntroColumns>(config);
            //BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
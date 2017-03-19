using System.Reflection;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance);
            config.Add(new MemoryDiagnoser());
            config.Add(new CsvExporter(
                CsvSeparator.CurrentCulture,
                new Reports.SummaryStyle() { PrintUnitsInHeader = true, PrintUnitsInContent = false, TimeUnit = Horology.TimeUnit.Second }
            ));
            BenchmarkRunner.Run<Intro.IntroColumns>(config);
            //BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
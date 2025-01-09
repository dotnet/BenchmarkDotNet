using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;


namespace BenchmarkDotNet.Samples
{
    //[MemoryDiagnoser]
    //[Config(typeof(Config))]
    [RPlotExporter(IntegratedExportEnum.HtmlExporterWithRPlotExporter)]
    public class IntroRPlot
    {
        [Benchmark]
        public void Benchmark()
        {
            var result = Calculate();
        }

        private int Calculate()
        {
            int sum = 0;
            for (int i = 0; i < 1000; i++)
            {
                sum += i;
            }
            return sum;
        }
    }

    public class Config : ManualConfig
    {
        public Config()
        {
            //AddExporter(CsvMeasurementsExporter.Default);
            //AddExporter(RPlotExporter.Default);
        }
    }

}